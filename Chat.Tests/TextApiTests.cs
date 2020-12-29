namespace Chat.Tests
{
    using System;
    using NUnit.Framework;

    using Chat.Api;

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
        public void PrivateMessageNotLogginTest()
        {
            // Arrage
            _coreTests.ConnectionTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"NotAuthorized\",\"Reason\":\"User is not logged in\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void PrivateMessageNotExistsTargetTest()
        {
            // Arrage
            _coreTests.AuthorizationTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"UserNotFound\",\"Reason\":\"Target not found\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void PrivateMessageTest()
        {
            // Arrage
            _coreTests.AuthorizationTest();
            _coreTests.AuthorizationTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1],
                PacketFactory.Pack("{\"Id\":0,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        #endregion Methods
    }
}
