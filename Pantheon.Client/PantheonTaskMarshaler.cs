using System;

namespace Pantheon.Client
{
    public class PantheonTaskMarshaler : ITaskMarshaler
    {
        public void MarshalTask(Action action)
        {
            action();
        }
    }
}