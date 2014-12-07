using System;
using Pantheon.Common;
using Pantheon.Common.DistributedObjects;

namespace Pantheon.Client.DistributedObjects
{
    public abstract class DistributedObject<TInterface> : DObjectBaseClient where TInterface : IDistributedObject
    {
        private TInterface _serverObject;

        public override Type DistributedType
        {
            get { return typeof(TInterface); }
        }

        public TInterface Server
        {
            get { return _serverObject; }
        }

        protected DistributedObject(INetworkManager networkManager, uint doid)
            : base(networkManager, doid)
        {
            _serverObject = (TInterface)ServerObject;
        }
    }
}