using DiscuitSharp.Core;
using DiscuitSharp.Core.Content;
using DiscuitSharp.Core.Group;
using FlagWaverBot.Models;


namespace FlagWaver.Core.Repository;

public class DiscuitRepository
{
    private readonly IDiscuitClient client;

    public DiscuitRepository(IDiscuitClient client) {
        this.client = client;
        }

    public async IAsyncEnumerable<IEnumerable<SlimPost>> PagedPosts(CommunityId commId, DateTime dateFrom, CancellationToken cancellationToken)
    {
        CursorIndex? cursor = null;
        bool fetchNextPage = true;

        while (fetchNextPage && !cancellationToken.IsCancellationRequested)
        {
            var postsCursor = await client.GetPosts(commId, cursor, Token: cancellationToken);

            if (postsCursor == null || postsCursor.Records == null || postsCursor.Records.Count == 0)
            {
                yield break;
            }

            yield return postsCursor.Records.Select(p => Map(p));

            fetchNextPage = postsCursor.Records.Any(post => post.EditedAt >= dateFrom);

            if (!fetchNextPage) // Stop if we find a post older than the cutoff date
                yield break;

            if (string.IsNullOrEmpty(postsCursor.Next))
                yield break;
            
            cursor = new CursorIndex { Value = postsCursor.Next };
        }
    }

    public async IAsyncEnumerable<IEnumerable<SlimComment>> PagedComments(PublicPostId postId, CancellationToken cancellationToken)
    {
        CursorIndex? cursor = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            var commentsCursor = await client.GetComments(postId, cursor, Token: cancellationToken);

            if (commentsCursor == null || commentsCursor.Records == null || commentsCursor.Records.Count == 0)
            {
                yield break;
            }

            yield return commentsCursor.Records.Select(p => Map(postId, p));

            if (string.IsNullOrEmpty(commentsCursor.Next))
                yield break;

            cursor = new CursorIndex { Value = commentsCursor.Next };
        }
    }

    private SlimPost Map(Post post)
        => new SlimPost(){ Id= post.Id!.Value, 
                PublicId = post.PublicId!.Value, 
                Title = post.Title ?? string.Empty,
                CreateUpdate = post.CreatedAt,
                LastUpdate = (post.EditedAt.HasValue) ? post.EditedAt.Value : null,
                Body = post switch
                {
                    TextPost tp => tp.Body ?? string.Empty,
                    LinkPost lp => lp?.Link?.Url ?? string.Empty,
                    ImagePost ip => ip?.Image?.Url ?? string.Empty,
                    _ => throw new InvalidOperationException()
                },
                Type = post switch
                {
                    TextPost tp =>  Post.Kind.Text,
                    LinkPost lp =>  Post.Kind.Link,
                    ImagePost ip => Post.Kind.Image,
                    _ => throw new InvalidOperationException()
                },
                numComments = post.NoComments
        };
    private SlimComment Map(PublicPostId Id, Comment comment)
        => new SlimComment()
        {
            Id = comment.Id!.Value,
            postId = Id,
            parentId = comment.ParentId,
            commenter = comment.Username ?? String.Empty,
            CreateUpdate = comment.CreatedAt,
            LastUpdate = (comment.EditedAt.HasValue) ? comment.EditedAt.Value : null,
            Body = comment.Body
        };
}
