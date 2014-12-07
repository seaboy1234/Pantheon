using System;

namespace Pantheon.Client.Services
{
    public interface IService : IDisposable
    {
        string Name { get; }
    }
}