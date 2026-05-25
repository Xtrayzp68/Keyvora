namespace Keyvora.Desktop.Hardware;

public static class SerialProtocol
{
    public const string NEWLINE = "\n";
    public const int DEFAULT_BAUD = 115200;

    public static string EncodeButtonPress(int buttonIndex) => $"BTN_{buttonIndex}";
    public static string EncodeButtonRelease(int buttonIndex) => $"BTN_{buttonIndex}_UP";
    public static string EncodePing() => "PING";
    public static string EncodePong() => "PONG";

    public static bool TryDecode(string message, out int buttonIndex)
    {
        buttonIndex = 0;
        if (string.IsNullOrWhiteSpace(message)) return false;

        var trimmed = message.Trim();

        if (trimmed.StartsWith("BTN_", StringComparison.Ordinal))
        {
            var parts = trimmed.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int idx))
            {
                buttonIndex = idx;
                return true;
            }
        }

        return false;
    }
}
