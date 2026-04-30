using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using FinTrack.Models;

namespace FinTrack.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        // Call this any time data changes to refresh the whole dashboard
        public void Refresh(List<Transaction> transactions, List<Budget> budgets)
        {
            UpdateSummaryCards(transactions);
            UpdateBudgetAlerts(budgets);
            UpdateBudgetOverview(budgets);
            UpdateRecentTransactions(transactions);
        }

        private void UpdateSummaryCards(List<Transaction> transactions)
        {
            decimal income = transactions
                .Where(t => t.Type == "Income")
                .Sum(t => t.Amount);

            decimal expenses = transactions
                .Where(t => t.Type == "Expense")
                .Sum(t => t.Amount);

            decimal balance = income - expenses;

            txtIncome.Text = $"${income:F2}";
            txtExpenses.Text = $"${expenses:F2}";
            txtBalance.Text = $"${balance:F2}";

            // Turn balance card red if negative
            balanceCard.Background = balance >= 0
                ? new SolidColorBrush(Color.FromRgb(52, 152, 219))   // blue
                : new SolidColorBrush(Color.FromRgb(231, 76, 60));    // red
        }

        private void UpdateBudgetAlerts(List<Budget> budgets)
        {
            var overBudget = budgets.Where(b => b.IsOverBudget).ToList();

            if (overBudget.Any())
            {
                // Show warning, hide green banner
                alertBorder.Visibility = System.Windows.Visibility.Visible;
                noAlertBorder.Visibility = System.Windows.Visibility.Collapsed;

                var messages = overBudget.Select(b =>
                    $"⚠ {b.Category}: over by ${b.Spent - b.Limit:F2}");
                txtAlerts.Text = string.Join("\n", messages);
            }
            else
            {
                // Show green banner, hide warning
                alertBorder.Visibility = System.Windows.Visibility.Collapsed;
                noAlertBorder.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void UpdateBudgetOverview(List<Budget> budgets)
        {
            lstBudgets.ItemsSource = null;
            lstBudgets.ItemsSource = budgets;
        }

        private void UpdateRecentTransactions(List<Transaction> transactions)
        {
            // Show the 5 most recent transactions
            var recent = transactions
                .OrderByDescending(t => t.Date)
                .Take(5)
                .ToList();

            lstRecentTransactions.ItemsSource = null;
            lstRecentTransactions.ItemsSource = recent;
        }
    }
}