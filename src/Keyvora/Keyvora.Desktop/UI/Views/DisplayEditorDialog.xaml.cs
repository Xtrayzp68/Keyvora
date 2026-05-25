namespace Keyvora.Desktop.UI.Views;

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Keyvora.Desktop.UI.ViewModels;

public partial class DisplayEditorDialog : Window
{
    private readonly DisplayEditorViewModel _vm;

    public DisplayEditorDialog(DisplayEditorViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;
        LoadPresetColors();
    }

    private void LoadPresetColors()
    {
        var colors = new[]
        {
            "#2D2D2D", "#1E1E1E", "#0078D4", "#E81123", "#107C10",
            "#FF8C00", "#9B59B6", "#F1C40F", "#1ABC9C", "#E67E22",
            "#34495E", "#ECF0F1", "#3498DB", "#E74C3C", "#2ECC71"
        };
        PresetColorsControl.ItemsSource = colors;
    }

    private void OnPresetColorClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string colorHex)
        {
            _vm.BackgroundColor = colorHex;
        }
    }

    private void OnBuiltInIconClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string resourceKey)
        {
            _vm.SelectBuiltIn(resourceKey);
        }
    }

    private void OnBrowseImageClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select an image",
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.ico)|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.ico|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            _vm.SelectCustomFile(dialog.FileName);
        }
    }

    private void OnClearIconClick(object sender, RoutedEventArgs e)
    {
        _vm.ClearIcon();
    }

    private void OnCropClick(object sender, RoutedEventArgs e)
    {
        var dialog = new CropDialog(_vm);
        dialog.Owner = this;
        dialog.ShowDialog();
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
