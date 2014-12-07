using System;
using System.Collections.Generic;

namespace Pantheon.Core
{
    /// <summary>
    ///   Represents a Message Director.
    /// </summary>
    /// <seealso cref="Message"></seealso>
    public interface IMessageDirector
    {
        /// <summary>
        ///   Gets the last pump time of this <see cref="IMessageDirector" /> .
        /// </summary>
        DateTime LastPump { get; }

        /// <summary>
        ///   Gets all <see cref="Message" /> s that are queued to be received.
        /// </summary>
        IEnumerable<Message> QueuedMessagesRecv { get; }

        /// <summary>
        ///   Gets all <see cref="Message" /> s that are queued to be sent.
        /// </summary>
        IEnumerable<Message> QueuedMessagesSend { get; }

        /// <summary>
        ///   Invoked whenever there are new messages to be read.
        /// </summary>
        event EventHandler OnMessagesAvailable;

        /// <summary>
        ///   Invoked whenever this <see cref="IMessageDirector" /> 's <see cref="Pump()" /> method
        ///   is called.
        /// </summary>
        event EventHandler OnPump;

        /// <summary>
        ///   Adds interest for the specified channel, causing this <see cref="IMessageDirector" />
        ///   to receive <see cref="Message" /> s that were sent on the channel.
        /// </summary>
        /// <param name="channel">The channel to add interest in.</param>
        void AddInterest(ulong channel);

        /// <summary>
        ///   Adds interest for the specified range of channels. This will cause this
        ///   <see cref="IMessageDirector" /> to receive <see cref="Message" /> s that were sent on
        ///   any of the channels in the specified range.
        /// </summary>
        /// <param name="low">The lowest inclusive channel to add interest in.</param>
        /// <param name="high">The highest inclusive channel to add interest in.</param>
        void AddInterest(ulong low, ulong high);

        /// <summary>
        ///   Processes all Message Director tasks.
        /// </summary>
        void Pump();

        /// <summary>
        ///   Registers a message with the central MessageDirector that will be broadcast when this
        ///   <see cref="IMessageDirector" /> is disconnects.
        /// </summary>
        /// <param name="message">
        ///   The message to send when this <see cref="IMessageDirector" /> disconnects.
        /// </param>
        void QueueOnDisconnect(Message message);

        /// <summary>
        ///   Queues a <see cref="Message" /> to be sent on the next pump.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void QueueSend(Message message);

        /// <summary>
        ///   Queues several <see cref="Message" /> s to be sent on the next pump.
        /// </summary>
        /// <param name="messages">The messages to be sent.</param>
        void QueueSend(IEnumerable<Message> messages);

        /// <summary>
        ///   Reads the next <see cref="Message" /> in the <see cref="QueuedMessagesRecv" /> queue
        ///   and returns it.
        /// </summary>
        /// <returns>The <see cref="Message" /> that was read.</returns>
        Message Read();

        /// <summary>
        ///   Reads the next <i>n</i> <see cref="Message" /> s from the
        ///   <see cref="QueuedMessagesRecv" /> queue and returns them.
        /// </summary>
        /// <param name="count">The number of messages to read.</param>
        /// <returns>
        ///   The messages that were read. This number may be smaller than the one defined in
        ///   <paramref name="count" /> .
        /// </returns>
        IEnumerable<Message> Read(int count);

        /// <summary>
        ///   Reads all <see cref="Message" /> s from the <see cref="QueuedMessagesRecv" /> queue
        ///   and returns them.
        /// </summary>
        /// <returns>The messages that were read.</returns>
        IEnumerable<Message> ReadAll();

        /// <summary>
        ///   Removes interest in a given channel, causing this <see cref="IMessageDirector" /> to
        ///   stop receiving <see cref="Message" /> s on the specified channel.
        /// </summary>
        /// <param name="channel">The channel to remove interest in.</param>
        void RemoveInterest(ulong channel);

        /// <summary>
        ///   Removes interest in the specified range of channels. This will cause this
        ///   <see cref="IMessageDirector" /> to no longer receive <see cref="Message" /> s on the
        ///   specified range of channels.
        /// </summary>
        /// <param name="low">The lowest inclusive channel to remove from interest.</param>
        /// <param name="high">The highest inclusive channel to remove from interest.</param>
        void RemoveInterest(ulong low, ulong high);
    }
}