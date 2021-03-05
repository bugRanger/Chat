namespace Chat.Tests.Core
{
    using System;

    using NUnit.Framework;

    [TestFixture]
    public class TextApiTests
    {
        #region Fields

        private CoreApiTests _coreTests;

        #endregion Fields

        #region Constructors

        [SetUp]
        public void SetUp()
        {
            _coreTests = new CoreApiTests();
            _coreTests.SetUp();
        }

        #endregion Constructors

        #region Methods

        [Test]
        public void Send_IsNotLoggined_Failure()
        {
            // Arrange
            _coreTests.ConnectionTest();

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"NotAuthorized\",\"Reason\":\"User is not logged in\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void Send_MessageIsEmpty_Failure()
        {
            // Arrange
            _coreTests.AuthorizationTest();
            _coreTests.AuthorizationTest();

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"\"}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Failure\",\"Reason\":\"Invalid parameters: Message\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void Send_TargetNotExists_Failure()
        {
            // Arrange
            _coreTests.AuthorizationTest();

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"UserNotFound\",\"Reason\":\"Target not found\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void Send_TargetIsEmpty_Failure()
        {
            // Arrange
            _coreTests.AuthorizationTest();

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"\",\"Message\":\"Hi!\"}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Failure\",\"Reason\":\"Not supported\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void Send_TargetIsMe_Success()
        {
            // Arrange
            _coreTests.AuthorizationTest();

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User1\",\"Message\":\"Hi!\"}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":0,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User1\",\"Message\":\"Hi!\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void Send_SourceIsEmpty_Success()
        {
            // Arrange
            _coreTests.AuthorizationTest();
            _coreTests.AuthorizationTest();

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"message\",\"Payload\":{\"Source\":\"\",\"Target\":\"User2\",\"Message\":\"Hi!\"}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1], _coreTests.MessageFactory.Pack("{\"Id\":0,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void Send_Correct_Success()
        {
            // Arrange
            _coreTests.AuthorizationTest();
            _coreTests.AuthorizationTest();

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1], _coreTests.MessageFactory.Pack("{\"Id\":0,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        #endregion Methods
    }
}
