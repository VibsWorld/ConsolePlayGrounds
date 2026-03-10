namespace EventStore.SyncApp;

using EventStore.SyncApp.Infrastructure;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine(
            """
            ╔════════════════════════════════════════════════╗
            ║      EventStore Sync Tool (Cloud ↔ Local)      ║
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

        Console.WriteLine("Select an option:");
        Console.WriteLine("1. Sync streams from Cloud to Local");
        Console.WriteLine("2. Delete stream(s) from Local EventStore");
        Console.WriteLine("3. Sync streams from CSV file");
        Console.WriteLine("4. Exit\n");
        Console.Write("Enter your choice (1-4): ");

        string choice = Console.ReadLine() ?? string.Empty;

        switch (choice.Trim())
        {
            case "1":
                await HandleSyncStreamsAsync(localClient);
                break;
            case "2":
                await HandleDeleteStreamsAsync(localClient);
                break;
            case "3":
                await HandleCsvSyncAsync(localClient);
                break;
            case "4":
                Console.WriteLine("Exiting...");
                localClient?.Dispose();
                return;
            default:
                Console.WriteLine("✗ Invalid choice. Exiting.");
                localClient?.Dispose();
                return;
        }

        localClient?.Dispose();
    }

    private static async Task HandleSyncStreamsAsync(EventStoreHelper localClient)
    {
        Console.WriteLine("\nEnter stream name(s) to sync (comma-separated):");
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
        }
    }

    private static async Task HandleDeleteStreamsAsync(EventStoreHelper localClient)
    {
        Console.WriteLine("\nSelect deletion type:");
        Console.WriteLine("1. Soft Delete (stream can be recovered)");
        Console.WriteLine("2. Hard Delete / Tombstone (permanent, cannot be recovered)");
        Console.Write("\nEnter your choice (1-2): ");

        string deleteChoice = Console.ReadLine() ?? string.Empty;
        bool isHardDelete = deleteChoice.Trim() == "2";

        Console.WriteLine("\nEnter stream name(s) to delete from local EventStore (comma-separated):");
        Console.WriteLine("Example: Orchid_ShipmentV2-guid1,Orchid_ShipmentV2-guid2\n");
        
        if (isHardDelete)
        {
            Console.WriteLine("⚠️⚠️  WARNING: HARD DELETE - This action PERMANENTLY removes the stream and CANNOT be undone! ⚠️⚠️\n");
        }
        else
        {
            Console.WriteLine("⚠️  Warning: Soft delete - Stream will be marked as deleted but can be recovered.\n");
        }

        string input = Console.ReadLine() ?? string.Empty;

        var streamNames = input
            .Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (streamNames.Count == 0)
        {
            Console.WriteLine("✗ No stream names provided. Exiting.");
            return;
        }

        string deleteType = isHardDelete ? "HARD DELETE (tombstone)" : "soft delete";
        Console.Write($"\nAre you sure you want to {deleteType} {streamNames.Count} stream(s)? (yes/no): ");
        string confirmation = Console.ReadLine() ?? string.Empty;

        if (!confirmation.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("✗ Deletion cancelled.");
            return;
        }

        try
        {
            string emoji = isHardDelete ? "💀" : "🗑️";
            Console.WriteLine($"\n{emoji}  Starting {deleteType} of {streamNames.Count} stream(s)...\n");

            int successCount = 0;
            int failureCount = 0;

            foreach (var streamName in streamNames)
            {
                try
                {
                    Console.WriteLine($"{emoji}  Deleting stream: {streamName}");
                    
                    if (isHardDelete)
                    {
                        await localClient.HardDeleteStreamAsync(streamName);
                    }
                    else
                    {
                        await localClient.DeleteStreamAsync(streamName);
                    }
                    
                    successCount++;
                    Console.WriteLine($"   ✓ Stream {streamName} deleted successfully ({deleteType})\n");
                }
                catch (Exception ex)
                {
                    failureCount++;
                    Console.WriteLine($"   ✗ Failed to delete stream {streamName}: {ex.Message}\n");
                }
            }

            Console.WriteLine(
                $"""

                ╔════════════════════════════════════════════════╗
                ║          Deletion Completed ({(isHardDelete ? "HARD" : "SOFT"),12})           ║
                ╠════════════════════════════════════════════════╣
                ║ Streams Deleted:   {successCount,40} ║
                ║ Streams Failed:    {failureCount,40} ║
                ╚════════════════════════════════════════════════╝
                """
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error during deletion: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    private static async Task HandleCsvSyncAsync(EventStoreHelper localClient)
    {
        Console.WriteLine("\nEnter the CSV filename (must be in the same directory as the exe):");
        Console.WriteLine("Example: streams.csv");
        Console.WriteLine("  ↵  Press ENTER to automatically select the most recently modified .csv file in the exe directory.\n");
        Console.Write("Filename: ");
        string fileName = Console.ReadLine() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            var csvFiles = Directory
                .GetFiles(AppContext.BaseDirectory, "*.csv")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            if (csvFiles.Count == 0)
            {
                Console.WriteLine($"✗ No .csv files found in: {AppContext.BaseDirectory}");
                return;
            }

            var latest = csvFiles[0];
            fileName = latest.Name;
            Console.WriteLine($"✓ Auto-selected: {fileName} (last modified: {latest.LastWriteTime:yyyy-MM-dd HH:mm:ss})");

            if (csvFiles.Count > 1)
            {
                Console.WriteLine($"  Other .csv files found ({csvFiles.Count - 1}):");
                foreach (var f in csvFiles.Skip(1))
                {
                    Console.WriteLine($"   • {f.Name} (last modified: {f.LastWriteTime:yyyy-MM-dd HH:mm:ss})");
                }
            }
        }

        Console.Write("\nEnter the column name in the CSV that contains the stream IDs: ");
        Console.WriteLine("  ↵  Press ENTER to automatically use the first column in the CSV.\n");
        Console.Write("Column name: ");
        string columnName = Console.ReadLine() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(columnName))
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            string headerLine = (await File.ReadAllLinesAsync(filePath)).FirstOrDefault() ?? string.Empty;
            columnName = headerLine.Split(',')[0].Trim();

            if (string.IsNullOrWhiteSpace(columnName))
            {
                Console.WriteLine("✗ Could not determine the first column name from the CSV. Exiting.");
                return;
            }

            Console.WriteLine($"✓ Auto-selected column: \"{columnName}\"");
        }

        Console.WriteLine("\nEnter stream prefix(es) to prepend to each CSV value (comma-separated):");
        Console.WriteLine("Example: Olive_DraftShipment_V3-,Orchid_ShipmentV2-\n");
        Console.Write("Prefix(es): ");
        string prefixInput = Console.ReadLine() ?? string.Empty;

        var streamPrefixes = prefixInput
            .Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (streamPrefixes.Count == 0)
        {
            Console.WriteLine("✗ No stream prefixes provided. Exiting.");
            return;
        }

        List<string> streamNames;
        try
        {
            streamNames = await ReadStreamNamesFromCsvAsync(fileName, columnName, streamPrefixes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to read CSV: {ex.Message}");
            return;
        }

        if (streamNames.Count == 0)
        {
            Console.WriteLine("✗ No stream names could be generated from the CSV. Exiting.");
            return;
        }

        Console.WriteLine($"\n✓ Generated {streamNames.Count} stream name(s) from CSV:");
        foreach (var name in streamNames.Take(5))
        {
            Console.WriteLine($"   • {name}");
        }
        if (streamNames.Count > 5)
        {
            Console.WriteLine($"   ... and {streamNames.Count - 5} more");
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
        }
    }

    /// <summary>
    /// Reads a single-column CSV file from the exe directory and generates stream names by
    /// combining each <paramref name="streamPrefixes"/> entry with each value found under
    /// <paramref name="columnName"/>.
    /// </summary>
    private static async Task<List<string>> ReadStreamNamesFromCsvAsync(
        string fileName,
        string columnName,
        IReadOnlyCollection<string> streamPrefixes
    )
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV file not found at: {filePath}");
        }

        string[] lines = await File.ReadAllLinesAsync(filePath);

        if (lines.Length == 0)
        {
            throw new InvalidOperationException("CSV file is empty.");
        }

        string[] headers = lines[0].Split(',');
        int columnIndex = Array.FindIndex(
            headers,
            h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase)
        );

        if (columnIndex == -1)
        {
            throw new InvalidOperationException(
                $"Column '{columnName}' not found. Available columns: {string.Join(", ", headers.Select(h => h.Trim()))}"
            );
        }

        var streamNames = new List<string>();

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] fields = line.Split(',');

            if (columnIndex >= fields.Length)
            {
                continue;
            }

            string value = fields[columnIndex].Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            foreach (var prefix in streamPrefixes)
            {
                streamNames.Add($"{prefix}{value}");
            }
        }

        return streamNames;
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
