namespace Chat.Api
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Chat.Api.Messages;
    using Chat.Api.Messages.Auth;
    using Chat.Api.Messages.Text;
    using Chat.Api.Messages.Call;

    public class MessageFactory : IMessageFactory
    {
        #region Constants

        private const int HEADER_SIZE = 4;

        #endregion Constants

        #region Fields

        private static readonly JsonSerializer _serializer;

        private static readonly Dictionary<string, Type> _messageToType;
        private static readonly Dictionary<Type, string> _typeToMessage;

        #endregion Fields

        #region Properties

        public bool UseHeader { get; }

        #endregion Properties

        #region Constructors

        static MessageFactory() 
        {
            _messageToType = new Dictionary<string, Type>();
            _typeToMessage = new Dictionary<Type, string>();

            _serializer = JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            Register("login", typeof(LoginRequest));
            Register("logout", typeof(LogoutRequest));
            Register("users", typeof(UsersBroadcast));
            Register("result", typeof(MessageResult));
            Register("message", typeof(MessageBroadcast));
            Register("user-offline", typeof(UserOfflineBroadcast));
            Register("call-request", typeof(CallRequest));
            Register("call-response", typeof(CallResponse));
            Register("call-broadcast", typeof(CallBroadcast));
            Register("call-invite", typeof(CallInviteRequest));
            Register("call-cancel", typeof(CallCancelRequest));
        }

        public MessageFactory(bool useHeader)
        {
            UseHeader = useHeader;
        }

        #endregion Constructors

        #region Methods

        public bool TryPack(int index, IMessage payload, out byte[] buffer) 
        {
            buffer = null;

            if (!_typeToMessage.TryGetValue(payload.GetType(), out string key))
            {
                return false;
            }

            using (var writter = new StringWriter())
            using (var jsonWritter = new JsonTextWriter(writter))
            {
                _serializer.Serialize(jsonWritter, new MessageContainer
                {
                    Id = index,
                    Type = key,
                    Payload = payload,
                });
                buffer = Pack(writter.ToString());
            }

            return true;
        }

        public bool TryUnpack(byte[] buffer, ref int offset, int count, out MessageContainer request)
        {
            request = null;

            if (buffer == null)
                return false;

            int length = count;
            int tempOffset = 0;

            if (UseHeader)
            {
                if (count <= HEADER_SIZE)
                    return false;

                length = BitConverter.ToInt32(buffer);
                tempOffset = HEADER_SIZE;
            }

            using (var memory = new MemoryStream(buffer, tempOffset, length))
            using (var reader = new StreamReader(memory, Encoding.UTF8))
            using (var jsonReader = new JsonTextReader(reader))
            {
                request = _serializer.Deserialize(jsonReader, typeof(MessageContainer)) as MessageContainer;
            }

            if (!_messageToType.TryGetValue(request.Type, out Type type))
                return false;

            request.Payload = ((JObject)request.Payload).ToObject(type, _serializer);

            offset += tempOffset + length;
            return true;
        }

        internal byte[] Pack(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);

            if (UseHeader)
            {
                bytes =
                    BitConverter.GetBytes(bytes.Length)
                    .Concat(bytes)
                    .ToArray();
            }

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
