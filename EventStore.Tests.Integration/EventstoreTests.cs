//https://github.com/vaibhavPH/EventStoreSamples/blob/main/Quickstart/Dotnet/esdb-sample-dotnet/Program.cs
//https://developers.eventstore.com/clients/grpc/getting-started.html#connection-string
//https://docs.kurrent.io/clients/grpc/reading-events.html
//string connectionString = $"esdb://admin:changeit@localhost:2113?tls=false&tlsVerifyCert=false";

using System.Collections.Concurrent;
using System.Text.Json;
using AutoFixture;
using EventStore.Application.Events;
using EventStore.Client;
using FluentAssertions;
using Testcontainers.EventStoreDb;
using Xunit.Abstractions;

namespace EventStore.Tests.Integration
{
    public class EventstoreTests : IAsyncLifetime, IClassFixture<EventstoreFixture>
    {
        private readonly string eventstoreImageName = "eventstore/eventstore:23.10.0-bookworm-slim";
        private readonly EventStoreDbContainer eventStoreContainer;
        private readonly Fixture fixture = new();
        private readonly ITestOutputHelper testOutputHelper;
        private readonly ConcurrentBag<string> listEventStreams = [];

        public EventstoreTests(ITestOutputHelper testOutputHelper)
        {
            eventStoreContainer = new EventStoreDbBuilder()
                .WithImage(eventstoreImageName)
                .WithPortBinding(EventStoreDbBuilder.EventStoreDbPort, true)
                .WithEnvironment("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP", "true")
                .Build();
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task TestEventStoreIntegration_ShouldBeSuccessful()
        {
            //Arrange
            string eventstream = $"TestStream-{Guid.NewGuid()}";
            int totalEventsCount = 15;
            await eventStoreContainer.StartAsync();
            ushort eventstorePort = eventStoreContainer.GetMappedPublicPort(
                EventStoreDbBuilder.EventStoreDbPort
            );
            testOutputHelper.WriteLine($"Eventstore Port is {eventstorePort}");
            string connectionString =
                $"esdb://localhost:{eventstorePort}?tls=false&tlsVerifyCert=false";
            var settings = EventStoreClientSettings.Create(connectionString);
            settings.OperationOptions.ThrowOnAppendFailure = false;
            await using var eventStoreClient = new EventStoreClient(settings);
            var testEvents = fixture.CreateMany<TestEventCreated>(totalEventsCount);

            //Act
            await Parallel.ForEachAsync(
                testEvents,
                async (x, _) =>
                {
                    await AppendDataToEventstoreStream(x, eventStoreClient, eventstream);
                }
            );

            //Assert
            var readStreamResult = eventStoreClient.ReadStreamAsync(
                Direction.Forwards,
                eventstream,
                StreamPosition.Start
            );

            var eventStream = await readStreamResult.ToListAsync();

            var eventsCreated = eventStream
                .Select(re => JsonSerializer.Deserialize<TestEventCreated>(re.Event.Data.ToArray()))
                .ToArray();

            testOutputHelper.WriteLine(
                System.Text.Json.JsonSerializer.Serialize(
                    eventsCreated,
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );

            eventsCreated.Length.Should().Be(totalEventsCount);
        }

        private async Task AppendDataToEventstoreStream(
            TestEventCreated x,
            EventStoreClient eventStoreClient,
            string eventstream
        )
        {
            var eventData = new EventData(
                Uuid.NewUuid(),
                nameof(TestEventCreated),
                JsonSerializer.SerializeToUtf8Bytes(x)
            );

            await eventStoreClient.AppendToStreamAsync(eventstream, StreamState.Any, [eventData]);

            listEventStreams.Add(eventstream);
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            foreach (var stream in listEventStreams) { }
        }
    }
}
