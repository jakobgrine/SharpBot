using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SharpBot.Modules
{
    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        [Group("clean")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public class CleanModule : ModuleBase<SocketCommandContext>
        {
            [Command]
            public async Task CleanAllMessagesAsync(int limit = 100)
            {
                var textChannel = Context.Guild.GetTextChannel(Context.Channel.Id);
                var messages = (await textChannel.GetMessagesAsync(limit).FlattenAsync())
                    .Where(m => DateTimeOffset.Now - m.CreatedAt < TimeSpan.FromDays(14));
                await textChannel.DeleteMessagesAsync(messages);
            }

            [Command("user")]
            public async Task CleanMessagesByUserAsync(IUser user, int limit = 100)
            {
                var textChannel = Context.Guild.GetTextChannel(Context.Channel.Id);
                var messages = new List<IMessage>();
                await foreach (var messageBatch in textChannel.GetMessagesAsync(limit))
                    messages.AddRange(from m in messageBatch where m.Author == user && DateTimeOffset.Now - m.CreatedAt < TimeSpan.FromDays(14) select m);
                await textChannel.DeleteMessagesAsync(messages);
            }
        }
    }
}
