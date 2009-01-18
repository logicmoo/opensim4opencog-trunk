using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace cogbot.Actions
{
    public class LocationCommand: Command
    {
        public LocationCommand(BotClient testClient)
		{
			Name = "location";
			Description = "Show current location of avatar.";
            Category = CommandCategory.Movement;
		}

		public override string Execute(string[] args, UUID fromAgentID)
		{
            return "CurrentSim: '" + Client.Network.CurrentSim.ToString() + "' Position: " + 
                Client.Self.SimPosition.ToString();
		}
    }
}
