using EventStore.Client;

namespace EventStore.SyncApp.Infrastructure;

public class EventStoreHelper(EventStoreClientSettings settings) : IDisposable
{
    private readonly EventStoreClient eventStoreClient = new(settings);

    public record EventDetails(byte[] Data, string EventType);

    public async Task<Dictionary<string, EventDetails>> GetAllEventsFromStreamName(
        string streamName
    )
    {
        Dictionary<string, EventDetails> dictionaryEvents = new();

        var readResult = eventStoreClient.ReadStreamAsync(
            direction: Direction.Forwards,
            streamName: streamName,
            revision: StreamPosition.Start,
            cancellationToken: CancellationToken.None
        );

        if (await readResult.ReadState == ReadState.StreamNotFound)
        {
            return [];
        }

        await foreach (var resolvedEvent in readResult)
        {
            var eventRecord = resolvedEvent.Event;
            if (eventRecord.ContentType == "application/json")
            {
                dictionaryEvents.Add(
                    eventRecord.EventId.ToString(),
                    new EventDetails(eventRecord.Data.ToArray(), eventRecord.EventType)
                );
            }
        }

        return dictionaryEvents;
    }

    public async Task AppendEventAsync(string streamName, EventData eventData)
    {
        await eventStoreClient.AppendToStreamAsync(
            streamName,
            StreamState.Any,
            new[] { eventData }
        );
    }

    public async Task DeleteStreamAsync(string streamName)
    {
        await eventStoreClient.DeleteAsync(
            streamName,
            StreamState.Any
        );
    }

    public static Task<EventStoreHelper> GetCloudEventStoreClient()
    {
        var settings = EventStoreClientSettings.Create(AppConfiguration.CloudEventStoreUrl);
        var eventStoreClient = new EventStoreHelper(settings);

        return Task.FromResult(eventStoreClient);
    }

    public static Task<EventStoreHelper> GetLocalEventStoreClient()
    {
        var settings = EventStoreClientSettings.Create(AppConfiguration.LocalEventStoreUrl);
        var eventStoreClient = new EventStoreHelper(settings);

        return Task.FromResult(eventStoreClient);
    }

    public void Dispose()
    {
        eventStoreClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
