using FTAPExcelTools.ProgressDialog;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Excel = Microsoft.Office.Interop.Excel;

namespace FTAPExcelTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HttpClient _client;
        String Path = string.Empty;
        private const string DATE_HEADER = "<DTYYYYMMDD>";
        private const string OPEN_HEADER = "<Open>";
        private const string CLOSE_HEADER = "<Close>";
        private const string HIGH_HEADER = "<High>";
        private const string LOW_HEADER = "<Low>";
        private const string SMA_3 = "SMA(3)";
        private const string SMA_5 = "SMA(5)";
        private const string SMA_8 = "SMA(8)";
        public MainWindow()
        {
            InitializeComponent();
            //
            _client = new HttpClient();
            _client.BaseAddress = new Uri(@"https://localhost:44318/");
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //
            var processes = from p in Process.GetProcessesByName("EXCEL")
                            select p;
            foreach (var process in processes)
            {
                process.Kill();
            }
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

        private void HandlerAndExportExcelFile(string path)
        {
            string pathSave = new FileInfo(path).Directory.FullName;
            Excel.Application oExcel = new Excel.Application();
            Excel.Workbook WB = oExcel.Workbooks.Open(path);
            Excel.Worksheet wks = (Excel.Worksheet)WB.Worksheets[1];

            //Export file SMA
            WB.Sheets.Add(After: WB.Sheets[WB.Sheets.Count]);
            Excel.Worksheet wksProcessed = (Excel.Worksheet)WB.Worksheets[2];

            int totalRows = wks.UsedRange.Rows.Count;
            int totalColumns = wks.UsedRange.Columns.Count;
            List<ColumnsExcel> columnsHeader = new List<ColumnsExcel>();
            int newIndex = 1;

            for (int i = 1; i <= totalColumns; i++)
            {
                string header = ((Excel.Range)wks.Cells[1, i]).Value?.ToString();
                switch (header)
                {
                    case DATE_HEADER:
                        columnsHeader.Add(new ColumnsExcel(newIndex, i, DATE_HEADER));
                        ((Excel.Range)wksProcessed.Cells[1, newIndex]).Value = DATE_HEADER;
                        newIndex++;
                        break;
                    case OPEN_HEADER:
                        columnsHeader.Add(new ColumnsExcel(newIndex, i, OPEN_HEADER));
                        ((Excel.Range)wksProcessed.Cells[1, newIndex]).Value = OPEN_HEADER;
                        newIndex++;
                        break;
                    case CLOSE_HEADER:
                        columnsHeader.Add(new ColumnsExcel(newIndex, i, CLOSE_HEADER));
                        ((Excel.Range)wksProcessed.Cells[1, newIndex]).Value = CLOSE_HEADER;
                        newIndex++;
                        break;
                    case HIGH_HEADER:
                        columnsHeader.Add(new ColumnsExcel(newIndex, i, HIGH_HEADER));
                        ((Excel.Range)wksProcessed.Cells[1, newIndex]).Value = HIGH_HEADER;
                        newIndex++;
                        break;
                    case LOW_HEADER:
                        columnsHeader.Add(new ColumnsExcel(newIndex, i, LOW_HEADER));
                        ((Excel.Range)wksProcessed.Cells[1, newIndex]).Value = LOW_HEADER;
                        newIndex++;
                        break;
                }
            }

            for (int row = 2; row <= totalRows; row++)
            {
                columnsHeader.ForEach(col =>
                {
                    ((Excel.Range)wksProcessed.Cells[row, col.NewIndex]).Value = ((Excel.Range)wks.Cells[row, col.ColumnIndex]).Value;
                });
            }

            int swksProcessedColumnsCount = wksProcessed.UsedRange.Columns.Count;
            int swksProcessedRowCount = wksProcessed.UsedRange.Rows.Count;
            ((Excel.Range)wksProcessed.Cells[1, swksProcessedColumnsCount + 1]).Value = SMA_3;
            columnsHeader.Add(new ColumnsExcel(newIndex, wksProcessed.UsedRange.Columns.Count + 1, SMA_3));
            newIndex++;

            ((Excel.Range)wksProcessed.Cells[1, swksProcessedColumnsCount + 2]).Value = SMA_5;
            columnsHeader.Add(new ColumnsExcel(newIndex, wksProcessed.UsedRange.Columns.Count + 2, SMA_5));
            newIndex++;

            ((Excel.Range)wksProcessed.Cells[1, swksProcessedColumnsCount + 3]).Value = SMA_8;
            columnsHeader.Add(new ColumnsExcel(newIndex, wksProcessed.UsedRange.Columns.Count + 3, SMA_8));
            newIndex++;

            swksProcessedColumnsCount = wksProcessed.UsedRange.Columns.Count;
            int indexCloseColumn = columnsHeader.FirstOrDefault(col => col.HeaderColumn == CLOSE_HEADER).NewIndex;
            int indexSMA3Column = columnsHeader.FirstOrDefault(col => col.HeaderColumn == SMA_3).NewIndex;
            int indexSMA5Column = columnsHeader.FirstOrDefault(col => col.HeaderColumn == SMA_5).NewIndex;
            int indexSMA8Column = columnsHeader.FirstOrDefault(col => col.HeaderColumn == SMA_8).NewIndex;

            for (int row = 4; row <= swksProcessedRowCount; row++)
            {
                double num1 = Double.Parse(((Excel.Range)wksProcessed.Cells[row - 2, indexCloseColumn]).Value.ToString());
                double num2 = Double.Parse(((Excel.Range)wksProcessed.Cells[row - 1, indexCloseColumn]).Value.ToString());
                double num3 = Double.Parse(((Excel.Range)wksProcessed.Cells[row, indexCloseColumn]).Value.ToString());
                double num4 = 0;
                double num5 = 0;
                ((Excel.Range)wksProcessed.Cells[row, indexSMA3Column]).Value = Math.Round(Convert.ToDouble((num1 + num2 + num3) / 3), 1);
                if (row >= 6)
                {
                    num4 = Double.Parse(((Excel.Range)wksProcessed.Cells[row - 3, indexCloseColumn]).Value.ToString());
                    num5 = Double.Parse(((Excel.Range)wksProcessed.Cells[row - 4, indexCloseColumn]).Value.ToString());
                    ((Excel.Range)wksProcessed.Cells[row, indexSMA5Column]).Value = Math.Round(Convert.ToDouble((num1 + num2 + num3 + num4 + num5) / 5), 1);
                }
                if (row >= 9)
                {
                    double num6 = Double.Parse(((Excel.Range)wksProcessed.Cells[row - 5, indexCloseColumn]).Value.ToString());
                    double num7 = Double.Parse(((Excel.Range)wksProcessed.Cells[row - 6, indexCloseColumn]).Value.ToString());
                    double num8 = Double.Parse(((Excel.Range)wksProcessed.Cells[row - 7, indexCloseColumn]).Value.ToString());
                    ((Excel.Range)wksProcessed.Cells[row, indexSMA8Column]).Value = Math.Round(Convert.ToDouble((num1 + num2 + num3 + num4 + num5 + num6 + num7 + num8) / 8), 1);
                }
            }

            string pathProcessSave = $"{pathSave}\\{WB.Name.Replace(".csv", "")} processed.csv";
            WB.SaveCopyAs(pathProcessSave);

            // export file use
            WB.Sheets.Add(After: WB.Sheets[WB.Sheets.Count]);
            Excel.Worksheet wksUse = (Excel.Worksheet)WB.Worksheets[3];

            int indexTIDCol = 1;
            int indexItemSetCol = 2;
            int indexPriceCol = 3;
            int TID = 1;

            //Add header
            ((Excel.Range)wksUse.Cells[1, indexTIDCol]).Value = "TID";
            ((Excel.Range)wksUse.Cells[1, indexItemSetCol]).Value = "item set";
            ((Excel.Range)wksUse.Cells[1, indexPriceCol]).Value = "price";

            int flagSMA3_5 = 0;
            int flagSMA5_8 = 0;
            int flagSMA3_8 = 0;

            for (int row = 2; row <= swksProcessedRowCount; row++)
            {
                //
                double SMA3Output = 0;
                double SMA5Output = 0;
                double SMA8Output = 0;
                Double.TryParse(((Excel.Range)wksProcessed.Cells[row, indexSMA3Column]).Value?.ToString(), out SMA3Output);
                Double.TryParse(((Excel.Range)wksProcessed.Cells[row, indexSMA5Column]).Value?.ToString(), out SMA5Output);
                Double.TryParse(((Excel.Range)wksProcessed.Cells[row, indexSMA8Column]).Value?.ToString(), out SMA8Output);

                double valueClose = Double.Parse(((Excel.Range)wksProcessed.Cells[row, indexCloseColumn])?.Value.ToString());
                double SMA3Value = SMA3Output;
                double SMA5Value = SMA5Output;
                double SMA8Value = SMA8Output;
                
                //
                ((Excel.Range)wksUse.Cells[row, indexTIDCol]).Value = TID++;
                ((Excel.Range)wksUse.Cells[row, indexPriceCol]).Value = Math.Round(Convert.ToDouble(valueClose), 0);

                string SMA3_5Value = string.Empty;
                string SMA5_8Value = string.Empty;
                string SMA3_8Value = string.Empty;

                // SMA3 & SMA5
                string itemSetValue = ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value?.ToString();
                if (row > flagSMA3_5)
                {
                    if (SMA3Value != 0 && SMA5Value != 0 && SMA3Value == SMA5Value)
                    {
                        for (int rowChild = row + 1; rowChild <= swksProcessedRowCount; rowChild++)
                        {
                            double SMA3ValueLoop = Double.Parse(((Excel.Range)wksProcessed.Cells[rowChild, indexSMA3Column]).Value.ToString());
                            double SMA5ValueLoop = Double.Parse(((Excel.Range)wksProcessed.Cells[rowChild, indexSMA5Column]).Value.ToString());
                            if (SMA3ValueLoop > SMA5ValueLoop)
                            {
                                SMA3_5Value = "A";
                                if (itemSetValue == null)
                                {
                                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = SMA3_5Value;
                                }
                                else
                                {
                                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = String.Format("{0},{1}", itemSetValue, SMA3_5Value);

                                }
                                flagSMA3_5 = row;
                                break;
                            }
                            else if (SMA3ValueLoop < SMA5ValueLoop)
                            {
                                SMA3_5Value = "B";
                                if (itemSetValue == null)
                                {
                                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = SMA3_5Value;
                                }
                                else
                                {
                                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = String.Format("{0},{1}", itemSetValue, SMA3_5Value);
                                }
                                flagSMA3_5 = row;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = String.Format("{0},{1}", itemSetValue, SMA3_5Value).Equals(",") ? string.Empty : String.Format("{0},{1}", itemSetValue, SMA3_5Value);
                }

                // SMA3 & SMA8
                itemSetValue = ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value?.ToString();
                if (row > flagSMA3_8)
                {
                    if (SMA3Value != 0 && SMA8Value != 0 && SMA3Value == SMA8Value)
                    {
                        for (int rowChild = row + 1; rowChild <= swksProcessedRowCount; rowChild++)
                        {
                            double SMA3ValueLoop = Double.Parse(((Excel.Range)wksProcessed.Cells[rowChild, indexSMA3Column]).Value.ToString());
                            double SMA8ValueLoop = Double.Parse(((Excel.Range)wksProcessed.Cells[rowChild, indexSMA8Column]).Value.ToString());
                            if (SMA3ValueLoop > SMA8ValueLoop)
                            {
                                SMA3_8Value = "C";
                                if (itemSetValue == null)
                                {
                                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = SMA3_8Value;
                                }
                                else
                                {
                                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = String.Format("{0},{1}", itemSetValue, SMA3_8Value);
                                }
                                flagSMA3_8 = row;
                                break;
                            }
                            else if (SMA3ValueLoop < SMA8ValueLoop)
                            {
                                SMA3_8Value = "D";
                                if (itemSetValue == null)
                                {
                                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = SMA3_8Value;
                                }
                                else
                                {
                                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = String.Format("{0},{1}", itemSetValue, SMA3_8Value);
                                }
                                flagSMA3_8 = row;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = String.Format("{0},{1}", itemSetValue, SMA3_8Value).Equals(",") ? string.Empty : String.Format("{0},{1}", itemSetValue, SMA3_8Value);
                }


                // SMA5 & SMA8
                itemSetValue = ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value?.ToString();
                if (row > flagSMA5_8)
                {
                    if (SMA5Value != 0 && SMA8Value != 0 && SMA5Value == SMA8Value)
                    {
                        for (int rowChild = row + 1; rowChild <= swksProcessedRowCount; rowChild++)
                        {
                            double SMA5ValueLoop = Double.Parse(((Excel.Range)wksProcessed.Cells[rowChild, indexSMA5Column]).Value.ToString());
                            double SMA8ValueLoop = Double.Parse(((Excel.Range)wksProcessed.Cells[rowChild, indexSMA8Column]).Value.ToString());
                            if (SMA5ValueLoop > SMA8ValueLoop)
                            {
                                SMA5_8Value = "E";
                                if (itemSetValue == null)
                                {
                                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = SMA5_8Value;
                                }
                                else
                                {
                                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = String.Format("{0},{1}", itemSetValue, SMA5_8Value);
                                }
                                flagSMA5_8 = row;
                                break;
                            }
                            else if (SMA5ValueLoop < SMA8ValueLoop)
                            {
                                SMA5_8Value = "F";
                                if (itemSetValue == null)
                                {
                                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = SMA5_8Value;
                                }
                                else
                                {
                                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = String.Format("{0},{1}", itemSetValue, SMA5_8Value);
                                }
                                flagSMA5_8 = row;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    ((Excel.Range)wksUse.Cells[row, indexItemSetCol]).Value = String.Format("{0},{1}", itemSetValue, SMA5_8Value).Equals(",") ? string.Empty : String.Format("{0},{1}", itemSetValue, SMA5_8Value);
                }


            }

            string pathUseFile = $"{pathSave}\\{WB.Name.Replace(".csv", "")} use.csv";
            WB.SaveCopyAs(pathUseFile);
            Path = pathUseFile;
            //
            WB.Close(false);
            oExcel.Quit();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Path == string.Empty) return;
            ProgressDialogResult resultLog = FTAPExcelTools.ProgressDialog.ProgressDialog.Execute(Application.Current.Windows.OfType<Window>().Where(o => o.Name == "mainWindow").SingleOrDefault(), "Exporting to Excel... plz watting !!", (bw) =>
            {
                HandlerAndExportExcelFile(Path);
                MessageBox.Show("Export Successful");
            });
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Path = SelectImportFileAction();
            tvPath.Text = Path;
        }

        private List<Transaction> GetDataFromExcel(string path)
        {
            List<Transaction> transactions = new List<Transaction>();
            Excel.Application oExcel = new Excel.Application();
            Excel.Workbook WB = oExcel.Workbooks.Open(path);
            Excel.Worksheet wks = (Excel.Worksheet)WB.Worksheets[1];
            int totalRows = wks.UsedRange.Rows.Count;
            int totalColumns = wks.UsedRange.Columns.Count;
            ProgressDialogResult resultLog = FTAPExcelTools.ProgressDialog.ProgressDialog.Execute(Application.Current.Windows.OfType<Window>().Where(o => o.Name == "mainWindow").SingleOrDefault(), "Importing Data... plz watting !!", (bw) =>
            {
                for (int i = 2; i <= totalRows; i++)
                {
                    Transaction transaction = new Transaction();
                    transaction.TID = ((Excel.Range)wks.Cells[i, 1]).Value?.ToString() != null ?
                                            Int32.Parse(((Excel.Range)wks.Cells[i, 1]).Value?.ToString()) : 0;
                    transaction.ItemSet = ((Excel.Range)wks.Cells[i, 2]).Value?.ToString();
                    transaction.Price = ((Excel.Range)wks.Cells[i, 3]).Value?.ToString() != null ?
                                            Double.Parse(((Excel.Range)wks.Cells[i, 3]).Value?.ToString()) : 0;
                    transactions.Add(transaction);
                }
            });
            WB.Close();
            oExcel.Quit();
            return transactions;
        }


        public async void ImportDatabase(List<Transaction> transactions)
        {
            string endpoint = "api/rule";
            var json = JsonConvert.SerializeObject(transactions, Newtonsoft.Json.Formatting.Indented);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(endpoint, httpContent);
            if(response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                MessageBox.Show("Import Successful !");
            }
            else
            {
                MessageBox.Show(response.StatusCode.ToString());
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (Path == string.Empty)
                return;
            else
            {
                ImportDatabase(GetDataFromExcel(Path));
            }
        }
    }
}
