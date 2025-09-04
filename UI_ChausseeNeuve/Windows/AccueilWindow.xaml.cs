using System.Windows;
using System.Windows.Controls;
using UI_ChausseeNeuve.Views;

namespace UI_ChausseeNeuve.Windows
{
    public partial class AccueilWindow : Window
    {
        public string HeaderTitle { get; set; } = "Projet";

        public AccueilWindow()
        {
            InitializeComponent();
            HeaderTitle = $"Projet - {AppState.CurrentProject.Name}";
            DataContext = this;

            if (NavBar != null)
            {
                NavBar.SectionSelected += OnSectionSelected;
            }

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
                    MainContent.Content = new ValeursAdmissiblesView();
                    CenterLogo.Visibility = Visibility.Collapsed;
                    break;
                case "resultats":
                    MainContent.Content = new ResultatView();
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
