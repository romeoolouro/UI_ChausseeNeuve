using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Windows
{
    public partial class NFP98086Window : Window
    {
        private readonly string _mode;
        public NFP98086ViewModel? ViewModel => DataContext as NFP98086ViewModel;

        public NFP98086Window() : this("CAM") { }

        public NFP98086Window(string mode)
        {
            InitializeComponent();
            _mode = string.IsNullOrWhiteSpace(mode) ? "CAM" : mode.ToUpperInvariant();
            DataContext = new NFP98086ViewModel();
            if (ViewModel != null)
            {
                ViewModel.OnCamValueSelected += (_, materialType) => { /* handled in ApplyCamFromValue via command */ };
                ViewModel.OnCamValueSelected += OnCamValueSelected; // real handler
                ViewModel.OnRiskValueSelected += OnRiskValueSelected;
            }

            AttachCellClick(GridPeriNonAuto, OnGridCellClick_PeriNonAuto);
            AttachCellClick(GridPeriAuto, OnGridCellClick_PeriAuto);
            AttachCellClick(GridUrbain, OnGridCellClick_Urbain);

            if (_mode == "RISQUE")
            {
                this.Title = "Norme NF P98-086 - R";
                if (CamPeriLeft != null) CamPeriLeft.Visibility = Visibility.Collapsed;
                if (CamPeriRight != null) CamPeriRight.Visibility = Visibility.Collapsed;
                if (CamUrbain != null) CamUrbain.Visibility = Visibility.Collapsed;
                if (RiskPeriBlock != null) RiskPeriBlock.Visibility = Visibility.Visible;
                if (RiskUrbainBlock != null) RiskUrbainBlock.Visibility = Visibility.Visible;
            }
            else
            {
                this.Title = "Norme NF P98-086 - CAM";
                if (CamPeriLeft != null) CamPeriLeft.Visibility = Visibility.Visible;
                if (CamPeriRight != null) CamPeriRight.Visibility = Visibility.Visible;
                if (CamUrbain != null) CamUrbain.Visibility = Visibility.Visible;
                if (RiskPeriBlock != null) RiskPeriBlock.Visibility = Visibility.Collapsed;
                if (RiskUrbainBlock != null) RiskUrbainBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void OnCamValueSelected(double value, string materialType)
        {
            ApplyCamFromValue(value, materialType);
        }

        private void OnRiskValueSelected(double value, string label)
        {
            ApplyRiskFromValue(value, label);
        }

        private void AttachCellClick(Grid grid, MouseButtonEventHandler handler)
        {
            if (grid != null)
            {
                grid.MouseLeftButtonUp += handler;
                grid.Cursor = Cursors.Hand;
            }
        }

        private void OnGridCellClick_PeriNonAuto(object sender, MouseButtonEventArgs e) => DispatchGridClick("PNON", sender, e, maxCols:4);
        private void OnGridCellClick_PeriAuto(object sender, MouseButtonEventArgs e) => DispatchGridClick("PAUTO", sender, e, maxCols:1);
        private void OnGridCellClick_Urbain(object sender, MouseButtonEventArgs e) => DispatchGridClick("URB", sender, e, maxCols:3);

        private void DispatchGridClick(string areaKey, object sender, MouseButtonEventArgs e, int maxCols)
        {
            if (ViewModel == null) return;
            if (sender is not Grid grid) return;
            var fe = e.OriginalSource as FrameworkElement;
            if (fe == null) return;
            var border = FindParent<Border>(fe);
            if (border == null) return;
            int row = Grid.GetRow(border);   // includes header row 0
            int col = Grid.GetColumn(border); // includes header col 0

            // Normalize row/col for data zones
            if (areaKey == "PNON")
            {
                // Data rows now 1..4 (MB, MTLH, Sols traites, Plate-forme GNT) and columns 1..5 (T5,T4,T3-,T3+,T2T1T0)
                if (row < 1 || row > 4 || col < 1 || col > 5) return;
                ViewModel.SelectCamCommand.Execute($"{areaKey}|{row-1}|{col-1}");
            }
            else if (areaKey == "PAUTO")
            {
                if (row < 1 || row > 3) return;
                // single value row wise
                ViewModel.SelectCamCommand.Execute($"{areaKey}|{row-1}|0");
            }
            else if (areaKey == "URB")
            {
                if (row < 1 || row > 3 || col < 1 || col > 3) return;
                ViewModel.SelectCamCommand.Execute($"{areaKey}|{row-1}|{col-1}");
            }
        }

        private void ApplyCamFromValue(double camValue, string materialType)
        {
            var selectWin = new MaterialSelectionWindow(camValue, materialType, valueKind: "CAM");
            selectWin.Owner = this;
            selectWin.ShowDialog();
        }

        private void ApplyRiskFromValue(double rValue, string label)
        {
            var selectWin = new MaterialSelectionWindow(rValue, label, valueKind: "RISQUE");
            selectWin.Owner = this;
            selectWin.ShowDialog();
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null && parent is not T)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        private void Risk_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                ViewModel?.SelectRiskCommand.Execute(tag);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
        private void Ok_Click(object sender, RoutedEventArgs e) { DialogResult = true; Close(); }

        // Classe interne conservée pour compatibilité Hot Reload (ancien stockage des tables CAM)
        internal static class NfpCamData { }
    }
}