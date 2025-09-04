using System.Windows.Controls;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Views
{
    /// <summary>
    /// Logique d'interaction pour ChargesView.xaml
    /// </summary>
    public partial class ChargesView : System.Windows.Controls.UserControl
    {
        public ChargesView()
        {
            InitializeComponent();

            // Créer le ViewModel et l'assigner comme DataContext
            var viewModel = new ChargesViewModel();

            // Connecter les notifications toast
            viewModel.ToastRequested += (message, type) =>
            {
                Services.ToastService.ShowToast(message, type);
            };

            DataContext = viewModel;
        }
    }
}
