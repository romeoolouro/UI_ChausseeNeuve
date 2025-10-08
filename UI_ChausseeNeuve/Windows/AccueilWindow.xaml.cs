using System.Windows;
using System.Windows.Controls;
using UI_ChausseeNeuve.Views;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Windows
{
    public partial class AccueilWindow : Window
    {
        public string HeaderTitle { get; set; } = "Projet";

        // Instance persistante pour la navigation
        private ValeursAdmissiblesViewModel _valeursAdmissiblesViewModel = new ValeursAdmissiblesViewModel();
        private ValeursAdmissiblesView _valeursAdmissiblesView;
        private NoteDeCalculView? _noteDeCalculView; // ajout

        public AccueilWindow()
        {
            InitializeComponent();
            HeaderTitle = $"Projet - {AppState.CurrentProject.Name}";
            DataContext = this;

            if (NavBar != null)
            {
                NavBar.SectionSelected += OnSectionSelected;
            }

            // Crée la vue et assigne le ViewModel unique
            _valeursAdmissiblesView = new ValeursAdmissiblesView();
            _valeursAdmissiblesView.DataContext = _valeursAdmissiblesViewModel;

            ShowHome();
        }

        private void OnSectionSelected(string key)
        {
            switch (key)
            {
                case "fichier":
                    MainContent.Content = new FileMenuView();
                    CenterLogo.Visibility = Visibility.Collapsed;
                    break;
                case "structure":
                    MainContent.Content = new StructureDeChausseeView();
                    CenterLogo.Visibility = Visibility.Collapsed;
                    break;
                case "charge":
                    MainContent.Content = new ChargesView();
                    CenterLogo.Visibility = Visibility.Collapsed;
                    break;
                case "biblio":
                    MainContent.Content = new BibliothequeView();
                    CenterLogo.Visibility = Visibility.Collapsed;
                    break;
                case "valeurs":
                    _valeursAdmissiblesViewModel.EnsureSyncedWithStructure();
                    MainContent.Content = _valeursAdmissiblesView;
                    CenterLogo.Visibility = Visibility.Collapsed;
                    break;
                case "resultats":
                    MainContent.Content = new ResultatView();
                    CenterLogo.Visibility = Visibility.Collapsed;
                    break;
                case "note": // nouvelle section
                    _noteDeCalculView ??= new NoteDeCalculView();
                    MainContent.Content = _noteDeCalculView;
                    CenterLogo.Visibility = Visibility.Collapsed;
                    break;
                default:
                    ShowHome();
                    break;
            }
        }

        private void ShowHome()
        {
            MainContent.Content = null;
            CenterLogo.Visibility = Visibility.Visible;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var w = new ModeSelectionWindow();
            System.Windows.Application.Current.MainWindow = w;
            w.Show();
            this.Hide();
        }
    }
}
