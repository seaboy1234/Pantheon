using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pantheon.Client.Events;

namespace Pantheon.Client.Services
{
    public interface ILoginManager
    {
        event EventHandler<LoginEventArgs> Authenticated;
    }
}