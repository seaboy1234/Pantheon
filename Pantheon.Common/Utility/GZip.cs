using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Pantheon.Common.IO;

namespace Pantheon.Common.Utility
{
    public static class GZip
    {
        public static void Compress(NetStream stream)
        {
            NetStream compressed = new NetStream();
            using (GZipStream gzip = new GZipStream(compressed, CompressionMode.Compress))
            {
                gzip.Write(stream.Data, 0, stream.Data.Length);
            }
            stream.Data = compressed.Data;
        }

        public static void Decompress(NetStream stream)
        {
            using (GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress))
            using (NetStream decompress = new NetStream())
            {
                const int count = 4096;
                int read = 1;
                while (read > 0)
                {
                    byte[] buffer = new byte[count];
                    read = gzip.Read(buffer, 0, count);
                    decompress.Write(buffer);
                }
                stream.Data = decompress.Data;
            }
        }
    }
}