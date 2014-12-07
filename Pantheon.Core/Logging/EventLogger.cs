using System;
using System.Collections.Generic;
using Pantheon.Core.Event;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core.Logging
{
    public class EventLogger : ServiceClient, IEventLogger
    {
        private ulong _defaultChannel;
        private Queue<Message> _messageQueue;
        private string _name;

        public override string Name
        {
            get { return "event_logger"; }
        }

        public event EventHandler<EventLoggedEventArgs> OnEventLogged = delegate { };

        public EventLogger(MessageRouter router)
            : this(router, 0)
        {
        }

        public EventLogger(MessageRouter router, ulong defaultChannel)
            : base(router, defaultChannel)
        {
            _messageQueue = new Queue<Message>();
            _name = "  ";
            _defaultChannel = defaultChannel;
        }

        public override bool DiscoverServer()
        {
            bool foundServer = base.DiscoverServer();
            if (!foundServer && _defaultChannel > 0)
            {
                ServiceManager.RegisterService(new ServicePointer(Name,
                                                                  "",
                                                                  ServiceChannel,
                                                                  true,
                                                                  false,
                                                                  false));
                Log(LogLevel.Info, "Opened new log.");
                Log(LogLevel.Info, "EventLog on channel {0}", _defaultChannel);
            }
            else if (!foundServer)
            {
                return false;
            }
            MessageRouter.RegisterRoute(Log, ServiceChannel);

            return true;
        }

        public void Log(LogLevel level, string message)
        {
            Message entry = new Message();

            entry.From = 0;

            entry.Write((sbyte)level);
            entry.Write(_name);
            entry.Write(message);
            if (!IsConnected)
            {
                _messageQueue.Enqueue(entry);
            }
            else
            {
                SendMessage(entry);
            }

            Log(LogLevel.Info, _name, message);
        }

        public void Log(LogLevel level, string format, params object[] args)
        {
            Log(level, string.Format(format, args));
        }

        public void Log(Exception exception)
        {
            Log(LogLevel.Error, exception.ToString());
        }

        public void LogCritical(Exception exception)
        {
            Log(LogLevel.Critical, exception.ToString());
        }

        public void LogOnExit(LogLevel level, string message)
        {
            Message entry = new Message();

            entry.Channels = new[] { ServiceChannel };

            entry.Write((byte)level);
            entry.Write(_name);
            entry.Write(message);

            MessageDirector.QueueOnDisconnect(entry);
        }

        public void LogOnExit(LogLevel level, string format, params object[] args)
        {
            LogOnExit(level, string.Format(format, args));
        }

        public void SetLoggerName(string name)
        {
            _name = name;
        }

        private void Log(Message message)
        {
            LogLevel level = (LogLevel)message.ReadSByte();
            string component = message.ReadString();
            string log = message.ReadString();

            Log(level, component, log);
        }

        private void Log(LogLevel level, string component, string message)
        {
            if (!string.IsNullOrEmpty(component))
            {
                component = " " + component;
            }

            string logMessage = string.Format(
                                    "{0} [{1}]{3} : {2}",
                                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    level,
                                    message,
                                    component);
            OnEventLogged(this, new EventLoggedEventArgs(this, level, logMessage));
        }
    }
}