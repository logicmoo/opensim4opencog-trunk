using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace cogbot.Actions
{
    public class LogoutCommand : Command
    {
        public LogoutCommand(BotClient testClient)
        {
            Name = "logout";
            Description = "Log this avatar out";
            Category = CommandCategory.TestClient;
        }

        public override string Execute(string[] args, UUID fromAgentID)
        {
            string name = Client.ToString();
			Client.ClientManager.Logout(Client);
            return "Logged " + name + " out";
        }
    }
}
