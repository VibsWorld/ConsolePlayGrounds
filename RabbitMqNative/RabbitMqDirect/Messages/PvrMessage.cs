namespace RabbitMqDirect.Messages;

#pragma warning disable IDE1006 // Naming Styles
public class PvrMessage
{
    public string messageId { get; set; }
    public string correlationId { get; set; }
    public string conversationId { get; set; }

    public string sourceAddress { get; set; }
    public string destinationAddress { get; set; }
    public string[] messageType { get; set; }
    public Message message { get; set; }
    public DateTime sentTime { get; set; }
    public Headers headers { get; set; }
    public Host host { get; set; }
}

public class Message
{
    public string correlationId { get; set; }
    public string date { get; set; }
    public string time { get; set; }
    public string zone { get; set; }
    public string id { get; set; }
    public DateTime timeStamp { get; set; }
    public string causationId { get; set; }
}

public class Headers
{
    public string MTActivityId { get; set; }
}

public class Host
{
    public string machineName { get; set; }
    public string processName { get; set; }
    public int processId { get; set; }
    public string assembly { get; set; }
    public string assemblyVersion { get; set; }
    public string frameworkVersion { get; set; }
    public string massTransitVersion { get; set; }
    public string operatingSystemVersion { get; set; }
}
#pragma warning restore IDE1006 // Naming Styles
