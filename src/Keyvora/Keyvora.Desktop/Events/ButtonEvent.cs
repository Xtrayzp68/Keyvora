namespace Keyvora.Desktop.Events;

public sealed record ButtonPressedEvent(int ButtonIndex, bool IsLongPress = false);
public sealed record ButtonReleasedEvent(int ButtonIndex);
