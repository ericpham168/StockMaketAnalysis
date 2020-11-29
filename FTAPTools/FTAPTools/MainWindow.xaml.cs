using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Excel = Microsoft.Office.Interop.Excel;

namespace FTAPTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string DATE_HEADER = "<DTYYYYMMDD>";
        private const string OPEN_HEADER = "<Open>";
        private const string CLOSE_HEADER = "<Close>";
        private const string HIGH_HEADER = "<High>";
        private const string LOW_HEADER = "<Low>";
        public MainWindow()
        {
            InitializeComponent();
        }

        private string SelectImportFileAction()
        {
            string ExcelFilePath = string.Empty;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Excel or CSV files (*.xlsx,*.csv)|*.xlsx;*.csv";
            if (dialog.ShowDialog() == true)
            {
                ExcelFilePath = dialog.FileName;
            }
            return ExcelFilePath;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string path = SelectImportFileAction();
            Excel.Application oExcel = new Excel.Application();
            Excel.Workbook WB = oExcel.Workbooks.Open(path);
            Excel.Worksheet wks = (Excel.Worksheet)WB.Worksheets[1];
            WB.Worksheets.Add("Excel Processed");
            Excel.Worksheet wksProcessed = (Excel.Worksheet)WB.Worksheets[2];

            int totalRows = wks.UsedRange.Rows.Count;
            int totalColumns = wks.UsedRange.Columns.Count;
            List<ColumnsExcel> columnsHeader = new List<ColumnsExcel>();
            int newIndex = 1;
            for(int i = 1; i <= totalColumns; i++)
            {
                string header = ((Excel.Range)wks.Cells[1, i]).Value?.ToString();
                switch (header)
                {
                    case DATE_HEADER:
                        columnsHeader.Add(new ColumnsExcel(newIndex, i, DATE_HEADER));
                        ((Excel.Range)wksProcessed.Cells[1, i]).Value = DATE_HEADER;
                        newIndex++;
                        break;
                    case OPEN_HEADER:
                        columnsHeader.Add(new ColumnsExcel(newIndex, i, OPEN_HEADER));
                        ((Excel.Range)wksProcessed.Cells[1, i]).Value = OPEN_HEADER;
                        newIndex++;
                        break;
                    case CLOSE_HEADER:
                        columnsHeader.Add(new ColumnsExcel(newIndex, i, CLOSE_HEADER));
                        ((Excel.Range)wksProcessed.Cells[1, i]).Value = CLOSE_HEADER;
                        newIndex++;
                        break;
                    case HIGH_HEADER:
                        columnsHeader.Add(new ColumnsExcel(newIndex, i, HIGH_HEADER));
                        ((Excel.Range)wksProcessed.Cells[1, i]).Value = HIGH_HEADER;
                        newIndex++;
                        break;
                    case LOW_HEADER:
                        columnsHeader.Add(new ColumnsExcel(newIndex, i, LOW_HEADER));
                        ((Excel.Range)wksProcessed.Cells[1, i]).Value = LOW_HEADER;
                        newIndex++;
                        break;
                }
            }
            for (int row = 2; row <= totalRows; row++)
            {
                columnsHeader.ForEach(columnsHeader =>
                {
                    ((Excel.Range)wksProcessed.Cells[row, columnsHeader.NewIndex]).Value = ((Excel.Range)wks.Cells[row, columnsHeader.ColumnIndex]).Value?.ToString();
                });
            }

            WB.Save();
            WB.Close();
            oExcel.Quit();  

        }
    }
}
