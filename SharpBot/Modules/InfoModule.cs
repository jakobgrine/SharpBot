using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace SharpBot.Modules
{
    [Group("info"), RequireOwner]
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("guild")]
        public async Task GuildInfoAsync(IGuild guild)
        {
            var embed = new EmbedBuilder()
                .WithTitle(guild.Name)
                .WithThumbnailUrl(guild.IconUrl)
                .AddField("Identifier", guild.Id, true)
                .Build();
            await ReplyAsync(embed: embed);
        }

        [Command("user")]
        public async Task UserInfoAsync(IUser user)
        {
            var embed = new EmbedBuilder()
                .WithTitle(user.Username)
                .WithThumbnailUrl(user.GetAvatarUrl())
                .AddField("Identifier", user.Id, true)
                .Build();
            await ReplyAsync(embed: embed);
        }
    }
}
