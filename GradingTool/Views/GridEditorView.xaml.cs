using System.Windows;
using System.Windows.Controls;
using GradingTool.Models;
using GradingTool.ViewModels;

namespace GradingTool.Views;

public partial class GridEditorView : UserControl
{
    public GridEditorView()
    {
        InitializeComponent();
    }

    private void ComboBox_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        e.Handled = true;
    }

    private void SuggestedCommentsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0)
        {
            var comboBox = (ComboBox)sender;
            var criterion = (CriterionModel)comboBox.DataContext;
            var selectedComment = e.AddedItems[0] as string;
            
            // Ignorer la sélection du placeholder
            if (selectedComment != null && !selectedComment.StartsWith("-- "))
            {
                var viewModel = (GridEditorViewModel)this.DataContext;
                viewModel.SelectSuggestedComment(selectedComment, criterion);
            }
            
            // Revenir au placeholder (première item)
            comboBox.SelectedIndex = 0;
        }
    }
}



