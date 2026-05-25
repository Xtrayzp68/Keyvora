namespace Keyvora.Desktop.Events;

public interface IEventBus
{
    void Publish<T>(T eventData) where T : class;
    IDisposable Subscribe<T>(Action<T> handler) where T : class;
}
