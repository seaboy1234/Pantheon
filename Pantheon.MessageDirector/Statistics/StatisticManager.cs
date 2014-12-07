using System;
using System.Timers;
using Pantheon.Core;

namespace Pantheon.MessageDirector.Statistics
{
    public class StatisticManager : IDisposable
    {
        private long _currentSize;
        private IMessageDirector _messageDirector;
        private Timer _timer;

        public StatisticManager(IMessageDirector messageDirector)
        {
            _messageDirector = messageDirector;
            _messageDirector.OnPump += OnPump;

            _timer = new Timer();
            _timer.AutoReset = true;
            _timer.Interval = 1000;
            _timer.Elapsed += OnSecond;
            //_timer.Start();
        }

        private void OnPump(object sender, EventArgs e)
        {
            if (e is PumpingEventArgs)
            {
                var @event = (PumpingEventArgs)e;
                _currentSize += @event.Size;
            }
        }

        private void OnSecond(object sender, ElapsedEventArgs e)
        {
            if (_currentSize > 100)
            {
                string message = string.Format(new DataSizeFormatProvider(), "{0:fs}", _currentSize);
                Console.WriteLine(message);
            }
            _currentSize = 0;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _timer.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer
                //       below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~StatisticManager() { // Do not change this code. Put cleanup
        // code in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }
}