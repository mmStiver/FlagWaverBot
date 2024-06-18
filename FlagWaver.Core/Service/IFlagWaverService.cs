using DiscuitSharp.Core.Auth;

namespace FlagWaver.Core.Service;

public interface IFlagWaverService
{
    Task<DiscuitUser?> DoAuthAsync(string username, string password, CancellationToken stoppingToken);
    Task DoWorkAsync(DiscuitUser user, CancellationToken stoppingToken);
}