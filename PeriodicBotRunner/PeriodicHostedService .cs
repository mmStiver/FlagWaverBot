using DiscuitSharp.Core.Auth;
using FlagWaver.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PeriodicBotRunner.Options;

namespace PeriodicBotRunner;

internal class PeriodicHostedService : BackgroundService
{
    private readonly ILogger<PeriodicHostedService> _logger;
    public IServiceScopeFactory ServiceScopeFactory { get; }
    internal EnvCredential Credentials { get; }
    DiscuitUser? AuthenticatedUser;
    private int _executionCount;

    public PeriodicHostedService(ILogger<PeriodicHostedService> logger, IServiceScopeFactory serviceScopeFactory, IOptions<EnvCredential> creds)
    {
        _logger = logger;
        this.ServiceScopeFactory = serviceScopeFactory;
        this.Credentials = creds.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service running.");

        using PeriodicTimer timer = new(TimeSpan.FromMinutes(10));
        try
        {
            do
            {
               // await service.DoWorkAsync(stoppingToken);
                using (IServiceScope scope = ServiceScopeFactory.CreateScope())
                {
                    IFlagWaverService scopedProcessingService = scope.ServiceProvider.GetRequiredService<IFlagWaverService>();

                    if(AuthenticatedUser is null)
                        AuthenticatedUser = await scopedProcessingService.DoAuthAsync(this.Credentials.UserName, this.Credentials.Password, stoppingToken);

                    await scopedProcessingService.DoWorkAsync(AuthenticatedUser, stoppingToken);
                }

            } while (await timer.WaitForNextTickAsync(stoppingToken));

        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");
        }
    }
}