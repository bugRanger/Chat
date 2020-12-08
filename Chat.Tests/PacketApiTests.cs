namespace Chat.Tests
{
    using System;
    using System.Text;
    using System.Linq;

    using NUnit.Framework;

    using Chat.Api;
    using Chat.Api.Messages;
    using Chat.Api.Messages.Auth;
    using Chat.Api.Messages.Text;
    using Chat.Api.Messages.Call;

    public class PacketApiTests
    {
        #region Fields

        private static TestCaseData[] Messages =
        {
            new TestCaseData("auth", "\"User\":\"User1\"", new AuthorizationRequest { User = "User1" }),
            new TestCaseData("unauth", "", new UnauthorizationRequest()),

            new TestCaseData("result", "\"Status\":\"Success\",\"Reason\":\"\"", new MessageResult { Status = StatusCode.Success, Reason = ""}),

            new TestCaseData("users", "\"Users\":[\"User1\",\"User2\"]", new UsersBroadcast { Users = new []{ "User1", "User2" } }),
            new TestCaseData("userOffline", "\"User\":\"User1\"", new UserOfflineBroadcast { User = "User1" }),

            new TestCaseData(
                "message", "\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"",
                new MessageBroadcast { Source = "User1", Target = "User2", Message = "Hi!" }),

            new TestCaseData(
                "call-request", "\"Source\":\"User1\",\"Target\":\"User2\",\"MediaPort\":888", 
                new CallRequest { Source = "User1", Target = "User2", MediaPort = 888 }),
            new TestCaseData(
                "call-broadcast", "\"CallId\":1,\"Source\":\"User1\",\"Target\":\"User2\",\"State\":\"Calling\"", 
                new CallBroadcast { CallId = 1, Source = "User1", Target = "User2", State = CallState.Calling }),
            new TestCaseData(
                "call-response", "\"CallId\":1,\"MediaId\":123", 
                new CallResponse { CallId = 1, MediaId = 123 }),
        };

        private IMessage _message;

        private byte[] _packetBytes;

        #endregion Fields

        // TODO Add negative tests.

        [TestCaseSource(nameof(Messages))]
        public void PackTests(string type, string payload, IMessage message)
        {
            // Arrage
            _message = message;
            var expected = Encoding.UTF8.GetBytes("{\"Id\":1,\"Type\":\"" + type + "\",\"Payload\":{" + payload + "}}");
            expected =
                BitConverter.GetBytes((ushort)expected.Length)
                .Concat(expected)
                .ToArray();

            // Act
            var result = PacketFactory.TryPack(1, _message, out _packetBytes);

            // Assert
            Assert.IsTrue(result);
            CollectionAssert.AreEqual(expected, _packetBytes);
        }

        [TestCaseSource(nameof(Messages))]
        public void UnpackTests(string type, string payload, IMessage message)
        {
            // Arrage
            var offset = 0;
            var expectedOffset = 33
                + Encoding.UTF8.GetBytes(type).Length
                + Encoding.UTF8.GetBytes(payload).Length;

            PackTests(type, payload, message);

            // Act
            var result = PacketFactory.TryUnpack(_packetBytes, ref offset, _packetBytes.Length, out var request);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(expectedOffset, offset);
            Assert.AreEqual(1, request.Id);
            Assert.AreEqual(type, request.Type);
            Assert.AreEqual(_message, request.Payload);
        }
    }
}