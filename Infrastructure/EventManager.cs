using System.Text.Json;

namespace WST;

public class EventManager
{
    private readonly Dictionary<Type, List<Func<object, Task>>> _asyncSubscribers = new();
    private readonly Dictionary<Type, List<Action<object>>> _syncSubscribers = new();

    // Subscribe to async events
    public void SubscribeAsync<T>(Func<T, Task> handler) where T : class
    {
        var type = typeof(T);
        if (!_asyncSubscribers.ContainsKey(type)) _asyncSubscribers[type] = new List<Func<object, Task>>();
        _asyncSubscribers[type].Add(async e => await handler(e as T));
    }

    // Subscribe to sync events
    public void Subscribe<T>(Action<T> handler) where T : class
    {
        var type = typeof(T);
        if (!_syncSubscribers.ContainsKey(type)) _syncSubscribers[type] = new List<Action<object>>();
        _syncSubscribers[type].Add(e => handler(e as T));
    }

    public void Publish<T>(T eventToPublish) where T : class
    {
        try
        {
            var type = typeof(T);
            if (_syncSubscribers.ContainsKey(type))
            {
                var handlers = _syncSubscribers[type];
                foreach (var handler in handlers) handler(eventToPublish);
            }

            if (_asyncSubscribers.ContainsKey(type))
            {
                var handlers = _asyncSubscribers[type];
                foreach (var handler in handlers) handler(eventToPublish);
            }
            
            if (type != typeof(OnTickEvent))
            {
                Console.WriteLine($"Event: {type.Name}");
                Console.WriteLine(JsonSerializer.Serialize(eventToPublish, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
        }
        catch (Exception e)
        { 
            Console.WriteLine("WST_EVENT_ERROR:");
            Console.WriteLine(e);
            var type = typeof(T);
            Console.WriteLine($"Event: {type.Name}");
            Console.WriteLine(JsonSerializer.Serialize(eventToPublish, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }

    }
}