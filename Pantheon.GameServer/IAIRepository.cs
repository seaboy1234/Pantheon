using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pantheon.Core.StateServer;
using Pantheon.GameServer.DistributedObjects;

namespace Pantheon.GameServer
{
    public interface IAIRepository
    {
        ulong ServiceChannel { get; }

        StateServerClient StateServer { get; }

        void Destroy(uint doid);

        void Destroy(DistributedObjectBase obj);

        DistributedObjectBase GenerateWithParent(Type type, ulong owner, IObjectGenerator generator, params object[] args);

        DistributedObjectBase GetObject(uint doid);

        void Log(string message);

        void Log(object obj);
    }
}