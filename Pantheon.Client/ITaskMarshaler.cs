using System;

namespace Pantheon.Client
{
    /// <summary>
    ///   Provides a method of marshaling application-tied tasks into the application's threading
    ///   environment.
    /// </summary>
    /// <remarks>
    ///   This method of passing tasks allows for Pantheon's threads to safely interface with
    ///   application threads. This was made to allow Unity3d support.
    /// </remarks>
    public interface ITaskMarshaler
    {
        /// <summary>
        ///   Marshals an <see cref="Action" /> to an application-owned thread.
        /// </summary>
        /// <param name="action">The <see cref="Action" /> to pass.</param>
        void MarshalTask(Action action);
    }
}