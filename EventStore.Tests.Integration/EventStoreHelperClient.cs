namespace EventStore.Tests.Integration;

using System;
using System.Linq;
using System.Threading.Tasks;
using EventStore.Client;

public class EventStoreHelperClient : IDisposable
{
    private readonly EventStoreClient eventStoreClient;

    public EventStoreHelperClient(string host, ushort port)
    {
        var settings = EventStoreClientSettings.Create(
            $"esdb://{host}:{port}?tls=false&tlsVerifyCert=false"
        );
        settings.OperationOptions.ThrowOnAppendFailure = false;
        eventStoreClient = new EventStoreClient(settings);
    }

    public async Task<T[]> ReadEvents<T>(string eventstream)
    {
        var readStreamResult = eventStoreClient.ReadStreamAsync(
            Direction.Forwards,
            eventstream,
            StreamPosition.Start
        );

        var eventStream = await readStreamResult.ToListAsync();
        if (eventStream?.Count < 1)
            return default;

        return eventStream
            .Select(re =>
            {
                var type = re.Event.EventType.Equals(
                    typeof(T).Name,
                    StringComparison.OrdinalIgnoreCase
                );
                if (type)
                    return System.Text.Json.JsonSerializer.Deserialize<T>(re.Event.Data.ToArray());

                return default;
            })
            .Where(x => x is not null)
            .ToArray();
    }

    public async Task<List<T>> GetAllEvents<T>()
    {
        var events = eventStoreClient.ReadAllAsync(Direction.Forwards, Position.Start);
        List<T> eventsT = new List<T>();
        await foreach (var e in events)
        {
            var obj = System.Text.Json.JsonSerializer.Deserialize<T>(e.Event.Data.ToArray());
            if (obj is not null)
                eventsT.Add(obj);
        }

        return eventsT;
    }

    public void Dispose() => eventStoreClient.Dispose();
}
