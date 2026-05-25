namespace Keyvora.Desktop.UI.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Keyvora.Desktop.Actions;
using Keyvora.Desktop.Profiles;

public sealed partial class ButtonGridViewModel : ObservableObject
{
    private readonly ActionRegistry _actionRegistry;

    public ObservableCollection<ButtonViewModel> Buttons { get; }

    [ObservableProperty]
    private int _gridColumns = 3;

    [ObservableProperty]
    private int _gridRows = 2;

    public int ButtonCount => GridColumns * GridRows;

    public ButtonGridViewModel(int columns, int rows, ActionRegistry actionRegistry)
    {
        _actionRegistry = actionRegistry;
        GridColumns = columns;
        GridRows = rows;
        Buttons = new ObservableCollection<ButtonViewModel>();
        CreateButtons();
    }

    private void CreateButtons()
    {
        Buttons.Clear();
        for (int i = 0; i < ButtonCount; i++)
        {
            Buttons.Add(new ButtonViewModel(i + 1, _actionRegistry));
        }
    }

    public void Resize(int columns, int rows)
    {
        GridColumns = columns;
        GridRows = rows;
        CreateButtons();
    }

    public void LoadFromProfile(Profile profile)
    {
        for (int i = 0; i < ButtonCount; i++)
        {
            var btnIndex = i + 1; // 1-based
            if (profile.Buttons.TryGetValue(btnIndex, out var mapping))
            {
                Buttons[i].LoadMapping(mapping);
            }
            else
            {
                Buttons[i].ClearMapping();
            }
        }
    }

    public void HandleDrop(int buttonIndex, string actionTypeId)
    {
        if (buttonIndex < 1 || buttonIndex > ButtonCount) return;
        Buttons[buttonIndex - 1].AssignAction(actionTypeId);
    }
}
