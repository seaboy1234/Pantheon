using System;

namespace Pantheon.Core.Messages
{
    public class DiscoverServiceMessage : Message
    {
        public DiscoverServiceMessage(string service, ulong replyOnChannel)
        {
            Channels = new[] { Channel.DiscoverService };
            From = replyOnChannel;
            Write(service);
        }
    }
}