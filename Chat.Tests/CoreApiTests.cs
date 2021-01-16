namespace Chat.Tests
{
    using System;
    using System.Net;
    using System.Text;
    using System.Linq;
    using System.Collections.Generic;

    using Moq;
    using NUnit.Framework;

    using Chat.Api;

    using Chat.Server;
    using Chat.Server.API;
    using Chat.Server.Auth;
    using Chat.Server.Call;
    using Chat.Server.Audio;

    [TestFixture]
    public class CoreApiTests
    {
        #region Properties

        public List<TestEvent> ActualEvent { get; private set; }
        public List<TestEvent> ExpectedEvent { get; private set; }

        public List<IPEndPoint> Remotes { get; private set; }
        public List<IAudioRouter> Routers { get; private set; }
        public MessageFactory MessageFactory { get; private set; }
        public CoreApi Core { get; private set; }
        public ICallingController Calls { get; private set; }
        public IAuthorizationController Authorization { get; private set; }

        public Mock<ITcpСontroller> NetworkMoq { get; private set; }

        #endregion Properties

        #region Constructors

        [SetUp]
        public void SetUp() 
        {
            ActualEvent = new List<TestEvent>();
            ExpectedEvent = new List<TestEvent>();

            Remotes = new List<IPEndPoint>();
            Routers = new List<IAudioRouter>();

            NetworkMoq = new Mock<ITcpСontroller>();
            NetworkMoq
                .Setup(s => s.Send(It.IsAny<IPEndPoint>(), It.IsAny<ArraySegment<byte>>()))
                .Callback<IPEndPoint, ArraySegment<byte>>((remote, data) =>
                {
                    ActualEvent.Add(new TestEvent(remote, data.ToArray()));
                });
            NetworkMoq
                .Setup(s => s.Disconnect(It.IsAny<IPEndPoint>()))
                .Callback<IPEndPoint>((remote) =>
                {
                    ActualEvent.Add(new TestEvent(remote));
                    NetworkMoq.Raise(s => s.ConnectionClosing += null, remote);
                });

            MessageFactory = new MessageFactory(true);
            Core = new CoreApi(NetworkMoq.Object, MessageFactory);
            Calls = new CallController((container) =>
            {
                Routers.Add(new BridgeRouter(container, new AudioProvider(NetworkMoq.Object)));
                return Routers[^1];
            });
            Authorization = new AuthorizationController();

            new AuthApi(Core, Authorization);
            new TextApi(Core, Authorization);
            new CallApi(Core, Authorization, Calls);
        }

        #endregion Constructors

        #region Methods

        [Test]
        public void UnauthorizationNotLogginTest()
        {
            // Arrange
            ConnectionTest();

            var request = MessageFactory.Pack("{\"Id\":1,\"Type\":\"logout\",\"Payload\":{}}");
            ExpectedEvent.Add(new TestEvent(Remotes[0], MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"NotAuthorized\",\"Reason\":\"User is not logged in\"}}")));

            // Act
            NetworkMoq.Raise(s => s.PreparePacket += null, Remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(0, Authorization.GetUsers().Count());
            Assert.AreEqual(false, Authorization.TryGet("User1", out _));
            CollectionAssert.AreEqual(ExpectedEvent, ActualEvent);
        }

        [Test]
        public void UnauthorizationTest()
        {
            // Arrange
            AuthorizationTest();
            AuthorizationTest();

            var request = MessageFactory.Pack("{\"Id\":1,\"Type\":\"logout\",\"Payload\":{}}");
            ExpectedEvent.Add(new TestEvent(Remotes[0], MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            ExpectedEvent.Add(new TestEvent(Remotes[0]));
            ExpectedEvent.Add(new TestEvent(Remotes[1], MessageFactory.Pack("{\"Id\":0,\"Type\":\"user-offline\",\"Payload\":{\"User\":\"User1\"}}")));

            // Act
            NetworkMoq.Raise(s => s.PreparePacket += null, Remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(1, Authorization.GetUsers().Count());
            Assert.AreEqual(false, Authorization.TryGet("User1", out _));
            CollectionAssert.AreEqual(ExpectedEvent, ActualEvent);
        }

        [Test]
        public void ReAuthorizationWithRenameTest()
        {
            // Arrange
            AuthorizationTest();

            var request = MessageFactory.Pack("{\"Id\":1,\"Type\":\"login\",\"Payload\":{\"User\":\"Dummy\"}}");
            ExpectedEvent.Add(new TestEvent(Remotes[0], MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            ExpectedEvent.Add(new TestEvent(Remotes[0], MessageFactory.Pack("{\"Id\":0,\"Type\":\"users\",\"Payload\":{\"Users\":[]}}")));

            // Act
            NetworkMoq.Raise(s => s.PreparePacket += null, Remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(1, Authorization.GetUsers().Count());
            Assert.AreEqual(true, Authorization.TryGet("Dummy", out _));
            CollectionAssert.AreEqual(ExpectedEvent, ActualEvent);
        }

        [Test]
        public void DuplicateAuthorizationAnotherAddressTest()
        {
            // Arrange
            AuthorizationTest();
            ConnectionTest();

            var request = MessageFactory.Pack("{\"Id\":1,\"Type\":\"login\",\"Payload\":{\"User\":\"User1\"}}");
            ExpectedEvent.Add(new TestEvent(Remotes[1], MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"AuthDuplicate\",\"Reason\":\"User exists\"}}")));

            // Act
            NetworkMoq.Raise(s => s.PreparePacket += null, Remotes[1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(1, Authorization.GetUsers().Count());
            Assert.AreEqual(true, Authorization.TryGet("User1", out _));
            CollectionAssert.AreEqual(ExpectedEvent, ActualEvent);
        }

        [Test]
        public void DuplicateAuthorizationTest()
        {
            // Arrange
            AuthorizationTest();

            var request = MessageFactory.Pack("{\"Id\":1,\"Type\":\"login\",\"Payload\":{\"User\":\"User1\"}}");
            ExpectedEvent.Add(new TestEvent(Remotes[0], MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"AuthDuplicate\",\"Reason\":\"User exists\"}}")));

            // Act
            NetworkMoq.Raise(s => s.PreparePacket += null, Remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(1, Authorization.GetUsers().Count());
            Assert.AreEqual(true, Authorization.TryGet("User1", out _));
            CollectionAssert.AreEqual(ExpectedEvent, ActualEvent);
        }

        [Test]
        public void AuthorizationTest()
        {
            // Arrange
            var remotes = Remotes.ToArray();
            var users = string.Join(",", Remotes.Select((s, i) => $"\"User{i + 1}\""));
            ConnectionTest();

            var request = MessageFactory.Pack("{\"Id\":1,\"Type\":\"login\",\"Payload\":{\"User\":\""+ $"User{Remotes.Count}" + "\"}}");
            ExpectedEvent.Add(new TestEvent(Remotes[^1], MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            ExpectedEvent.Add(new TestEvent(Remotes[^1], MessageFactory.Pack("{\"Id\":0,\"Type\":\"users\",\"Payload\":{\"Users\":[" + users + "]}}")));
            foreach (var remote in remotes)
            {
                ExpectedEvent.Add(new TestEvent(remote, MessageFactory.Pack("{\"Id\":0,\"Type\":\"users\",\"Payload\":{\"Users\":[\"" + $"User{Remotes.Count}" + "\"]}}")));
            }

            // Act
            NetworkMoq.Raise(s => s.PreparePacket += null, Remotes[^1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(true, Authorization.TryGet($"User{Remotes.Count}", out _));
            CollectionAssert.AreEqual(ExpectedEvent, ActualEvent);
        }

        [Test]
        public void AuthorizationWithInvalidParametersTest()
        {
            // Arrange
            ConnectionTest();

            var request = MessageFactory.Pack("{\"Id\":1,\"Type\":\"login\",\"Payload\":{\"User\":\"\"}}");
            ExpectedEvent.Add(new TestEvent(Remotes[^1], MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Failure\",\"Reason\":\"Invalid parameters: User\"}}")));

            // Act
            NetworkMoq.Raise(s => s.PreparePacket += null, Remotes[^1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(false, Authorization.TryGet(Remotes[^1], out _));
            CollectionAssert.AreEqual(ExpectedEvent, ActualEvent);
        }

        [Test]
        public void ConnectionTest() 
        {
            // Arrange
            Remotes.Add(new IPEndPoint(IPAddress.Parse($"127.0.0.{Remotes.Count + 1}"), 5000));

            // Act
            NetworkMoq.Raise(s => s.ConnectionAccepted += null, Remotes[^1]);

            // Assert
            Assert.AreEqual(false, Authorization.TryGet(Remotes[^1], out _));
            CollectionAssert.AreEqual(ExpectedEvent, ActualEvent);
        }

        [Test]
        public void DisconnectionTest()
        {
            // Arrange
            AuthorizationTest();
            AuthorizationTest();

            ExpectedEvent.Add(new TestEvent(Remotes[0], MessageFactory.Pack("{\"Id\":0,\"Type\":\"user-offline\",\"Payload\":{\"User\":\"User2\"}}")));

            // Act
            NetworkMoq.Raise(s => s.ConnectionClosing += null, Remotes[^1]);

            // Assert
            Assert.AreEqual(false, Authorization.TryGet(Remotes[^1], out _));
            CollectionAssert.AreEqual(ExpectedEvent, ActualEvent);
        }

        #endregion Methods
    }
}
