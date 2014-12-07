using System;

namespace Pantheon.Core
{
    public class ServicePointer
    {
        public bool AllowAnonymous { get; set; }

        public ulong Channel { get; set; }

        public bool IsClientService { get; set; }

        public bool IsLocal { get; set; }

        public string Name { get; set; }

        public string Server { get; set; }

        public ServicePointer(string name,
                              string server,
                              ulong channel,
                              bool isLocal,
                              bool isClientService,
                              bool allowAnonymous)
        {
            Name = name;
            Server = server;
            Channel = channel;
            IsLocal = isLocal;
            AllowAnonymous = allowAnonymous;
            IsClientService = isClientService;
        }

        public Message ToMessage()
        {
            Message message = new Message();

            message.Write(Name);
            message.Write(Server);
            message.Write(Channel);
            message.Write(IsClientService);
            message.Write(AllowAnonymous);

            return message;
        }
    }
}