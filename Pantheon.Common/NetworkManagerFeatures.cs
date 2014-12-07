using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pantheon.Common
{
    [Flags]
    public enum NetworkManagerFeatures
    {
        None = 0,
        Tcp = 1,
        Udp = 2,
        NotifyError = 4,
        Client = 8,
        Server = 16,

        LidgrenClient = Udp | NotifyError | Client,
        LidgrenServer = Udp | NotifyError | Server,

        TcpClient = Tcp | NotifyError | Client,
        TcpServer = Tcp | NotifyError | Server
    }
}