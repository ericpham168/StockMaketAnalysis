using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTAPExcelTools
{
    class ColumnsExcel
    {
        public ColumnsExcel(int newIndex, int index, string header)
        {
            NewIndex = newIndex;
            ColumnIndex = index;
            HeaderColumn = header;
        }

        public int NewIndex { get; set; }
        public int ColumnIndex { get; set; }
        public string HeaderColumn { get; set; }
    }
}
