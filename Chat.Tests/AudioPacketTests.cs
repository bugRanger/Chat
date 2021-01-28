namespace Chat.Tests
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
        public void PackTests()
        {
            // Arrange
            var expected = new byte[] { 17, 0, 100, 0, 0, 0, 200, 0, 0, 0, 1, 2, 3 };
            _packet = new AudioPacket
            {
                RouteId = 100,
                SequenceId = 200,
                Payload = new byte[] { 1, 2, 3 },
            };

            // Act
            _packetBytes = _packet.Pack().ToArray();

            // Assert
            CollectionAssert.AreEqual(expected, _packetBytes);
        }

        [Test]
        public void UnpackTests()
        {
            // Arrange
            var offset = 0;

            PackTests();

            // Act
            var result = _packet.TryUnpack(_packetBytes, ref offset, _packetBytes.Length);

            // Assert
            Assert.AreEqual(true, result);
            Assert.AreEqual(17, offset);
            Assert.AreEqual(100, _packet.RouteId);
            Assert.AreEqual(200, _packet.SequenceId);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, _packet.Payload);
        }

        #endregion Methods
    }
}
