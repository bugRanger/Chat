namespace Chat.Api
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    using Chat.Api.Messages;
    using Chat.Api.Messages.Auth;
    using Chat.Api.Messages.Text;
    using Chat.Api.Messages.Call;

    public static class PacketFactory
    {
        #region Constants

        private const int HEADER_SIZE = 2;

        #endregion Constants

        #region Fields

        private static readonly Dictionary<string, Type> _messageToType;
        private static readonly Dictionary<Type, string> _typeToMessage;

        #endregion Fields

        #region Constructors

        static PacketFactory() 
        {
            _messageToType = new Dictionary<string, Type>();
            _typeToMessage = new Dictionary<Type, string>();

            Register("auth", typeof(AuthorizationRequest));
            Register("unauth", typeof(UnauthorizationRequest));
            Register("users", typeof(UsersBroadcast));
            Register("result", typeof(MessageResult));
            Register("message", typeof(MessageBroadcast));
            Register("userOffline", typeof(UserOfflineBroadcast));
            Register("call-request", typeof(CallRequest));
            Register("call-broadcast", typeof(CallBroadcast));
            Register("call-response", typeof(CallResponse));
            Register("call-reject", typeof(CallRejectRequest));
        }

        #endregion Constructors

        #region Methods

        public static bool TryPack(int index, IMessage payload, out byte[] buffer) 
        {
            buffer = null;

            if (!_typeToMessage.TryGetValue(payload.GetType(), out string key))
                return false;

            var message = JsonConvert.SerializeObject(new MessageContainer
            {
                Id = index,
                Type = key,
                Payload = payload,
            });

            buffer = Pack(message);

            return true;
        }

        public static bool TryUnpack(byte[] buffer, ref int offset, int count, out MessageContainer request)
        {
            request = null;

            if (buffer == null || count - offset <= HEADER_SIZE)
                return false;
            
            var length = BitConverter.ToUInt16(buffer);
            if (count - offset < HEADER_SIZE + length)
                return false;

            var message = Encoding.UTF8.GetString(buffer, HEADER_SIZE, length);

            request = JsonConvert.DeserializeObject<MessageContainer>(message);

            if (!_messageToType.TryGetValue(request.Type, out Type type))
                return false;

            request.Payload = JsonConvert.DeserializeObject(request.Payload.ToString(), type);

            offset += HEADER_SIZE + length;
            return true;
        }

        internal static byte[] Pack(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            bytes =
                BitConverter.GetBytes((ushort)bytes.Length)
                .Concat(bytes)
                .ToArray();

            return bytes;
        }

        private static void Register(string key, Type type)
        {
            _messageToType[key] = type;
            _typeToMessage[type] = key;
        }

        #endregion Methods
    }
}
