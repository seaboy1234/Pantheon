using System;
using System.IO;

namespace Pantheon.Core.Logging
{
    public class FileLogOutput : LogOutput, IDisposable
    {
        private StreamWriter _writer;

        public FileLogOutput(IEventLogger logger, string file)
            : base(logger)
        {
            if (!Directory.Exists(Path.Combine(Path.GetDirectoryName(file))))
            {
                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(file)));
            }
            _writer = new StreamWriter(File.Open(file, FileMode.CreateNew));
            _writer.AutoFlush = true;
        }

        protected override void OnLogged(object sender, Core.Event.EventLoggedEventArgs e)
        {
            _writer.WriteLine(e.Message);
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
                    // TODO: dispose managed state (managed objects).
                }

                _writer.Close();
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~FileLogOutput() { // Do not change this code. Put cleanup
        // code in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }
}