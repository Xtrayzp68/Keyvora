namespace Keyvora.Desktop.UI.Views;

using System;
using System.Windows;
using Keyvora.Desktop.UI.ViewModels;

public partial class CropDialog : Window
{
    private readonly DisplayEditorViewModel _vm;

    public CropDialog(DisplayEditorViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;
    }

    private void OnZoomInClick(object sender, RoutedEventArgs e)
    {
        _vm.ImageScale = Math.Min(5.0, _vm.ImageScale + 0.25);
    }

    private void OnZoomOutClick(object sender, RoutedEventArgs e)
    {
        _vm.ImageScale = Math.Max(0.5, _vm.ImageScale - 0.25);
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        _vm.ImageScale = 1.0;
        _vm.ImageOffsetX = 0;
        _vm.ImageOffsetY = 0;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
