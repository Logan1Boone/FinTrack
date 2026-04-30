using System;
using System.Windows;
using System.Windows.Controls;
using FinTrack.Models;

namespace FinTrack.Views
{
    public partial class BudgetView : UserControl
    {
        public BudgetView()
        {
            InitializeComponent();
        }

        private void SaveBudget_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string category = txtBudgetCategory.Text.Trim();
                decimal limit = Convert.ToDecimal(txtBudgetAmount.Text);

                if (string.IsNullOrEmpty(category))
                {
                    MessageBox.Show("Please enter a category name.");
                    return;
                }

                // Save to the shared DataManager instead of a local variable
                var budget = new Budget
                {
                    Category = category,
                    Limit = limit,
                    Spent = 0m
                };

                MainWindow.DataManager.AddBudget(budget);
                MainWindow.DataManager.SaveAll();

                txtBudgetStatus.Text = $"✔ Budget for {category} saved: ${limit:F2}";
                txtBudgetStatus.Foreground = System.Windows.Media.Brushes.Green;

                // Tell the dashboard to refresh so it shows the new budget
                MainWindow.RefreshDashboard();

                txtBudgetCategory.Clear();
                txtBudgetAmount.Clear();
            }
            catch
            {
                MessageBox.Show("Please enter a valid budget amount.");
            }
        }
    }
}