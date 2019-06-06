using System.Collections.Generic;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools.Common
{
    internal static class CommandOptionsListExtensions
    {
        internal static string ToConsoleString(this IList<CommandOption> list) =>
                list != null && list.Any()
                    ? string.Join(',', list.Select(t => $"{t.LongName}-'{t.Value()}'"))
                    : string.Empty;

    }
}
