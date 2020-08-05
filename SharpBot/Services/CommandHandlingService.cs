using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpBot.TypeReaders;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace SharpBot.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _provider;
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        public CommandHandlingService(CommandService commands, DiscordSocketClient discord, IServiceProvider provider, IConfiguration config, ILogger<CommandHandlingService> log)
        {
            _commands = commands;
            _discord = discord;
            _provider = provider;
            _config = config;
            _logger = log;

            _commands.CommandExecuted += CommandExecutedAsync;
            _discord.MessageReceived += MessageReceivedAsync;

            _commands.AddTypeReader<IGuild>(new GuildTypeReader<SocketGuild>());
            _commands.AddTypeReader<Uri>(new UriTypeReader());
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }

        private async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message))
                return;
            if (message.Source != MessageSource.User)
                return;

            var prefixLength = 0;
            if (!message.HasStringPrefix(_config["prefix"], ref prefixLength))
                return;

            var context = new SocketCommandContext(_discord, message);
            await _commands.ExecuteAsync(context, prefixLength, _provider);
        }

        private async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            var commandName = command.IsSpecified ? command.Value.Name : "A command";
            await _logger.LogMessage(new LogMessage(LogSeverity.Info,
                "CommandExecution",
                $"{commandName} was executed at {DateTime.UtcNow}."));

            if (!string.IsNullOrEmpty(result?.ErrorReason))
                await context.Channel.SendMessageAndDeleteAsync($":x: {result.ErrorReason}", alsoDelete: context.Message);
        }
    }
}
