namespace Chat.Tests.Audio
{
    using System;

    using NUnit.Framework;

    using Chat.Audio;
    using System.Collections.Generic;

    [TestFixture]
    public class JitterBufferTests
    {
        #region Constants

        private int STEP = 33;

        #endregion Constants

        #region Fields

        private List<byte> _pulled;

        private JitterBuffer<byte> _jitter;

        #endregion Fields

        #region Constructors

        [SetUp]
        public void SetUp()
        {
            _pulled = new List<byte>();

            _jitter = new JitterBuffer<byte>(STEP);
            _jitter.Pulled += (data) => _pulled.Add(data);
        }

        #endregion Constructors

        #region Methods

        [Test]
        public void Push_IsOrdered_Pulled()
        {
            // Arrange
            var pushed = new List<byte>();
            pushed.Add(1);

            // Act - Push first data.
            _jitter.Push(1, pushed[^1]);
            _jitter.WaitSync();

            // Assert
            CollectionAssert.AreEqual(_pulled, pushed);

            // Arrange
            pushed.Add(2);

            // Act - Push second data.
            _jitter.Push(2, pushed[^1]);
            _jitter.WaitSync();

            // Assert
            CollectionAssert.AreEqual(_pulled, pushed);
        }

        [Test]
        public void Push_IsReordered_WaitPulled()
        {
            // Arrange
            var pushed = new List<byte>();
            pushed.Add(1);

            // Act - Push first data.
            _jitter.Push(1, pushed[^1]);
            _jitter.WaitSync();

            // Assert
            CollectionAssert.AreEqual(_pulled, pushed);

            // Act - Push third data.
            _jitter.Push(3, 3);
            _jitter.WaitSync();

            // Assert
            CollectionAssert.AreEqual(_pulled, pushed);

            // Arrange
            pushed.Add(2);

            // Act - Push second data.
            _jitter.Push(2, pushed[^1]);
            _jitter.WaitSync();

            // Assert
            CollectionAssert.AreEqual(_pulled, pushed);

            //// TODO Нельзя просто сливать данные, они должны плавно вернуться на push модель
            //// Иначе буфер потребителя будет переполнен, начнуться потери уже на нем.
            //-------------------------------------------------------
            // Arrange
            pushed.Add(3);

            // Act - Push third data.
            _jitter.Push(4, 4);
            _jitter.WaitSync();

            // Assert
            CollectionAssert.AreEqual(_pulled, pushed);

            // Arrange
            pushed.Add(4);

            // Act - Push third data.
            _jitter.Push(5, 5);
            _jitter.WaitSync();

            // Assert
            CollectionAssert.AreEqual(_pulled, pushed);

            // Arrange
            pushed.Add(5);

            // Act - Wait ticks
            _jitter.Await();

            // Assert
            CollectionAssert.AreEqual(_pulled, pushed);

            //-------------------------------------------------------

            //// Arrange
            //pushed.Add(3);

            //// Act - Change tick.
            //_jitter.Turn();
            //_jitter.WaitSync();

            //// Assert
            //CollectionAssert.AreEqual(_pulled, pushed);
        }

        [Test]
        public void Push_Duplicate_Skiped() 
        {
            // Arrange
            var pushed = new List<byte>();
            pushed.Add(1);

            // Act - Push first data.
            _jitter.Push(1, pushed[^1]);
            _jitter.WaitSync();

            // Assert
            CollectionAssert.AreEqual(_pulled, pushed);

            // Arrange
            pushed.Add(2);

            // Act - Push second data.
            _jitter.Push(2, pushed[^1]);
            _jitter.WaitSync();

            // Assert
            CollectionAssert.AreEqual(_pulled, pushed);

            // Act - Push duplicate.
            _jitter.Push(1, 1);
            _jitter.WaitSync();

            // Assert
            CollectionAssert.AreEqual(_pulled, pushed);

            // Act - Push duplicate.
            _jitter.Push(2, 2);
            _jitter.WaitSync();

            // Assert
            CollectionAssert.AreEqual(_pulled, pushed);

            // Arrange
            pushed.Add(3);

            // Act - Push second data.
            _jitter.Push(3, pushed[^1]);
            _jitter.WaitSync();

            // Assert
            CollectionAssert.AreEqual(_pulled, pushed);
        }

        #endregion Methods
    }
}
