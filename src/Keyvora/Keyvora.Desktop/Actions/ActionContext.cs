namespace Keyvora.Desktop.Actions;

using System.Threading;
using System.Threading.Tasks;

public sealed class ActionContext : IActionContext
{
    public int ButtonIndex { get; init; }
    public string ProfileName { get; init; } = string.Empty;
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

    public ActionContext(int buttonIndex, string profileName, CancellationToken ct = default)
    {
        ButtonIndex = buttonIndex;
        ProfileName = profileName;
        CancellationToken = ct;
    }
}
