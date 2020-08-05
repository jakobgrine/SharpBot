using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Victoria;
using SharpBot.Services;

namespace SharpBot.Modules
{
    [Group("shutdown")]
    [RequireOwner]
    public class ShutdownModule : ModuleBase<SocketCommandContext>
    {
        private readonly LavaNode _lavaNode;
        private readonly AudioService _audioService;

        public ShutdownModule(LavaNode lavaNode, AudioService audioService)
        {
            _lavaNode = lavaNode;
            _audioService = audioService;
        }

        [Command]
        public async Task ShutdownAsync()
        {
            var message = await ReplyAsync("Do you really want to shut the bot down?");
            var emote = new Emoji("✅");
            await message.AddReactionAsync(emote);

            message.WaitForReaction(Context, emote, async () =>
            {
                await message.DeleteAsync();
                await Context.Message.DeleteAsync();
                await Context.Client.StopAsync();

                await _audioService.Dispose();
                if (_lavaNode.IsConnected)
                    await _lavaNode.DisconnectAsync();
            });
        }

        [Command("force")]
        [Alias("now")]
        public async Task ForceShutdownAsync()
        {
            await Context.Message.DeleteAsync();
            await Context.Client.StopAsync();

            await _audioService.Dispose();
            if (_lavaNode.IsConnected)
                await _lavaNode.DisconnectAsync();
        }
    }
}
