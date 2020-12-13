namespace Chat.Server.login
{
    using System.Net;

    public class User : IUser
    {
        public string Name { get; set; }

        public IPEndPoint Remote { get; set; }
    }
}