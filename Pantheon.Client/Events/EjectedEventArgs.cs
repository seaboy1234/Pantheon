using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pantheon.Common;

namespace Pantheon.Client.Events
{
    public class EjectedEventArgs : EventArgs
    {
        private readonly DisconnectCode _code;
        private readonly string _name;
        private readonly string _reason;

        public DisconnectCode DisconnectCode
        {
            get { return _code; }
        }

        public string DisconnectCodeName
        {
            get { return _name; }
        }

        public string Reason
        {
            get { return _reason; }
        }

        public EjectedEventArgs(DisconnectCode code, string reason)
        {
            _code = code;
            _reason = reason;
            _name = DisconnectCodeLanguageProvider.GetLanguageFor(code);
        }
    }
}