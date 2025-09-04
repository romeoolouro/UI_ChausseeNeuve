using System.Windows.Controls;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Views
{
    /// <summary>
    /// Logique d'interaction pour BibliothequeView.xaml
    /// </summary>
    public partial class BibliothequeView : UserControl
    {
        public BibliothequeView()
        {
            InitializeComponent();

            // Créer le ViewModel et l'assigner comme DataContext
            var viewModel = new BibliothequeViewModel();

            // Connecter les notifications toast
            viewModel.ToastRequested += (message, type) =>
            {
                Services.ToastService.ShowToast(message, type);
            };

            DataContext = viewModel;
        }
    }
}
