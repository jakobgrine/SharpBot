using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace SharpBot.TypeReaders
{
    public class UriTypeReader : TypeReader
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (Uri.TryCreate(input, UriKind.Absolute, out Uri result))
                return TypeReaderResult.FromSuccess(result);

            return TypeReaderResult.FromError(CommandError.ObjectNotFound, "Invalid Uri.");
        }
    }
}
