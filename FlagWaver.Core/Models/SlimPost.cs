using DiscuitSharp.Core.Content;
using static DiscuitSharp.Core.Content.Post;

namespace FlagWaverBot.Models;

public class SlimPost
{
    public required string Title { get; init; }

    public required PostId Id { get; init; }

    public required Kind Type { get; init; }

    public required PublicPostId PublicId { get; init; }

    public required string Body { get; init; }
    public required DateTime CreateUpdate { get; init; }
    public required DateTime? LastUpdate { get; init; }
    public required int numComments { get; init; }
}
