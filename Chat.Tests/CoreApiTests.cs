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
    using Chat.Server.Auth;
    using Chat.Api;

    [TestFixture]
    public partial class CoreApiTests
    {
        #region Fields

        private List<TestEvent> _actualEvent;
        private List<TestEvent> _expectedEvent;

        private List<IPEndPoint> _remotes;

        private CoreApi _core;
        private AuthorizationController _auth;

        private Mock<ITcpСontroller> _networkMoq;

        #endregion Fields

        #region Constructors

        [SetUp]
        public void SetUp() 
        {
            _actualEvent = new List<TestEvent>();
            _expectedEvent = new List<TestEvent>();

            _remotes = new List<IPEndPoint>();

            _auth = new AuthorizationController();

            _networkMoq = new Mock<ITcpСontroller>();
            _networkMoq
                .Setup(s => s.Send(It.IsAny<IPEndPoint>(), It.IsAny<byte[]>()))
                .Callback<IPEndPoint, byte[]>((remote, data) => 
                {
                    _actualEvent.Add(new TestEvent(remote, (byte[])data.Clone()));
                });
            _networkMoq
                .Setup(s => s.Disconnect(It.IsAny<IPEndPoint>(), It.IsAny<bool>()))
                .Callback<IPEndPoint, bool>((remote, inactive) =>
                {
                    _actualEvent.Add(new TestEvent(remote, inactive));
                    _networkMoq.Raise(s => s.ConnectionClosing += null, remote, inactive);
                });

            _core = new CoreApi(_networkMoq.Object, _auth);
        }

        #endregion Constructors

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

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"unauth\",\"Payload\":{}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"NotAuthorized\",\"Reason\":\"User is not logged in\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(0, _auth.GetUsers().Count());
            Assert.AreEqual(false, _auth.TryGet("User1", out _));
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void UnauthorizationTest()
        {
            // Arrage
            AuthorizationTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"unauth\",\"Payload\":{}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _expectedEvent.Add(new TestEvent(_remotes[0], false));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(0, _auth.GetUsers().Count());
            Assert.AreEqual(false, _auth.TryGet("User1", out _));
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void ReAuthorizationWithRenameTest()
        {
            // Arrage
            AuthorizationTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"auth\",\"Payload\":{\"User\":\"Dummy\"}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":0,\"Type\":\"users\",\"Payload\":{\"Users\":[]}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(1, _auth.GetUsers().Count());
            Assert.AreEqual(true, _auth.TryGet("Dummy", out _));
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void DuplicateAuthorizationAnotherAddressTest()
        {
            // Arrage
            AuthorizationTest();
            ConnectionTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"auth\",\"Payload\":{\"User\":\"User1\"}}");
            _expectedEvent.Add(new TestEvent(_remotes[1], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"AuthDuplicate\",\"Reason\":\"User exists\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(1, _auth.GetUsers().Count());
            Assert.AreEqual(true, _auth.TryGet("User1", out _));
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void DuplicateAuthorizationTest()
        {
            // Arrage
            AuthorizationTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"auth\",\"Payload\":{\"User\":\"User1\"}}");
            _expectedEvent.Add(new TestEvent(_remotes[0], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"AuthDuplicate\",\"Reason\":\"User exists\"}}")));

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(1, _auth.GetUsers().Count());
            Assert.AreEqual(true, _auth.TryGet("User1", out _));
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }

        [Test]
        public void AuthorizationTest()
        {
            // Arrage
            var remotes = _remotes.ToArray();
            var users = string.Join(",", _remotes.Select((s, i) => $"\"User{i + 1}\""));
            ConnectionTest();

            var request = PacketFactory.Pack("{\"Id\":1,\"Type\":\"auth\",\"Payload\":{\"User\":\""+ $"User{_remotes.Count}" + "\"}}");
            _expectedEvent.Add(new TestEvent(_remotes[^1], PacketFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _expectedEvent.Add(new TestEvent(_remotes[^1], PacketFactory.Pack("{\"Id\":0,\"Type\":\"users\",\"Payload\":{\"Users\":[" + users + "]}}")));
            foreach (var remote in remotes)
            {
                _expectedEvent.Add(new TestEvent(remote, PacketFactory.Pack("{\"Id\":0,\"Type\":\"users\",\"Payload\":{\"Users\":[\"" + $"User{_remotes.Count}" + "\"]}}")));
            }

            // Act
            _networkMoq.Raise(s => s.PreparePacket += null, _remotes[_remotes.Count - 1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(true, _auth.TryGet($"User{_remotes.Count}", out _));
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
            Assert.IsFalse(_auth.TryGet(_remotes[^1], out _));
            CollectionAssert.AreEqual(_expectedEvent, _actualEvent);
        }
    }
}
