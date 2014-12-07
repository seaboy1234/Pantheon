using System;
using Pantheon.Common.IO;
using Pantheon.Core.Logging;

namespace Pantheon.Core
{
    public static class NetStreamExtensions
    {
        static NetStreamExtensions()
        {
            NetStream.AddDataHandlers(typeof(NetStreamExtensions));
        }

        public static AuthenticationState ReadAuthenticationState(this NetStream stream)
        {
            return (AuthenticationState)stream.ReadByte();
        }

        public static LogLevel ReadLogLevel(this NetStream stream)
        {
            return (LogLevel)stream.ReadByte();
        }

        public static MessageCode ReadMessageCode(this NetStream stream)
        {
            return (MessageCode)stream.ReadInt32();
        }

        public static void Write(this NetStream stream, MessageCode code)
        {
            stream.Write((int)code);
        }

        public static void Write(this NetStream stream, LogLevel code)
        {
            stream.Write((byte)code);
        }

        public static void Write(this NetStream stream, AuthenticationState status)
        {
            stream.Write((byte)status);
        }
    }
}