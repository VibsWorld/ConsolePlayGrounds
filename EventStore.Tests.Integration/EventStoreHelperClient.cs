namespace EventStore.Tests.Integration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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

    public record EventDetails(byte[] Data, string EventType);

    public async Task<Dictionary<string, EventDetails>> GetAllEventsFromStreamName(string streamName)
    {
        Console.WriteLine($"Reading all events from stream: {streamName}");
        Dictionary<string, EventDetails> dictionaryEvents = new();

        var readResult = eventStoreClient.ReadStreamAsync(
            direction: Direction.Forwards,
            streamName: streamName,
            revision: StreamPosition.Start, // Start from the beginning of the stream
            cancellationToken: CancellationToken.None // Optional
        );

        if (await readResult.ReadState == ReadState.StreamNotFound)
        {
            Console.WriteLine($"Stream '{streamName}' not found.");
            return [];
        }

        await foreach (var resolvedEvent in readResult)
        {
            // The ResolvedEvent contains the EventRecord and potentially a link event
            var eventRecord = resolvedEvent.Event;

            Console.WriteLine($"Event ID: {eventRecord.EventId}, Type: {eventRecord.EventType}");

            // Deserialize the event data
            if (eventRecord.ContentType == "application/json")
            {
                // Convert the data to a string
                var jsonString = Encoding.UTF8.GetString(eventRecord.Data.ToArray());
                Console.WriteLine($"\tData (JSON): {jsonString}");

                dictionaryEvents.Add(eventRecord.EventId.ToString(), new EventDetails(eventRecord.Data.ToArray(), eventRecord.EventType));
                
                // Example of deserializing to a specific type (you would use your actual event type)
                try
                {
                    // This assumes you know the type, which usually requires a Type Mapper
                    // based on eventRecord.EventType in a real application.
                    // For this simple example, we'll just show the JSON.

                    // Example: var myEvent = JsonSerializer.Deserialize<MyEventType>(jsonString);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"\tFailed to deserialize event data: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"\tData (Raw Bytes): {eventRecord.Data.Length} bytes of content type {eventRecord.ContentType}");
            }
        }

        Console.WriteLine($"Finished reading stream: {streamName}");
        return dictionaryEvents;
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
