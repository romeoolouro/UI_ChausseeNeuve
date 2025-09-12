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

        public NFP98086Window() : this("CAM") { }

        public NFP98086Window(string mode)
        {
            InitializeComponent();
            _mode = string.IsNullOrWhiteSpace(mode) ? "CAM" : mode.ToUpperInvariant();

            AttachCellClick(GridPeriNonAuto, OnPeriNonAutoCellClick);
            AttachCellClick(GridPeriAuto, OnPeriAutoCellClick);
            AttachCellClick(GridUrbain, OnUrbainCellClick);

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

        private void AttachCellClick(Grid grid, MouseButtonEventHandler handler)
        {
            if (grid != null)
            {
                grid.MouseLeftButtonUp += handler;
                grid.Cursor = Cursors.Hand;
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

        private void OnPeriNonAutoCellClick(object sender, MouseButtonEventArgs e)
        {
            if (TryGetCellValue(sender as Grid, e, out double value, out string materialType))
            {
                ApplyCamFromValue(value, materialType);
            }
        }

        private void OnPeriAutoCellClick(object sender, MouseButtonEventArgs e)
        {
            if (TryGetCellValue(sender as Grid, e, out double value, out string materialType))
            {
                ApplyCamFromValue(value, materialType);
            }
        }

        private void OnUrbainCellClick(object sender, MouseButtonEventArgs e)
        {
            if (TryGetCellValue(sender as Grid, e, out double value, out string materialType))
            {
                ApplyCamFromValue(value, materialType);
            }
        }

        private bool TryGetCellValue(Grid grid, MouseButtonEventArgs e, out double cam, out string materialType)
        {
            cam = 0;
            materialType = "";
            if (grid == null) return false;

            var element = e.OriginalSource as FrameworkElement;
            if (element == null) return false;

            var border = FindParent<Border>(element);
            if (border == null) return false;

            if (border.Child is TextBlock tb)
            {
                var text = tb.Text?.Trim();
                if (double.TryParse(text?.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
                {
                    cam = v;

                    int row = Grid.GetRow(border);
                    if (grid == GridPeriNonAuto || grid == GridPeriAuto)
                    {
                        materialType = row switch
                        {
                            1 => "bitumineux",
                            2 => "traites_hydrauliques",
                            3 => "granulaires",
                            _ => "bitumineux"
                        };
                    }
                    else if (grid == GridUrbain)
                    {
                        materialType = row switch
                        {
                            1 => "bitumineux",
                            2 => "traites_hydrauliques",
                            3 => "giratoire",
                            _ => "bitumineux"
                        };
                    }

                    return true;
                }
            }
            return false;
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
                var parts = tag.Split('|');
                if (parts.Length >= 2 && double.TryParse(parts[0].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var r))
                {
                    string label = parts.Length > 1 ? parts[1] : "R NFP";
                    ApplyRiskFromValue(r, label);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
        private void Ok_Click(object sender, RoutedEventArgs e) { DialogResult = true; Close(); }
    }
}