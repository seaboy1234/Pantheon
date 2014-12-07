using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pantheon.Client.DistributedObjects;

namespace Pantheon.Client.Events
{
    public class DistributedObjectEventArgs
    {
        private readonly ObjectEventAction _action;
        private readonly DObjectBaseClient _object;

        public ObjectEventAction Action
        {
            get { return _action; }
        }

        public DObjectBaseClient DistributedObject
        {
            get { return _object; }
        }

        public DistributedObjectEventArgs(DObjectBaseClient obj, ObjectEventAction action)
        {
            _object = obj;
            _action = action;
        }

        public enum ObjectEventAction
        {
            Invalid = 0,
            Generated,
            GeneratedOwner,
            Destroyed
        }
    }
}