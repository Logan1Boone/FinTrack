using System;
using System.Collections.Generic;

namespace FinTrack.Models
{
    // Represents a single money transaction (income or expense)
    public class Transaction
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public string Category { get; set; } = "";  // e.g. "Food", "Rent"
        public string Type { get; set; } = "";       // "Income" or "Expense"
    }

    // Represents a spending budget for a category
    public class Budget
    {
        public int Id { get; set; }
        public string Category { get; set; } = "";
        public decimal Limit { get; set; }           // Max allowed spending
        public decimal Spent { get; set; }           // How much has been spent
        public bool IsOverBudget => Spent > Limit;   // Auto-calculated alert
    }

    // Represents a summary report for a time period
    public class FinancialReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetSavings => TotalIncome - TotalExpenses;
        public List<Transaction> Transactions { get; set; } = new();
    }
}