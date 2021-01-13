namespace Chat.Tests
{
    using System;
    using System.Linq;
    using System.Net;

    using NUnit.Framework;

    using Chat.Api;
    using Chat.Api.Messages.Call;
    using Chat.Media;
    using Chat.Server.Call;

    [TestFixture]
    public class CallApiTests
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
        public void BridgeRouteTest()
        {
            // Arrange
            InviteCallingTest();

            var packetRoute1 = new AudioPacket
            {
                RouteId = 1,
                Timestamp = 100,
                Payload = new byte[] { 1 },
            }
            .Pack();

            var packetRoute2 = new AudioPacket
            {
                RouteId = 2,
                Timestamp = 200,
                Payload = new byte[] { 2 },
            }
            .Pack();

            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Routers[^1][2], new AudioPacket { RouteId = 2, Timestamp = 100, Payload = new byte[] { 1 }, }.Pack()));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Routers[^1][1], new AudioPacket { RouteId = 1, Timestamp = 200, Payload = new byte[] { 2 }, }.Pack()));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, null, packetRoute1.Array, packetRoute1.Offset, packetRoute1.Count);
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, null, packetRoute2.Array, packetRoute2.Offset, packetRoute2.Count);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void CancelCallingTest()
        {
            // Arrange
            InviteCallingTest();

            _coreTests.Authorization.TryGet("User1", out var user1);

            var expectedId = -1951180698;
            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-cancel\",\"Payload\":{\"SessionId\":-1951180698}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], PacketFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\"],\"State\":\"Idle\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(false, _coreTests.Calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(1, _coreTests.Routers[^1].Count);
            Assert.AreEqual(new IPEndPoint(user1.Remote.Address, 8888), _coreTests.Routers[^1][1]);
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void RejectCallingTest()
        {
            // Arrange
            InitCallingTest();

            _coreTests.Authorization.TryGet("User1", out var user1);

            var expectedId = -1951180698;
            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-cancel\",\"Payload\":{\"SessionId\":-1951180698}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], PacketFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\"],\"State\":\"Idle\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(false, _coreTests.Calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(1, _coreTests.Routers[^1].Count);
            Assert.AreEqual(new IPEndPoint(user1.Remote.Address, 8888), _coreTests.Routers[^1][1]);
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void InviteCallingNotFoundTest()
        {
            // Arrange
            _coreTests.AuthorizationTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-invite\",\"Payload\":{\"SessionId\":-1951180698,\"RoutePort\":8888}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"CallNotFound\",\"Reason\":\"Call not found\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void InviteCallingNotLogginTest()
        {
            // Arrange
            _coreTests.ConnectionTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-invite\",\"Payload\":{\"SessionId\":-1951180698,\"RoutePort\":8888}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"NotAuthorized\",\"Reason\":\"User is not logged in\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void InviteCallingTest()
        {
            // Arrange
            InitCallingTest();

            _coreTests.Authorization.TryGet("User1", out var user1);
            _coreTests.Authorization.TryGet("User2", out var user2);

            var expectedId = -1951180698;
            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-invite\",\"Payload\":{\"SessionId\":-1951180698,\"RoutePort\":8888}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1], PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-response\",\"Payload\":{\"SessionId\":-1951180698,\"RouteId\":2}}")));

            foreach (var remote in _coreTests.Remotes)
            {
                _coreTests.ExpectedEvent.Add(new TestEvent(remote, PacketFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\",\"User2\"],\"State\":\"Active\"}}")));
            }

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(true, _coreTests.Calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(true, session.Contains(user1));
            Assert.AreEqual(true, session.Contains(user2));
            Assert.AreEqual(2, session.GetParticipants().Count());
            Assert.AreEqual(2, _coreTests.Routers[^1].Count);
            Assert.AreEqual(new IPEndPoint(user1.Remote.Address, 8888), _coreTests.Routers[^1][1]);
            Assert.AreEqual(new IPEndPoint(user2.Remote.Address, 8888), _coreTests.Routers[^1][2]);
            Assert.AreEqual(expectedId, session.Id);
            Assert.AreEqual(CallState.Active, session.State);
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void InitCallingNotLogginTest()
        {
            // Arrange
            _coreTests.ConnectionTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-request\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"RoutePort\":8888}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"NotAuthorized\",\"Reason\":\"User is not logged in\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void DuplicateInitCallingTest()
        {
            // Arrange
            InitCallingTest();

            _coreTests.Authorization.TryGet("User1", out var user1);
            var expectedId = -1951180698;
            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-request\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"RoutePort\":7777}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"CallDuplicate\",\"Reason\":\"Call exists\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(true, _coreTests.Calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(2, session.GetParticipants().Count());
            Assert.AreEqual(1, _coreTests.Routers[^1].Count);
            Assert.AreEqual(new IPEndPoint(user1.Remote.Address, 8888), _coreTests.Routers[^1][1]);
            Assert.AreEqual(expectedId, session.Id);
            Assert.AreEqual(CallState.Calling, session.State);
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void InitCallingNotExistsTargetTest()
        {
            // Arrange
            _coreTests.AuthorizationTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-request\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"RoutePort\":8888}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"UserNotFound\",\"Reason\":\"Target not found\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void InitCallingTest()
        {
            // Arrange
            _coreTests.AuthorizationTest();
            _coreTests.AuthorizationTest();

            _coreTests.Authorization.TryGet("User1", out var user1);
            _coreTests.Authorization.TryGet("User2", out var user2);

            var expectedId = -1951180698;
            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-request\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"RoutePort\":8888}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-response\",\"Payload\":{\"SessionId\":-1951180698,\"RouteId\":1}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], PacketFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\",\"User2\"],\"State\":\"Calling\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1], PacketFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\",\"User2\"],\"State\":\"Calling\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.PreparePacket += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(true, _coreTests.Calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(true, session.Contains(user1));
            Assert.AreEqual(true, session.Contains(user2));
            Assert.AreEqual(2, session.GetParticipants().Count());
            Assert.AreEqual(1, _coreTests.Routers[^1].Count);
            Assert.AreEqual(new IPEndPoint(user1.Remote.Address, 8888), _coreTests.Routers[^1][1]);
            Assert.AreEqual(expectedId, session.Id);
            Assert.AreEqual(CallState.Calling, session.State);
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        #endregion Methods
    }
}
