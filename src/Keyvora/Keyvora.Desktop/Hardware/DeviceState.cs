namespace Keyvora.Desktop.Hardware;

public sealed class DeviceState
{
    public bool IsConnected { get; set; }
    public string? PortName { get; set; }
    public int BaudRate { get; set; } = 115200;
    public int ButtonCount { get; set; } = 6;
    public DateTime? LastEventTime { get; set; }
    public int EventsPerMinute { get; set; }
}
