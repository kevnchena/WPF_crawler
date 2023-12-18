using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using LiveCharts;
using LiveCharts.Wpf;

namespace WPF_crawler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string defaultURL = "https://data.moenv.gov.tw/api/v2/aqx_p_432?api_key=e8dd42e6-9b8b-43f8-991e-b3dee723a52d&limit=1000&sort=ImportDate%20desc&format=JSON";
        
        AQIdata aqidata = new AQIdata();
        List<Field> fields = new List<Field>();
        List<Record> records = new List<Record>();
        SeriesCollection seriescollection = new SeriesCollection();
        List<Record> selectedRecords = new List<Record>();
        public MainWindow()
        {
            InitializeComponent();
            UrlTextBox.Text = defaultURL;
            selectedRecords.Clear();
        }

        private async void fetchButton_Click(object sender, RoutedEventArgs e)
        {
            string url = UrlTextBox.Text;
            ContentTextBox.Text = "正在抓取網路資料...";

            string data = await FetchContentAsync(url);
            ContentTextBox.Text= data;
            aqidata=JsonSerializer.Deserialize<AQIdata>(data);
            fields=aqidata.fields.ToList();
            records=aqidata.records.ToList();
            selectedRecords=selectedRecords.ToList();
            statusTextBlock.Text = $"共有{records.Count}筆資料";
            DisplayAQIData();
        }

        private void DisplayAQIData()
        {
            RecordDataGrid.ItemsSource= records;

            Record record = records[0];
            DataWrapPanel.Children.Clear();

            foreach (Field field in fields)
            {
                var propertyInfo = record.GetType().GetProperty(field.id);
                if(propertyInfo != null )
                {
                    var value = propertyInfo.GetValue(record)as string;
                    if(double.TryParse(value, out double v))
                    {
                        CheckBox cb = new CheckBox
                        {
                            Content = field.info.label,
                            Tag = field.id,
                            Margin = new Thickness(3),
                            FontSize = 14,
                            FontWeight=FontWeights.Bold,
                            Width = 100

                        };
                        cb.Checked += UpdateChart;
                        cb.Unchecked += UpdateChart;
                        DataWrapPanel.Children.Add(cb);
                    }
                }
            }
        }

        private void UpdateChart(object sender, EventArgs e)
        {
            seriescollection.Clear();

            foreach (CheckBox cb in DataWrapPanel.Children)
            {
                if (cb.IsChecked == true)
                {
                    List<string> labels = new List<string>();
                    String tag = cb.Tag as String;
                    ColumnSeries columnSeries = new ColumnSeries();
                    ChartValues<double> values = new ChartValues<double>();

                    foreach (Record record in selectedRecords)
                    {
                        var propertyInfo = record.GetType().GetProperty(tag);
                        if (propertyInfo != null)
                        {
                            var value = propertyInfo.GetValue(record) as string;
                            if (double.TryParse(value, out double v))
                            {
                                values.Add(v);
                                labels.Add(record.sitename);
                            }
                        }
                    }
                columnSeries.Values=values;
                columnSeries.Title = tag;
                columnSeries.LabelPoint = point => $"{labels[(int)point.X]}:{point.Y.ToString()}";
                seriescollection.Add(columnSeries);

                }
                AQIChart.Series = seriescollection;
            }
        }
        private async Task<string> FetchContentAsync(string url)
        {
            using (var client = new HttpClient())
            {
                client.Timeout=TimeSpan.FromSeconds(200);//設定超時200

                try
                {
                    return await client.GetStringAsync(url);
                }
                catch (TaskCanceledException ex)
                {
                    MessageBox.Show("請求超時或被取消");
                    throw;
                } catch (Exception ex)
                {
                    MessageBox.Show($"讀取數據時發生錯誤{ex.Message}");
                    throw;
                }
            }
        }

        private void RecordDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() +1).ToString();
        }

        private void RecordDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedRecords = RecordDataGrid.SelectedItems.Cast<Record>().ToList();
            statusTextBlock.Text = $"總共選取{selectedRecords.Count()}筆記錄";
        }
    }
}
