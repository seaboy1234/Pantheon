using System;

namespace Pantheon.Core.Logging
{
    public enum LogLevel : sbyte
    {
        Debug = -1,
        Info,
        Warning,
        Error,
        Critical,
    }
}