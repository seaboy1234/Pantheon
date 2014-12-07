using System;
using Pantheon.Core.Event;

namespace Pantheon.Core.Logging
{
    public abstract class LogOutput
    {
        private IEventLogger _logger;

        protected IEventLogger Logger
        {
            get { return _logger; }
        }

        protected LogOutput(IEventLogger logger)
        {
            _logger = logger;
            _logger.OnEventLogged += OnLogged;
        }

        protected abstract void OnLogged(object sender, EventLoggedEventArgs args);
    }
}