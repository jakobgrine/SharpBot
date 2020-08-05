using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SharpBot.TypeReaders
{
    public class GuildTypeReader<T> : TypeReader
        where T : class, IGuild
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var results = new Dictionary<ulong, TypeReaderValue>();
            var guilds = await context.Client.GetGuildsAsync(CacheMode.CacheOnly).ConfigureAwait(false);

            //By Id
            if (ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out ulong id))
                AddResult(results, await context.Client.GetGuildAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T, 1.00f);

            //By Name
            foreach (var guild in guilds.Where(x => string.Equals(input, x.Name, StringComparison.OrdinalIgnoreCase)))
                AddResult(results, guild as T, guild.Name == input ? 0.80f : 0.70f);

            if (results.Count > 0)
                return TypeReaderResult.FromSuccess(results.Values);

            return TypeReaderResult.FromError(CommandError.ObjectNotFound, "Guild not found.");
        }

        private void AddResult(Dictionary<ulong, TypeReaderValue> results, T guild, float score)
        {
            if (guild != null && !results.ContainsKey(guild.Id))
                results.Add(guild.Id, new TypeReaderValue(guild, score));
        }
    }
}
