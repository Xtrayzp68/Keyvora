namespace Keyvora.Desktop.UI.Views;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Keyvora.Desktop.Profiles;
using Keyvora.Desktop.UI.ViewModels;

public partial class MainWindow : Window
{
    private readonly Dictionary<object, Point> _buttonMouseDownPositions = new();
    private DispatcherTimer? _singleClickTimer;
    private ButtonViewModel? _pendingSingleClickButton;

    public MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.GridDimensionsChanged += OnGridDimensionsChanged;
    }

    private void OnButtonDrop(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement element) return;
        if (element.DataContext is not ButtonViewModel targetVm) return;

        var mainVm = DataContext as MainViewModel;
        if (mainVm == null) return;

        if (sender is Controls.KeyvoraButton btn)
            btn.SetDropTargetHighlight(false);

        // Button reorder drag (from another button)
        if (e.Data.GetDataPresent("ButtonIndex"))
        {
            var sourceIndex = (int)e.Data.GetData("ButtonIndex");
            if (sourceIndex != targetVm.Index)
            {
                mainVm.SwapButtons(sourceIndex, targetVm.Index);
            }
            e.Handled = true;
            return;
        }

        // Action type assignment drag (from action list)
        if (e.Data.GetDataPresent(typeof(string)))
        {
            var actionTypeId = (string)e.Data.GetData(typeof(string));
            if (!string.IsNullOrEmpty(actionTypeId))
            {
                mainVm.ButtonGrid.HandleDrop(targetVm.Index, actionTypeId);
            }
        }

        e.Handled = true;
    }

    private void OnButtonDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("ButtonIndex"))
        {
            e.Effects = DragDropEffects.Move;
        }
        else if (e.Data.GetDataPresent(typeof(string)))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        if (sender is Controls.KeyvoraButton btn)
        {
            btn.SetDropTargetHighlight(true);
        }

        e.Handled = true;
    }

    private void OnButtonDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Controls.KeyvoraButton btn)
        {
            btn.SetDropTargetHighlight(false);
        }
        e.Handled = true;
    }

    private void OnButtonPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element) return;
        if (element.DataContext is not ButtonViewModel buttonVm) return;

        buttonVm.IsSelected = true;
        _buttonMouseDownPositions[sender] = e.GetPosition(element);
    }

    private void OnButtonPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        if (sender is not FrameworkElement element) return;
        if (element.DataContext is not ButtonViewModel buttonVm) return;

        // Only handle click if no significant mouse movement (click, not drag)
        if (_buttonMouseDownPositions.TryGetValue(sender, out var downPos))
        {
            var upPos = e.GetPosition(element);
            if (Math.Abs(upPos.X - downPos.X) < 10 &&
                Math.Abs(upPos.Y - downPos.Y) < 10)
            {
                if (e.ClickCount == 2)
                {
                    // Second click of a double click - ignore, the double-click handler will fire
                    return;
                }

                // Defer single click to distinguish from double click
                _pendingSingleClickButton = buttonVm;
                _singleClickTimer?.Stop();
                _singleClickTimer = new DispatcherTimer(
                    TimeSpan.FromMilliseconds(300),
                    DispatcherPriority.Normal,
                    OnSingleClickTimerElapsed,
                    Dispatcher);
                _singleClickTimer.Start();
            }
        }
    }

    private void OnSingleClickTimerElapsed(object? sender, EventArgs e)
    {
        _singleClickTimer?.Stop();
        if (_pendingSingleClickButton != null)
        {
            OpenDisplayEditor(_pendingSingleClickButton);
            _pendingSingleClickButton = null;
        }
    }

    private void OnButtonMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Cancel pending single click
        _singleClickTimer?.Stop();
        _pendingSingleClickButton = null;

        if (sender is not FrameworkElement element) return;
        if (element.DataContext is not ButtonViewModel buttonVm) return;

        buttonVm.IsSelected = true;
        OpenActionEditor(buttonVm);
    }

    private void OpenDisplayEditor(ButtonViewModel buttonVm)
    {
        var mainVm = DataContext as MainViewModel;
        if (mainVm == null) return;

        var editorVm = new DisplayEditorViewModel();
        editorVm.LoadFromButton(buttonVm);

        var editor = new DisplayEditorDialog(editorVm);
        editor.Owner = this;

        if (editor.ShowDialog() == true)
        {
            editorVm.ApplyToButton(buttonVm);

            // Persist to profile
            var profileManager = mainVm.GetProfileManager();
            if (profileManager != null)
            {
                var existingMapping = profileManager.GetButtonMapping(buttonVm.Index);
                var mapping = new Profiles.ButtonMapping
                {
                    ActionTypeId = buttonVm.ActionTypeId,
                    Label = buttonVm.Label,
                    IconType = buttonVm.IconType,
                    IconPath = buttonVm.IconType switch
                    {
                        "BuiltIn" => buttonVm.IconPath,
                        "CustomFile" => buttonVm.IconPath,
                        "Text" => string.IsNullOrEmpty(buttonVm.Icon) ? null : buttonVm.Icon,
                        _ => null
                    },
                    BackgroundColor = buttonVm.BackgroundColor,
                    ImageScale = buttonVm.ImageScale,
                    ImageOffsetX = buttonVm.ImageOffsetX,
                    ImageOffsetY = buttonVm.ImageOffsetY,
                    ActionConfigJson = existingMapping?.ActionConfigJson,
                    IsEnabled = existingMapping?.IsEnabled ?? true
                };
                profileManager.UpdateButtonMapping(buttonVm.Index, mapping);
            }
        }
    }

    private void OpenActionEditor(ButtonViewModel buttonVm)
    {
        var mainVm = DataContext as MainViewModel;
        if (mainVm == null) return;

        if (buttonVm.HasAction && !string.IsNullOrWhiteSpace(buttonVm.ActionConfigJson))
        {
            mainVm.ActionEditor.LoadExistingConfig(buttonVm.ActionTypeId, buttonVm.ActionConfigJson);
        }
        else
        {
            mainVm.ActionEditor.LoadExistingConfig(string.Empty, null);
        }

        if (buttonVm.HasAction)
        {
            var action = mainVm.ActionEditor.AvailableActions.FirstOrDefault(a => a.TypeId == buttonVm.ActionTypeId);
            if (action != null)
                mainVm.ActionEditor.SelectedAction = action;
        }

        var editor = new ActionEditorDialog(mainVm.ActionEditor, buttonVm);
        editor.Owner = this;

        if (editor.ShowDialog() == true && buttonVm.HasAction)
        {
            var profileManager = mainVm.GetProfileManager();
            var configJson = editor.Tag as string;
            var existingMapping = profileManager?.GetButtonMapping(buttonVm.Index);
            var mapping = new Profiles.ButtonMapping
            {
                ActionTypeId = buttonVm.ActionTypeId,
                Label = buttonVm.Label,
                IconType = existingMapping?.IconType ?? "None",
                IconPath = existingMapping?.IconPath,
                BackgroundColor = existingMapping?.BackgroundColor ?? "#2D2D2D",
                ActionConfigJson = configJson,
                IsEnabled = existingMapping?.IsEnabled ?? true
            };
            profileManager?.UpdateButtonMapping(buttonVm.Index, mapping);
        }
    }

    private void OnGridDimensionsChanged(int cols, int rows)
    {
        UpdateUniformGrid(cols, rows);
    }

    private void OnButtonGridLoaded(object sender, RoutedEventArgs e)
    {
        UpdateUniformGrid(ViewModel.ButtonGrid.GridColumns, ViewModel.ButtonGrid.GridRows);
    }

    private void UpdateUniformGrid(int cols, int rows)
    {
        var grid = FindVisualChild<UniformGrid>(ButtonGridItemsControl);
        if (grid != null)
        {
            grid.Columns = cols;
            grid.Rows = rows;
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    private void OnProfileTabPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToHorizontalOffset(
                scrollViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
        }
    }

    private bool _isTabDragging;
    private FrameworkElement? _tabDragSource;
    private int _tabDragSourceIndex;
    private int _tabDragInsertIndex = -1;
    private double _tabDragStartX;

    private bool _dragEnded;

    private void OnProfileTabPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _tabDragStartX = e.GetPosition(ProfileTabScroll).X;
        _dragEnded = false;
    }

    private void OnProfileTabPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        if (!_isTabDragging)
        {
            if (_dragEnded) return;

            var source = FindParentButton(e.OriginalSource as DependencyObject);
            if (source == null) return;
            if (source.DataContext is not ProfileItem profileItem) return;

            var dx = e.GetPosition(ProfileTabScroll).X - _tabDragStartX;
            if (Math.Abs(dx) < 10) return;

            _isTabDragging = true;
            _tabDragSource = source;
            ProfileTabItemsControl.CaptureMouse();

            var profiles = ProfileTabItemsControl.ItemsSource.Cast<ProfileItem>().ToList();
            _tabDragSourceIndex = profiles.IndexOf(profileItem);
            _tabDragInsertIndex = _tabDragSourceIndex;
            Log($"DragStart: sourceIdx={_tabDragSourceIndex}, profile='{profileItem.Name}'");
        }

        var mouseX = e.GetPosition(ProfileTabScroll).X;
        var offset = mouseX - _tabDragStartX;
        _tabDragSource!.RenderTransform = new TranslateTransform(offset, 0);

        var insertAt = ComputeInsertIndex(mouseX);
        if (insertAt != _tabDragInsertIndex)
        {
            Log($"InsertIndex: {_tabDragInsertIndex} -> {insertAt}");
            _tabDragInsertIndex = insertAt;
            UpdateTabGap(insertAt);
        }
    }

    private static string LogPath => Path.Combine(Path.GetTempPath(), "Keyvora_DragTrace.log");
    private static void Log(string msg)
    {
        try { File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {msg}{Environment.NewLine}"); }
        catch { }
    }

    private void OnProfileTabPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;

        Log($"MouseUp: _isTabDragging={_isTabDragging}, _tabDragSourceIndex={_tabDragSourceIndex}, _tabDragInsertIndex={_tabDragInsertIndex}");

        if (_isTabDragging)
        {
            _isTabDragging = false;

            // prevent re-entrant drag during cleanup/reload
            _dragEnded = true;

            ProfileTabItemsControl.ReleaseMouseCapture();

            if (_tabDragSource != null)
                _tabDragSource.RenderTransform = Transform.Identity;

            var dragSourceIndex = _tabDragSourceIndex;
            var dragInsertIndex = _tabDragInsertIndex;

            Log($"MouseUp: saved indices - source={dragSourceIndex}, insert={dragInsertIndex}");

            _tabDragSource = null;
            _tabDragInsertIndex = -1;

            ClearTabGap();

            var mainVm = DataContext as MainViewModel;
            var profileManager = mainVm?.GetProfileManager();
            Log($"MouseUp: manager=null?{profileManager == null}, condition={dragInsertIndex >= 0 && dragInsertIndex != dragSourceIndex}");
            if (profileManager != null && dragInsertIndex >= 0 &&
                dragInsertIndex != dragSourceIndex)
            {
                Log($"MouseUp: calling ReorderProfile({dragSourceIndex}, {dragInsertIndex})");
                profileManager.ReorderProfile(dragSourceIndex, dragInsertIndex);
                Log($"MouseUp: calling LoadProfiles()");
                mainVm!.ProfileSelector.LoadProfiles();
                Log($"MouseUp: done");
            }

            e.Handled = true;
        }
    }

    private static Button? FindParentButton(DependencyObject? child)
    {
        if (child is Button btn) return btn;
        var current = child;
        while (current != null)
        {
            if (current is Button found) return found;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private int ComputeInsertIndex(double mouseX)
    {
        var profiles = ProfileTabItemsControl.ItemsSource.Cast<ProfileItem>().ToList();
        var count = profiles.Count;
        if (count == 0) return -1;

        for (int i = 0; i < count; i++)
        {
            if (i == _tabDragSourceIndex) continue;

            var container = ProfileTabItemsControl.ItemContainerGenerator.ContainerFromIndex(i);
            if (container is not FrameworkElement fe) continue;
            var button = FindVisualChild<Button>(fe);
            if (button == null) continue;

            var center = button.TranslatePoint(
                new Point(button.ActualWidth / 2, 0),
                ProfileTabScroll).X;

            if (button.RenderTransform is TranslateTransform tt)
                center -= tt.X;

            if (mouseX < center)
            {
                // source removal shifts higher indices left by one
                return i > _tabDragSourceIndex ? i - 1 : i;
            }
        }

        return count - 1;
    }

    private void UpdateTabGap(int insertAt)
    {
        ClearTabGap();

        var profiles = ProfileTabItemsControl.ItemsSource.Cast<ProfileItem>().ToList();
        var count = profiles.Count;
        if (count == 0 || insertAt < 0 || insertAt >= count) return;

        double gap = _tabDragSource?.ActualWidth ?? 80;

        for (int i = 0; i < count; i++)
        {
            if (i == _tabDragSourceIndex) continue;

            var container = ProfileTabItemsControl.ItemContainerGenerator.ContainerFromIndex(i);
            if (container is not FrameworkElement fe) continue;
            var button = FindVisualChild<Button>(fe);
            if (button == null) continue;

            bool shift;
            double direction;

            if (insertAt < _tabDragSourceIndex)
            {
                // source dragged LEFT: tabs between insertAt and source shift RIGHT
                shift = i >= insertAt && i < _tabDragSourceIndex;
                direction = gap;
            }
            else
            {
                // source dragged RIGHT: tabs between source+1 and insertAt shift LEFT
                shift = i > _tabDragSourceIndex && i <= insertAt;
                direction = -gap;
            }

            button.RenderTransform = shift
                ? new TranslateTransform(direction, 0)
                : Transform.Identity;
        }
    }

    private void ClearTabGap()
    {
        var count = ProfileTabItemsControl.Items.Count;
        for (int i = 0; i < count; i++)
        {
            if (i == _tabDragSourceIndex) continue;

            var container = ProfileTabItemsControl.ItemContainerGenerator.ContainerFromIndex(i);
            if (container is not FrameworkElement fe) continue;
            var button = FindVisualChild<Button>(fe);
            if (button != null)
                button.RenderTransform = Transform.Identity;
        }
    }
}
