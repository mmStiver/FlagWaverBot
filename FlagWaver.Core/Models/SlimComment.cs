using DiscuitSharp.Core;
using DiscuitSharp.Core.Content;

namespace FlagWaverBot.Models;

public class SlimComment
{
    public required CommentId Id { get; init; }
    public  PublicPostId postId { get; init; }
    public  CommentId? parentId { get; init; }
    public required string commenter { get; init; }
    public required string Body { get; init; }
    public required DateTime CreateUpdate { get; init; }
    public DateTime? LastUpdate { get; init; }
}
