namespace Chat.Api
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    using Chat.Api.Messages;

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

            Register("auth", typeof(AuthorizationBroadcast));
            Register("message", typeof(MessageBroadcast));
        }

        #endregion Constructors

        #region Methods

        public static bool TryPack(IMessage payload, out byte[] buffer) 
        {
            buffer = null;

            if (!_typeToMessage.TryGetValue(payload.GetType(), out string key))
                return false;

            var message = JsonConvert.SerializeObject(new MessageRequest
            {
                Type = key,
                Payload = payload,
            });

            buffer = Encoding.UTF8.GetBytes(message);

            buffer =
                BitConverter.GetBytes((ushort)buffer.Length)
                .Concat(buffer)
                .ToArray();

            return true;
        }

        public static bool TryUnpack(byte[] buffer, ref int offset, int count, out MessageRequest request)
        {
            request = null;

            if (buffer == null || count - offset <= HEADER_SIZE)
                return false;
            
            var length = BitConverter.ToUInt16(buffer);
            if (count - offset < HEADER_SIZE + length)
                return false;

            var message = Encoding.UTF8.GetString(buffer, HEADER_SIZE, length);

            request = JsonConvert.DeserializeObject<MessageRequest>(message);

            if (!_messageToType.TryGetValue(request.Type, out Type type))
                return false;

            request.Payload = JsonConvert.DeserializeObject(request.Payload.ToString(), type);

            offset += HEADER_SIZE + length;
            return true;
        }

        private static void Register(string key, Type type)
        {
            _messageToType[key] = type;
            _typeToMessage[type] = key;
        }

        #endregion Methods
    }
}
