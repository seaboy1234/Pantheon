using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pantheon.Common.DistributedObjects
{
    public class IdAttribute : Attribute
    {
        private readonly int _id;

        public int Id
        {
            get { return _id; }
        }

        public IdAttribute(int id)
        {
            _id = id;
        }
    }
}