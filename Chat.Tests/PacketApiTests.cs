namespace Chat.Tests
{
    using System;
    using System.Text;
    using System.Linq;

    using NUnit.Framework;

    using Chat.Api;
    using Chat.Api.Messages;

    public class PacketApiTests
    {
        #region Fields

        private IMessage _message;

        private byte[] _packetBytes;

        #endregion Fields

        // TODO Add negative tests.
        // TODO Add other messages.

        [Test]
        public void PackTests()
        {
            // Arrage
            var expected = PacketFactory.Pack("{\"Id\":1,\"Type\":\"auth\",\"Payload\":{\"User\":\"User1\"}}");

            _message = new AuthorizationBroadcast { User = "User1" };

            // Act
            var result = PacketFactory.TryPack(1, _message, out _packetBytes);

            // Assert
            Assert.IsTrue(result);
            CollectionAssert.AreEqual(expected, _packetBytes);
        }

        [Test]
        public void UnpackTests()
        {
            // Arrage
            var offset = 0;
            PackTests();

            // Act
            var result = PacketFactory.TryUnpack(_packetBytes, ref offset, _packetBytes.Length, out var request);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(51, offset);
            Assert.AreEqual(1, request.Id);
            Assert.AreEqual("auth", request.Type);
            Assert.AreEqual(_message, request.Payload);
        }
    }
}