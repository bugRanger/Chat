namespace Chat.Tests
{
    using System;
    using System.Net;
    using System.Text;
    using System.Linq;
    using System.Collections.Generic;

    using Moq;
    using NUnit.Framework;

    using Chat.Server;
    using Chat.Server.API;
    using Chat.Server.login;
    using Chat.Server.Call;

    using Chat.Api;
    using Chat.Api.Messages.Call;
    using Chat.Server.Audio;
    using Chat.Media;

    [TestFixture]
    public class CoreApiTests
    {
        #region Fields

        private List<TestEvent> _actualEvent;
        private List<TestEvent> _expectedEvent;

        private List<IPEndPoint> _remotes;
        private List<IAudioRouter> _routers;

        private CoreApi _core;
        private ICallingController _calls;
        private IAuthorizationController _authorization;
        
        private Mock<ITcpСontroller> _networkMoq;

        #endregion Fields

        #region Constructors

        [SetUp]
        public void SetUp() 
        {
            _actualEvent = new List<TestEvent>();
            _expectedEvent = new List<TestEvent>();

            _remotes = new List<IPEndPoint>();
            _routers = new List<IAudioRouter>();

            _networkMoq = new Mock<ITcpСontroller>();
            _networkMoq
                .Setup(s => s.Send(It.IsAny<IPEndPoint>(), It.IsAny<ArraySegment<byte>>()))
                .Callback<IPEndPoint, ArraySegment<byte>>((remote, data) =>
                {
                    _actualEvent.Add(new TestEvent(remote, data.ToArray()));
                });
            _networkMoq
                .Setup(s => s.Disconnect(It.IsAny<IPEndPoint>(), It.IsAny<bool>()))
                .Callback<IPEndPoint, bool>((remote, inactive) =>
                {
                    _actualEvent.Add(new TestEvent(remote, inactive));
                    _networkMoq.Raise(s => s.ConnectionClosing += null, remote, inactive);
                });

            _core = new CoreApi(_networkMoq.Object);
            _calls = new CallController((container) =>
            {
                _routers.Add(new RedirectionRouter(container, new AudioProvider(_networkMoq.Object)));
                return _routers[^1];
            });
            _authorization = new AuthorizationController();

            new AuthApi(_core, _authorization);
            new TextApi(_core, _authorization);
            new CallApi(_core, _authorization, _calls);
        }

        #endregion Constructors

        #region Methods

        [Test]
        public void AudioRouteTest() 
        {
            // Arrage
            InviteCallingTest();

            var packetRoute1 = new AudioPacket
            {
                RouteId = 1,
                Payload = new byte[] { 1 },
            }
            .Pack();

            var packetRoute2 = new AudioPacket
            {
                RouteId = 2,
                Payload = new byte[] { 2 },
            }
            .Pack();

            _expectedEvent.Add(new TestEvent(_routers[^1][2], new AudioPacket { RouteId = 2, Payload = new byte[] { 1 }, }.Pack()));
            _expectedEvent.Add(new TestEvent(_routers[^1][1], new AudioPacket { RouteId = 1, Payload = new byte[] { 2 }, }.Pack()));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, null, packetRoute1.Array, packetRoute1.Offset, packetRoute1.Count);
            _networkMoq.Raise(s => s.PreparePacket += null, null, packetRoute2.Array, packetRoute2.Offset, packetRoute2.Count);

            // Assert
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void CancelCallingTest()
        {
            // Arrage
            InviteCallingTest();

            _authorization.TryGet("User1", out var user1);

            var expectedId = -1951180698;
            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-cancel\",\"Payload\":{\"SessionId\":-1951180698}}");
            _expectedEvent.Add(new TestEvent(_remotes[1], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\"],\"State\":\"Idle\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(false, _calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(1, _routers[^1].Count);
            Assert.AreEqual(new IPEndPoint(user1.Remote.Address, 8888), _routers[^1][1]);
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void RejectCallingTest()
        {
            // Arrage
            InitCallingTest();

            _authorization.TryGet("User1", out var user1);

            var expectedId = -1951180698;
            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-cancel\",\"Payload\":{\"SessionId\":-1951180698}}");
            _expectedEvent.Add(new TestEvent(_remotes[1], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\"],\"State\":\"Idle\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(false, _calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(1, _routers[^1].Count);
            Assert.AreEqual(new IPEndPoint(user1.Remote.Address, 8888), _routers[^1][1]);
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void InviteCallingNotFoundTest()
        {
            // Arrage
            AuthorizationTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-invite\",\"Payload\":{\"SessionId\":-1951180698,\"RoutePort\":8888}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"CallNotFound\",\"Reason\":\"Call not found\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void InviteCallingNotLogginTest() 
        {
            // Arrage
            ConnectionTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-invite\",\"Payload\":{\"SessionId\":-1951180698,\"RoutePort\":8888}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"NotAuthorized\",\"Reason\":\"User is not logged in\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void InviteCallingTest()
        {
            // Arrage
            InitCallingTest();

            _authorization.TryGet("User1", out var user1);
            _authorization.TryGet("User2", out var user2);

            var expectedId = -1951180698;
            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-invite\",\"Payload\":{\"SessionId\":-1951180698,\"RoutePort\":8888}}");
            _expectedEvent.Add(new TestEvent(_remotes[1], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _expectedEvent.Add(new TestEvent(_remotes[1], PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-response\",\"Payload\":{\"SessionId\":-1951180698,\"RouteId\":2}}")));

            foreach (var remote in _remotes)
            {
                _expectedEvent.Add(new TestEvent(remote, PacketFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\",\"User2\"],\"State\":\"Active\"}}")));
            }

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(true, _calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(true, session.Contains(user1));
            Assert.AreEqual(true, session.Contains(user2));
            Assert.AreEqual(2, session.GetParticipants().Count());
            Assert.AreEqual(2, _routers[^1].Count);
            Assert.AreEqual(new IPEndPoint(user1.Remote.Address, 8888), _routers[^1][1]);
            Assert.AreEqual(new IPEndPoint(user2.Remote.Address, 8888), _routers[^1][2]);
            Assert.AreEqual(CallState.Active, session.State);
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void InitCallingNotLogginTest()
        {
            // Arrage
            ConnectionTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-request\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"RoutePort\":8888}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"NotAuthorized\",\"Reason\":\"User is not logged in\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void DuplicateInitCallingTest() 
        {
            // Arrage
            InitCallingTest();

            _authorization.TryGet("User1", out var user1);
            var expectedId = -1951180698;
            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-request\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"RoutePort\":7777}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"CallDuplicate\",\"Reason\":\"Call exists\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(true, _calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(2, session.GetParticipants().Count());
            Assert.AreEqual(1, _routers[^1].Count);
            Assert.AreEqual(new IPEndPoint(user1.Remote.Address, 8888), _routers[^1][1]);
            Assert.AreEqual(CallState.Calling, session.State);
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void InitCallingNotExistsTargetTest()
        {
            // Arrage
            AuthorizationTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-request\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"RoutePort\":8888}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"UserNotFound\",\"Reason\":\"Target not found\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void InitCallingTest() 
        {
            // Arrage
            AuthorizationTest();
            AuthorizationTest();

            _authorization.TryGet("User1", out var user1);
            _authorization.TryGet("User2", out var user2);

            var expectedId = -1951180698;
            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-request\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"RoutePort\":8888}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"call-response\",\"Payload\":{\"SessionId\":-1951180698,\"RouteId\":1}}")));
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\",\"User2\"],\"State\":\"Calling\"}}")));
            _expectedEvent.Add(new TestEvent(_remotes[1], PacketFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\",\"User2\"],\"State\":\"Calling\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(true, _calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(true, session.Contains(user1));
            Assert.AreEqual(true, session.Contains(user2));
            Assert.AreEqual(2, session.GetParticipants().Count());
            Assert.AreEqual(1, _routers[^1].Count);
            Assert.AreEqual(new IPEndPoint(user1.Remote.Address, 8888), _routers[^1][1]);
            Assert.AreEqual(CallState.Calling, session.State);
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void PrivateMessageNotLogginTest()
        {
            // Arrage
            ConnectionTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"NotAuthorized\",\"Reason\":\"User is not logged in\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void PrivateMessageNotExistsTargetTest()
        {
            // Arrage
            AuthorizationTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"UserNotFound\",\"Reason\":\"Target not found\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void PrivateMessageTest() 
        {
            // Arrage
            AuthorizationTest();
            AuthorizationTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _expectedEvent.Add(new TestEvent(_remotes[1], 
                PacketFactory.Pack("{\"Id\":0,\"Type\":\"message\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"Message\":\"Hi!\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void UnauthorizationNotLogginTest()
        {
            // Arrage
            ConnectionTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"logout\",\"Payload\":{}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"NotAuthorized\",\"Reason\":\"User is not logged in\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(0, _authorization.GetUsers().Count());
            Assert.AreEqual(false, _authorization.TryGet("User1", out _));
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void UnauthorizationTest()
        {
            // Arrage
            AuthorizationTest();
            AuthorizationTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"logout\",\"Payload\":{}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _expectedEvent.Add(new TestEvent(_remotes[0], false));
            _expectedEvent.Add(new TestEvent(_remotes[1], PacketFactory.Pack("{\"Id\":0,\"Type\":\"user-offline\",\"Payload\":{\"User\":\"User1\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(1, _authorization.GetUsers().Count());
            Assert.AreEqual(false, _authorization.TryGet("User1", out _));
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void ReAuthorizationWithRenameTest()
        {
            // Arrage
            AuthorizationTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"login\",\"Payload\":{\"User\":\"Dummy\"}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":0,\"Type\":\"users\",\"Payload\":{\"Users\":[]}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(1, _authorization.GetUsers().Count());
            Assert.AreEqual(true, _authorization.TryGet("Dummy", out _));
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void DuplicateAuthorizationAnotherAddressTest()
        {
            // Arrage
            AuthorizationTest();
            ConnectionTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"login\",\"Payload\":{\"User\":\"User1\"}}");
            _expectedEvent.Add(new TestEvent(_remotes[1], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"AuthDuplicate\",\"Reason\":\"User exists\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(1, _authorization.GetUsers().Count());
            Assert.AreEqual(true, _authorization.TryGet("User1", out _));
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void DuplicateAuthorizationTest()
        {
            // Arrage
            AuthorizationTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"login\",\"Payload\":{\"User\":\"User1\"}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"AuthDuplicate\",\"Reason\":\"User exists\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(1, _authorization.GetUsers().Count());
            Assert.AreEqual(true, _authorization.TryGet("User1", out _));
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void AuthorizationTest()
        {
            // Arrage
            var remotes = _remotes.ToArray();
            var users = string.Join(",", _remotes.Select((s, i) => $"\"User{i + 1}\""));
            ConnectionTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"login\",\"Payload\":{\"User\":\""+ $"User{_remotes.Count}" + "\"}}");
            _expectedEvent.Add(new TestEvent(_remotes[^1], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _expectedEvent.Add(new TestEvent(_remotes[^1], PacketFactory.Pack("{\"Id\":0,\"Type\":\"users\",\"Payload\":{\"Users\":[" + users + "]}}")));
            foreach (var remote in remotes)
            {
                _expectedEvent.Add(new TestEvent(remote, PacketFactory.Pack("{\"Id\":0,\"Type\":\"users\",\"Payload\":{\"Users\":[\"" + $"User{_remotes.Count}" + "\"]}}")));
            }

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[^1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(true, _authorization.TryGet($"User{_remotes.Count}", out _));
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void ConnectionTest() 
        {
            // Arrage
            _remotes.Add(new IPEndPoint(IPAddress.Parse($"127.0.0.{_remotes.Count + 1}"), 5000));

            // Act
            _networkMoq.Raise(s => s.ConnectionAccepted += null, _remotes[^1]);

            // Assert
            Assert.IsFalse(_authorization.TryGet(_remotes[^1], out _));
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void DisconnectionTest()
        {
            // Arrage
            AuthorizationTest();
            AuthorizationTest();

            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":0,\"Type\":\"user-offline\",\"Payload\":{\"User\":\"User2\"}}")));

            // Act
            _networkMoq.Raise(s => s.ConnectionClosing += null, _remotes[^1], false);

            // Assert
            Assert.IsFalse(_authorization.TryGet(_remotes[^1], out _));
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        #endregion Methods
    }
}
