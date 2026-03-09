namespace EventStore.SyncApp;

using EventStore.SyncApp.Infrastructure;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine(
            """
            ╔════════════════════════════════════════════════╗
            ║      EventStore Sync Tool (Cloud → Local)      ║
            ╚════════════════════════════════════════════════╝
            """
        );

        Console.WriteLine("Testing connection to local EventStore...");

        var localClient = await EventStoreHelper.GetLocalEventStoreClient();

        try
        {
            var testStream = await localClient.GetAllEventsFromStreamName("$stats");
            Console.WriteLine("✓ Connected to local EventStore successfully\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to connect to local EventStore: {ex.Message}");
            Console.WriteLine(
                "Make sure your Docker container is running and accessible at localhost:2113"
            );
            localClient?.Dispose();
            return;
        }

        Console.WriteLine("Enter stream name(s) to sync (comma-separated):");
        Console.WriteLine("Example: Orchid_ShipmentV2-guid1,Orchid_ShipmentV2-guid2\n");

        string input = Console.ReadLine() ?? string.Empty;

        var streamNames = input
            .Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (streamNames.Count == 0)
        {
            Console.WriteLine("✗ No stream names provided. Exiting.");
            localClient?.Dispose();
            return;
        }

        var cloudClient = await EventStoreHelper.GetCloudEventStoreClient();

        try
        {
            Console.WriteLine($"\n📡 Starting sync of {streamNames.Count} stream(s)...\n");

            int totalStreamsProcessed = 0;
            int totalEventsProcessed = 0;
            int totalEventsFailed = 0;

            foreach (var streamName in streamNames)
            {
                var (successCount, failureCount) = await SyncStreamAsync(
                    cloudClient,
                    localClient,
                    streamName
                );
                totalStreamsProcessed++;
                totalEventsProcessed += successCount;
                totalEventsFailed += failureCount;
            }

            Console.WriteLine(
                $"""

                ╔════════════════════════════════════════════════╗
                ║           Sync Completed Successfully          ║
                ╠════════════════════════════════════════════════╣
                ║ Streams Processed: {totalStreamsProcessed, 40} ║
                ║ Events Synced:     {totalEventsProcessed, 40} ║
                ║ Events Failed:     {totalEventsFailed, 40} ║
                ╚════════════════════════════════════════════════╝
                """
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error during sync: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
        finally
        {
            cloudClient?.Dispose();
            localClient?.Dispose();
        }
    }

    private static async Task<(int successCount, int failureCount)> SyncStreamAsync(
        EventStoreHelper cloudClient,
        EventStoreHelper localClient,
        string streamName
    )
    {
        Console.WriteLine($"📦 Syncing stream: {streamName}");

        var events = await cloudClient.GetAllEventsFromStreamName(streamName);

        if (events.Count == 0)
        {
            Console.WriteLine($"   ℹ️  No events found in stream {streamName}\n");
            return (0, 0);
        }

        int successCount = 0;
        int failureCount = 0;
        int batchSize = 10;

        for (int i = 0; i < events.Count; i++)
        {
            try
            {
                var eventEntry = events.ElementAt(i);
                var eventData = new EventStore.Client.EventData(
                    EventStore.Client.Uuid.NewUuid(),
                    eventEntry.Value.EventType,
                    eventEntry.Value.Data
                );

                await localClient.AppendEventAsync(streamName, eventData);
                successCount++;

                if ((i + 1) % batchSize == 0 || (i + 1) == events.Count)
                {
                    Console.WriteLine($"   ⏳ Progress: {i + 1}/{events.Count} events processed");
                }
            }
            catch (Exception ex)
            {
                failureCount++;
                if (failureCount <= 3) // Show first 3 errors
                {
                    Console.WriteLine($"   ⚠️  Event {i + 1} failed: {ex.Message}");
                }
            }
        }

        Console.WriteLine(
            $"   ✓ Stream {streamName} synced: {successCount}/{events.Count} events added\n"
        );

        return (successCount, failureCount);
    }
}
