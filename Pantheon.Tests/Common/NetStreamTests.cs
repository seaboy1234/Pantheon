using System;
using System.Linq;
using Pantheon.Common.IO;
using Xunit;

namespace Pantheon.Tests.Common
{
    public class NetStreamTests
    {
        [Fact]
        public void NewStreamIsEmpty()
        {
            NetStream stream = new NetStream();

            Assert.True(stream.Data.Length == 0);
        }

        [Fact]
        public void StreamAdvancesPosition()
        {
            byte[] data = new byte[] { 0xff, 0x00, 0x13, 0x32, 0xf2 };

            NetStream stream = new NetStream();
            stream.Write(data);

            Assert.True(stream.Position == data.Length);
        }

        [Fact]
        public void StreamDataMatches()
        {
            byte[] data = new byte[] { 0xff, 0x00, 0x13, 0x32, 0xf2 };
            byte[] fake = new byte[] { 0xfe };
            NetStream stream = new NetStream(data);

            Assert.True(stream.Data.Length == data.Length);

            Assert.True(Enumerable.SequenceEqual(stream.Data, data));

            Assert.False(Enumerable.SequenceEqual(stream.Data, fake));
        }

        [Fact]
        public void StreamWritesImplicit()
        {
            byte[] data = new byte[] { 0xff, 0x00, 0x13, 0x32, 0xf2 };

            NetStream stream = new NetStream();
            Assert.True(stream.WriteObject(data));

            Assert.True(Enumerable.SequenceEqual(stream.Data, data));
        }
    }
}