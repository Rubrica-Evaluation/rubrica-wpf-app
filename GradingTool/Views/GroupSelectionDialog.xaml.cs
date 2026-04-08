using GradingTool.Models;
using System.Windows;
using System.Windows.Controls;

namespace GradingTool.Views;

public partial class GroupSelectionDialog : Window
{
    private const string NewGroupKey = "__new__";

    /// <summary>
    /// null = annulé, "" = nouveau groupe générique, groupCode = groupe existant ou groupe à créer.
    /// </summary>
    public string? SelectedGroupCode { get; private set; }

    public GroupSelectionDialog(string fileName, List<GroupModel> existingGroups, string? suggestedGroupCode = null, string? suggestedDisplayName = null)
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;

        MessageText.Text = $"Dans quel groupe importer les étudiants de « {fileName} » ?";

        GroupComboBox.Items.Add(new ComboBoxItem { Content = "— Nouveau groupe —", Tag = NewGroupKey });

        bool hasDetectedNew = !string.IsNullOrEmpty(suggestedGroupCode)
            && existingGroups.All(g => g.GroupCode != suggestedGroupCode);

        int preselectedIndex = 0;

        if (hasDetectedNew)
        {
            var label = string.IsNullOrEmpty(suggestedDisplayName) ? suggestedGroupCode : suggestedDisplayName;
            GroupComboBox.Items.Add(new ComboBoxItem { Content = $"Créer « {label} »", Tag = suggestedGroupCode });
            preselectedIndex = 1;
        }

        foreach (var group in existingGroups)
            GroupComboBox.Items.Add(new ComboBoxItem { Content = group.DisplayName, Tag = group.GroupCode });

        if (!hasDetectedNew && !string.IsNullOrEmpty(suggestedGroupCode))
        {
            var existingIndex = existingGroups.FindIndex(g => g.GroupCode == suggestedGroupCode);
            // +1 pour "Nouveau groupe", pas de "Créer" donc décalage de 1
            if (existingIndex >= 0)
                preselectedIndex = existingIndex + 1;
        }

        GroupComboBox.SelectedIndex = preselectedIndex;
    }

    private void GroupComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Rien à faire — on lit la sélection à la confirmation
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (GroupComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
        {
            var tag = item.Tag as string;
            SelectedGroupCode = tag == NewGroupKey ? string.Empty : tag;
        }
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedGroupCode = null;
        DialogResult = false;
    }
}
