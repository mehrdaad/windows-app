using System;
using System.Runtime.CompilerServices;

namespace wallabag.Data.Services
{
    public interface ILoggingService
    {
        void WriteLine(string text, LoggingCategory category = LoggingCategory.Debug, [CallerMemberName] string member = "", [CallerLineNumber] int lineNumber = 0);
        void WriteLineIf(bool condition, string text, LoggingCategory category = LoggingCategory.Debug, [CallerMemberName] string member = "", [CallerLineNumber] int lineNumber = 0);
        void WriteObject(object obj, LoggingCategory category = LoggingCategory.Debug, [CallerMemberName] string member = "", [CallerLineNumber] int lineNumber = 0);
        void TrackException(Exception e, LoggingCategory category = LoggingCategory.Critical, [CallerMemberName] string member = "", [CallerLineNumber] int lineNumber = 0);

        string Log { get; }
    }

    public enum LoggingCategory { Info, Warning, Critical, Debug }
}
