using System;

namespace Pantheon.MessageDirector.Statistics
{
    public class PumpingEventArgs : EventArgs
    {
        private readonly long _size;

        public long Size
        {
            get { return _size; }
        }

        public PumpingEventArgs(long size)
        {
            _size = size;
        }
    }
}