using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using Pantheon.Common.IO;

namespace Pantheon.Common
{
    public static class NetStreamExtensions
    {
        /// <summary>
        ///   Copies the contents of this <see cref="NetStream" /> to a
        ///   <see cref="NetOutgoingMessage" /> .
        /// </summary>
        /// <param name="message">The message that should be written to.</param>
        public static void CopyTo(this NetStream stream, NetOutgoingMessage message)
        {
            message.Write(stream.Data);
        }

        /// <summary>
        ///   Writes a <see cref="NetIncomingMessage" /> to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="message">
        ///   The <see cref="NetIncomingMessage" /> to write to the stream.
        /// </param>
        public static void Write(this NetStream stream, NetIncomingMessage message)
        {
            stream.Write(message.ReadBytes(message.LengthBytes - message.PositionInBytes));
            stream.Position = 0;
        }
    }
}