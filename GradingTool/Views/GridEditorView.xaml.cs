using System.Windows;
using System.Windows.Controls;

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
}
