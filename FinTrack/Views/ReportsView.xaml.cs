using System.Windows;
using System.Windows.Controls;

namespace FinTrack.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
        }

        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            txtReportOutput.Text = "Report generation will be implemented here.";
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            txtReportOutput.Text = "CSV export will be implemented here.";
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            txtReportOutput.Text = "PDF export will be implemented here.";
        }
    }
}