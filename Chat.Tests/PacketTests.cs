namespace Chat.Tests
{
    using System;
    using System.Text;
    using System.Linq;

    using NUnit.Framework;

    using Chat.Api;
    using Chat.Api.Messages;

    public class PacketTests
    {
        #region Fields

        private IMessage _message;

        private byte[] _bytes;

        #endregion Fields


        [Test]
        public void PackTests()
        {
            // Arrage
            var expected = Encoding.UTF8.GetBytes("{\"Id\":1,\"Type\":\"auth\",\"Payload\":{\"User\":\"User1\"}}");
            expected = 
                BitConverter.GetBytes((ushort)expected.Length)
                .Concat(expected)
                .ToArray();

            _message = new AuthorizationBroadcast { User = "User1" };

            // Act
            var result = PacketFactory.TryPack(_message, out _bytes);

            // Assert
            Assert.IsTrue(result);
            CollectionAssert.AreEqual(expected, _bytes);
        }

        [Test]
        public void UnpackTests()
        {
            // Arrage
            var offset = 0;
            PackTests();

            // Act
            var result = PacketFactory.TryUnpack(_bytes, ref offset, _bytes.Length, out var request);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(51, offset);
            Assert.AreEqual("auth", request.Type);
            Assert.AreEqual(_message, request.Payload);
        }

        // TODO Add negative tests.
    }
}