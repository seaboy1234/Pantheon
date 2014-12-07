using System;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core
{
    public static class Channel
    {
        public const ulong LogFile = 4;
        public static readonly uint AIPrefix = 2;
        public static readonly ulong AllClientAgents = 11;
        public static readonly ulong AllStateServers = 10;
        public static readonly uint CallbackPrefix = 8;
        public static readonly uint ClientPrefix = 7;
        public static readonly uint ClPrefix = 3;
        public static readonly ulong DiscoverService = 2;
        public static readonly uint DoPrefix = 1;
        public static readonly uint LazyPrefix = 13;
        public static readonly ulong MessageDirector = 1;
        public static readonly ulong QueryService = 3;
        private static Random _Random = new Random();

        /// <summary>
        ///   Combines the bits of the two values into a channel. Use this for combining Doid
        ///   prefixes with an object's Id.
        /// </summary>
        /// <param name="prefix">The first half of the channel.</param>
        /// <param name="value">The second half of the channel.</param>
        /// <returns></returns>
        public static ulong Combine(uint prefix, uint value)
        {
            return (ulong)prefix << 32 | value;
        }

        /// <summary>
        /// Generates a random channel to be used for single-use callbacks.
        ///
        /// This channel is prefixed with <see cref="CallbackPrefix"/>.
        /// </summary>
        /// <returns>A random channel id.</returns>
        public static ulong GenerateCallback()
        {
            // int.Min/MaxValue is used to give the full range of bits, allowing for 2^32 channel
            // options. This allows for much better control of random numbers.
            //
            // Value needs to be unchecked to allow for easy conversion of the bits in order to
            // allow for a direct conversion.
            //
            // We're not using a uint for the specific reason that System.Random does not support
            // unsigned values and converting them would result in an overflow exception.
            return (ulong)CallbackPrefix << 32 | unchecked((uint)_Random.Next(int.MinValue, int.MaxValue));
        }

        public static ulong GenerateService()
        {
            return (ulong)LazyPrefix << 32 | unchecked((uint)_Random.Next(int.MinValue, int.MaxValue));
        }

        /// <summary>
        /// Splits the channel's value away from the actual channel value.
        /// This allows for a two-way conversion between a value generated with
        /// <see cref="Combine(uint, uint)"/>.
        ///
        /// Use this method to get the prefix half.
        /// </summary>
        /// <param name="channel">The full channel value to be split.</param>
        /// <returns>The channel, excluding the value.</returns>
        public static uint GetPrefix(ulong channel)
        {
            byte[] data = BitConverter.GetBytes(channel);
            return BitConverter.ToUInt32(data, 4);
        }

        public static bool IsChannelReserved(ulong channel)
        {
            return channel < 100 ||
                IsObjectChannel(channel) ||
                IsGameChannel(channel) ||
                GetPrefix(channel) == CallbackPrefix;
        }

        public static bool IsGameChannel(ulong channel)
        {
            return GetPrefix(channel) == ClientPrefix;
        }

        /// <summary>
        ///   Checks if this channel has any of the distributed object prefixes.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static bool IsObjectChannel(ulong channel)
        {
            var prefix = GetPrefix(channel);
            return prefix >= DoPrefix || prefix <= ClPrefix;
        }

        /// <summary>
        ///   Checks if there is no response to a channel query.
        /// </summary>
        /// <param name="router">Any <see cref="MessageRouter" /> .</param>
        /// <param name="channel">The channel to query.</param>
        /// <returns>
        ///   <c>true</c> if no response was received on the specified channel; <c>false</c>
        ///   otherwise.
        /// </returns>
        public static bool IsOpen(MessageRouter router, ulong channel)
        {
            return Message.QueryChannel(router, channel) != null;
        }

        /// <summary>
        /// Splits the channel's prefix away from the actual channel value.
        /// This allows for a two-way conversion between a value generated with
        /// <see cref="Combine(uint, uint)"/>.
        ///
        /// Use this method to get the value half.
        /// </summary>
        /// <param name="channel">The full channel value to split</param>
        /// <returns>The channel, excluding the prefixing 32 bits.</returns>
        public static uint SplitPrefix(ulong channel)
        {
            byte[] data = BitConverter.GetBytes(channel);
            return BitConverter.ToUInt32(data, 0);
        }
    }
}