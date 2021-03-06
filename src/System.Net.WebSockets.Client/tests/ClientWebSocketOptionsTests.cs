// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.WebSockets.Client.Tests
{
    public class ClientWebSocketOptionsTests
    {
        [Fact]
        public static void UseDefaultCredentials_Roundtrips()
        {
            var cws = new ClientWebSocket();
            Assert.False(cws.Options.UseDefaultCredentials);
            cws.Options.UseDefaultCredentials = true;
            Assert.True(cws.Options.UseDefaultCredentials);
            cws.Options.UseDefaultCredentials = false;
            Assert.False(cws.Options.UseDefaultCredentials);
        }

        [Fact]
        public static void SetBuffer_InvalidArgs_Throws()
        {
            var cws = new ClientWebSocket();
            Assert.Throws<ArgumentOutOfRangeException>("receiveBufferSize", () => cws.Options.SetBuffer(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>("receiveBufferSize", () => cws.Options.SetBuffer(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>("sendBufferSize", () => cws.Options.SetBuffer(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("receiveBufferSize", () => cws.Options.SetBuffer(0, 0, new ArraySegment<byte>(new byte[1])));
            Assert.Throws<ArgumentOutOfRangeException>("receiveBufferSize", () => cws.Options.SetBuffer(0, 1, new ArraySegment<byte>(new byte[1])));
            Assert.Throws<ArgumentOutOfRangeException>("sendBufferSize", () => cws.Options.SetBuffer(1, 0, new ArraySegment<byte>(new byte[1])));
            Assert.Throws<ArgumentNullException>("buffer.Array", () => cws.Options.SetBuffer(1, 1, default(ArraySegment<byte>)));
            Assert.Throws<ArgumentOutOfRangeException>("buffer", () => cws.Options.SetBuffer(1, 1, new ArraySegment<byte>(new byte[0])));
        }

        [Fact]
        public static void KeepAliveInterval_Roundtrips()
        {
            var cws = new ClientWebSocket();
            Assert.True(cws.Options.KeepAliveInterval > TimeSpan.Zero);

            cws.Options.KeepAliveInterval = TimeSpan.Zero;
            Assert.Equal(TimeSpan.Zero, cws.Options.KeepAliveInterval);

            cws.Options.KeepAliveInterval = TimeSpan.MaxValue;
            Assert.Equal(TimeSpan.MaxValue, cws.Options.KeepAliveInterval);

            cws.Options.KeepAliveInterval = Timeout.InfiniteTimeSpan;
            Assert.Equal(Timeout.InfiniteTimeSpan, cws.Options.KeepAliveInterval);

            Assert.Throws<ArgumentOutOfRangeException>("value", () => cws.Options.KeepAliveInterval = TimeSpan.MinValue);
        }
    }
}
