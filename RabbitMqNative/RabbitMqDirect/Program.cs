using System.Text;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Spectre.Console;

namespace RabbitMqDirect;

public class Program
{
    //https://www.rabbitmq.com/client-libraries/dotnet-api-guide
    //https://www.youtube.com/watch?v=-ZRnq8ke_bU&list=PLLWMQd6PeGY0IReztlVcGLVZk9mzc4Hvr&index=2
    static async Task Main(string[] args)
    {
        AnsiConsole.MarkupLine("[bold yellow]RabbitMQ Direct Message Reader[/]");
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        var config = builder.Build();

        var appSettings = config.Get<AppSettings>();

        foreach (var queue in appSettings.RabbitMqDirect.LastUsedQueues)
        {
            AnsiConsole.MarkupLine($"[green]Last Used Queue:[/] {queue}");
        }

        args = args.Length == 0 ? ["birch_incidents:close_incident"] : args;

        var factory = new ConnectionFactory()
        {
            HostName = "hefty-elk.rmq.cloudamqp.com",
            UserName = "vsakroma",
            Password = "8TVbUTfS3YDZv0RjvvS1S-H_uXEhSGzZ",
            VirtualHost = "ParcelVision.Retail",
        };
        var queueName = "birch_incidents:shipment_incident_comment_added_error";

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), $"TEST.txt");

        await File.WriteAllTextAsync(filePath, "Test");

        using (var connection = await factory.CreateConnectionAsync())
        using (var channel = await connection.CreateChannelAsync())
        {
            // Ensure the queue exists
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true, // Example durability
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            bool finished = false;

            StringBuilder sb = new StringBuilder();

            // --- Step 1 & 2: Consume/Get All Messages and Deserialize ---
            // We use BasicGet to pull messages one-by-one until the queue is empty
            while (!finished)
            {
                // BasicGet is synchronous and pulls a single message.
                BasicGetResult result = await channel.BasicGetAsync(queueName, autoAck: false);

                if (result is null)
                {
                    // No more messages in the queue
                    Console.WriteLine("No more messages in the queue.");
                    finished = true;
                }
                else
                {
                    try
                    {
                        var body = result.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);

                        Console.WriteLine("*************Message*************");
                        sb.AppendLine("*************************Message*************");
                        sb.Append(json ?? "Null object");
                        Console.WriteLine(json ?? "Null object");
                        sb.AppendLine("**************************");
                        Console.WriteLine("**************************");
                        // Deserialize the JSON string to your message object
                        //var message = JsonSerializer.Deserialize<MyMessage>(json);

                        //if (message != null)
                        //{
                        //    messages.Add(message);
                        //}

                        // Acknowledge the message *after* successfully processing it
                        //await channel.BasicAckAsync(result.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        // Log the error and optionally reject the message
                        Console.WriteLine($"Error processing message: {ex.Message}");
                        await channel.BasicNackAsync(
                            result.DeliveryTag,
                            multiple: false,
                            requeue: true
                        );
                    }
                }
            } //End of while loop

            filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                $"RabbitMqMessages_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            );

            await File.WriteAllTextAsync(filePath, sb.ToString());

            // --- Step 3 & 4: Store (already done) and Sort the Collection ---

            // Order the collected messages by CreatedTime in DESCENDING order
            // var sortedMessages = messages.OrderByDescending(m => m.CreatedTime).ToList();
        }
    }
}

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

public class AppSettings
{
    public Rabbitmqdirect RabbitMqDirect { get; set; }
}

public class Rabbitmqdirect
{
    public Connectionsettings ConnectionSettings { get; set; }
    public string[] LastUsedQueues { get; set; }
}

public class Connectionsettings
{
    public string HostName { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public int Port { get; set; }
    public string VirtualHost { get; set; }
}
