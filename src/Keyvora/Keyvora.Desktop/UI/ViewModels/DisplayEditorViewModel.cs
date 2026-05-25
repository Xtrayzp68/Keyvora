namespace Keyvora.Desktop.UI.ViewModels;

using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

public sealed partial class DisplayEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private string _backgroundColor = "#2D2D2D";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImageSelected))]
    [NotifyPropertyChangedFor(nameof(PreviewSource))]
    private string _iconType = "None";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImageSelected))]
    [NotifyPropertyChangedFor(nameof(PreviewSource))]
    private string _iconPath = string.Empty;

    [ObservableProperty]
    private string _customImagePath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageTransform))]
    private double _imageScale = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageTransform))]
    private double _imageOffsetX;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageTransform))]
    private double _imageOffsetY;

    public bool IsImageSelected => IconType == "BuiltIn" || IconType == "CustomFile";

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

    public ImageSource? PreviewSource
    {
        get
        {
            if (IconType == "BuiltIn" && !string.IsNullOrEmpty(IconPath))
            {
                return System.Windows.Application.Current.TryFindResource(IconPath) as ImageSource;
            }
            if (IconType == "CustomFile" && !string.IsNullOrEmpty(CustomImagePath) && File.Exists(CustomImagePath))
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.UriSource = new Uri(CustomImagePath, UriKind.Absolute);
                img.EndInit();
                return img;
            }
            return null;
        }
    }

    public List<BuiltInIconItem> BuiltInIcons { get; } = new()
    {
        new("Keyboard", "IconKeyboard"),
        new("Launch", "IconLaunch"),
        new("Folder", "IconFolder"),
        new("Music", "IconMusic"),
        new("Macro", "IconMacro"),
        new("Text", "IconText"),
        new("Settings", "IconSettings"),
        new("Home", "IconHome"),
        new("Power", "IconPower"),
        new("Arrow", "IconArrow"),
        new("Star", "IconStar"),
        new("User", "IconUser"),
        new("Camera", "IconCamera"),
    };

    public void SelectBuiltIn(string resourceKey)
    {
        IconType = "BuiltIn";
        IconPath = resourceKey;
        CustomImagePath = string.Empty;
        OnPropertyChanged(nameof(PreviewSource));
        OnPropertyChanged(nameof(IsImageSelected));
    }

    public void SelectCustomFile(string filePath)
    {
        IconType = "CustomFile";
        CustomImagePath = filePath;
        IconPath = filePath;
        OnPropertyChanged(nameof(PreviewSource));
        OnPropertyChanged(nameof(IsImageSelected));
    }

    public void ClearIcon()
    {
        IconType = "None";
        IconPath = string.Empty;
        CustomImagePath = string.Empty;
        OnPropertyChanged(nameof(PreviewSource));
        OnPropertyChanged(nameof(IsImageSelected));
    }

    public void LoadFromButton(ButtonViewModel buttonVm)
    {
        Icon = buttonVm.Icon;
        Label = buttonVm.Label;
        BackgroundColor = buttonVm.BackgroundColor;
        IconType = buttonVm.IconType;
        ImageScale = buttonVm.ImageScale;
        ImageOffsetX = buttonVm.ImageOffsetX;
        ImageOffsetY = buttonVm.ImageOffsetY;
        if (buttonVm.IconType == "BuiltIn" || buttonVm.IconType == "CustomFile")
        {
            IconPath = buttonVm.IconPath;
            CustomImagePath = buttonVm.IconType == "CustomFile" ? buttonVm.IconPath : string.Empty;
        }
        else
        {
            IconPath = string.Empty;
            CustomImagePath = string.Empty;
        }
        OnPropertyChanged(nameof(PreviewSource));
        OnPropertyChanged(nameof(IsImageSelected));
    }

    public void ApplyToButton(ButtonViewModel buttonVm)
    {
        buttonVm.Icon = Icon;
        buttonVm.Label = Label;
        buttonVm.BackgroundColor = BackgroundColor;
        buttonVm.IconType = IconType;
        buttonVm.ImageScale = ImageScale;
        buttonVm.ImageOffsetX = ImageOffsetX;
        buttonVm.ImageOffsetY = ImageOffsetY;

        if (IconType == "BuiltIn")
            buttonVm.IconPath = IconPath;
        else if (IconType == "CustomFile")
            buttonVm.IconPath = CustomImagePath;
        else
            buttonVm.IconPath = string.Empty;
    }
}

public sealed class BuiltInIconItem
{
    public string Name { get; }
    public string ResourceKey { get; }

    public BuiltInIconItem(string name, string resourceKey)
    {
        Name = name;
        ResourceKey = resourceKey;
    }
}
