using System;
using System.Windows.Forms;
using System.Collections;
using kwm.KwmAppControls.AppKfs;

namespace Tbx.Utils
{
    /// <summary>
    /// Sorts ListView according to custom columns Comparers.
    /// </summary>
    public class ListViewSorter : IComparer
    {
        /// <summary>
        /// Columns Comparers.
        /// </summary>.
        protected Comparer[] comparers;

        /// <summary>
        /// Current column index.
        /// </summary>
        protected int index;

        /// <summary>
        /// Parent ListView.
        /// </summary>
        protected ListView list;

        /// <summary>
        /// In which order the sorting should take place.
        /// </summary>
        protected bool Ascending = true;
        /// <summary>
        /// Creates new instance.
        /// </summary>
        public ListViewSorter()
            : this(null)
        {
        }

        /// <summary>
        /// Creates new instance.
        /// </summary>
        public ListViewSorter(ListView list)
            : this(list, new Comparer[list.Columns.Count])
        {
        }

        /// <summary>
        /// Creates new instance.
        /// </summary>
        public ListViewSorter(ListView list, Comparer[] comparers)
        {
            this.List = list;
            this.Comparers = comparers;
        }

        /// <summary>
        /// Parent ListView.
        /// </summary>
        public ListView List
        {
            get { return this.list; }
            set
            {
                if (this.list == value) return;

                // unwire old
                if (this.list != null)
                {
                    this.list.ColumnClick -= new ColumnClickEventHandler(this.Column_Click);
                    this.list.ListViewItemSorter = null;
                }

                // setup data
                this.list = value;
                this.Comparers = this.list != null ?
                    new Comparer[this.list.Columns.Count] :
                    null;

                // wire new
                if (this.list != null)
                {
                    this.list.Sorting = SortOrder.None;
                    this.index = 0;
                    this.list.ColumnClick += new ColumnClickEventHandler(this.Column_Click);
                    this.list.ListViewItemSorter = this;
                }
            }
        } // List

        /// <summary>
        /// Column Comparer.
        /// </summary>
        public Comparer this[ColumnHeader column]
        {
            get { return this[column.Index]; }
            set { this[column.Index] = value; }
        }

        /// <summary>
        /// Column Comparer.
        /// </summary>
        public Comparer this[int index]
        {
            get { return this.comparers[index]; }
            set { this.comparers[index] = value; }
        }

        /// <summary>
        /// Columns Comparers.
        /// </summary>
        public Comparer[] Comparers
        {
            get { return this.comparers; }
            set
            {
                // verify value
                if (this.list.Columns.Count != value.Length)
                    throw new Exception("Invalid value.");

                // setup data
                this.comparers = value;
                this.index = 0;
            }
        } // Comparers

        /// <summary>
        /// Sort list by asked column.
        /// </summary>
        public void SortBy(int index, bool asc)
        {
            Logging.Log("SortBy : " + index + ", " + asc);
            if (this[index] == null) return;

            this.index = index;
            this.Ascending = asc;
            this.List.Sort();
        }

        /// <summary>
        /// Compares two items.
        /// </summary>
        int IComparer.Compare(object x, object y)
        {
            //Logging.Log("Compare called " + x.ToString() + ", " + y.ToString());

            if (this.list == null) return 0;

            ListViewItem ix = x as ListViewItem;
            ListViewItem iy = y as ListViewItem;

            if (ix.ListView != this.list ||
                iy.ListView != this.list)
                throw new Exception("Invalid arguments.");

            // compare
            return (Ascending ? 1 : -1) * this[this.index](index, ix, iy);
        }

        // Handles sorting requests from the UI.
        protected void Column_Click(object sender, ColumnClickEventArgs e)
        {
            ListView list = sender as ListView;
            if (list != this.list)
                throw new Exception("Invalid arguments.");

            // Sort. This inverts the sort order.
            SortBy(e.Column, this.index != e.Column || !this.Ascending);
        }

        /// <summary>
        /// Basic comparer.
        /// </summary>
        public delegate int Comparer(int colHeader, ListViewItem x, ListViewItem y);

        /// <summary>
        /// Standard strings comparer.
        /// </summary>
        public static int CompareStrings(int index, ListViewItem a, ListViewItem b)
        {
            string x = a.SubItems[index].Text;
            string y = b.SubItems[index].Text;

            return x.CompareTo(y);
        }

        public static int NonComparable(int index, ListViewItem a, ListViewItem b)
        {
            return 0;
        }
        /// <summary>
        /// Standard numbers comparer.
        /// </summary>
        public static int CompareNumbers(int index, ListViewItem a, ListViewItem b)
        {
            string x = a.SubItems[index].Text;
            string y = b.SubItems[index].Text;

            double dx = double.Parse(x);
            double dy = double.Parse(y);

            return dx.CompareTo(dy);
        }

        /// <summary>
        /// Standard dates comparer.
        /// </summary>
        public static int CompareDates(int index, ListViewItem a, ListViewItem b)
        {
            string x = a.SubItems[index].Text;
            string y = b.SubItems[index].Text;

            DateTime dx = DateTime.Parse(x);
            DateTime dy = DateTime.Parse(y);

            return DateTime.Compare(dx, dy);
        }

        /// <summary>
        /// Compares the column "File name". Need to do special sorting to sort folders
        /// on top of any files.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareFileItem(int index, ListViewItem x, ListViewItem y)
        {
            KListViewItem a = x as KListViewItem;
            KListViewItem b = y as KListViewItem;

            if (a.IsDirectory && b.IsFile)
                return -1;
            if (a.IsFile && b.IsDirectory)
                return 1;

            return a.Text.CompareTo(b.Text);
        }
    }
}