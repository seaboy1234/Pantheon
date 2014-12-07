using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pantheon.Common;
using Pantheon.Core;
using Pantheon.Core.MessageRouting;
using Pantheon.Core.StateServer;

namespace Pantheon.ClientAgent
{
    public class ClientManager
    {
        private List<PantheonClient> _clients;
        private Dictionary<string, ulong> _openChannels;
        private MessageRouter _router;
        private StateServerClient _stateServer;
        private string _version;

        public IEnumerable<PantheonClient> Clients
        {
            get { return _clients.AsReadOnly(); }
        }

        public IMessageDirector MessageDirector
        {
            get { return _router.MessageDirector; }
        }

        public ClientManager(MessageRouter router)
        {
            _clients = new List<PantheonClient>();
            _openChannels = new Dictionary<string, ulong>();
            _version = "UNSET";

            _router = router;

            _stateServer = ServiceManager.GetServiceHandle<StateServerClient>(router);
        }

        public void AddClient(INetworkManager client)
        {
            if (!string.IsNullOrEmpty(client.Host))
            {
                _clients.Add(new PantheonClient(this, client));
            }
        }

        public void ClientDisconnecting(PantheonClient client)
        {
            _clients.Remove(client);
        }

        public ulong GetNewId()
        {
            if (!_stateServer.IsConnected)
            {
                _stateServer.DiscoverServer();
            }
            ulong channel = _stateServer.CreateObjectOwner();
            int tries = 0;

            while (channel == 0 && tries++ < 3)
            {
                channel = _stateServer.CreateObjectOwner();
            }
            return channel;
        }

        public async Task<ServicePointer> GetService(string name)
        {
            ulong channel;
            if (_openChannels.TryGetValue(name, out channel))
            {
                return new ServicePointer(name, "", channel, false, true, true);
            }
            return await ServiceManager.QueryServiceAsync(_router, name);
        }

        public string GetVersion()
        {
            return _version;
        }

        public void OpenChannels(IReadOnlyDictionary<string, ulong> channels)
        {
            foreach (var item in channels)
            {
                _openChannels.Add(item.Key, item.Value);
            }
        }

        public void SetVersion(string version)
        {
            _version = version;
        }
    }
}