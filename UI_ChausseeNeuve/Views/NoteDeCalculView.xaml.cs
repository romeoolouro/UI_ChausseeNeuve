using System.Windows.Controls;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Views
{
    public partial class NoteDeCalculView : UserControl
    {
        public NoteDeCalculView()
        {
            InitializeComponent();
            // Cr�er et assigner le ViewModel directement
            DataContext = new NoteDeCalculViewModel();
        }
    }
}
