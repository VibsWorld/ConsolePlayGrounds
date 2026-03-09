namespace EventStore.SyncApp.Infrastructure;

public class AppConfiguration
{
    // Cloud EventStore (KurrentDb)
    public const string CloudEventStoreUrl =
        "esdb+discover://cd3fqd5o0aevmo9u2nfg-1.mesdb.eventstore.cloud:2113";
    public const string CloudEventStorePassword = "RlvUvweBR3gT6X32qoL1XAGKjraJRs3Z"; // Replace with valid password

    // Local EventStore (Docker)
    public const string LocalEventStoreUrl = "esdb://admin:changeit@localhost:2113?tls=false&tlsVerifyCert=false";
}