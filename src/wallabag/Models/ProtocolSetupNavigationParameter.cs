namespace wallabag.Models
{
    public class ProtocolSetupNavigationParameter
    {
        public ProtocolSetupNavigationParameter(string username, string server)
        {
            Username = username;
            Server = server;
        }

        public string Username { get; set; }
        public string Server { get; set; }
    }
}
