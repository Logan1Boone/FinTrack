using System;
using System.Collections.Generic;
using System.Windows;
using FinTrack.Services;
using System.Windows.Controls;


namespace FinTrack
{
    public partial class MainWindow : Window
    {
        public static DataManager DataManager { get; } = new DataManager();

        public MainWindow()
        {
            InitializeComponent();
            DataManager.LoadAll();
            RefreshDashboard();
        }

        public static void RefreshDashboard()
        {
            var window = (MainWindow)Application.Current.MainWindow;
            window.Dashboard.Refresh(DataManager.Transactions, DataManager.Budgets);
        }
    }
}