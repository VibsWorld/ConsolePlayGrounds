namespace RedisPlay.Console1;

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Helpers.CSV;
using StackExchange.Redis;

public class Program
{
    static async Task Main(string[] args)
    {
        var options = new ConfigurationOptions
        {
            EndPoints = { "localhost" },
            Password = "",
            Ssl = false
        };

        //options.CertificateValidation += Options_CertificateValidation;

        var cache = ConnectionMultiplexer.Connect(options).GetDatabase();

        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CarrierStatuses.csv");
        ArgumentNullException.ThrowIfNull(filePath);

        var hashKeySample = "poplar:carrier_status_mappings";

        Console.WriteLine("Adding a string key with name = vibs");

        await cache.StringSetAsync("name", "vibs");

        var carrierStatuses = CSVHelperMethods.ReadCsvFromFilePath<CarrierStatuses>(filePath);

        if (carrierStatuses?.Count > 0)
        {
            foreach (var carrierStatus in carrierStatuses)
            {
                Console.WriteLine(
                    $"Adding a hashget with key - {string.Concat(carrierStatus.CarrierId, ":", carrierStatus.CarrierStatusCode)}"
                );

                Console.WriteLine(
                    $"\t Value - {carrierStatus.CarrierId} {carrierStatus.CarrierStatusCode} {carrierStatus.CarrierStatusCode}"
                );

                await cache.HashSetAsync(
                    hashKeySample,
                    new RedisValue(
                        string.Concat(carrierStatus.CarrierId, ":", carrierStatus.CarrierStatusCode)
                    ),
                    new RedisValue(System.Text.Json.JsonSerializer.Serialize(carrierStatus))
                );
            }
        }

        Console.WriteLine("\n");

        await GetAllEndpointsAndKeys(cache);

        Console.ReadLine();
    }

    /// <summary>
    /// Get All Endpoints and Keys
    /// </summary>
    /// <param name="cache"></param>
    /// <returns></returns>
    public static async Task GetAllEndpointsAndKeys(IDatabase cache)
    {
        var endPoints = cache.Multiplexer.GetEndPoints();
        int i = 1;
        foreach (var endPoint in endPoints)
        {
            Console.WriteLine(
                $"Fetching Endpoint No.{i++} - {endPoint} with Address Family as '{endPoint.AddressFamily}'"
            );
            var server = cache.Multiplexer.GetServer(endPoint);
            var keys = server.Keys();

            foreach (var keyVar in keys)
            {
                //var hashKeys = await cache.HashGetAllAsync(keyVar);
                var redisKeyType = await cache.KeyTypeAsync(keyVar);
                switch (redisKeyType)
                {
                    case RedisType.None:
                        break;
                    case RedisType.String:
                        Console.WriteLine("\t*******************");
                        Console.WriteLine("\tNow adding string get");
                        Console.WriteLine("\t" + await cache.StringGetAsync(keyVar));
                        Console.WriteLine("\t *******************");
                        break;
                    case RedisType.List:
                        break;
                    case RedisType.Set:
                        break;
                    case RedisType.SortedSet:
                        break;

                    case RedisType.Hash:
                        Console.WriteLine("\t*******************");
                        foreach (var hashget in await cache.HashGetAllAsync(keyVar))
                        {
                            Console.WriteLine($"\tNow adding hash value");

                            Console.WriteLine(
                                "\t" + await cache.HashGetAsync(keyVar, hashget.Name)
                            );
                        }
                        Console.WriteLine("\t*******************");
                        break;
                    case RedisType.Stream:
                        break;
                    case RedisType.Unknown:
                        break;
                    default:
                        break;
                }
            }
        }
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
