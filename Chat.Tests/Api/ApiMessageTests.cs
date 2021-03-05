namespace Chat.Tests.Api
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

    public class ApiMessageTests
    {
        #region Constants

        private const int HEADER_LENGTH = 4;
        private const int MESSAGE_PATTERN_LENGTH = 31;

        #endregion Constants

        #region Fields

        private static TestCaseData[] CorrectMessages =
        {
            new TestCaseData("login", "\"User\":\"User1\"", new LoginRequest { User = "User1" }),
            new TestCaseData("logout", "", new LogoutRequest()),

            new TestCaseData("result", "\"Status\":\"Success\",\"Reason\":\"\"", new MessageResult { Status = StatusCode.Success, Reason = ""}),

            new TestCaseData("users", "\"Users\":[\"User1\",\"User2\"]", new UsersBroadcast { Users = new []{ "User1", "User2" } }),
            new TestCaseData("user-offline", "\"User\":\"User1\"", new UserOfflineBroadcast { User = "User1" }),

            new TestCaseData(
                "message", "\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"",
                new MessageBroadcast { Source = "User1", Target = "User2", Message = "Hi!" }),

            new TestCaseData(
                "call-request", "\"Source\":\"User1\",\"Target\":\"User2\",\"RoutePort\":888",
                new CallRequest { Source = "User1", Target = "User2", RoutePort = 888 }),
            new TestCaseData(
                "call-broadcast", "\"SessionId\":1,\"Participants\":[\"User1\"],\"State\":\"Calling\"",
                new CallBroadcast { SessionId = 1, Participants = new []{ "User1" }, State = CallState.Calling }),
            new TestCaseData(
                "call-response", "\"SessionId\":1,\"RouteId\":123",
                new CallResponse { SessionId = 1, RouteId = 123 }),
            new TestCaseData(
                "call-invite", "\"SessionId\":1,\"RoutePort\":888",
                new CallInviteRequest{ SessionId = 1, RoutePort = 888 }),
            new TestCaseData(
                "call-cancel", "\"SessionId\":1",
                new CallCancelRequest { SessionId = 1 }),
        };

        private IMessage _message;
        private byte[] _packetBytes;

        #endregion Fields

        #region Methods

        // TODO Add negative tests.

        [TestCaseSource(nameof(CorrectMessages))]
        public void Pack_Correct_Success(string type, string payload, IMessage message)
        {
            // Arrange
            _message = message;

            var messageFactory = new MessageFactory(true);
            var expected = Encoding.UTF8.GetBytes("{\"Id\":1,\"Type\":\"" + type + "\",\"Payload\":{" + payload + "}}");
            expected =
                BitConverter.GetBytes(expected.Length)
                .Concat(expected)
                .ToArray();

            // Act
            var result = messageFactory.TryPack(1, _message, out _packetBytes);

            // Assert
            Assert.IsTrue(result);
            CollectionAssert.AreEqual(expected, _packetBytes);
        }

        [TestCaseSource(nameof(CorrectMessages))]
        public void Unpack_Correct_Success(string type, string payload, IMessage message)
        {
            // Arrange
            var offset = 0;
            var messageFactory = new MessageFactory(true);
            var expectedOffset = MESSAGE_PATTERN_LENGTH
                + HEADER_LENGTH
                + Encoding.UTF8.GetBytes(type).Length
                + Encoding.UTF8.GetBytes(payload).Length;

            Pack_Correct_Success(type, payload, message);

            // Act
            var result = messageFactory.TryUnpack(_packetBytes, ref offset, _packetBytes.Length, out var request);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(expectedOffset, offset);
            Assert.AreEqual(1, request.Id);
            Assert.AreEqual(type, request.Type);
            Assert.AreEqual(_message, request.Payload);
        }

        [Test]
        public void Unpack_WithOffset_Success()
        {
            // Arrange
            var offset = 0;
            var expectedOffset = 162;
            var messageFactory = new MessageFactory(true);

            var multiPacket = new byte[0]
                .Concat(BitConverter.GetBytes(50))
                .Concat(Encoding.UTF8.GetBytes("{\"Id\":1,\"Type\":\"login\",\"Payload\":{\"User\":\"User1\"}}"))
                .Concat(BitConverter.GetBytes(50))
                .Concat(Encoding.UTF8.GetBytes("{\"Id\":2,\"Type\":\"login\",\"Payload\":{\"User\":\"User2\"}}"))
                .Concat(BitConverter.GetBytes(50))
                .Concat(Encoding.UTF8.GetBytes("{\"Id\":3,\"Type\":\"login\",\"Payload\":{\"User\":\"User3\"}}"))
                .ToArray();

            for (int i = 1; i <= 3; i++)
            {
                var message = new LoginRequest { User = $"User{i}" };

                // Act
                var success = messageFactory.TryUnpack(multiPacket, ref offset, multiPacket.Length, out var request);

                // Assert
                Assert.AreEqual(true, success);
                Assert.AreEqual(54 * i, offset);
                Assert.AreEqual(i, request.Id);
                Assert.AreEqual("login", request.Type);
                Assert.AreEqual(message, request.Payload);
            }

            // Act
            var result = messageFactory.TryUnpack(multiPacket, ref offset, multiPacket.Length, out var requestNulleble);

            // Assert
            Assert.AreEqual(false, result);
            Assert.AreEqual(expectedOffset, offset);
            Assert.AreEqual(null, requestNulleble);
        }

        [TestCaseSource(nameof(CorrectMessages))]
        public void Pack_WithoutHeader_Success(string type, string payload, IMessage message)
        {
            // Arrange
            _message = message;

            var messageFactory = new MessageFactory(false);
            var expected = Encoding.UTF8.GetBytes("{\"Id\":1,\"Type\":\"" + type + "\",\"Payload\":{" + payload + "}}");

            // Act
            var result = messageFactory.TryPack(1, _message, out _packetBytes);

            // Assert
            Assert.IsTrue(result);
            CollectionAssert.AreEqual(expected, _packetBytes);
        }

        [TestCaseSource(nameof(CorrectMessages))]
        public void Unpack_WithoutHeader_Success(string type, string payload, IMessage message)
        {
            // Arrange
            var offset = 0;
            var messageFactory = new MessageFactory(false);
            var expectedOffset = MESSAGE_PATTERN_LENGTH
                + Encoding.UTF8.GetBytes(type).Length
                + Encoding.UTF8.GetBytes(payload).Length;

            Pack_WithoutHeader_Success(type, payload, message);

            // Act
            var result = messageFactory.TryUnpack(_packetBytes, ref offset, _packetBytes.Length, out var request);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(expectedOffset, offset);
            Assert.AreEqual(1, request.Id);
            Assert.AreEqual(type, request.Type);
            Assert.AreEqual(_message, request.Payload);
        }

        #endregion Methods
    }
}