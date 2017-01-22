using System.Windows.Input;

namespace wallabag.Data.Common.Helpers
{
    public static class CommandHelper
    {
        public static void Execute(this ICommand command) => command.Execute(null);
    }
}
