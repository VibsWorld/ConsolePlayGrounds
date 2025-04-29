using System.Net;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Common.Fixtures.Email;

public sealed class SmtpFixture : IAsyncDisposable
{
    private const string DnsSmtp = "smtp4dev";
    private const string ImageSmtp = "rnwood/smtp4dev:v3";

    private readonly ushort portSmtp = 25;
    private readonly ushort imapSmtp = 143;
    private readonly ushort portHttpInterface = 80;

    private readonly INetwork dockerNetwork = new NetworkBuilder().Build();

    private readonly IContainer containerSmtp;

    public ushort PortSmtp => containerSmtp.GetMappedPublicPort(portSmtp);

    public ushort ImapSmtp => containerSmtp.GetMappedPublicPort(imapSmtp);

    public ushort PortHttpInterface => containerSmtp.GetMappedPublicPort(portHttpInterface);

    public SmtpFixture() => containerSmtp = BuildSmtpContainer();

    public async Task StartAsync() => await containerSmtp.StartAsync().ConfigureAwait(false);

    public async ValueTask DisposeAsync() => await containerSmtp.DisposeAsync();

    private IContainer BuildSmtpContainer() =>
        new ContainerBuilder()
            .WithNetwork(dockerNetwork)
            .WithHostname(DnsSmtp)
            .WithImage(ImageSmtp)
            .WithPortBinding(portHttpInterface)
            .WithPortBinding(PortSmtp)
            .WithPortBinding(imapSmtp)
            .WithEnvironment("ServerOptions__Urls", $"http://*:{portHttpInterface}")
            .WithEnvironment("ServerOptions__HostName", DnsSmtp)
            .WithEnvironment("ServerOptions__AuthenticationRequired", "true")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(portHttpInterface))
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(PortSmtp))
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request =>
                        request
                            .ForPort(portHttpInterface)
                            .ForPath("/")
                            .ForStatusCode(HttpStatusCode.OK)
                    )
            )
            .Build();
}
