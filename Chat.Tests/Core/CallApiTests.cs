namespace Chat.Tests.Core
{
    using System;
    using System.Linq;
    using System.Net;

    using NUnit.Framework;

    using Chat.Audio;
    using Chat.Server.Call;
    using Chat.Api.Messages.Call;

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
        public void GetAudio_ActiveCall_SendOnRoutes()
        {
            // Arrange
            CallInvite_CallingState_InActive();

            _coreTests.Authorization.TryGet("User1", out var user1);
            _coreTests.Authorization.TryGet("User2", out var user2);

            var route1 = new IPEndPoint(user1.Remote.Address, 8888);
            var sended1 = new AudioPacket
            {
                RouteId = 1,
                SequenceId = 100,
                Payload = new byte[] { 1 },
            }
            .Pack();

            var route2 = new IPEndPoint(user2.Remote.Address, 7777);
            var sended2 = new AudioPacket
            {
                RouteId = 1,
                SequenceId = 200,
                Payload = new byte[] { 2 },
            }
            .Pack();

            var received2 = sended1;
            var received1 = sended2;

            _coreTests.ExpectedEvent.Add(new TestEvent(route2, received2));
            _coreTests.ExpectedEvent.Add(new TestEvent(route1, received1));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, route1, sended1.Array, sended1.Offset, sended1.Count);
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, route2, sended2.Array, sended2.Offset, sended2.Count);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void Disconnect_ActiveCall_InIdle()
        {
            // Arrange
            CallInvite_CallingState_InActive();

            _coreTests.Authorization.TryGet("User1", out var user1);

            var expectedId = -1951180698;
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\"],\"State\":\"Idle\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":0,\"Type\":\"user-offline\",\"Payload\":{\"User\":\"User2\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ConnectionClosing += null, _coreTests.Remotes[^1]);

            // Assert
            Assert.AreEqual(false, _coreTests.Authorization.TryGet(_coreTests.Remotes[^1], out _));
            Assert.AreEqual(false, _coreTests.Calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(0, _coreTests.Routers[^1].Count);
            Assert.AreEqual(true, _coreTests.Container.HasReleased(1));
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void CallCancel_ActiveState_InIdle()
        {
            // Arrange
            CallInvite_CallingState_InActive();

            _coreTests.Authorization.TryGet("User1", out var user1);

            var expectedId = -1951180698;
            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-cancel\",\"Payload\":{\"SessionId\":-1951180698}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\"],\"State\":\"Idle\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, _coreTests.Remotes[1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(false, _coreTests.Calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(0, _coreTests.Routers[^1].Count);
            Assert.AreEqual(true, _coreTests.Container.HasReleased(1));
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void CallReject_CallingState_InIdle()
        {
            // Arrange
            CallInit_Correct_InCalling();

            _coreTests.Authorization.TryGet("User1", out var user1);

            var expectedId = -1951180698;
            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-cancel\",\"Payload\":{\"SessionId\":-1951180698}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\"],\"State\":\"Idle\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, _coreTests.Remotes[1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(false, _coreTests.Calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(0, _coreTests.Routers[^1].Count);
            Assert.AreEqual(true, _coreTests.Container.HasReleased(1));
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void CallInvite_NotExistsCall_Rejected()
        {
            // Arrange
            _coreTests.AuthorizationTest();

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-invite\",\"Payload\":{\"SessionId\":-1951180698,\"RoutePort\":7777}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"CallNotFound\",\"Reason\":\"Call not found\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void CallInvite_NotLoggin_Rejected()
        {
            // Arrange
            _coreTests.ConnectionTest();

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-invite\",\"Payload\":{\"SessionId\":-1951180698,\"RoutePort\":7777}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"NotAuthorized\",\"Reason\":\"User is not logged in\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void CallInvite_OutRangeRouteId_Rejected()
        {
            // Arrange
            CallInit_Correct_InCalling();

            _coreTests.Authorization.TryGet("User1", out var user1);
            _coreTests.Authorization.TryGet("User2", out var user2);

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-invite\",\"Payload\":{\"SessionId\":-1951180698,\"RoutePort\":77777}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Failure\",\"Reason\":\"Invalid parameters: RoutePort\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, _coreTests.Remotes[1], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void CallInvite_WithZeroRouteId_Rejected()
        {
            // Arrange
            CallInit_Correct_InCalling();

            _coreTests.Authorization.TryGet("User1", out var user1);
            _coreTests.Authorization.TryGet("User2", out var user2);

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-invite\",\"Payload\":{\"SessionId\":-1951180698,\"RoutePort\":0}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Failure\",\"Reason\":\"Invalid parameters: RoutePort\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, _coreTests.Remotes[1], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void CallInvite_CallingState_InActive()
        {
            // Arrange
            CallInit_Correct_InCalling();

            _coreTests.Authorization.TryGet("User1", out var user1);
            _coreTests.Authorization.TryGet("User2", out var user2);

            var expectedId = -1951180698;
            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-invite\",\"Payload\":{\"SessionId\":-1951180698,\"RoutePort\":7777}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-response\",\"Payload\":{\"SessionId\":-1951180698,\"RouteId\":1}}")));

            foreach (var remote in _coreTests.Remotes)
            {
                _coreTests.ExpectedEvent.Add(new TestEvent(remote, _coreTests.MessageFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\",\"User2\"],\"State\":\"Active\"}}")));
            }

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, _coreTests.Remotes[1], request, 0, request.Length);

            // Assert
            Assert.AreEqual(true, _coreTests.Calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(true, session.Contains(user1));
            Assert.AreEqual(true, session.Contains(user2));
            Assert.AreEqual(2, session.GetParticipants().Count());
            Assert.AreEqual(2, _coreTests.Routers[^1].Count);
            Assert.AreEqual(true, _coreTests.Routers[^1].Contains(new IPEndPoint(user1.Remote.Address, 8888)));
            Assert.AreEqual(true, _coreTests.Routers[^1].Contains(new IPEndPoint(user2.Remote.Address, 7777)));
            Assert.AreEqual(expectedId, session.Id);
            Assert.AreEqual(CallState.Active, session.State);
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void CallInit_NotLoggin_Rejected()
        {
            // Arrange
            _coreTests.ConnectionTest();

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-request\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"RoutePort\":8888}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"NotAuthorized\",\"Reason\":\"User is not logged in\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void CallInit_Duplicate_Rejected()
        {
            // Arrange
            CallInit_Correct_InCalling();

            _coreTests.Authorization.TryGet("User1", out var user1);
            var expectedId = -1951180698;
            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-request\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"RoutePort\":8888}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"CallDuplicate\",\"Reason\":\"Call exists\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(true, _coreTests.Calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(2, session.GetParticipants().Count());
            Assert.AreEqual(1, _coreTests.Routers[^1].Count);
            Assert.AreEqual(true, _coreTests.Routers[^1].Contains(new IPEndPoint(user1.Remote.Address, 8888)));
            Assert.AreEqual(expectedId, session.Id);
            Assert.AreEqual(CallState.Calling, session.State);
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void CallInit_WithInvalidParameters_Rejected()
        {
            // Arrange
            _coreTests.AuthorizationTest();
            _coreTests.AuthorizationTest();

            _coreTests.Authorization.TryGet("User1", out var user1);
            _coreTests.Authorization.TryGet("User2", out var user2);

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-request\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"RoutePort\":88888}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Failure\",\"Reason\":\"Invalid parameters: RoutePort\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void CallInit_NotExistsTarget_Rejected()
        {
            // Arrange
            _coreTests.AuthorizationTest();

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-request\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"RoutePort\":8888}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"UserNotFound\",\"Reason\":\"Target not found\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void CallInit_TargetIsMe_Rejected()
        {
            // Arrange
            _coreTests.AuthorizationTest();

            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-request\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User1\",\"RoutePort\":8888}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Failure\",\"Reason\":\"Don't target at yourself\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        [Test]
        public void CallInit_Correct_InCalling()
        {
            // Arrange
            _coreTests.AuthorizationTest();
            _coreTests.AuthorizationTest();

            _coreTests.Authorization.TryGet("User1", out var user1);
            _coreTests.Authorization.TryGet("User2", out var user2);

            var expectedId = -1951180698;
            var request = _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-request\",\"Payload\":{\"Source\":\"User1\",\"Target\":\"User2\",\"RoutePort\":8888}}");
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"result\",\"Payload\":{\"Status\":\"Success\",\"Reason\":\"\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":1,\"Type\":\"call-response\",\"Payload\":{\"SessionId\":-1951180698,\"RouteId\":1}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[0], _coreTests.MessageFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\",\"User2\"],\"State\":\"Calling\"}}")));
            _coreTests.ExpectedEvent.Add(new TestEvent(_coreTests.Remotes[1], _coreTests.MessageFactory.Pack("{\"Id\":0,\"Type\":\"call-broadcast\",\"Payload\":{\"SessionId\":-1951180698,\"Participants\":[\"User1\",\"User2\"],\"State\":\"Calling\"}}")));

            // Act
            _coreTests.NetworkMoq.Raise(s => s.ReceivedFrom += null, _coreTests.Remotes[0], request, 0, request.Length);

            // Assert
            Assert.AreEqual(true, _coreTests.Calls.TryGet(expectedId, out ICallSession session));
            Assert.AreEqual(true, session.Contains(user1));
            Assert.AreEqual(true, session.Contains(user2));
            Assert.AreEqual(2, session.GetParticipants().Count());
            Assert.AreEqual(1, _coreTests.Routers[^1].Count);
            Assert.AreEqual(false, _coreTests.Container.HasReleased(1));
            Assert.AreEqual(true, _coreTests.Routers[^1].Contains(new IPEndPoint(user1.Remote.Address, 8888)));
            Assert.AreEqual(expectedId, session.Id);
            Assert.AreEqual(CallState.Calling, session.State);
            CollectionAssert.AreEqual(_coreTests.ExpectedEvent, _coreTests.ActualEvent);
        }

        #endregion Methods
    }
}
