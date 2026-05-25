namespace Keyvora.Desktop.Hardware;

using System.Threading.Tasks;

public interface IDeviceManager : IDisposable
{
    bool IsConnected { get; }
    string? PortName { get; }
    string[] GetAvailablePorts();
    bool Connect(string portName);
    void Disconnect();
    Task<bool> AutoConnectAsync(IReadOnlyList<string>? knownPorts = null);
}
