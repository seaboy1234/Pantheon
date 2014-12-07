using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pantheon.Common;
using Pantheon.Core;
using Pantheon.Core.Logging;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core
{
    /// <summary>
    ///   Represents the primary entry point for a standalone Pantheon Server Application.
    /// </summary>
    public abstract class InternalRepository : IDisposable
    {
        private bool _disposedValue = false;
        private IMessageDirector _local;
        private EventLogger _logger;
        private IMessageDirector _messageDirector;
        private INetworkManager _networkManager;
        private List<LogOutput> _outputs;
        private MessageRouter _router;
        private NodeMessageDirector _top;

        /// <summary>
        ///   Gets an event logger for this <see cref="InternalRepository" /> .
        /// </summary>
        public EventLogger EventLogger
        {
            get { return _logger; }
        }

        /// <summary>
        ///   Gets a <see cref="IMessageDirector" /> for this <see cref="InternalRepository" /> .
        /// </summary>
        public IMessageDirector MessageDirector
        {
            get { return _messageDirector; }
        }

        /// <summary>
        ///   Gets a <see cref="T:MessageRouter" /> for this <see cref="InternalRepository" /> . It
        ///   is recommended to use <see cref="Router()" /> if you are going to pass this to a
        ///   object that requires a <see cref="T:MessageRouter" /> .
        /// </summary>
        public MessageRouter MessageRouter
        {
            get { return _router; }
        }

        /// <summary>
        ///   Gives direct access to a <see cref="INetworkManager" /> .
        /// </summary>
        public INetworkManager NetworkManager
        {
            get { return _networkManager; }
        }

        static InternalRepository()
        {
            PantheonInitializer.InitializeAll();
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="InternalRepository" /> class using the
        ///   specified <see cref="INetworkManager" /> .
        /// </summary>
        /// <param name="networkManager">
        ///   An <see cref="INetworkManager" /> to use to connect into a Pantheon Cluster.
        /// </param>
        protected InternalRepository(INetworkManager networkManager)
        {
            _networkManager = networkManager;
            if (!networkManager.IsConnected)
            {
                networkManager.Connect();
            }

            _outputs = new List<LogOutput>();

            _local = new MessageDirectorClient(networkManager);

            _top = new NodeMessageDirector(_local);
            _messageDirector = _top.AddMessageDirector();

            _router = new MessageRouter(_messageDirector);

            ServiceManager.EnableServiceDirectory(Router());

            _logger = new EventLogger(_router, 99);
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting
        ///   unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///   Enables the console log output on the local <see cref="T:EventLogger" /> .
        /// </summary>
        public void EnableConsoleOutput()
        {
            _outputs.Add(new ConsoleLogOutput(_logger));
        }

        public void EnableCustomOutput(LogOutput output)
        {
            _outputs.Add(output);
        }

        /// <summary>
        ///   Enables the file log output on the local <see cref="T:EventLogger" /> .
        /// </summary>
        /// <param name="path">The location to save the log file.</param>
        public void EnableLogFileOutput(string path)
        {
            _outputs.Add(new FileLogOutput(_logger, path));
        }

        /// <summary>
        ///   Creates a new local <see cref="IMessageDirector" /> and returns a dedicated
        ///   <see cref="T:MessageRouter" /> .
        /// </summary>
        /// <returns></returns>
        public MessageRouter Router()
        {
            return new MessageRouter(new NodeMessageDirector(_top));
        }

        /// <summary>
        ///   Blocks execution of the program until <see cref="NetworkManager" /> has been
        ///   disconnected. During this time, the <see cref="InternalRepository" /> will perform
        ///   standard update and message pumping calls.
        /// </summary>
        /// <seealso cref="Update()"></seealso>
        public void Run()
        {
            while (_networkManager.IsConnected)
            {
                Update();
                _local.Pump();
                Thread.Sleep(1);
            }
        }

        /// <summary>
        ///   Asynchronously operates the <see cref="InternalRepository" /> .
        /// </summary>
        /// <returns>The <see cref="Task" /> .</returns>
        public async Task RunAsync()
        {
            await Task.Run(new Action(Run));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _logger.Dispose();
                    _outputs.Clear();
                    if (_networkManager.IsConnected)
                    {
                        _networkManager.Disconnect();
                    }
                    _router.DestroyRoute(this);
                }
                _disposedValue = true;
            }
        }

        /// <summary>
        ///   Method used to perform standard user-specified update tasks.
        /// </summary>
        protected virtual void Update()
        {
        }
    }
}