using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pantheon.Common.Utility;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core.Logging
{
    public static class LogFile
    {
        private static StreamWriter _File;
        private static MessageRouter _Router;

        public static event EventHandler<LoggingEventArgs> MessageLogged = delegate { };

        public static void Critical(string message)
        {
            Log(LogLevel.Critical, message);
        }

        public static void Critical(string format, params object[] args)
        {
            Critical(string.Format(format, args));
        }

        public static void Critical(object obj)
        {
            Critical(obj.ToString());
        }

        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public static void Debug(string format, params object[] args)
        {
            Debug(string.Format(format, args));
        }

        public static void Debug(object obj)
        {
            Debug(obj.ToString());
        }

        public static void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        public static void Error(string format, params object[] args)
        {
            Error(string.Format(format, args));
        }

        public static void Error(object obj)
        {
            Error(obj.ToString());
        }

        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public static void Info(string format, params object[] args)
        {
            Info(string.Format(format, args));
        }

        public static void Info(object obj)
        {
            Info(obj.ToString());
        }

        public static void Initialize(MessageRouter router)
        {
            _Router = router;
            _Router.EnableRoutingOn(typeof(LogFile));
        }

        public static void Log(LogLevel level, string log)
        {
            if (_Router == null)
            { // LogFile is disabled on this instance.
                return;
            }

            DateTime now = DateTime.Now;
            Message message = new Message();
            message.AddChannel(Channel.LogFile);
            message.Write(now);
            message.Write(level);
            message.Write(log);

            MessageLogged(null, new LoggingEventArgs(level, log, now));

            _Router.MessageDirector.QueueSend(message);
        }

        public static void Log(LogLevel level, string format, params object[] args)
        {
            Log(level, string.Format(format, args));
        }

        public static void Log(LogLevel level, object obj)
        {
            Log(level, obj.ToString());
        }

        public static void SetLogOutput(string path)
        {
            if (_File != null)
            {
                _File.Close();
                MessageLogged -= LogOutput;
            }

            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            _File = File.CreateText(path);
            _File.AutoFlush = true;
            MessageLogged += LogOutput;
        }

        public static void Warn(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public static void Warn(string format, params object[] args)
        {
            Warn(string.Format(format, args));
        }

        public static void Warn(object obj)
        {
            Warn(obj.ToString());
        }

        private static void LogOutput(object sender, LoggingEventArgs e)
        {
            DateTime now = e.Logged;
            LogLevel level = e.Level;
            string log = e.Message;

            string message = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1} : {2}", now, level, log);

            _File.WriteLine(message);
        }

        [MessageHandler(Channel.LogFile)]
        private static void MessageReceived(Message message)
        {
            DateTime now = message.ReadDateTime();
            LogLevel level = message.ReadLogLevel();
            string log = message.ReadString();

            MessageLogged(null, new LoggingEventArgs(level, log, now));
        }
    }
}