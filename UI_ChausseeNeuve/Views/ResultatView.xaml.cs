using System.Windows.Controls;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Views
{
    /// <summary>
    /// Vue pour l'affichage des résultats de calcul de la chaussée
    /// </summary>
    public partial class ResultatView : UserControl
    {
        public ResultatView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Accès au ViewModel depuis le code-behind si nécessaire
        /// </summary>
        public ResultatViewModel? ViewModel => DataContext as ResultatViewModel;
    }
}
