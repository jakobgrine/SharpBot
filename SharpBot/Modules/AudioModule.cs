using Discord;
using Discord.Commands;
using SharpBot.Services;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;

namespace SharpBot.Modules
{
    public class AudioModule : SharpModuleBase
    {
        private readonly LavaNode _lavaNode;
        private readonly AudioService _audioService;

        public AudioModule(LavaNode lavaNode, AudioService audioService)
        {
            _lavaNode = lavaNode;
            _audioService = audioService;
        }

        [Command("join")]
        [Alias("connect", "summon")]
        public async Task JoinAsync(IVoiceChannel channel = null)
        {
            if (_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAndDeleteAsync(":x: I am already connected to a voice channel.");
                return;
            }

            channel ??= (Context.User as IVoiceState).VoiceChannel;
            if (channel == null)
            {
                await ReplyAndDeleteAsync(":x: You have to be connected to a voice channel or specify a voice channel to connect to.");
                return;
            }

            try
            {
                await _lavaNode.JoinAsync(channel, Context.Channel as ITextChannel);
            }
            catch (Exception exception)
            {
                await ReplyAndDeleteAsync($":x: {exception.Message}");
                return;
            }

            await ReplyAndDeleteAsync($"Joined **{channel.Name}** and bound to **{Context.Channel.Name}**.");
        }

        [Command("leave")]
        [Alias("disconnect")]
        public async Task LeaveAsync()
        {
            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                return;
            }

            var channel = _lavaNode.GetPlayer(Context.Guild).VoiceChannel;

            await _lavaNode.LeaveAsync(channel);

            await ReplyAndDeleteAsync($"Left **{channel.Name}**.");
        }

        [Command("play")]
        public async Task PlayAsync([Remainder] string query = null)
        {
            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            if (query == null && player.Queue.TryDequeue(out var queueable))
            {
                if (!(queueable is LavaTrack track))
                {
                    await ReplyAndDeleteAsync(":x: Next item in queue is not a track.");
                    return;
                }

                await player.PlayAsync(track);
            }

            var isUri = Uri.TryCreate(query, UriKind.Absolute, out _);
            var searchResponse = isUri
                ? await _lavaNode.SearchAsync(query)
                : await _lavaNode.SearchYouTubeAsync(query);
            if (!(searchResponse.LoadStatus != LoadStatus.LoadFailed && searchResponse.LoadStatus != LoadStatus.NoMatches) && !isUri)
                searchResponse = await _lavaNode.SearchSoundCloudAsync(query);

            if (searchResponse.LoadStatus == LoadStatus.LoadFailed || searchResponse.LoadStatus == LoadStatus.NoMatches)
            {
                await ReplyAndDeleteAsync($":x: No search results for `{query}`.");
                return;
            }

            var tracks = searchResponse.Tracks;

            if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused)
            {
                if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                {
                    player.Queue.Enqueue(tracks);
                    await ReplyAndDeleteAsync($"Enqueued {tracks.Count} tracks.");
                    return;
                }
                else
                {
                    var eta = "";

                    var track = tracks.First();
                    player.Queue.Enqueue(track);
                    var artwork = await track.FetchArtworkAsync();
                    var embed = new EmbedBuilder()
                        .WithTitle("Enqueued")
                        .WithDescription($"[{track.Title}]({track.Url})")
                        .WithThumbnailUrl(artwork)
                        .AddField("Duration", $"`{track.Duration.StripMilliseconds()}`", true)
                        .AddField("ETA", $"`{eta}`");
                    await ReplyAndDeleteAsync(embed: embed.Build());

                    return;
                }
            }
            else
            {
                await player.PlayAsync(tracks.First());

                if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                {
                    player.Queue.Enqueue(tracks.Skip(1));
                    await ReplyAndDeleteAsync($"Enqueued {tracks.Count - 1} tracks.");
                    return;
                }

                await Context.Message.DeleteAsync();
            }
        }

        [Command("stop")]
        public async Task StopAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                return;
            }

            if (player.PlayerState == PlayerState.Stopped)
            {
                await ReplyAndDeleteAsync(":x: Playback is already stopped.");
                return;
            }

            await player.StopAsync();
            await ReplyAndDeleteAsync("Stopped.");
        }

        [Command("pause")]
        public async Task PauseAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAndDeleteAsync(":x: There is nothing playing at the moment.");
                return;
            }

            await player.PauseAsync();
            await ReplyAndDeleteAsync($"Paused.");
        }

        [Command("resume")]
        public async Task ResumeAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Paused)
            {
                await ReplyAndDeleteAsync(":x: Playback is not paused at the moment.");
                return;
            }

            await player.ResumeAsync();
            await ReplyAndDeleteAsync($"Resumed.");
        }

        [Group("skip")]
        public class SkipModule : SharpModuleBase
        {
            private readonly LavaNode _lavaNode;

            public SkipModule(LavaNode lavaNode) => _lavaNode = lavaNode;

            [Command]
            public async Task SkipAsync()
            {
                // TODO: Implement skipping mechanics with reactions (one original message and reactions to agree)
                if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
                {
                    await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                    return;
                }

                if (player.PlayerState != PlayerState.Playing)
                {
                    await ReplyAndDeleteAsync(":x: There is nothing playing at the moment.");
                    return;
                }

                //var voiceChannelUsers = (player.VoiceChannel as SocketVoiceChannel).Users.Where(x => !x.IsBot).ToArray();
                //if (_audioManager.VotedUsers.Contains(Context.User.Id))
                //{
                //    await ReplyAndDeleteAsync(":x: You can only vote once.");
                //    return;
                //}

                //_audioManager.VotedUsers.Add(Context.User.Id);
                //var percentage = (int)((double)_audioManager.VotedUsers.Count / voiceChannelUsers.Length * 100);
                //if (percentage < 50)
                //    await ReplyAndDeleteAsync($"At least 50% need to vote to skip this song. Currently at **{percentage}%**.");
                //_audioManager.VotedUsers.Clear();

                if (player.Queue.Count < 1)
                    await player.StopAsync();
                else
                    await player.SkipAsync();
                await ReplyAndDeleteAsync($"Skipped.");
            }

            [Command("to")]
            public async Task SkipToAsync(int index)
            {
                if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
                {
                    await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                    return;
                }

                if (player.PlayerState != PlayerState.Playing)
                {
                    await ReplyAndDeleteAsync(":x: There is nothing playing at the moment.");
                    return;
                }

                if (index < 1)
                {
                    await ReplyAndDeleteAsync(":x: The index cannot be smaller than 1.");
                    return;
                }

                if (index > player.Queue.Count)
                {
                    await ReplyAndDeleteAsync(":x: There are not that many tracks in the queue.");
                    return;
                }

                for (int i = 0; i < index - 1; i++)
                    player.Queue.RemoveAt(0);
                await player.SkipAsync();
                await ReplyAndDeleteAsync($"Skipped to track {index}.");
            }
        }

        [Group("seek")]
        public class SeekModule : SharpModuleBase
        {
            private readonly LavaNode _lavaNode;

            public SeekModule(LavaNode lavaNode) => _lavaNode = lavaNode;

            [Command]
            public async Task SeekAsync(TimeSpan timeSpan)
            {
                if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
                {
                    await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                    return;
                }

                if (player.PlayerState != PlayerState.Playing)
                {
                    await ReplyAndDeleteAsync(":x: There is nothing playing at the moment.");
                    return;
                }

                await player.SeekAsync(timeSpan);
                await ReplyAndDeleteAsync($"The track was seeked to **{timeSpan}**.");
            }

            [Command("forwards")]
            [Alias("forward", "f", "+")]
            public async Task SeekForwardsAsync(TimeSpan timeSpan)
            {
                if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
                {
                    await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                    return;
                }

                if (player.PlayerState != PlayerState.Playing)
                {
                    await ReplyAndDeleteAsync(":x: There is nothing playing at the moment.");
                    return;
                }

                var position = player.Track.Position + timeSpan;
                await player.SeekAsync(position);
                await ReplyAndDeleteAsync($"The track was seeked {timeSpan} forwards to **{position.StripMilliseconds()}**.");
            }

            [Command("backwards")]
            [Alias("backward", "b", "-")]
            public async Task SeekBackwardsAsync(TimeSpan timeSpan)
            {
                if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
                {
                    await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                    return;
                }

                if (player.PlayerState != PlayerState.Playing)
                {
                    await ReplyAndDeleteAsync(":x: There is nothing playing at the moment.");
                    return;
                }

                var position = player.Track.Position - timeSpan;
                await player.SeekAsync(position);
                await ReplyAndDeleteAsync($"The track was seeked {timeSpan} backwards to **{position.StripMilliseconds()}**.");
            }
        }

        [Command("volume")]
        public async Task VolumeAsync(ushort volume)
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                return;
            }

            await player.UpdateVolumeAsync(volume);
            await ReplyAndDeleteAsync($"Player volume changed to **{volume}**.");
        }

        [Command("nowplaying")]
        [Alias("np")]
        public async Task NowPlayingAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAndDeleteAsync(":x: There is nothing playing at the moment.");
                return;
            }

            var track = player.Track;
            var artwork = await track.FetchArtworkAsync();

            var embed = new EmbedBuilder()
                .WithTitle(track.Title)
                .WithThumbnailUrl(artwork)
                .WithUrl(track.Url)
                .AddField("Position", $"`{track.Position.StripMilliseconds()}` of `{track.Duration}`");

            await ReplyAndDeleteAsync(embed: embed.Build());
        }

        [Command("queue")]
        public async Task QueueAsync()
        {
            // TODO: Paginate queue (use reactions as control)

            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                return;
            }

            var stringBuilder = new StringBuilder();
            var pageSize = 20;
            var index = 1;
            foreach (LavaTrack track in player.Queue.Take(pageSize))
            {
                var spacer = index < 10 ? " " : "";
                var line = $"`{index++}.{spacer}` [{track.Title}]({track.Url})";
                if (stringBuilder.Length + line.Length >= 2048)
                    break;
                stringBuilder.AppendLine(line);
            }

            if (stringBuilder.Length == 0)
                stringBuilder.Append("The queue is empty.");

            Console.WriteLine(stringBuilder);

            var embed = new EmbedBuilder()
                .WithTitle($"Queue for `{player.VoiceChannel.Name}`")
                .WithDescription(stringBuilder.ToString());

            if (player.Queue.Count > pageSize)
                embed.AddField("Length", player.Queue.Count);

            await ReplyAndDeleteAsync(embed: embed.Build());
        }

        [Command("lyrics", RunMode = RunMode.Async)]
        public async Task ShowGeniusLyrics()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAndDeleteAsync(":x: There is nothing playing at the moment.");
                return;
            }

            var lyrics = await player.Track.FetchLyricsFromGeniusAsync();
            if (string.IsNullOrWhiteSpace(lyrics))
                lyrics = await player.Track.FetchLyricsFromOVHAsync();

            if (string.IsNullOrWhiteSpace(lyrics))
                await ReplyAndDeleteAsync($"No lyrics found for {player.Track.Title}.");

            lyrics = lyrics.Truncate(2042);

            await ReplyAsync($"```{lyrics}```");
        }

        [Command("shuffle")]
        public async Task ShuffleAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                return;
            }

            if (player.Queue.Count < 1)
            {
                await ReplyAndDeleteAsync(":x: The queue is empty.");
                return;
            }

            player.Queue.Shuffle();
            await ReplyAndDeleteAsync("The queue was shuffled.");
        }

        [Command("repeat")]
        public async Task RepeatAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAndDeleteAsync(":x: I am not connected to a voice channel.");
                return;
            }

            if (player.Track == null || player.PlayerState != PlayerState.Playing && player.PlayerState != PlayerState.Paused)
            {
                await ReplyAndDeleteAsync(":x: There is nothing playing at the moment.");
                return;
            }

            if (await _audioService.ToggleRepeat(Context.Guild))
                await ReplyAndDeleteAsync("Repeat is enabled.");
            else
                await ReplyAndDeleteAsync("Repeat is disabled.");
        }
    }
}
