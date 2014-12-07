using System;
using System.Collections.Generic;
using System.Linq;
using Pantheon.Common.IO;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core
{
    public class Message : NetStream
    {
        private ulong[] _channels;
        private byte _channelsCount;
        private ulong _from;

        public ulong[] Channels
        {
            get
            {
                return _channels;
            }
            set
            {
                if (value.Length > byte.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("Cannot send to more than 255 channels");
                }
                _channels = value;
                _channelsCount = (byte)value.Length;
            }
        }

        public ulong From
        {
            get { return _from; }
            set { _from = value; }
        }

        public Message()
        {
            _channels = new ulong[0];
            _from = 0;
        }

        public Message(ulong from, IEnumerable<ulong> channels)
            : this()
        {
            _channels = channels.ToArray();
            _from = from;
        }

        public Message(NetStream source)
            : this()
        {
            ReadFrom(source);
        }

        public static string QueryChannel(MessageRouter router, ulong channel)
        {
            Message message = new Message();
            message.From = Channel.GenerateCallback();
            message.AddChannel(channel);
            message.Write(MessageCode.ALL_QueryChannel);

            var reply = message.AwaitReply(router);

            return reply == null ? null : reply.ReadString();
        }

        public void AddChannel(ulong channel)
        {
            if (_channelsCount >= _channels.Length)
            {
                Array.Resize(ref _channels, _channelsCount + 1);
            }
            _channels[_channelsCount++] = channel;
        }

        public Message Copy()
        {
            NetStream stream = new NetStream();
            WriteTo(stream);
            stream.Position = 0;
            return new Message(stream);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            hash ^= Data.GetHashCode() / 2;
            hash ^= Channels.GetHashCode() / 3;
            hash ^= From.GetHashCode() / 5;

            return hash;
        }

        public void ReadFrom(NetStream source)
        {
            int length = source.ReadUInt16();
            int channelsCount = source.ReadByte();
            Channels = new ulong[channelsCount];
            for (int i = 0; i < channelsCount; i++)
            {
                Channels[i] = source.ReadUInt64();
            }

            From = source.ReadUInt64();
            length -= sizeof(ulong) * (channelsCount + 1) + sizeof(byte);

            if (length < 0)
            {
                length = 0;
            }

            Data = source.Read(length);
#if DEBUG
            if (Channels.Length == 1 && Channels[0] == 0)
            {
                Console.WriteLine("---");
            }
#endif
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Length + buffer.Length > ushort.MaxValue)
            {
                throw new ArgumentException("Max message contents is 64KB.");
            }
            base.Write(buffer, offset, count);
        }

        public void WriteTo(NetStream stream)
        {
            stream.Write((short)(Length + sizeof(ulong) * (Channels.Length + 1) + sizeof(byte)));
            stream.Write((byte)Channels.Length);
            foreach (ulong value in Channels)
            {
                stream.Write(value);
            }
            stream.Write(From);
            stream.Write(Data);

#if DEBUG
            if (Channels.Length == 1 && Channels[0] == 0)
            {
                Console.WriteLine("===");
            }
#endif
        }
    }
}