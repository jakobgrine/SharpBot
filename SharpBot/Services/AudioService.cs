using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;

namespace SharpBot.Services
{
    public class AudioService
    {
        private const string PlayPauseEmoji = "⏯️";
        private const string SkipEmoji = "⏭";
        private const string StopEmoji = "⏹️";
        private const string RepeatOneEmoji = "🔂";
        private const string LyricsEmoji = "📜";

        private readonly DiscordSocketClient _client;
        private readonly LavaNode _lavaNode;
        private readonly ILogger _logger;
        //public readonly HashSet<ulong> VotedUsers = new HashSet<ulong>();
        private readonly Dictionary<ulong, IUserMessage> _nowPlayingMessages = new Dictionary<ulong, IUserMessage>();
        private readonly Dictionary<ulong, bool> _repeat = new Dictionary<ulong, bool>();
        private readonly IEmote[] _musicControlEmojis = new[] {
            new Emoji(PlayPauseEmoji),
            new Emoji(SkipEmoji),
            new Emoji(StopEmoji),
            new Emoji(RepeatOneEmoji),
            new Emoji(LyricsEmoji)
        };

        public AudioService(DiscordSocketClient client, LavaNode lavaNode, ILogger<AudioService> log)
        {
            _client = client;
            _lavaNode = lavaNode;
            _logger = log;

            _client.Ready += ReadyAsync;
            _client.ReactionAdded += ReactionAddedAsync;

            _lavaNode.OnLog += _logger.LogMessage;

            _lavaNode.OnPlayerUpdated += PlayerUpdatedAsync;
            //_lavaNode.OnStatsReceived += StatsReceivedAsync;

            _lavaNode.OnTrackStarted += TrackStartedAsync;
            _lavaNode.OnTrackEnded += TrackEndedAsync;
            _lavaNode.OnTrackStuck += TrackStuckAsync;
            _lavaNode.OnTrackException += TrackExceptionAsync;
            _lavaNode.OnWebSocketClosed += WebSocketClosedAsync;
        }

        public async Task<bool> ToggleRepeat(IGuild guild)
        {
            _repeat[guild.Id] = !_repeat.GetValueOrDefault(guild.Id);

            if (!_nowPlayingMessages.TryGetValue(guild.Id, out var message))
                return _repeat[guild.Id];
            await message.ModifyAsync(m =>
            {
                var embed = message.Embeds.FirstOrDefault();
                if (embed == null)
                    return;

                var builder = embed.ToEmbedBuilder();
                if (builder.Fields.Any(x => x.Name == "Repeat"))
                    builder.Fields = builder.Fields.Where(x => x.Name != "Repeat").ToList();
                else
                    builder.AddField("Repeat", "Enabled", true);

                m.Embed = builder.Build();
            });

            return _repeat[guild.Id];
        }

        private async Task ReadyAsync()
        {
            if (!_lavaNode.IsConnected)
                await _lavaNode.ConnectAsync();
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> reactionMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!(channel is IGuildChannel guildChannel))
                return;
            var guild = guildChannel.Guild;
            if (!_nowPlayingMessages.TryGetValue(guild.Id, out var message) || message.Id != reactionMessage.Id)
                return;

            var user = reaction.User.GetValueOrDefault() ?? _client.GetUser(reaction.UserId);
            if (user.IsBot)
                return;

            await message.RemoveReactionAsync(reaction.Emote, user);

            if (!_lavaNode.TryGetPlayer(guild, out var player))
                return;

            if (reaction.Emote.Name == PlayPauseEmoji)
            {
                if (player.PlayerState == PlayerState.Playing)
                    await player.PauseAsync();
                else if (player.PlayerState == PlayerState.Paused)
                    await player.ResumeAsync();
            }
            else if (reaction.Emote.Name == SkipEmoji && player.PlayerState == PlayerState.Playing && player.Queue.Count > 0)
                await player.SkipAsync();
            else if (reaction.Emote.Name == StopEmoji && (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused))
                await player.StopAsync();
            else if (reaction.Emote.Name == RepeatOneEmoji && (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused))
                await ToggleRepeat(guild);
            else if (reaction.Emote.Name == LyricsEmoji)
            {
                var lyrics = await player.Track.FetchLyricsFromGeniusAsync();
                if (string.IsNullOrWhiteSpace(lyrics))
                    lyrics = await player.Track.FetchLyricsFromOVHAsync();

                if (string.IsNullOrWhiteSpace(lyrics))
                    return;
                lyrics = lyrics.Truncate(1018);

                await message.ModifyAsync(m =>
                {
                    var embed = message.Embeds.FirstOrDefault();
                    if (embed == null)
                        return;

                    var builder = embed.ToEmbedBuilder();
                    if (builder.Fields.Any(x => x.Name == "Lyrics"))
                        builder.Fields = builder.Fields.Where(x => x.Name != "Lyrics").ToList();
                    else
                        builder.AddField("Lyrics", $"```{lyrics}```");

                    m.Embed = builder.Build();
                });
            }
        }

        private async Task PlayerUpdatedAsync(PlayerUpdateEventArgs arg)
        {
            await _logger.LogMessage(new LogMessage(LogSeverity.Info,
                "AudioManager",
                $"Player update received for {arg.Player.VoiceChannel.Name}."));
        }

        //private async Task StatsReceivedAsync(StatsEventArgs arg) => await _logger.LogMessage(new LogMessage(LogSeverity.Info,
        //    "AudioManager",
        //    $"Lavalink Uptime {arg.Uptime}."));

        private async Task TrackStartedAsync(TrackStartEventArgs arg)
        {
            await _logger.LogMessage(new LogMessage(LogSeverity.Info,
                "AudioManager",
                $"Track \"{arg.Track.Title}\" started in \"{arg.Player.VoiceChannel.Name}\" on {arg.Player.VoiceChannel.Guild.Name}\"."));

            var artwork = await arg.Track.FetchArtworkAsync();
            var embed = new EmbedBuilder()
                .WithTitle("Now Playing")
                .WithDescription($"[{arg.Track.Title}]({arg.Track.Url})")
                .WithThumbnailUrl(artwork)
                .AddField("Duration", $"`{arg.Track.Duration.StripMilliseconds()}`", true);

            if (_nowPlayingMessages.ContainsKey(arg.Player.TextChannel.Guild.Id))
            {
                var message = _nowPlayingMessages[arg.Player.TextChannel.Guild.Id];
                await message.ModifyAsync(x => x.Embed = embed.Build());
            }
            else
            {
                var message = await arg.Player.TextChannel.SendMessageAsync(embed: embed.Build());
                await message.AddReactionsAsync(_musicControlEmojis);
                _nowPlayingMessages[arg.Player.TextChannel.Guild.Id] = message;
            }
        }

        private async Task TrackEndedAsync(TrackEndedEventArgs arg)
        {
            await _logger.LogMessage(new LogMessage(LogSeverity.Info,
                "AudioManager",
                $"Track \"{arg.Track.Title}\" ended in \"{arg.Player.VoiceChannel.Name}\" on {arg.Player.VoiceChannel.Guild.Name}\"."));

            var guildId = arg.Player.TextChannel.Guild.Id;
            var repeat = _repeat[guildId];

            if (arg.Reason == TrackEndReason.Cleanup
                || arg.Reason == TrackEndReason.Stopped
                || (arg.Player.Queue.Count == 0 && arg.Player.Track == null && !repeat))
            {
                await _nowPlayingMessages[guildId].DeleteAsync();
                _nowPlayingMessages.Remove(guildId);
            }

            if (!arg.Reason.ShouldPlayNext())
                return;

            var player = arg.Player;
            if (repeat)
                await arg.Player.PlayAsync(arg.Track);
            else
            {
                if (!player.Queue.TryDequeue(out var queueable))
                    return;

                if (queueable is LavaTrack track)
                    await arg.Player.PlayAsync(track);
            }
        }

        private async Task TrackStuckAsync(TrackStuckEventArgs arg) => await _logger.LogMessage(new LogMessage(LogSeverity.Error,
            "AudioManager",
            $"Track stuck received for \"{arg.Track.Title}\" in \"{arg.Player.VoiceChannel.Name}\" on {arg.Player.VoiceChannel.Guild.Name}\"."));

        private async Task TrackExceptionAsync(TrackExceptionEventArgs arg) => await _logger.LogMessage(new LogMessage(LogSeverity.Critical,
            "AudioManager",
            $"Track exception received for \"{arg.Track.Title}\" in \"{arg.Player.VoiceChannel.Name}\" on {arg.Player.VoiceChannel.Guild.Name}\"."));

        private async Task WebSocketClosedAsync(WebSocketClosedEventArgs arg) => await _logger.LogMessage(new LogMessage(LogSeverity.Critical,
            "AudioManager",
            $"Discord WebSocket connection closed with following reason: {arg.Reason}"));

        public async Task Dispose()
        {
            foreach (var message in _nowPlayingMessages.Values)
                await message.DeleteAsync();
        }
    }
}
