using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pantheon.Common;

namespace Pantheon.Client
{
    public sealed class DisconnectCodeLanguageProvider : ILanguageProvider
    {
        private static DisconnectCodeLanguageProvider _Instance;
        private Dictionary<DisconnectCode, string> _disconnectCodes;

        static DisconnectCodeLanguageProvider()
        {
            _Instance = new DisconnectCodeLanguageProvider();
        }

        private DisconnectCodeLanguageProvider()
        {
            _disconnectCodes = new Dictionary<DisconnectCode, string>();
        }

        public static string GetLanguageFor(int key)
        {
            return _Instance.GetLanguage(key);
        }

        public static string GetLanguageFor(DisconnectCode code)
        {
            return _Instance.GetLanguage(code);
        }

        public static void OverrideName(DisconnectCode code, string name)
        {
            _Instance._disconnectCodes.Add(code, name);
        }

        public static void OverrideName(int code, string name)
        {
            OverrideName((DisconnectCode)code, name);
        }

        public string GetLanguage(int key)
        {
            return GetLanguage((DisconnectCode)key);
        }

        public string GetLanguage(string key)
        {
            throw new NotSupportedException();
        }

        private string GetLanguage(DisconnectCode code)
        {
            string output;
            if (_disconnectCodes.TryGetValue(code, out output))
            {
                return output;
            }
            return code.ToString();
        }
    }
}