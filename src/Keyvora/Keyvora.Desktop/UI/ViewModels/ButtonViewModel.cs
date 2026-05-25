namespace Keyvora.Desktop.UI.ViewModels;

using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Keyvora.Desktop.Actions;
using Keyvora.Desktop.Profiles;

public sealed partial class ButtonViewModel : ObservableObject
{
    private readonly int _index;
    private readonly ActionRegistry _actionRegistry;

    public int Index => _index;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCustomIcon))]
    private string _icon = string.Empty;

    [ObservableProperty]
    private string _backgroundColor = "#2D2D2D";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage))]
    [NotifyPropertyChangedFor(nameof(HasCustomIcon))]
    [NotifyPropertyChangedFor(nameof(IconSource))]
    private string _iconType = "None";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage))]
    [NotifyPropertyChangedFor(nameof(HasCustomIcon))]
    [NotifyPropertyChangedFor(nameof(IconSource))]
    private string _iconPath = string.Empty;

    public bool HasCustomIcon =>
        IconType == "Text" && !string.IsNullOrEmpty(Icon) ||
        IconType == "BuiltIn" ||
        IconType == "CustomFile";

    public bool HasImage => IconType == "BuiltIn" || IconType == "CustomFile";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageTransform))]
    private double _imageScale = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageTransform))]
    private double _imageOffsetX;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageTransform))]
    private double _imageOffsetY;

    public Transform ImageTransform
    {
        get
        {
            var group = new TransformGroup();
            group.Children.Add(new ScaleTransform(ImageScale, ImageScale));
            group.Children.Add(new TranslateTransform(ImageOffsetX, ImageOffsetY));
            return group;
        }
    }

    public ImageSource? IconSource
    {
        get
        {
            if (IconType == "BuiltIn" && !string.IsNullOrEmpty(IconPath))
            {
                return Application.Current.TryFindResource(IconPath) as ImageSource;
            }
            if (IconType == "CustomFile" && !string.IsNullOrEmpty(IconPath) && File.Exists(IconPath))
            {
                var uri = new Uri(IconPath, UriKind.Absolute);
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.UriSource = uri;
                img.EndInit();
                return img;
            }
            return null;
        }
    }

    [ObservableProperty]
    private string _actionTypeId = string.Empty;

    public string? ActionConfigJson { get; set; }

    [ObservableProperty]
    private string _actionDisplayName = "Empty";

    [ObservableProperty]
    private bool _hasAction;

    [ObservableProperty]
    private bool _isSelected;

    public ButtonViewModel(int index, ActionRegistry actionRegistry)
    {
        _index = index;
        _actionRegistry = actionRegistry;
    }

    public void LoadMapping(ButtonMapping mapping)
    {
        Label = mapping.Label;
        Icon = mapping.IconPath ?? string.Empty;
        IconType = mapping.IconType;
        IconPath = mapping.IconType == "BuiltIn" || mapping.IconType == "CustomFile"
            ? mapping.IconPath ?? string.Empty
            : string.Empty;
        ImageScale = mapping.ImageScale;
        ImageOffsetX = mapping.ImageOffsetX;
        ImageOffsetY = mapping.ImageOffsetY;
        BackgroundColor = mapping.BackgroundColor;
        ActionTypeId = mapping.ActionTypeId;
        ActionConfigJson = mapping.ActionConfigJson;
        HasAction = !string.IsNullOrEmpty(mapping.ActionTypeId);

        var action = _actionRegistry.Get(mapping.ActionTypeId);
        ActionDisplayName = action?.DisplayName ?? "Unknown Action";
    }

    public void ClearMapping()
    {
        Label = string.Empty;
        Icon = string.Empty;
        IconType = "None";
        IconPath = string.Empty;
        ImageScale = 1.0;
        ImageOffsetX = 0;
        ImageOffsetY = 0;
        BackgroundColor = "#2D2D2D";
        ActionTypeId = string.Empty;
        ActionDisplayName = "Empty";
        HasAction = false;
    }

    public void AssignAction(string actionTypeId, string? customLabel = null)
    {
        var action = _actionRegistry.Get(actionTypeId);
        if (action == null) return;

        ActionTypeId = actionTypeId;
        ActionDisplayName = action.DisplayName;
        HasAction = true;
        if (customLabel != null)
            Label = customLabel;
    }
}
