using System.Windows;
using System.Windows.Controls;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Views
{
    /// <summary>
    /// Logique d'interaction pour BibliothequeView.xaml
    /// </summary>
    public partial class BibliothequeView : UserControl
    {
        // DP pour masquer/afficher la barre d'actions en bas
        public static readonly DependencyProperty ShowActionBarProperty = DependencyProperty.Register(
            nameof(ShowActionBar), typeof(bool), typeof(BibliothequeView), new PropertyMetadata(true));

        public bool ShowActionBar
        {
            get => (bool)GetValue(ShowActionBarProperty);
            set => SetValue(ShowActionBarProperty, value);
        }

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
