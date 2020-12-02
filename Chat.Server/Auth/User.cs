namespace Chat.Server.Auth
{
    using System.Net;

    public class User : IUser
    {
        public string Name { get; set; }

        public IPEndPoint Remote { get; set; }
    }
}