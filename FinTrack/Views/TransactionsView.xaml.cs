using System;
using System.Windows;
using System.Windows.Controls;
using FinTrack.Models;

namespace FinTrack.Views
{
    public partial class TransactionsView : UserControl
    {
        public TransactionsView()
        {
            InitializeComponent();
        }

        private void AddTransaction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string description = txtDescription.Text.Trim();
                decimal amount = Convert.ToDecimal(txtAmount.Text);
                string category = ((ComboBoxItem)cmbCategory.SelectedItem)?.Content?.ToString() ?? "";
                string type = ((ComboBoxItem)cmbType.SelectedItem)?.Content?.ToString() ?? "";

                // Save to the shared DataManager
                var transaction = new Transaction
                {
                    Date = DateTime.Now,
                    Description = description,
                    Amount = amount,
                    Category = category,
                    Type = type
                };

                MainWindow.DataManager.AddTransaction(transaction);
                MainWindow.DataManager.SaveAll();

                // Refresh the display list
                lstTransactions.Items.Add(
                    $"{DateTime.Now.ToShortDateString()} | {description} | {category} | {type} | ${amount:F2}");

                // Tell the dashboard to refresh
                MainWindow.RefreshDashboard();

                txtDescription.Clear();
                txtAmount.Clear();
            }
            catch
            {
                MessageBox.Show("Please enter valid transaction data.");
            }
        }

        private void DeleteTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (lstTransactions.SelectedItem != null)
            {
                lstTransactions.Items.Remove(lstTransactions.SelectedItem);
                MainWindow.RefreshDashboard();
            }
        }
    }
}