using DiscuitSharp.Core.Content;
using DiscuitSharp.Core.Group;
using FlagWaverBot.Models;
using FlagWaverBot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.RegularExpressions;
using FlagWaver.Core.Repository;
using FlagWaver.Core.Utility;
using Microsoft.Extensions.Options;
using DiscuitSharp.Core.Auth;
using DiscuitSharp.Core;
using System.Net.Http;

namespace FlagWaver.Core.Service;

public sealed class FlagWaverService
    : IFlagWaverService
{
    Channel<(PublicPostId, Comment)> msgQueue = Channel.CreateBounded<(PublicPostId, Comment)>(10);
    DiscuitClient Client { get; }
    DiscuitRepository repo { get; }
    ILogger<IFlagWaverService> Logger { get; }

    public FlagWaverService(ILogger<IFlagWaverService> logger, IHttpClientFactory fac) 
    {
        var httpClient = new HttpClient(new HttpClientHandler() { CookieContainer = new CookieContainer() })
        {
            BaseAddress = new Uri("https://discuit.net/api/")
        };
        this.Client = new (httpClient);
        this.repo = new DiscuitRepository(Client);
        this.Logger = logger;
    }

    public async Task DoWorkAsync(DiscuitUser user, CancellationToken token)
    {
        DateTime myState = DateTime.Now;

        myState = DateTime.Now;
        Environment.SetEnvironmentVariable("FLAG_BOG_LASTRUN", myState.ToLongDateString());

        var retrievedState = Environment.GetEnvironmentVariable("FLAG_BOG_LASTRUN");
        if (string.IsNullOrEmpty(retrievedState))
        {
            myState = (DateTime.TryParse(retrievedState, out DateTime dt) ? dt : DateTime.Now);
        }
        Console.WriteLine($"Retrieved state: {retrievedState}");

        //Start processing task
        var queueTask = ProcessComments(msgQueue, token);
        List<Comment> pendingComments = new();
        await foreach (var page in repo.PagedPosts(new CommunityId(Constants.vexillologyId), myState, token))
        {
            foreach (var post in page)
            {
                if (post.numComments == 0) continue;

                IEnumerable<SlimComment> comments = await GetAllComments(post.PublicId, token);

                var mentions = comments.GrabMentions()
                    .Where(c => !comments.GrabResponses(user.Username ?? string.Empty)
                    .Select(r => r.parentId).Contains(c.Id));

                if (mentions.Count() == 0) continue;

                foreach (var m in mentions)
                {
                    if (m.Body.IndexOf("!wavethis", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        SendFromCommentMention(post.PublicId, m);
                    }
                    else
                     if (m.Body.IndexOf("!wave", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        SendFromPostMention(post, m);
                    }
                }

            }
            msgQueue.Writer.Complete();
            await queueTask;
        }

    }

    void SendFromCommentMention(PublicPostId postId, SlimComment parent)
        {
            string[] postLinks = ExtractLinks(parent.Body).ToArray();
            var body = GenerateBody(postLinks);

            var newComment = new Comment(parent.Id, body);
            msgQueue.Writer.TryWrite((postId, newComment));
        }
    
    void SendFromPostMention(SlimPost post, SlimComment parent)
        {
            string[] postLinks = post.Type switch
            {
                Post.Kind.Text => GetLinksFromText(post.Body),
                Post.Kind.Image => new string[1] { "https://discuit.net" + post.Body },
                Post.Kind.Link => new string[1] { GetLink(post.Body) },
                _ => new string[0]
            };

            var body = GenerateBody(postLinks);

            var newComment = new Comment(parent.Id, body);
            msgQueue.Writer.TryWrite((post.PublicId, newComment));
        }
    
    async Task ProcessComments(ChannelReader<(PublicPostId, Comment)> channel, CancellationToken token = default)
        {
            while (await channel.WaitToReadAsync(token))
            {
                if (channel.TryRead(out var msg))
                {
                    (PublicPostId postId, Comment comment) = msg;
                    var res = await Client.Create(postId, comment.ParentId!.Value, new Comment(comment.ParentId!.Value, comment.Body));

                }
                await Task.Delay(400);
            }

        }
    
    
    string GenerateBody(string[] links)
        {
            StringBuilder sb = new("Here you go:");
            sb.AppendLine();
            sb.AppendLine();
            ;
            for (int i = 0; i < links.Length; i++)
            {
                sb.AppendLine($"link #{i + 1}: [Image]({GenFlagLink(links[i])})");
            }
            sb.AppendLine();
            sb.AppendLine("-----------");
            sb.AppendLine();
            sb.AppendLine("Beep Boop I'm a bot. [About](https://github.com/mmStiver/FlagWaverBot). Maintained by mmStiver");
            return sb.ToString();
        }
    
    string GetLink(string body)
            => IsImageLink(body) ? body : string.Empty;
    
    
    string[] GetLinksFromText(string body)
        {
            // Regular expression to find all <a> tags and extract href attributes
            string pattern = @"<a\s+(?:[^>]*?\s+)?href=[""']([^""']+)[""']";
            List<string> links = new List<string>();

            // Use Regex to find matches
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            MatchCollection matches = regex.Matches(body);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)  // Ensure there is at least one group for the URL
                {
                    // Add the link to the list
                    links.Add(match.Groups[1].Value);
                }
            }

            return links.ToArray();
        }
    
    async Task<IEnumerable<SlimComment>> GetAllComments(PublicPostId id, CancellationToken ct)
        {
            List<SlimComment> parents = new();
            await foreach (var page in repo.PagedComments(id, ct))
            {
                parents.AddRange(page);
            }
            return parents;
        }
    
    string GenFlagLink(string value)
        => "https://krikienoid.github.io/flagwaver/#?src=" +
                     WebUtility.UrlEncode($"{value}")
                     ;
    List<string> ExtractLinks(string markdownText)
        {
            // Regex pattern to match Markdown links
            string pattern = @"\[(.*?)\]\((.*?)\)";
            List<string> links = new List<string>();

            foreach (Match match in Regex.Matches(markdownText, pattern))
            {
                string url = match.Groups[2].Value;
                if (IsImageLink(url))
                {
                    links.Add(url);
                }
            }

            return links;
        }
    
    bool IsImageLink(string url)
        {
            string[] imageExtensions = { ".jpeg", ".jpg", ".png", ".gif", ".bmp", ".svg" };
            foreach (var ext in imageExtensions)
            {
                if (url.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

    public async Task<DiscuitUser?> DoAuthAsync(string un, string pwd, CancellationToken stoppingToken)
    {
          _ = await Client.GetInitial();
        return await Client.Authenticate(new Credentials(un, pwd));

    }
}
