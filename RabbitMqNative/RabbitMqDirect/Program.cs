using System.Text;
using System.Text.Json;
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
        args = args.Length == 0 ? ["birch_application:five_minutes_ended_error"] : args;
        var queueName = args[0];
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

        var factory = new ConnectionFactory()
        {
            HostName = "localhost",
            UserName = "pvRetailDev",
            Password = "pvRetailDev",
            VirtualHost = "ParcelVision.Retail",
        };

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
                        var rss = (byte[])result.BasicProperties.Headers["MT-Fault-Message"];
                        string text = Encoding.UTF8.GetString(rss);
                        var body = result.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);

                        var pvrMessage = JsonSerializer.Deserialize<RabbitMqMessage>(json);

                        if (pvrMessage?.message is not null)
                        {
                            //Console.WriteLine(pvrMessage.message + "," + text);

                            var mss = pvrMessage.message;
                            var mss1 = mss.GetType();

                            var incident = JsonSerializer.Deserialize<SystemShipmentIncidentRaised>(
                                mss
                            );
                            Console.WriteLine(
                                "'" + incident.shipmentId + "'" /*+ "," + text*/
                            );
                            Console.WriteLine(",");
                            //var message = JsonSerializer.Serialize<SystemShipmentIncidentRaised>()

                            //Console.WriteLine("*************Message*************");
                            //sb.AppendLine("*************************Message*************");
                            //sb.Append(json ?? "Null object");
                            //Console.WriteLine(json ?? "Null object");
                            //sb.AppendLine("**************************");
                            //Console.WriteLine("**************************");
                        }

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
                $"RabbitMqMessages_birch_application:basket_paid_error.txt"
            );

            await File.WriteAllTextAsync(filePath, sb.ToString());

            // --- Step 3 & 4: Store (already done) and Sort the Collection ---

            // Order the collected messages by CreatedTime in DESCENDING order
            // var sortedMessages = messages.OrderByDescending(m => m.CreatedTime).ToList();
        }
    }
}

#pragma warning disable IDE1006 // Naming Styles
public class RabbitMqMessage
{
    public string messageId { get; set; }
    public string correlationId { get; set; }
    public string conversationId { get; set; }

    public string sourceAddress { get; set; }
    public string destinationAddress { get; set; }
    public string[] messageType { get; set; }
    public JsonElement message { get; set; }
    public DateTime sentTime { get; set; }
    public Headers headers { get; set; }
    public Host host { get; set; }
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

public class SystemShipmentIncidentRaised
{
    public string incidentId { get; set; }
    public string shipmentId { get; set; }
    public string customerId { get; set; }
    public string threePlId { get; set; }
    public string query { get; set; }
    public string type { get; set; }
    public string subject { get; set; }
    public string message { get; set; }
    public int version { get; set; }
    public string id { get; set; }
    public DateTime timeStamp { get; set; }
    public string correlationId { get; set; }
    public string causationId { get; set; }
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
