using System;

namespace Pantheon.Core.Logging
{
    public class LoggingEventArgs
    {
        private readonly LogLevel _level;
        private readonly DateTime _logged;
        private readonly string _message;

        public LogLevel Level
        {
            get { return _level; }
        }

        public DateTime Logged
        {
            get { return _logged; }
        }

        public string Message
        {
            get { return _message; }
        }

        public LoggingEventArgs(LogLevel level, string message, DateTime logged)
        {
            _level = level;
            _message = message;
            _logged = logged;
        }
    }
}