using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace SharpBot.Modules
{
    public class SharpModuleBase : ModuleBase<SocketCommandContext>
    {
        protected async Task ReplyAndDeleteAsync(string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, TimeSpan? after = null, bool alsoDeleteUserMessage = true)
        {
            var message = await Context.Channel.SendMessageAsync(text, isTTS, embed, options);
            _ = Task.Run(async () =>
            {
                await Task.Delay(after ?? TimeSpan.FromSeconds(5));
                await message.DeleteAsync();
                if (alsoDeleteUserMessage)
                    await Context.Message.DeleteAsync();
            });
        }
    }
}
