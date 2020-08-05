using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SharpBot.Modules
{
    public class HelpModule : SharpModuleBase
    {
        private readonly IConfiguration _config;
        private readonly CommandService _commands;
        private readonly IServiceProvider _serviceProvider;

        public HelpModule(IConfiguration config, CommandService commands, IServiceProvider serviceProvider)
        {
            _config = config;
            _commands = commands;
            _serviceProvider = serviceProvider;
        }

        [Command("help")]
        [Alias("usage", "howto")]
        private async Task HelpAsync(string query = null)
        {
            var prefix = _config["prefix"];

            if (!string.IsNullOrWhiteSpace(query))
            {
                var result = _commands.Search(Context, query);
                if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.ErrorReason))
                {
                    await ReplyAndDeleteAsync($":x: {result.ErrorReason}");
                    return;
                }
                if (result.Commands.Count < 1)
                {
                    await ReplyAndDeleteAsync(":x: Unknown command.");
                    return;
                }
                else
                {
                    var match = result.Commands.First();
                    var command = match.Command;

                    var embed = new EmbedBuilder()
                        .WithTitle($"{prefix}{command.Name}")
                        .AddField("Signature", GetCommandSignature(command));
                    if (command.Aliases.Count > 2)
                        embed.AddField("Aliases", string.Join(", ", command.Aliases.Skip(1).Select(x => $"`{x}`")), true);
                    else if (command.Aliases.Count > 1)
                        embed.AddField("Alias", $"`{command.Aliases[1]}`", true);

                    await ReplyAndDeleteAsync(embed: embed.Build(), after: TimeSpan.FromSeconds(20));
                }
            }
            else
            {
                // TODO: List commands grouped by modules

                var commands = await _commands.GetExecutableCommandsAsync(Context, _serviceProvider);
                var description = string.Join(", ", commands.Select(x => $"`{prefix}{x.Name}`"));

                var embed = new EmbedBuilder()
                    .WithTitle("Help")
                    .WithDescription(description)
                    .WithFooter($"Type\u2002{prefix}help [command]\u2002for more info on a specific command.");

                await ReplyAndDeleteAsync(embed: embed.Build(), after: TimeSpan.FromSeconds(20));
            }
        }

        private string GetCommandSignature(CommandInfo command)
        {
            var prefix = _config["prefix"];
            var parameters = command.Parameters.Select(parameter =>
            {
                var s = parameter.Name;
                if (parameter.IsOptional)
                {
                    var defaultValue = parameter.DefaultValue != null ? $" = {parameter.DefaultValue}" : "";
                    s = $"[{s}{defaultValue}]";
                }
                return s;
            });
            return $"{prefix}{command.Name} {string.Join(" ", parameters)}";
        }
    }
}
