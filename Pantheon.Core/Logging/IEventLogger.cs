using System;
using Pantheon.Core.Event;

namespace Pantheon.Core.Logging
{
    public interface IEventLogger
    {
        IMessageDirector MessageDirector { get; }

        event EventHandler<EventLoggedEventArgs> OnEventLogged;

        void Log(LogLevel level, string message);

        void Log(LogLevel level, string format, params object[] args);

        void Log(Exception exception);
    }
}