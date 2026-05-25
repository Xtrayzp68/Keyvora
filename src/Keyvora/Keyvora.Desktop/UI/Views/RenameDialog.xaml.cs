namespace Keyvora.Desktop.UI.Views;

using System.Windows;
using System.Windows.Input;

public partial class RenameDialog : Window
{
    public string? Result { get; private set; }

    public RenameDialog(string currentName)
    {
        InitializeComponent();
        NameTextBox.Text = currentName;
        NameTextBox.SelectAll();
        NameTextBox.Focus();
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "Le nom ne peut pas être vide.", "Erreur",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        Result = name;
        DialogResult = true;
    }

    private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            OnOkClick(sender, e);
    }
}
