namespace Keyvora.Desktop.Hardware;

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Keyvora.Desktop.Events;

public sealed class SerialDeviceManager : IDeviceManager, IDisposable
{
    private const int DEFAULT_BAUD = 115200;
    private const int READ_TIMEOUT_MS = 100;

    private readonly IEventBus _eventBus;
    private readonly int _baudRate;
    private SerialPort? _port;
    private CancellationTokenSource? _cts;
    private Task? _readTask;

    public bool IsConnected => _port?.IsOpen ?? false;
    public string? PortName => _port?.PortName;

    private static readonly Regex ButtonRegex = new(@"^BTN_(\d+)$", RegexOptions.Compiled);

    public SerialDeviceManager(IEventBus eventBus, int baudRate = DEFAULT_BAUD)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _baudRate = baudRate;
    }

    public string[] GetAvailablePorts() => SerialPort.GetPortNames();

    public bool Connect(string portName)
    {
        Disconnect();

        try
        {
            _port = new SerialPort(portName, _baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = READ_TIMEOUT_MS,
                WriteTimeout = READ_TIMEOUT_MS,
                NewLine = "\n",
                DtrEnable = true,
                RtsEnable = true
            };

            _port.Open();
            _cts = new CancellationTokenSource();
            _readTask = Task.Run(() => ReadLoop(_cts.Token), _cts.Token);

            _eventBus.Publish(new DeviceConnectedEvent(portName));
            return true;
        }
        catch (Exception ex)
        {
            _eventBus.Publish(new DeviceErrorEvent($"Failed to connect to {portName}", ex));
            _port?.Dispose();
            _port = null;
            return false;
        }
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        _readTask?.Wait(TimeSpan.FromSeconds(1));
        _readTask?.Dispose();
        _cts?.Dispose();
        _cts = null;
        _readTask = null;

        if (_port?.IsOpen == true)
        {
            var portName = _port.PortName;
            try { _port.Close(); }
            catch { /* best effort */ }
            _eventBus.Publish(new DeviceDisconnectedEvent(portName));
        }

        _port?.Dispose();
        _port = null;
    }

    public async Task<bool> AutoConnectAsync(IReadOnlyList<string>? knownPorts = null)
    {
        var ports = knownPorts ?? GetAvailablePorts();
        if (ports.Count == 0) return false;

        foreach (var port in ports)
        {
            if (Connect(port))
            {
                await Task.Delay(500); // wait for device ready blink
                return true;
            }
        }

        return false;
    }

    private void ReadLoop(CancellationToken ct)
    {
        var lineBuffer = new List<byte>();

        while (!ct.IsCancellationRequested && _port?.IsOpen == true)
        {
            try
            {
                string? line = _port.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var match = ButtonRegex.Match(line.Trim());
                if (match.Success && int.TryParse(match.Groups[1].Value, out int btnIndex))
                {
                    _eventBus.Publish(new ButtonPressedEvent(btnIndex));
                }
            }
            catch (TimeoutException) { /* normal in read loop */ }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _eventBus.Publish(new DeviceErrorEvent("Serial read error", ex));
                break;
            }
        }

        if (_port?.IsOpen == true)
        {
            var portName = _port.PortName;
            _eventBus.Publish(new DeviceDisconnectedEvent(portName));
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}
