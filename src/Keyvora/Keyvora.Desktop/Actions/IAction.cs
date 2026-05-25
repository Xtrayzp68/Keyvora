namespace Keyvora.Desktop.Actions;

using System.Threading.Tasks;

public interface IAction
{
    string TypeId { get; }
    string DisplayName { get; }
    string Description { get; }
    Task ExecuteAsync(IActionContext context);
    IActionConfig? Config { get; set; }
}

public interface IActionConfig
{
    string Serialize();
    void Deserialize(string json);
}

public interface IActionContext
{
    int ButtonIndex { get; }
    string ProfileName { get; }
    CancellationToken CancellationToken { get; }
}
