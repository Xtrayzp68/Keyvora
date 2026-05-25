namespace Keyvora.Desktop.UI.Views;

using System.Windows;
using System.Windows.Input;

public partial class GridConfigDialog : Window
{
    public int Columns { get; private set; } = 3;
    public int Rows { get; private set; } = 2;

    public GridConfigDialog(int currentColumns, int currentRows)
    {
        InitializeComponent();
        ColumnsTextBox.Text = currentColumns.ToString(System.Globalization.CultureInfo.InvariantCulture);
        RowsTextBox.Text = currentRows.ToString(System.Globalization.CultureInfo.InvariantCulture);
        ColumnsTextBox.Focus();
        ColumnsTextBox.SelectAll();
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ColumnsTextBox.Text, out var cols) || cols < 1 || cols > 8)
        {
            MessageBox.Show(this, "Les colonnes doivent être entre 1 et 8.", "Erreur",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!int.TryParse(RowsTextBox.Text, out var rows) || rows < 1 || rows > 6)
        {
            MessageBox.Show(this, "Les lignes doivent être entre 1 et 6.", "Erreur",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Columns = cols;
        Rows = rows;
        DialogResult = true;
    }

    private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            OnOkClick(sender, e);
    }
}
