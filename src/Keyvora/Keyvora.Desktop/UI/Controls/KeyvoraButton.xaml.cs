namespace Keyvora.Desktop.UI.Controls;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

public partial class KeyvoraButton : UserControl
{
    private Point _dragStartPoint;
    private GiveFeedbackEventHandler? _giveFeedbackHandler;
    private DragGhostHelper? _activeGhost;

    public KeyvoraButton()
    {
        InitializeComponent();
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (e.LeftButton != MouseButtonState.Pressed || DataContext == null)
            return;

        var pos = e.GetPosition(this);
        if (Math.Abs(pos.X - _dragStartPoint.X) < 10 &&
            Math.Abs(pos.Y - _dragStartPoint.Y) < 10)
            return;

        var prop = DataContext.GetType().GetProperty("ActionTypeId");
        var actionTypeId = prop?.GetValue(DataContext) as string;

        var indexProp = DataContext.GetType().GetProperty("Index");
        var index = indexProp?.GetValue(DataContext) as int? ?? 0;

        if (string.IsNullOrEmpty(actionTypeId))
            return;

        var data = new DataObject();
        data.SetData("ActionTypeId", actionTypeId);
        data.SetData("ButtonIndex", index);

        CleanupPreviousDrag();

        _activeGhost = new DragGhostHelper();
        _activeGhost.StartDrag(this);

        _giveFeedbackHandler = new GiveFeedbackEventHandler((_, args) =>
        {
            _activeGhost?.UpdatePosition();
            args.UseDefaultCursors = false;
            args.Handled = true;
        });
        GiveFeedback += _giveFeedbackHandler;

        Opacity = 0.25;
        DragDrop.DoDragDrop(this, data, DragDropEffects.Move | DragDropEffects.Copy);
        Opacity = 1.0;

        CleanupPreviousDrag();
    }

    private void CleanupPreviousDrag()
    {
        if (_giveFeedbackHandler != null)
        {
            GiveFeedback -= _giveFeedbackHandler;
            _giveFeedbackHandler = null;
        }
        _activeGhost?.Dispose();
        _activeGhost = null;
    }

    public void SetDropTargetHighlight(bool highlight)
    {
        ButtonBorder.BorderBrush = highlight
            ? new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0x00))
            : new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
        ButtonBorder.BorderThickness = new Thickness(highlight ? 2 : 1);
    }

    private void OnClearAction(object sender, RoutedEventArgs e)
    {
        var win = Window.GetWindow(this);
        if (win?.DataContext is not ViewModels.MainViewModel vm)
            return;

        if (DataContext is ViewModels.ButtonViewModel btn)
        {
            vm.ClearButtonAction(btn.Index);
        }
    }
}
