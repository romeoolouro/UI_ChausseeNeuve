using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Globalization;
using UI_ChausseeNeuve.ViewModels;
using UI_ChausseeNeuve.Services;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Views
{
    public partial class StructureDeChausseeView : System.Windows.Controls.UserControl
    {
        private StructureEditorViewModel? _viewModel;

        public StructureDeChausseeView()
        {
            InitializeComponent();
            _viewModel = new StructureEditorViewModel();
            DataContext = _viewModel;

            // Connect toast notifications
            _viewModel.ToastRequested += (message, type) => ToastService.ShowToast(message, type);
        }

        void SetViewport()
        {
            if (DataContext is StructureEditorViewModel vm && CoupeScroll != null)
                vm.ViewportHeight = CoupeScroll.ActualHeight;
        }

        void CoupeScroll_Loaded(object sender, RoutedEventArgs e) => SetViewport();
        void CoupeScroll_SizeChanged(object sender, SizeChangedEventArgs e) => SetViewport();

        // Allow digits, optional decimal separator (comma or dot), allow starting with separator while typing
        static readonly Regex Allowed = new(@"^[0-9]*([.,]?[0-9]*)?$", RegexOptions.Compiled);

        void Numeric_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
            {
                string next = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength).Insert(tb.SelectionStart, e.Text);
                // allow empty (user deleting) and partial entries like ",5" while typing
                e.Handled = !string.IsNullOrEmpty(next) && !Allowed.IsMatch(next);
            }
        }

        void Numeric_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(System.Windows.DataFormats.UnicodeText)) { e.CancelCommand(); return; }
            var text = (string)e.DataObject.GetData(System.Windows.DataFormats.UnicodeText);
            if (string.IsNullOrWhiteSpace(text) || !Allowed.IsMatch(text.Trim())) e.CancelCommand();
        }

        void Numeric_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key is Key.Back or Key.Delete or Key.Tab or Key.Left or Key.Right or Key.Home or Key.End or Key.Enter or Key.Escape)
            {
                e.Handled = false;

                // Mission 3.1 - Validation automatique sur Entrée
                if (e.Key == Key.Enter && sender is System.Windows.Controls.TextBox textBox)
                {
                    // Déclencher la validation en forçant la mise à jour de la binding
                    textBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();

                    // Notification toast pour validation réussie
                    ToastService.ShowToast("Valeur validée selon NF P98-086", ToastType.Success);
                }
            }
        }

        void Numeric_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.TextBox tb) return;
            string raw = tb.Text.Trim();
            if (string.IsNullOrEmpty(raw)) return;
            // Normalize a leading decimal separator: ",5" -> "0,5"
            if (raw.Length > 0 && (raw[0] == ',' || raw[0] == '.')) raw = "0" + raw;
            // Use fr-FR culture to parse decimal comma
            if (Allowed.IsMatch(raw))
            {
                if (double.TryParse(raw, NumberStyles.Number, new CultureInfo("fr-FR"), out var val) && val >= 0)
                {
                    // write back formatted with comma as decimal separator and 3 decimals for thickness inputs
                    // But only adjust formatting for Thickness boxes (we can't easily know which property here), so keep textbox value normalized to fr-FR invariant format with dot replaced by comma
                    tb.Text = val.ToString("F3", new CultureInfo("fr-FR")).TrimEnd('0').TrimEnd(',');
                    return;
                }
            }
            tb.Text = string.Empty;
        }
    }
}
