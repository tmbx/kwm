using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace kwm.KwmAppControls
{
    /// <summary>
    /// This class is used to handle the UI of a workspace application. The
    /// same control is used to manage the applications of all workspaces.
    /// </summary>
    public partial class BaseAppControl : UserControl
    {
        /// <summary>
        /// Application currently associated to the control, if any. 
        /// </summary>
        protected KwsApp m_srcApp = null;

        /// <summary>
        /// Return the ID of the applications managed by this control.
        /// </summary>
        public virtual UInt32 ID { get { throw new Exception("unimplemented"); } }

        public BaseAppControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Call this method to assign to this component a new application 
        /// instance from which it can populate its visual state and receive
        /// new events. This implements the Template Method design pattern, 
        /// which means its the base class that knows which steps are required,
        /// but it's up to the childs to implements these steps.
        /// </summary>
        public virtual void SetDataSource(KwsApp app)
        {
            if (m_srcApp != null) UnregisterAppEventHandlers();
            m_srcApp = app;
            UpdateControls();
            if (m_srcApp != null) RegisterAppEventHandlers();
        }

        /// <summary>
        /// Stop listening to the current application.
        /// </summary>
        protected virtual void UnregisterAppEventHandlers()
        {
            m_srcApp.OnNeedToUpdateControl -= HandleOnNeedToUpdateControl;
        }

        /// <summary>
        /// Start listening to the current application.
        /// </summary>
        protected virtual void RegisterAppEventHandlers()
        {
            m_srcApp.OnNeedToUpdateControl += HandleOnNeedToUpdateControl;
        }

        /// <summary>
        /// Return the run level of the application that should be displayed by
        /// the control.
        /// </summary>
        protected virtual KwsRunLevel GetRunLevel()
        {
            if (m_srcApp == null) return KwsRunLevel.Stopped;
            return m_srcApp.Helper.GetRunLevel();
        }

        /// <summary>
        /// Clear the control and update the content of its internal controls 
        /// according to m_srcApp, if any.
        /// </summary>
        protected virtual void UpdateControls()
        {
            throw new Exception("unimplemented");
        }

        /// <summary>
        /// Called when the application control needs to update itself because
        /// the application state has changed.
        /// </summary>
        private void HandleOnNeedToUpdateControl(object _sender, EventArgs _args)
        {
            UpdateControls();
        }
    }
}
