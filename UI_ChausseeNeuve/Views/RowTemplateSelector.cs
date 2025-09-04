using System.Windows;
using System.Windows.Controls;
using UI_ChausseeNeuve.ViewModels;

namespace UI_ChausseeNeuve.Views
{
    public class RowTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? LayerTemplate { get; set; }
        public DataTemplate? InterfaceTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is LayerRowVM) return LayerTemplate;
            if (item is InterfaceRowVM) return InterfaceTemplate;
            return base.SelectTemplate(item, container);
        }
    }
}
