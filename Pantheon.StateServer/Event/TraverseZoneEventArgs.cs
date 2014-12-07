using System;

namespace Pantheon.StateServer.Event
{
    [Obsolete]
    public class TraverseZoneEventArgs : EventArgs
    {
        public StateServerObject Target { get; set; }

        public Zone Zone { get; set; }

        public TraverseZoneEventArgs(Zone zone, StateServerObject target)
        {
            Target = target;
            Zone = zone;
        }
    }
}