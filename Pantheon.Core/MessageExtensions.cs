using System;
using System.Threading;
using System.Threading.Tasks;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core
{
    public static class MessageExtensions
    {
        public static Message AwaitReply(this Message message, MessageRouter router)
        {
            return AwaitReply(message, router, 200);
        }

        public static Message AwaitReply(this Message message, MessageRouter router, int timeout)
        {
            Message reply = null;
            int tries = 0;
            int waiting = 0;
            if (message.From == 0)
            {
                message.From = Channel.GenerateCallback();
            }

            router.RegisterRoute(repl =>
            {
                reply = repl;
            }, message.From);

            send:
            router.MessageDirector.QueueSend(message);
            router.MessageDirector.Pump();
            while (reply == null && waiting < timeout)
            {
                Thread.Sleep(1);
                waiting += 1;
                if ((DateTime.Now - router.MessageDirector.LastPump).TotalMilliseconds >= 50)
                {
                    router.MessageDirector.Pump();
                }
            }

            if (reply == null && tries < 3)
            {
                tries++;
                goto send;
            }

            return reply;
        }

        public static async Task<Message> SendAsync(this Message message, MessageRouter router)
        {
            return await SendAsync(message, router, 200);
        }

        public static async Task<Message> SendAsync(this Message message, MessageRouter router, int timeout)
        {
            var response = await new MessageCallback(router, message, timeout);
            return response;
        }

        public static async Task<Message> SendAsync(this Message message,
            MessageRouter router, int timeout, MessageCode expectedHeader)
        {
            var response = await new MessageCallback(router, message, timeout)
                                     .Expect(expectedHeader);
            return response;
        }
    }
}