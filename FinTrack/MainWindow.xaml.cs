using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FinTrack.Models;
using FinTrack.Services;

namespace FinTrack;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var dm = new DataManager();

        // Add a test transaction
        dm.AddTransaction(new Transaction
        {
            Date = DateTime.Now,
            Description = "Grocery run",
            Amount = 85.50m,
            Category = "Food",
            Type = "Expense"
        });

        // Add a test budget
        dm.AddBudget(new Budget
        {
            Category = "Food",
            Limit = 300m,
            Spent = 0m
        });

        // Save to files
        dm.SaveAll();

        // Reload from files and print results
        var dm2 = new DataManager();
        dm2.LoadAll();
        Console.WriteLine($"Loaded {dm2.Transactions.Count} transactions.");
        Console.WriteLine($"Loaded {dm2.Budgets.Count} budgets.");

        // Test a LINQ query
        var byCategory = dm2.GetSpendingByCategory();
        foreach (var kvp in byCategory)
            Console.WriteLine($"{kvp.Key}: ${kvp.Value}"); 
    }
}