using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BossArenaRandomizer.Core;

namespace BossArenaRandomizer.Views.Pages
{
    public partial class BossView : UserControl
    {
        public BossView()
        {
            InitializeComponent();
        }

        private void SelectionGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row?.Item is BossSelection item)
                item.IsSelected = !item.IsSelected;
        }

        private void SelectionGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Space || sender is not DataGrid grid || grid.SelectedItem is not BossSelection item)
                return;

            item.IsSelected = !item.IsSelected;
            e.Handled = true;
        }

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                    return match;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}