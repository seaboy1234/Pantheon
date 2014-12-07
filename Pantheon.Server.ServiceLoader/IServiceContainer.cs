using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pantheon.Server.ServiceLoader
{
    public interface IServiceContainer
    {
        PantheonService GetServiceManager(string service);

        T GetServiceManager<T>(string service) where T : PantheonService;

        bool IsServiceActive(string service);

        bool IsServiceRemote(string service);
    }
}