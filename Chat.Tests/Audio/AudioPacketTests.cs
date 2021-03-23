namespace Chat.Tests.Audio
{
    using System;

    using NUnit.Framework;

    using Chat.Audio;

    [TestFixture]
    public class AudioPacketTests
    {
        #region Fields

        private AudioPacket _packet;

        private byte[] _packetBytes;

        #endregion Fields

        #region Methods

        [Test]
        public void Pack_Correct_Success()
        {
            // Arrange
            var expected = new byte[] { 14, 0, 100, 0, 0, 0, 1, 200, 0, 0, 0, 1, 2, 3 };
            _packet = new AudioPacket
            {
                RouteId = 100,
                Mark = true,
                SequenceId = 200,
                Payload = new byte[] { 1, 2, 3 },
            };

            // Act
            _packetBytes = _packet.Pack().ToArray();

            // Assert
            CollectionAssert.AreEqual(expected, _packetBytes);
        }

        [Test]
        public void Unpack_Correct_Success()
        {
            // Arrange
            var offset = 0;

            Pack_Correct_Success();

            // Act
            var result = _packet.TryUnpack(_packetBytes, ref offset, _packetBytes.Length);

            // Assert
            Assert.AreEqual(true, result);
            Assert.AreEqual(14, offset);
            Assert.AreEqual(100, _packet.RouteId);
            Assert.AreEqual(true, _packet.Mark);
            Assert.AreEqual(200, _packet.SequenceId);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, _packet.Payload);
        }

        #endregion Methods
    }
}
