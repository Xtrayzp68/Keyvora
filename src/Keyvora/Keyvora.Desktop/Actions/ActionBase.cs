namespace Keyvora.Desktop.Actions;

using System.Threading.Tasks;

public abstract class ActionBase : IAction
{
    public abstract string TypeId { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }
    public IActionConfig? Config { get; set; }

    public abstract Task ExecuteAsync(IActionContext context);

    public override string ToString() => DisplayName;
}
