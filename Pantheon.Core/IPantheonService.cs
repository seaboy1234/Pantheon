using System;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core
{
    public interface IPantheonService
    {
        IMessageDirector MessageDirector { get; }

        MessageRouter MessageRouter { get; }

        string Name { get; }

        ulong ServiceChannel { get; }
    }
}