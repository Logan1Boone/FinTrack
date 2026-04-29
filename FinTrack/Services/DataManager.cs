using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Serialization;
using FinTrack.Models;

namespace FinTrack.Services
{
    public class DataManager
    {
        // File paths where data is saved
        private readonly string _jsonFilePath = "transactions.json";
        private readonly string _xmlFilePath = "budgets.xml";

        // In-memory lists that hold the app's data while it's running
        public List<Transaction> Transactions { get; private set; } = new();
        public List<Budget> Budgets { get; private set; } = new();
        private int _nextTransactionId = 1;
        private int _nextBudgetId = 1;

        // ── CRUD: Transactions ─────────────────────────────────────────

        public void AddTransaction(Transaction t)
        {
            t.Id = _nextTransactionId++;
            Transactions.Add(t);
            UpdateBudgetSpending(t);
        }

        public void RemoveTransaction(int id)
        {
            var t = Transactions.FirstOrDefault(x => x.Id == id);
            if (t != null) Transactions.Remove(t);
        }

        // ── CRUD: Budgets ──────────────────────────────────────────────

        public void AddBudget(Budget b)
        {
            b.Id = _nextBudgetId++;
            Budgets.Add(b);
        }

        public void RemoveBudget(int id)
        {
            var b = Budgets.FirstOrDefault(x => x.Id == id);
            if (b != null) Budgets.Remove(b);
        }

        // Updates the "Spent" amount on a budget when a new expense is added
        private void UpdateBudgetSpending(Transaction t)
        {
            if (t.Type != "Expense") return;
            var budget = Budgets.FirstOrDefault(b => b.Category == t.Category);
            if (budget != null)
                budget.Spent += t.Amount;
        }

        // ── LINQ Queries ───────────────────────────────────────────────

        // Get all transactions in a date range
        public List<Transaction> GetTransactionsByDateRange(DateTime start, DateTime end)
        {
            return Transactions
                .Where(t => t.Date >= start && t.Date <= end)
                .OrderBy(t => t.Date)
                .ToList();
        }

        // Get total spending grouped by category (used for pie chart)
        public Dictionary<string, decimal> GetSpendingByCategory()
        {
            return Transactions
                .Where(t => t.Type == "Expense")
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
        }

        // Get monthly expense totals (used for line chart)
        public Dictionary<string, decimal> GetMonthlyExpenses()
        {
            return Transactions
                .Where(t => t.Type == "Expense")
                .GroupBy(t => t.Date.ToString("yyyy-MM"))
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
        }

        // Build a financial summary report for a date range
        public FinancialReport GenerateReport(DateTime start, DateTime end)
        {
            var filtered = GetTransactionsByDateRange(start, end);
            return new FinancialReport
            {
                StartDate = start,
                EndDate = end,
                TotalIncome = filtered.Where(t => t.Type == "Income").Sum(t => t.Amount),
                TotalExpenses = filtered.Where(t => t.Type == "Expense").Sum(t => t.Amount),
                Transactions = filtered
            };
        }

        // Return any budgets where spending has exceeded the limit
        public List<Budget> GetOverBudgetAlerts()
        {
            return Budgets.Where(b => b.IsOverBudget).ToList();
        }

        // ── Serialization: JSON ────────────────────────────────────────

        public void SaveTransactionsToJson()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Transactions, options);
            File.WriteAllText(_jsonFilePath, json);
        }

        public void LoadTransactionsFromJson()
        {
            if (!File.Exists(_jsonFilePath)) return;
            string json = File.ReadAllText(_jsonFilePath);
            Transactions = JsonSerializer.Deserialize<List<Transaction>>(json) ?? new();
            _nextTransactionId = Transactions.Any() ? Transactions.Max(t => t.Id) + 1 : 1;
        }

        // ── Serialization: XML ─────────────────────────────────────────

        public void SaveBudgetsToXml()
        {
            var serializer = new XmlSerializer(typeof(List<Budget>));
            using var writer = new StreamWriter(_xmlFilePath);
            serializer.Serialize(writer, Budgets);
        }

        public void LoadBudgetsFromXml()
        {
            if (!File.Exists(_xmlFilePath)) return;
            var serializer = new XmlSerializer(typeof(List<Budget>));
            using var reader = new StreamReader(_xmlFilePath);
            Budgets = (List<Budget>?)serializer.Deserialize(reader) ?? new();
            _nextBudgetId = Budgets.Any() ? Budgets.Max(b => b.Id) + 1 : 1;
        }

        // Call at app startup to load saved data
        public void LoadAll()
        {
            LoadTransactionsFromJson();
            LoadBudgetsFromXml();
        }

        // Call before app closes to save everything
        public void SaveAll()
        {
            SaveTransactionsToJson();
            SaveBudgetsToXml();
        }
    }
}