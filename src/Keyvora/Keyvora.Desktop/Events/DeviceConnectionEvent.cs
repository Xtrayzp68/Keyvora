namespace Keyvora.Desktop.Events;

public sealed record DeviceConnectedEvent(string PortName);
public sealed record DeviceDisconnectedEvent(string PortName);
public sealed record DeviceErrorEvent(string Message, Exception? Exception = null);
