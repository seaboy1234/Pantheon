using System;
using Pantheon.Common.DistributedObjects;
using Pantheon.Core.MessageRouting;

namespace Pantheon.GameServer.DistributedObjects
{
    /// <summary>
    ///   Serves as a generic base class for Distributed Objects.
    /// </summary>
    /// <typeparam name="T">The type of object this class represents.</typeparam>
    /// <typeparam name="TRepository">The type of AI Repository to use for this class.</typeparam>
    public abstract class DistributedObject<T, TRepository> : DistributedObjectBase
        where T : IDistributedObject
        where TRepository : AIRepositoryBase
    {
        /// <summary>
        ///   Gets the AIRepository that controls this
        ///   <see cref="DistributedObject{T, TRepository}" /> .
        /// </summary>
        public TRepository AIRepository
        {
            get { return (TRepository)Repository; }
        }

        public override Type DistributedType
        {
            get { return typeof(T); }
        }

        /// <summary>
        ///   Gets this object's representation as it is on the StateServer. Calls to this
        ///   property's methods will be translated into RPC calls to the StateServer.
        /// </summary>
        /// <remarks>
        ///   This property casts the value of the <see cref="RemoteObject" /> property.
        /// </remarks>
        public T StateServer
        {
            get { return (T)RemoteObject; }
        }

        protected DistributedObject(MessageRouter router, ulong owner)
            : base(router, owner)
        {
        }
    }
}