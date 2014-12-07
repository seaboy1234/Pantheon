using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pantheon.Client
{
    public interface ILanguageProvider
    {
        string GetLanguage(string key);

        string GetLanguage(int key);
    }
}