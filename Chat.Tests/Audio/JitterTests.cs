namespace Chat.Tests.Audio
{
    using System;

    using NUnit.Framework;

    using Chat.Net.Jitter;

    [TestFixture]
    public class JitterTests
    {
        #region Classes

        class Packet : IPacket
        {
            #region Properties

            public bool Mark { get; }

            public uint SequenceId { get; }

            #endregion Properties

            #region Constructors

            public Packet(uint seq, bool mark) 
            {
                Mark = mark;
                SequenceId = seq;
            }

            #endregion Constructors
        }

        #endregion Classes

        #region Fields

        private JitterQueue<Packet> _queue;
        private Packet packet;
        private uint? index;

        #endregion Fields

        #region Constructors

        [SetUp]
        public void SetUp() 
        {
            _queue = new JitterQueue<Packet>(3);
        }

        #endregion Constructors

        #region Methods

        [Test]
        public void Push_Ordered_Pulled() 
        {
            // Arrange
            packet = new Packet(1, true);

            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(1, packet.SequenceId);
            Assert.AreEqual(true, packet.Mark);
            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(1, index);
            Assert.AreEqual(1, packet.SequenceId);
            Assert.AreEqual(true, packet.Mark);


            // Act
            index = _queue.Pull(true, out packet);
            // Assert
            Assert.AreEqual(null, packet);            Assert.AreEqual(null, index);

            // Arrange
            packet = new Packet(2, false);
            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(2, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);
            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(2, index);
            Assert.AreEqual(2, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);


            // Act
            index = _queue.Pull(true, out packet);
            // Assert
            Assert.AreEqual(null, packet);            Assert.AreEqual(null, index);

            // Arrange
            packet = new Packet(3, false);
            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(3, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);
            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(3, index);
            Assert.AreEqual(3, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);
        }

        [Test]
        public void Push_NonFirst_Buffered()
        {
            // Arrange
            packet = new Packet(2, false);

            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreEqual(null, packet);
            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreEqual(null, packet);


            // Act
            index = _queue.Pull(true, out packet);
            // Assert
            Assert.AreEqual(null, packet);            Assert.AreEqual(null, index);

            // Arrange
            packet = new Packet(1, false);
            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreNotEqual(null, packet);
            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(1, index);
            Assert.AreEqual(1, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);


            // Act
            index = _queue.Pull(true, out packet);
            // Assert
            Assert.AreEqual(null, packet);            Assert.AreEqual(null, index);

            // Arrange
            packet = new Packet(3, false);
            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreNotEqual(null, packet);
            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(2, index);
            Assert.AreEqual(2, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);

            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreNotEqual(null, packet);
            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(3, index);
            Assert.AreEqual(3, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);
        }

        [Test]
        public void Push_Reordered_Pulled()
        {
            // Arrange
            packet = new Packet(1, true);
            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(1, packet.SequenceId);
            Assert.AreEqual(true, packet.Mark);
            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(1, index);
            Assert.AreEqual(1, packet.SequenceId);
            Assert.AreEqual(true, packet.Mark);
            
            // Arrange
            packet = new Packet(3, false);
            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreEqual(null, packet);
            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreEqual(null, packet);
            // Act
            index = _queue.Pull(true, out packet);
            // Assert
            Assert.AreEqual(null, packet);
            Assert.AreEqual(null, index);

            // Arrange
            packet = new Packet(2, false);
            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(2, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);
            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(2, index);
            Assert.AreEqual(2, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);
            
            // Act
            packet = _queue.Peek();
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(3, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);
            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(3, index);
            Assert.AreEqual(3, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);
        }

        [Test]
        public void Push_Reordered_Restored()
        {
            // Arrange
            Push_Reordered_Pulled();
            packet = new Packet(4, false);
            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(4, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);
            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(4, index);
            Assert.AreEqual(4, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);
        }

        [Test]
        public void Pull_Duplicate_Skipped()
        {
            // Arrange
            packet = new Packet(1, true);
            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(1, packet.SequenceId);
            Assert.AreEqual(true, packet.Mark);
            
            // Arrange
            packet = new Packet(1, true);
            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(1, packet.SequenceId);
            Assert.AreEqual(true, packet.Mark);
            
            // Arrange
            packet = new Packet(1, true);
            // Act
            _queue.Push(packet);
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(1, index);
            Assert.AreEqual(1, packet.SequenceId);
            Assert.AreEqual(true, packet.Mark);
        }

        [Test]
        public void Pull_Hungry_Restored()
        {
            // Arrange
            packet = new Packet(1, true);
            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(1, packet.SequenceId);
            Assert.AreEqual(true, packet.Mark);
            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(1, index);
            Assert.AreEqual(1, packet.SequenceId);
            Assert.AreEqual(true, packet.Mark);

            // Arrange
            packet = new Packet(3, false);
            // Act
            _queue.Push(packet);
            packet = _queue.Peek();
            // Assert
            Assert.AreEqual(null, packet);

            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreEqual(null, packet);
            Assert.AreEqual(null, index);

            // Act
            index = _queue.Pull(true, out packet);
            // Assert
            Assert.AreEqual(null, packet);
            Assert.AreEqual(null, index);
            // Act
            index = _queue.Pull(true, out packet);
            // Assert
            Assert.AreEqual(null, packet);
            Assert.AreEqual(null, index);

            // Act
            index = _queue.Pull(true, out packet);
            // Assert
            Assert.AreEqual(2, index);
            Assert.AreEqual(null, packet);
            
            // Act
            packet = _queue.Peek();
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(3, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);
            // Act
            index = _queue.Pull(true, out packet);
            // Assert
            Assert.AreNotEqual(null, packet);
            Assert.AreEqual(3, index);
            Assert.AreEqual(3, packet.SequenceId);
            Assert.AreEqual(false, packet.Mark);
        }

        [Test]
        public void Pull_None_Ended()
        {
            // Arrange
            Push_Ordered_Pulled();

            // Act
            packet = _queue.Peek();
            // Assert
            Assert.AreEqual(null, packet);

            // Act
            index = _queue.Pull(false, out packet);
            // Assert
            Assert.AreEqual(null, packet);
            Assert.AreEqual(null, index);
            // Act
            index = _queue.Pull(true, out packet);
            // Assert
            Assert.AreEqual(null, packet);
            Assert.AreEqual(null, index);
            // Act
            index = _queue.Pull(true, out packet);
            // Assert
            Assert.AreEqual(null, packet);
            Assert.AreEqual(null, index);

            // Act
            index = _queue.Pull(true, out packet);
            // Assert
            Assert.AreEqual(4, index);
            Assert.AreEqual(null, packet);
    
            // Act
            packet = _queue.Peek();
            // Assert
            Assert.AreEqual(null, packet);

            // Act
            index = _queue.Pull(true, out packet);
            // Assert
            Assert.AreEqual(5, index);
            Assert.AreEqual(null, packet);
        }

        #endregion Methods
    }
}
