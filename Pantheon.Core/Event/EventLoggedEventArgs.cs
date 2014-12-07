using System;
using Pantheon.Core.Logging;

namespace Pantheon.Core.Event
{
    public class EventLoggedEventArgs : EventArgs
    {
        private IEventLogger _eventLogger;
        private LogLevel _level;
        private string _message;

        public IEventLogger EventLogger
        {
            get { return _eventLogger; }
        }

        public LogLevel Level
        {
            get { return _level; }
        }

        public string Message
        {
            get { return _message; }
        }

        public EventLoggedEventArgs(IEventLogger eventLogger, LogLevel level, string message)
        {
            _eventLogger = eventLogger;
            _level = level;
            _message = message;
        }
    }
}