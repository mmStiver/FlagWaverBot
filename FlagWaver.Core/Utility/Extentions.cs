using FlagWaverBot.Models;

namespace FlagWaver.Core.Utility;

static internal class Extentions
{
    internal static IEnumerable<SlimComment> GrabMentions(this IEnumerable<SlimComment> sauce)
        => sauce.Where(comm => comm.Body.IndexOf("!wave", StringComparison.OrdinalIgnoreCase) >= 0 || comm.Body.IndexOf("!wavethis", StringComparison.OrdinalIgnoreCase) >= 0);
    internal static IEnumerable<SlimComment> GrabResponses(this IEnumerable<SlimComment> sauce, string userName)
        => sauce.Where(comm => string.Compare(comm.commenter, userName, true) == 0);

}
