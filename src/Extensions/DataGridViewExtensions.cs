using System.Data;
using System.Reflection;
using System.Windows.Forms;

namespace Jannesrsa.Tools.AssemblyReference.Extensions
{
    /// <summary>
    /// DataGridView extension methods
    /// </summary>
    internal static class DataGridViewExtensions
    {
        public static DataRow GetSelectedRow(this DataGridView dataGridView)
        {
            return dataGridView.GetDataRow(dataGridView.CurrentRow.Index);
        }

        public static DataRow GetDataRow(this DataGridView dataGridView, int rowIndex)
        {
            var currentDataRowView = (DataRowView)dataGridView.Rows[rowIndex].DataBoundItem;
            return currentDataRowView?.Row;
        }

        public static void RowCellClick(this DataGridView dataGridView, int columnIndex, int rowIndex)
        {
            var t = typeof(DataGridView);
            var p = new object[1];
            p[0] = new DataGridViewCellEventArgs(columnIndex, rowIndex);
            var m = t.GetMethod("OnCellClick", BindingFlags.NonPublic | BindingFlags.Instance);
            m.Invoke(dataGridView, p);
        }

        public static void SetColumnSortMode(this DataGridView dataGridView, DataGridViewColumnSortMode sortMode)
        {
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.SortMode = sortMode;
            }
        }
    }
}