using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using System.Diagnostics;
using kwm.Utils;
using Tbx.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    /// <summary>
    /// Identify a stage in the pipeline.
    /// </summary>
    public enum PipelineStage
    {
        /// <summary>
        /// The pipeline has entered a new round.
        /// </summary>
        Entry = 0,

        /// <summary>
        /// Perform the initial scan of the share.
        /// </summary>
        InitScan,

        /// <summary>
        /// Apply buffered server operations.
        /// </summary>
        ServerOp,

        /// <summary>
        /// Perform a partial scan of the share.
        /// </summary>
        PartialScan,

        /// <summary>
        /// Handle the file transfers.
        /// </summary>
        Transfer,

        /// <summary>
        /// Update the server file local statuses.
        /// </summary>
        LocalUpdate,

        /// <summary>
        /// Update the status view and the UI view.
        /// </summary>
        UI,

        /// <summary>
        /// The pipeline has completed a round.
        /// </summary>
        Exit
    }

    /// <summary>
    /// Identify the action taken by a pipeline stage.
    /// </summary>
    public enum PipelineAction
    {
        /// <summary>
        /// The pipeline stage didn't tell us what to do next. Error.
        /// </summary>
        None,

        /// <summary>
        /// The pipeline stage passes the hand to the next stage.
        /// </summary>
        Pass,

        /// <summary>
        /// The pipeline stage needs to wait for an event before passing to the
        /// next stage.
        /// </summary>
        Stall
    }

    /// <summary>
    /// Represent the state machine of the share.
    /// </summary>
    public class KfsPipeline
    {
        /// <summary>
        /// Reference to the share.
        /// </summary>
        private KfsShare Share;

        /// <summary>
        /// Current pipeline stage.
        /// </summary>
        private PipelineStage CurrentStage = PipelineStage.Entry;

        /// <summary>
        /// Action taken by the current pipeline stage.
        /// </summary>
        private PipelineAction StageAction = PipelineAction.None;

        /// <summary>
        /// True if the Run() method is running. We detect re-entrance with this flag.
        /// </summary>
        private bool RunFlag = false;

        /// <summary>
        /// This flag is true if the pipeline stages are being synchronized. All pending
        /// operations are performed without considering the value of the timers. This
        /// is used to bring the state of the share up-to-date immediately, e.g. when
        /// the user activated the share, when the user clicked on "update view" or to
        /// force the UI view to update itself after a server operation has been
        /// applied.
        /// </summary>
        private bool SyncFlag = false;

        /// <summary>
        /// This flag is true if the whole pipeline must be run again with SyncFlag
        /// set to true. This is used to bring the whole pipeline fully up-to-date on
        /// certain occasions.
        /// </summary>
        private bool FullSyncFlag = false;

        /// <summary>
        /// This flag is true if the current stage must pass the hand to the next
        /// stage as soon as possible. This is used by stages to avoid performing
        /// more than one operation during each run.
        /// </summary>
        private bool PassFlag = false;

        /// <summary>
        /// Date at which the pipeline needs to be woken up by the timer thread. This
        /// field is MaxValue if the pipeline does not need to be woken up. This field is
        /// cleared when Run() is called and updated when the pipeline stalls.
        /// </summary>
        private DateTime TimerDate = DateTime.MaxValue;

        /// <summary>
        /// Date at which the pipeline needs to run again. This field is null if the
        /// pipeline needs to be run again at the first opportunity, i.e. whenever the
        /// pipeline is not stalled. The Run() and SyncPipeline() methods set this
        /// field to null. The entry stage sets this field to infinity. The lower
        /// stages may update this field with a value inferior to its previous value.
        /// The exit stage stalls the pipeline if this field is non-null.
        /// </summary>
        private DateTime NextRunDate = DateTime.MinValue;

        /// <summary>
        /// Wake-up timer associated to this pipeline.
        /// </summary>
        private KWakeupTimer WakeupTimer;

        /// <summary>
        /// Return true if the KFS share needs to stop.
        /// </summary>
        private bool StopFlag { get { return !Share.App.IsStarted(); } }

        /// <summary>
        /// Return true if the share has online capability.
        /// </summary>
        private bool OnlineFlag { get { return Share.App.Helper.IsOnlineCapable(); } }

        /// <summary>
        /// Set to true when a run has been queued by RunQueued.
        /// </summary>
        [NonSerialized]
        private bool QueuedRun = false;

        public KfsPipeline(KfsShare S)
        {
            Share = S;
            Share.Pipeline = this;
            WakeupTimer = new KWakeupTimer();
            WakeupTimer.TimerWakeUpCallback = OnTimerEvent;
        }

        public delegate void RunDelegate(string reason, bool fullFlag);

        public void RunQueued(string reason, bool fullFlag)
        {
            if (!QueuedRun)
            {
                QueuedRun = true;
                Misc.MainForm.BeginInvoke(new KfsPipeline.RunDelegate(Share.Pipeline.Run), new object[] { reason, fullFlag });
            }
        }

        /// <summary>
        /// Run the pipeline until it stalls. If FullFlag is true, all the
        /// stages of the pipeline will be synchronized.
        /// </summary>
        public void Run(String reason, bool fullFlag)
        {
            Logging.Log(1, "Running pipeline in stage " + CurrentStage + ": " + reason +
                         ", fullFlag: " + fullFlag + ".");

            QueuedRun = false;

            // Set the FullSyncFlag as needed. This is done even if the pipeline
            // is being re-entered.
            if (fullFlag) FullSyncFlag = true;

            // Set the next run date to null. We don't want to be stuck in the
            // exit stage. This is done even if the pipeline is being re-entered.
            NextRunDate = DateTime.MinValue;

            // Don't re-enter the pipeline.
            if (RunFlag)
            {
                Logging.Log("The pipeline is already running, exiting.");
                return;
            }

            // We're now running the pipeline.
            RunFlag = true;

            try
            {
                while (true)
                {
                    // We stall the pipeline when the UI gate has been entered.
                    if (Share.Gate.EntryCount > 0)
                    {
                        Logging.Log("The UI gate has been entered, stalling the pipeline.");
                        break;
                    }

                    StageAction = PipelineAction.None;

                    // Process the current stage.
                    if (CurrentStage == PipelineStage.Entry) EntryStage();
                    else if (CurrentStage == PipelineStage.InitScan) InitScanStage();
                    else if (CurrentStage == PipelineStage.ServerOp) ServerOpStage();
                    else if (CurrentStage == PipelineStage.PartialScan) PartialScanStage();
                    else if (CurrentStage == PipelineStage.Transfer) TransferStage();
                    else if (CurrentStage == PipelineStage.LocalUpdate) LocalUpdateStage();
                    else if (CurrentStage == PipelineStage.UI) UiStage();
                    else if (CurrentStage == PipelineStage.Exit) ExitStage();
                    else Debug.Assert(false);

                    // Stall the pipeline.
                    if (StageAction == PipelineAction.Stall)
                    {
                        // Disable the timer.
                        if (TimerDate == DateTime.MaxValue) WakeupTimer.WakeMeUp(-1);

                        // Enable the timer.
                        else WakeupTimer.WakeMeUp(Math.Max((Int64)(TimerDate - DateTime.Now).TotalMilliseconds + 1, 1));

                        break;
                    }

                    // Pass to the next stage.
                    else if (StageAction == PipelineAction.Pass)
                    {
                        CurrentStage = (PipelineStage)(((int)CurrentStage + 1) % 8);
                    }

                    // Oops.
                    else throw new Exception("pipeline action not set");
                }
            }

            catch (Exception ex)
            {
                Share.FatalError(ex);
            }

            // We're no longer running the pipeline.
            RunFlag = false;
        }

        /// <summary>
        /// Called by the wake-up timer.
        /// </summary>
        public void OnTimerEvent(Object[] args)
        {
            Run("timer event at time " + DateTime.Now, false);
        }

        /// <summary>
        /// Handle the entry stage.
        /// </summary>
        private void EntryStage()
        {
            Logging.Log("EntryStage() called.");

            // We're trying to stop.
            if (StopFlag)
            {
                // We can stop now.
                if (Share.TryStop())
                {
                    StallPipeline("Stopped the share", DateTime.MaxValue);

                    // Notify the workspace.
                    if (Share.App.AppStatus == KwsAppStatus.Stopping)
                        Share.App.Helper.OnAppStopped(Share.App);

                    return;
                }
            }

            // If FullSyncFlag is true, set SyncFlag to true, otherwise clear
            // SyncFlag and ReportErrorToUserFlag. FullSyncFlag is cleared in
            // all cases.
            SyncFlag = FullSyncFlag;
            if (!FullSyncFlag) Share.ReportErrorToUserFlag = false;
            FullSyncFlag = false;

            // Set NextRunDate to infinity if the synchronization flag is not set,
            // otherwise set it to the minimum.
            NextRunDate = SyncFlag ? DateTime.MinValue : DateTime.MaxValue;

            // If FullSyncFlag was set, then it is not safe to change the allowed
            // operations level until the pipeline had the chance to update itself
            // properly.
            if (!SyncFlag)
            {
                AllowedOpStatus initialStatus = Share.AllowedOp;
                string initialFailure = Share.ServerOpFsFailExplanation;

                Share.InitialAllowedOp = Share.AllowedOp;

                Logging.Log("EntryStage : Allow all user operations by default.");
                // Allow all user operations by default.
                Share.AllowedOp = AllowedOpStatus.All;

                // If the share is inactive, the filesystem is not scanned, the
                // server operations are messed-up or a meta-data operation is being
                // applied, disallow all user operations.
                if (!Share.ActiveFlag)
                    Share.DisallowUserOp("share is inactive", AllowedOpStatus.None);
                else if (Share.InitialScanStatus != InitScanStatus.OK)
                    Share.DisallowUserOp("share is not scanned", AllowedOpStatus.None);
                else if (Share.ServerOpPermFailFlag)
                    Share.DisallowUserOp("permanent server operation failure", AllowedOpStatus.None);
                else if (Share.MetaDataManager.Status != MetaDataManagerStatus.Idle)
                    Share.DisallowUserOp("meta-data operation in progress", AllowedOpStatus.None);

                // If the unified view is stale or if there are type conflicts,
                // allow only download operations 
                else if (Share.ServerOpFsFailFlag)
                    Share.DisallowUserOp("unified view is stale", AllowedOpStatus.Download);
                else if (Share.TypeConflictFlag)
                    Share.DisallowUserOp("type conflicts present", AllowedOpStatus.Download);

                // Update UI if the allowedOp changed, or if the reason
                // we are disallowed changed, notify UI.
                if (initialStatus != Share.AllowedOp || initialFailure != Share.ServerOpFsFailExplanation)
                    Share.RequestStatusViewUpdate("AllowedOp changed to " + Share.AllowedOp);
            }

            // Pass to the next stage.
            NextStage();
        }

        /// <summary>
        /// Handle the initial scan.
        /// </summary>
        private void InitScanStage()
        {
            // The initial scan is done, pass to the next stage.
            if (Share.InitialScanStatus == InitScanStatus.OK)
            {
                NextStage();
                return;
            }

            // The initial scan thread is running. We need to wait for it to
            // finish.
            if (Share.ScanRunningFlag)
            {
                StallPipeline("waiting for initial scan thread to finish", DateTime.MaxValue);
                return;
            }

            // Check if we need to start a scan.
            bool startFlag = false;

            // We're stopping or we've set the pass flag.
            if (StopFlag || PassFlag) startFlag = false;

            // The initial scan is forced.
            else if (SyncFlag) startFlag = true;

            // Normally we only do the initial scan if the share is active
            // and we have caught up.
            else if (Share.ActiveFlag && Share.App.NotifiedCaughtUpFlag)
            {
                // The initial scan was never done. Do it now.
                if (Share.InitialScanStatus == InitScanStatus.None) startFlag = true;

                // The last initial scan failed. Check if we're ready to run it
                // again.
                else
                {
                    DateTime deadline = Share.InitialScanDate.AddMilliseconds(KfsShare.ScanFailDelay);

                    // It's time to run it again.
                    if (deadline < DateTime.Now) startFlag = true;

                      // Wait until we're ready to run it.
                    else ScheduleRun("do initial scan after failure", deadline);
                }
            }

            // Start the scan and wait for it.
            if (startFlag)
            {
                // Scan started. Sync the pipeline to force the server 
                // operations to be applied and set the pass flag to get out of
                // this stage when we get the results.
                if (Share.StartInitialScan())
                {
                    SyncPipeline("doing initial scan", false);
                    StallPipeline("doing initial scan", DateTime.MaxValue);
                    PassFlag = true;
                }

                // Failure. Request another run and pass to the next stage.
                else
                {
                    ScheduleRun("cannot start initial scan", DateTime.MinValue);
                    NextStage();
                }
            }

            // Pass to the next stage.
            else
            {
                NextStage();
            }
        }

        /// <summary>
        /// Handle server operations.
        /// </summary>
        private void ServerOpStage()
        {
            // We're waiting for the filesystem marker event.
            if (Share.ServerOpRunningFlag)
            {
                DateTime deadline = Share.ServerOpAppDate.AddMilliseconds(KfsShare.ServerOpMarkerDelay);

                // We've waited too long. Pretend we got it.
                if (deadline < DateTime.Now)
                {
                    Logging.Log(2, "Did not receive KFS marker event, pretending we got it.");
                    Share.ServerOpRunningFlag = false;
                    NextStage();
                }

                // Wait until we get it.
                else StallPipeline("waiting for marker event", deadline);

                return;
            }

            // We have buffered events. Check if we need to apply them.
            if (Share.HaveBufferedServerOp())
            {
                bool startFlag = false;

                // We're stopping or we've set the pass flag.
                if (StopFlag || PassFlag) startFlag = false;

                // We have to process the buffered events now.
                else if (SyncFlag) startFlag = true;

                // Check if it's time to process the buffered events.
                else
                {
                    UInt64 delay = KfsShare.ServerOpSuccessDelay;
                    if (Share.ServerOpFsFailFlag) delay = KfsShare.ServerOpFailDelay;
                    DateTime deadline = Share.ServerOpAppDate.AddMilliseconds(delay);
                    if (deadline < DateTime.Now) startFlag = true;
                    else ScheduleRun("there are buffered server events", deadline);
                }

                // Apply the operations. Synchronize the pipeline to force the partial
                // scan to be done.
                if (startFlag)
                {
                    Share.ApplyServerOperation();
                    SyncPipeline("applied server operations", false);

                    // The operations modified the filesystem, so stall until we get
                    // the event and set the pass flag to get out of this stage when
                    // we got it.
                    if (Share.ServerOpRunningFlag)
                    {
                        DateTime deadline = Share.ServerOpAppDate.AddMilliseconds(KfsShare.ServerOpMarkerDelay);
                        StallPipeline("operations modified the filesystem", deadline);
                        PassFlag = true;
                        return;
                    }
                }
            }

            // Pass to the next stage.
            NextStage();
        }

        /// <summary>
        /// Handle partial scans.
        /// </summary>
        private void PartialScanStage()
        {
            // The partial scan thread is running. We need to wait for it to
            // finish.
            if (Share.ScanRunningFlag)
            {
                StallPipeline("waiting for partial scan thread to finish", DateTime.MaxValue);
                return;
            }

            // If the initial scan was not done or the local view is up-to-date,
            // no partial scan is needed.
            if (Share.InitialScanStatus != InitScanStatus.OK || !Share.StaleTree.IsStale)
            {
                NextStage();
                return;
            }

            bool startFlag = false;

            // We're stopping or we've set the pass flag.
            if (StopFlag || PassFlag) startFlag = false;

            // The partial scan is forced.
            else if (SyncFlag) startFlag = true;

            // Normally we only do the partial scan if the share is active and we have caught up.
            else if (Share.ActiveFlag && Share.App.NotifiedCaughtUpFlag)
            {
                DateTime now = DateTime.Now;
                startFlag = true;

                // Check if enough time has passed since we last scanned the 
                // share.
                DateTime deadline = Share.PartialScanDate.AddMilliseconds(KfsShare.PartialScanLongDelay);

                if (deadline >= now)
                {
                    Logging.Log("Not enough time has passed since the last partial scan.");
                    startFlag = false;
                }

                // Check if enough time has passed since we received the last
                // filesystem event.
                if (startFlag)
                {
                    deadline = Share.StaleFsEventDate.AddMilliseconds(KfsShare.PartialScanShortDelay);

                    if (deadline >= now)
                    {
                        Logging.Log("Not enough time has passed since we've received a filesystem event.");
                        startFlag = false;
                    }
                }

                if (!startFlag) ScheduleRun("partial scan", deadline);
            }

            // Start the scan and wait for it. Synchronize the pipeline to force
            // the local statuses to be updated and set the pass flag to get out
            // of this stage when we get the results.
            if (startFlag)
            {
                Share.StartPartialScan();
                SyncPipeline("doing partial scan", false);
                StallPipeline("doing partial scan", DateTime.MaxValue);
                PassFlag = true;
            }

            // Pass to the next stage.
            else NextStage();
        }

        /// <summary>
        /// Handle the file transfers.
        /// </summary>
        private void TransferStage()
        {
            // Move all downloaded files. The local statuses of the downloaded
            // files are updated immediately, so we need not flag the downloaded
            // files for update. However, we have to update the status view.
            if (Share.DownloadManager.DoneTree.Count > 0)
            {
                Share.RequestStatusViewUpdate("KfsFileLine::TransferStage : DoneTree.count > 0");
                Share.DownloadManager.MoveAllDownloadedFiles();

                // Clear the cache if it is full and no downloads are in progress.
                if (Share.DownloadManager.OrderTree.Count == 0)
                    Share.DownloadManager.ClearCacheIfNeeded();
            }

            // Clear all completed uploads. If we're here, then the phase 2 event
            // for the current batch has been received. However, it is not
            // guaranteed that it has been applied on the server view. Hence, we
            // request the pipeline to be synchronized and we disallow all operations
            // until the the event had the chance to be applied and the UI view to
            // be updated. It is not necessary to flag the uploaded files for
            // update, since the application of the phase 2 event will do that.
            // However, it is necessary to update the UI view in case the phase 2
            // event has already been applied.
            if (Share.UploadManager.Status == UploadManagerStatus.Idle &&
                Share.UploadManager.DoneTree.Count > 0)
            {
                Share.RequestStatusViewUpdate("TransferStage : UploadManager.Idle && DoneTree.Count > 0");
                Share.DisallowUserOp("file upload batch completed", AllowedOpStatus.None);
                SyncPipeline("file upload batch completed", true);
                Share.UploadManager.ClearAllUploadedFiles();
            }

            Debug.Assert(Share.DownloadManager.DoneTree.Count == 0);

            // We have online capability.
            if (OnlineFlag)
            {
                // The meta-data manager has a queued operation. Get a ticket if we
                // don't have one, otherwise start the operation.
                if (Share.MetaDataManager.Status == MetaDataManagerStatus.Queued)
                {
                    if (Share.MetaDataManager.Ticket == null) Share.MetaDataManager.AskTicket();
                    else Share.MetaDataManager.StartOperation();
                }

                // The download manager is idle and there are files to download. Get a
                // ticket if we don't have one, otherwise start a new batch.
                if (Share.DownloadManager.Status == DownloadManagerStatus.Idle &&
                    Share.DownloadManager.OrderTree.Count > 0)
                {
                    if (Share.DownloadManager.Ticket == null) Share.DownloadManager.AskTicket();
                    else Share.DownloadManager.StartBatch();
                }

                // The upload and meta-data managers are idle and there are files to
                // upload. Get a ticket if we don't have one, otherwise start a new
                // batch. Note that it is important that the meta-data manager be idle
                // here, since otherwise it may be creating the directories we need
                // for the uploads.
                if (Share.UploadManager.Status == UploadManagerStatus.Idle &&
                    Share.MetaDataManager.Status == MetaDataManagerStatus.Idle &&
                    Share.UploadManager.OrderTree.Count > 0)
                {
                    Debug.Assert(Share.UploadManager.DoneTree.Count == 0);
                    if (Share.UploadManager.Ticket == null) Share.UploadManager.AskTicket();
                    else Share.UploadManager.StartBatch();
                }
            }

            // Pass to the next stage.
            NextStage();
        }

        /// <summary>
        /// Handle local statuses update.
        /// </summary>
        private void LocalUpdateStage()
        {
            bool startFlag = false;

            // We're stopping or we've set the pass flag.
            if (StopFlag || PassFlag) startFlag = false;

            // There is nothing to update.
            else if (!Share.LocalUpdateFlag) startFlag = false;

            // The update is forced.
            else if (SyncFlag) startFlag = true;

            // Normally we only update the local statuses if the share is
            // active and we have caught up.
            else if (Share.ActiveFlag && Share.App.NotifiedCaughtUpFlag)
            {
                // If we're here, then there are statuses that we can't manage to
                // update, so this is the failure case.
                DateTime deadline = Share.LocalUpdateDate.AddMilliseconds(KfsShare.LocalUpdateFailDelay);
                if (deadline < DateTime.Now) startFlag = true;
                else ScheduleRun("local status update", deadline);
            }

            // Refresh the statuses.
            if (startFlag)
            {
                Share.UpdateLocalStatus();

                // We failed, retry later.
                if (Share.LocalUpdateFlag)
                    ScheduleRun("local status update failed",
                                Share.LocalUpdateDate.AddMilliseconds(KfsShare.LocalUpdateFailDelay));
            }

            // Pass to the next stage.
            NextStage();
        }

        /// <summary>
        /// Handle the refresh of the status view and UI view.
        /// </summary>
        private void UiStage()
        {
            bool startFlag = false;

            // Normally we only do the update if the share is active.
            if (Share.UpdateStatusViewFlag && Share.ActiveFlag)
            {
                if (SyncFlag) startFlag = true;

                else
                {
                    DateTime deadline = Share.StatusViewStaleDate.AddMilliseconds(KfsShare.StatusViewDelay);
                    if (deadline < DateTime.Now) startFlag = true;
                    else ScheduleRun("status view update", deadline);
                }
            }

            if (startFlag) Share.UpdateStatusViewIfNeeded();

            NextStage();
        }

        /// <summary>
        /// Handle the pipeline exit.
        /// </summary>
        private void ExitStage()
        {
            // We must do another run.
            if (NextRunDate == DateTime.MinValue) NextStage();

            // We must wait for something to happen.
            else
            {
                Debug.Assert(SyncFlag == false);
                Debug.Assert(FullSyncFlag == false);
                StallPipeline("waiting for events", NextRunDate);
            }
        }

        /// <summary>
        /// This method must be called whenever the processing stops in a stage
        /// of the pipeline because an event must be received to continue. If
        /// Deadline is not MaxValue, the timer thread will run the pipeline again
        /// at Deadline. The pipeline will do another full run when this method is
        /// called. Do not call this outside the pipeline.
        /// </summary>
        private void StallPipeline(String reason, DateTime deadline)
        {
            Debug.Assert(StageAction == PipelineAction.None);

            String ls = "Pipeline stall in stage " + CurrentStage + ": " + reason + ".";
            if (deadline != DateTime.MaxValue) ls += " Wake-up date: " + deadline + ".";
            else ls += " Wake-up date: none.";
            Logging.Log(ls);

            TimerDate = deadline;
            StageAction = PipelineAction.Stall;
        }

        /// <summary>
        /// Pass to the next pipeline stage. Do not call this outside the
        /// pipeline.
        /// </summary>
        private void NextStage()
        {
            Debug.Assert(StageAction == PipelineAction.None);
            StageAction = PipelineAction.Pass;
            PassFlag = false;
        }

        /// Request the pipeline to be synchronized. If FullFlag is true, the pipeline
        /// will be fully synchronized on the next run. Otherwise, the remaining
        /// stages of the pipeline will be synchronized. Do not call this outside the
        /// pipeline.
        private void SyncPipeline(String reason, bool fullFlag)
        {
            String ls = "";

            if (fullFlag)
            {
                ls += "Requesting full synchronization in stage " + CurrentStage;
                FullSyncFlag = true;
            }

            else
            {
                ls += "Synchronizing remaining stages in stage " + CurrentStage;
                SyncFlag = true;
            }

            Logging.Log(ls + ": " + reason + ".");
            NextRunDate = DateTime.MinValue;
        }

        /// <summary>
        /// This method must be called to request the pipeline to run again at
        /// Deadline. If Deadline is MinValue, the pipeline will be run again
        /// immediately. Do not call this outside the pipeline.
        /// </summary>
        private void ScheduleRun(String reason, DateTime deadline)
        {
            String ls = "Scheduling another run in stage " + CurrentStage + ": " + reason + ".";

            if (deadline != DateTime.MinValue)
            {
                ls += " When: " + deadline + ".";
                if (deadline < NextRunDate) NextRunDate = deadline;
            }

            else
            {
                ls += " When: now.";
                NextRunDate = DateTime.MinValue;
            }

            Logging.Log(ls);
        }
    }
}