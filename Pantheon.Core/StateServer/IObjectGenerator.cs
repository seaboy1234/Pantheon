using System;
using System.Collections.Generic;
using Pantheon.Common.IO;
using Pantheon.Core.DistributedServices;

namespace Pantheon.Core.StateServer
{
    public interface IObjectGenerator
    {
        uint GenerateObject(ulong owner, int type, IDictionary<int, object> obj);
    }
}