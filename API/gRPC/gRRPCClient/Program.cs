using Grpc.Net.Client;
using GrpcGreeterClient;

// See https://aka.ms/new-console-template for more information
using var channel = GrpcChannel.ForAddress("http://localhost:5002");
var client = new Greeter.GreeterClient(channel);
var reply = await client.SayHelloAsync(new HelloRequest { Name = "GreeterClient Vibs" });
Console.WriteLine("Greeting: " + reply.Message);
Console.WriteLine("Press any key to exit...");
Console.ReadKey();


//using var channel = GrpcChannel.ForAddress("https://localhost:7042");
//var client = new Greeter.GreeterClient(channel);
//var reply = await client.SayHelloAsync(
//                  new HelloRequest { Name = "GreeterClient" });
//Console.WriteLine("Greeting: " + reply.Message);
//Console.WriteLine("Press any key to exit...");
//Console.ReadKey();
