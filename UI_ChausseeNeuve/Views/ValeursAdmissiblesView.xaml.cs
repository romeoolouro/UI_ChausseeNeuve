using System.Windows.Controls;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Views
{
    /// <summary>
    /// Vue pour la configuration des valeurs admissibles et paramètres de calcul
    /// </summary>
    public partial class ValeursAdmissiblesView : UserControl
    {
        public ValeursAdmissiblesView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Accès au ViewModel depuis le code-behind si nécessaire
        /// </summary>
        public ValeursAdmissiblesViewModel? ViewModel => DataContext as ValeursAdmissiblesViewModel;
    }
}
