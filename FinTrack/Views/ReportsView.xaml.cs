using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using FinTrack.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace FinTrack.Views
{
    public partial class ReportsView : UserControl
    {
        // Stores the last generated report so Export buttons can use it
        private FinancialReport? _currentReport;

        public ReportsView()
        {
            InitializeComponent();

            // Set default date range to the current month
            dpStartDate.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dpEndDate.SelectedDate = DateTime.Now;

            Loaded += (_, __) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LoadPieChart();
                    LoadLineChart();
                }), System.Windows.Threading.DispatcherPriority.Render);
            };
        }

        //  REPORT GENERATION 

        // Called when the user clicks "Generate Report"
        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            // Esure the user picked both dates
            if (dpStartDate.SelectedDate == null || dpEndDate.SelectedDate == null)
            {
                MessageBox.Show("Please select both a start and end date.");
                return;
            }

            DateTime start = dpStartDate.SelectedDate.Value;
            DateTime end = dpEndDate.SelectedDate.Value;

            if (start > end)
            {
                MessageBox.Show("Start date must be before end date.");
                return;
            }

            // Asks DataManager to build the report
            _currentReport = MainWindow.DataManager.GenerateReport(start, end);

            // Show the summary panel with the results
            txtSummary.Text =
                $"Period:          {start:MM/dd/yyyy}  →  {end:MM/dd/yyyy}\n" +
                $"Total Income:    ${_currentReport.TotalIncome:F2}\n" +
                $"Total Expenses:  ${_currentReport.TotalExpenses:F2}\n" +
                $"Net Savings:     ${_currentReport.NetSavings:F2}\n" +
                $"Transactions:    {_currentReport.Transactions.Count}";

            pnlSummary.Visibility = Visibility.Visible;

            // Refresh both charts to match the selected date range
            LoadPieChart(start, end);
            LoadLineChart(start, end);

            txtStatus.Text = "Report generated successfully.";
            txtStatus.Foreground = System.Windows.Media.Brushes.Green;
        }

        //  PIE CHART -Builds the spending-by-category pie chart
        // start/end are optional — if not provided, uses ALL transactions
        private void LoadPieChart(DateTime? start = null, DateTime? end = null)
        {
            // Get spending grouped by category from DataManager
            // If a date range was given, filter first; otherwise use everything
            var transactions = start.HasValue
                ? MainWindow.DataManager.GetTransactionsByDateRange(start.Value, end!.Value)
                : MainWindow.DataManager.Transactions;

            var spendingByCategory = transactions
                .Where(t => t.Type == "Expense")
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            if (!spendingByCategory.Any())
            {
                txtStatus.Text = "No expense data available for the selected range.";
                txtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                return;
            }

            // Define a set of colors to cycle through for each slice
            var colors = new[]
            {
                SKColor.Parse("#2ECC71"), // green
                SKColor.Parse("#E74C3C"), // red
                SKColor.Parse("#3498DB"), // blue
                SKColor.Parse("#F39C12"), // orange
                SKColor.Parse("#9B59B6"), // purple
                SKColor.Parse("#1ABC9C"), // teal
                SKColor.Parse("#E67E22"), // dark orange
            };

            int colorIndex = 0;

            // Build one PieSeries per category
            var series = spendingByCategory.Select(kvp =>
                new PieSeries<double>
                {
                    Name = kvp.Key,
                    Values = new double[] { (double)kvp.Value },
                    Fill = new SolidColorPaint(colors[colorIndex++ % colors.Length]),

                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize = 10,

                    // show only dollar amount on slice
                    DataLabelsFormatter = point => $"${point.Coordinate.PrimaryValue:F0}"
                }
            ).ToArray();

            pieChart.Series = series;
    
        }

    // LINE CHART 
     // Builds the monthly expense trend line chart
        private void LoadLineChart(DateTime? start = null, DateTime? end = null)
        {
            var transactions = start.HasValue
                ? MainWindow.DataManager.GetTransactionsByDateRange(start.Value, end!.Value)
                : MainWindow.DataManager.Transactions;

            // Group expenses by month (format: "2026-04")
            var monthlyData = transactions
                .Where(t => t.Type == "Expense")
                .GroupBy(t => t.Date.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .ToList();

            if (!monthlyData.Any())
            {
                return;
            }

            // The Y values (dollar amounts per month)
            var values = monthlyData
                .Select(g => (double)g.Sum(t => t.Amount))
                .ToArray();

            // The X axis labels (month names)
            var labels = monthlyData
                .Select(g =>
                {
                    // Convert "2026-04" into "Apr 2026" for readability
                    var date = DateTime.ParseExact(g.Key, "yyyy-MM", null);
                    return date.ToString("MMM yyyy");
                })
                .ToArray();

            // Build the line series
            lineChart.Series = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = "Monthly Expenses",
                    Values = values,
                    Stroke = new SolidColorPaint(SKColor.Parse("#E74C3C")) { StrokeThickness = 3 },
                    Fill = null,
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometryStroke = new SolidColorPaint(SKColor.Parse("#E74C3C")) { StrokeThickness = 2 },
                    GeometrySize = 8,

                    // REMOVE point labels
                    DataLabelsPaint = null
                }
            };

            // Set the X axis to show month labels instead of numbers
            lineChart.XAxes = new[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsRotation = 0,
                    TextSize = 10
                }
            };

            lineChart.YAxes = new[]
            {
                new Axis
                {
                    Labeler = value => $"${value:F0}",
                    TextSize = 10,
                    MinStep = 1
                }
            };


lineChart.DrawMargin = new LiveChartsCore.Measure.Margin(20, 20, 20, 50);
        }

    //  CSV EXPORT 

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var transactions = _currentReport?.Transactions
                ?? MainWindow.DataManager.Transactions;

            if (!transactions.Any())
            {
                MessageBox.Show("No transactions to export.");
                return;
            }

            try
            {
                // Build the CSV content line by line
                // StringBuilder is efficient for building large strings
                var sb = new StringBuilder();

                // Header row
                sb.AppendLine("Id,Date,Description,Category,Type,Amount");

                // One row per transaction
                foreach (var t in transactions)
                {
                    // Wrap description in quotes in case it contains a comma
                    sb.AppendLine(
                        $"{t.Id}," +
                        $"{t.Date:MM/dd/yyyy}," +
                        $"\"{t.Description}\"," +
                        $"{t.Category}," +
                        $"{t.Type}," +
                        $"{t.Amount:F2}"
                    );
                }

                // Save to the desktop so it's easy to find
                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"FinTrack_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                );

                File.WriteAllText(filePath, sb.ToString());

                txtStatus.Text = $"CSV saved to Desktop: {Path.GetFileName(filePath)}";
                txtStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}");
            }
        }

//  PDF EXPORT 

private void ExportPdf_Click(object sender, RoutedEventArgs e)
{
    var report = _currentReport ?? MainWindow.DataManager.GenerateReport(
        DateTime.MinValue, DateTime.Now);

    if (!report.Transactions.Any())
    {
        MessageBox.Show("No transactions to include in the report.");
        return;
    }

    try
    {
        string filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"FinTrack_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        );

        // Helper functions to avoid repeating XUnitPt.FromPoint everywhere
        XUnitPt Pt(double value) => XUnitPt.FromPoint(value);
        XRect Rect(double x, double y, double w, double h) =>
            new XRect(XUnitPt.FromPoint(x), XUnitPt.FromPoint(y),
                      XUnitPt.FromPoint(w), XUnitPt.FromPoint(h));

        var pdf = new PdfDocument();
        pdf.Info.Title = "FinTrack Financial Report";
        pdf.Info.Author = "FinTrack App";

        PdfPage page = pdf.AddPage();
        XGraphics gfx = XGraphics.FromPdfPage(page);

        var titleFont   = new XFont("Arial", 20, XFontStyleEx.Bold);
        var headingFont = new XFont("Arial", 13, XFontStyleEx.Bold);
        var bodyFont    = new XFont("Arial", 11, XFontStyleEx.Regular);
        var smallFont   = new XFont("Arial",  9, XFontStyleEx.Regular);

        double margin = 50;
        double y = margin;

        // ── Title ──
        gfx.DrawString("FinTrack Financial Report", titleFont,
            XBrushes.DarkBlue, Rect(margin, y, page.Width.Point - margin * 2, 30),
            XStringFormats.TopLeft);
        y += 35;

        gfx.DrawString($"Generated: {DateTime.Now:MMMM dd, yyyy}", smallFont,
            XBrushes.Gray, Rect(margin, y, page.Width.Point - margin * 2, 20),
            XStringFormats.TopLeft);
        y += 30;

        gfx.DrawLine(XPens.LightGray, Pt(margin), Pt(y), Pt(page.Width.Point - margin), Pt(y));
        y += 15;

        // ── Summary Section ──
        gfx.DrawString("Summary", headingFont, XBrushes.Black,
            Rect(margin, y, 200, 20), XStringFormats.TopLeft);
        y += 25;

        var summaryLines = new[]
        {
            ("Period:",        $"{report.StartDate:MM/dd/yyyy} — {report.EndDate:MM/dd/yyyy}"),
            ("Total Income:",  $"${report.TotalIncome:F2}"),
            ("Total Expenses:",$"${report.TotalExpenses:F2}"),
            ("Net Savings:",   $"${report.NetSavings:F2}"),
            ("Transactions:",  $"{report.Transactions.Count}"),
        };

        foreach (var (label, value) in summaryLines)
        {
            gfx.DrawString(label, bodyFont, XBrushes.Black,
                Rect(margin, y, 150, 18), XStringFormats.TopLeft);
            gfx.DrawString(value, bodyFont, XBrushes.Black,
                Rect(margin + 160, y, 200, 18), XStringFormats.TopLeft);
            y += 20;
        }

        y += 15;
        gfx.DrawLine(XPens.LightGray, Pt(margin), Pt(y), Pt(page.Width.Point - margin), Pt(y));
        y += 15;

        //  Transaction Table 
        gfx.DrawString("Transactions", headingFont, XBrushes.Black,
            Rect(margin, y, 200, 20), XStringFormats.TopLeft);
        y += 25;

        string[] headers = { "Date", "Description", "Category", "Type", "Amount" };
        double[] colX    = { margin, margin + 80, margin + 280, margin + 380, margin + 450 };

        foreach (var (header, x) in headers.Zip(colX))
        {
            gfx.DrawString(header, headingFont, XBrushes.DarkBlue,
                Rect(x, y, 150, 18), XStringFormats.TopLeft);
        }
        y += 20;

        gfx.DrawLine(XPens.LightGray, Pt(margin), Pt(y), Pt(page.Width.Point - margin), Pt(y));
        y += 8;

        foreach (var t in report.Transactions)
        {
            if (y > page.Height.Point - margin - 30)
            {
                page = pdf.AddPage();
                gfx  = XGraphics.FromPdfPage(page);
                y    = margin;
            }

            string desc = t.Description.Length > 25
                ? t.Description[..25] + "…"
                : t.Description;

            var rowBrush = t.Type == "Income" ? XBrushes.DarkGreen : XBrushes.DarkRed;

            gfx.DrawString(t.Date.ToString("MM/dd/yyyy"), smallFont, XBrushes.Black,
                Rect(colX[0], y, 75,  16), XStringFormats.TopLeft);
            gfx.DrawString(desc, smallFont, XBrushes.Black,
                Rect(colX[1], y, 195, 16), XStringFormats.TopLeft);
            gfx.DrawString(t.Category, smallFont, XBrushes.Black,
                Rect(colX[2], y, 95,  16), XStringFormats.TopLeft);
            gfx.DrawString(t.Type, smallFont, rowBrush,
                Rect(colX[3], y, 65,  16), XStringFormats.TopLeft);
            gfx.DrawString($"${t.Amount:F2}", smallFont, rowBrush,
                Rect(colX[4], y, 80,  16), XStringFormats.TopLeft);

            y += 18;
        }

        pdf.Save(filePath);

        txtStatus.Text = $"PDF saved to Desktop: {Path.GetFileName(filePath)}";
        txtStatus.Foreground = System.Windows.Media.Brushes.Green;
    }
    catch (Exception ex)
    {
        MessageBox.Show($"PDF export failed: {ex.Message}");
    }
}
        

//  PUBLIC REFRESH METHOD 
// Called by MainWindow.RefreshDashboard() so charts updatewhen transactions or budgets change in other views
        public void Refresh()
        {
            LoadPieChart();
            LoadLineChart();
        }
    }
}