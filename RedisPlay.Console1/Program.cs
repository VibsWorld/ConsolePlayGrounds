namespace RedisPlay.Console1;

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Helpers.CSV;
using StackExchange.Redis;

public class Program
{
    static void Main(string[] args)
    {
        var options = new ConfigurationOptions
        {
            EndPoints = { "" },
            Password = "",
            Ssl = true
        };

        //options.CertificateValidation += Options_CertificateValidation;

        var cache = ConnectionMultiplexer.Connect(options).GetDatabase();

        var endPoint = cache.Multiplexer.GetEndPoints().First();
        var server = cache.Multiplexer.GetServer(endpoint: endPoint);
        var keys = server.Keys();

        foreach (var keyVar in keys)
        {
            Console.WriteLine(keyVar);
        }

        Console.ReadLine();

        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CarrierStatuses.csv");
        ArgumentNullException.ThrowIfNull(filePath);

        var carrierStatuses = CSVHelperMethods.ReadCsvFromFilePath<CarrierStatuses>(filePath);
        var key = "poplar:carrier_status_mappings";

        Console.WriteLine("Keys are");

        if (carrierStatuses?.Count > 0)
        {
            foreach (var carrierStatus in carrierStatuses)
            {
                Console.WriteLine(
                    $"Adding {carrierStatus.CarrierId} - {carrierStatus.CarrierStatusCode} - {carrierStatus.CarrierStatus}"
                );

                //cache.HashSet(
                //    key,
                //    new RedisValue(
                //        string.Concat(carrierStatus.CarrierId, ":", carrierStatus.CarrierStatusCode)
                //    ),
                //    new RedisValue(System.Text.Json.JsonSerializer.Serialize(carrierStatus))
                //);
            }
        }

        Console.ReadLine();
    }

    private static bool Options_CertificateValidation(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        System.Net.Security.SslPolicyErrors sslPolicyErrors
    )
    {
        return true;
        //if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.RemoteCertificateChainErrors)
        //{
        //    // check that the untrusted ca is in the chain
        //    return true;
        //}
        //return false;
    }
}
