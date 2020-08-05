using Discord.Commands;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SharpBot.Modules
{
    public class FunModule : SharpModuleBase
    {
        private readonly string[] numberEmojis =
            {
                ":one:",
                ":two:",
                ":three:",
                ":four:",
                ":five:",
                ":six:",
                ":seven:",
                ":eight:",
                ":nine:"
            };

        [Command("ping")]
        public async Task PingAsync() => await ReplyAndDeleteAsync("Pong!");

        [Command("dice")]
        [Alias("roll")]
        public async Task DiceAsync(int max = 6)
        {
            var rand = (new Random()).Next(max);
            if (rand < 9)
                await ReplyAndDeleteAsync($"You rolled a {numberEmojis[rand]}.");
            else
                await ReplyAndDeleteAsync($"You rolled a {rand}.");
        }
    }
}
