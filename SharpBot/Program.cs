using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpBot.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Victoria;

namespace SharpBot
{
    class Program
    {
        private readonly IConfiguration _config;
        private readonly CommandHandlingService _commands;
        private readonly DiscordSocketClient _client;

        static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        public Program()
        {
            _config = BuildConfig();

            var collection = new ServiceCollection();
            ConfigureServices(collection);
            var services = collection.BuildServiceProvider();

            _client = services.GetRequiredService<DiscordSocketClient>();
            _commands = services.GetRequiredService<CommandHandlingService>();

            var logger = services.GetRequiredService<ILogger<Program>>();
            _client.Log += logger.LogMessage;
            services.GetRequiredService<LavaNode>().OnLog += logger.LogMessage;
            services.GetRequiredService<CommandService>().Log += logger.LogMessage;

            services.GetRequiredService<LavaConfig>().LogSeverity = LogSeverity.Info;
        }

        private async Task MainAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _config["discord:token"]);
            await _client.StartAsync();

            await _commands.InitializeAsync();

            await Task.Delay(Timeout.Infinite);
        }

        private void ConfigureServices(IServiceCollection serviceCollection)
        {
            var lavalinkConfig = _config.GetSection("lavalink");

            // Base
            serviceCollection.AddSingleton<DiscordSocketClient>();
            serviceCollection.AddSingleton<CommandService>();
            serviceCollection.AddSingleton<CommandHandlingService>();
            // Audio
            serviceCollection.AddSingleton<LavaNode>();
            serviceCollection.AddSingleton(new LavaConfig
            {
                Authorization = lavalinkConfig["password"],
                Hostname = lavalinkConfig["hostname"],
                Port = ushort.Parse(lavalinkConfig["port"]),
                SelfDeaf = bool.Parse(lavalinkConfig["self_deaf"]),
                ReconnectAttempts = int.Parse(lavalinkConfig["reconnect:attempts"]),
                ReconnectDelay = TimeSpan.FromSeconds(int.Parse(lavalinkConfig["reconnect:delay"]))
            });
            serviceCollection.AddSingleton<AudioService>();
            // Logging
            serviceCollection.AddLogging(ConfigureLogging);
            // Configuration
            serviceCollection.AddSingleton(_config);
            serviceCollection.BuildServiceProvider();
        }

        private void ConfigureLogging(ILoggingBuilder builder)
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        }

        private IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build();
        }
    }
}
