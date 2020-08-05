using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Victoria;
using Victoria.Interfaces;

namespace SharpBot
{
    public static class Extensions
    {
        public static TimeSpan StripMilliseconds(this TimeSpan time) => new TimeSpan(time.Days, time.Hours, time.Minutes, time.Seconds);

        public static void Enqueue<T>(this DefaultQueue<T> queue, IEnumerable<T> ts) where T : IQueueable
        {
            foreach (var t in ts)
                queue.Enqueue(t);
        }

        public static async Task SendMessageAndDeleteAsync(this IMessageChannel channel, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, TimeSpan? after = null, IUserMessage alsoDelete = null)
        {
            var message = await channel.SendMessageAsync(text, isTTS, embed, options);
            _ = Task.Run(async () =>
              {
                  await Task.Delay(after ?? TimeSpan.FromSeconds(5));
                  await message.DeleteAsync();
                  if (alsoDelete != null)
                      await alsoDelete.DeleteAsync();
              });
        }

        public static Task LogMessage(this ILogger logger, LogMessage message)
        {
            var logLevel = message.Severity switch
            {
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                _ => LogLevel.Debug,
            };
            logger.Log(logLevel, message.ToString());
            return Task.CompletedTask;
        }

        public static void WaitForReaction(this IUserMessage message, SocketCommandContext context, IEmote emote, Func<Task> action)
        {
            async Task handler(Cacheable<IUserMessage, ulong> reactionMessage, ISocketMessageChannel channel, SocketReaction reaction)
            {
                var user = reaction.User.GetValueOrDefault() ?? context.Client.GetUser(reaction.UserId);

                if (user.IsBot || reactionMessage.Id != message.Id)
                    return;

                if (reaction.Emote.Name != emote.Name)
                    await message.RemoveReactionAsync(reaction.Emote, user);
                else
                {
                    await action();
                    context.Client.ReactionAdded -= handler;
                }
            }
            context.Client.ReactionAdded += handler;
        }

        public static EmbedBuilder ToEmbedBuilder(this Embed embed) => new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                IconUrl = embed.Author?.IconUrl,
                Name = embed.Author?.Name,
                Url = embed.Author?.Url
            },
            Color = embed.Color,
            Description = embed.Description,
            Fields = embed.Fields.Select(x => new EmbedFieldBuilder
            {
                IsInline = x.Inline,
                Name = x.Name,
                Value = x.Value
            }).ToList(),
            Footer = new EmbedFooterBuilder
            {
                IconUrl = embed.Footer?.IconUrl,
                Text = embed.Footer?.Text
            },
            ImageUrl = embed.Image?.Url,
            ThumbnailUrl = embed.Thumbnail?.Url,
            Timestamp = embed.Timestamp,
            Title = embed.Title,
            Url = embed.Url
        };

        public static string Truncate(this string value, int maxChars) => value.Length > maxChars ? value.Substring(0, maxChars - 3) + "..." : value;
    }
}
