using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace cogbot.Actions.System
{
    public class QuitCommand : Command, SystemApplicationCommand
    {
        public QuitCommand(BotClient testClient)
		{
			Name = "quit";
			Description = "Log all avatars out and shut down";
            Category = CommandCategory.BotClient;
		}

        public override CmdResult Execute(string[] args, UUID fromAgentID, OutputDelegate WriteLine)
		{
            // This is a dummy command. Calls to it should be intercepted and handled specially
            return Failure("This command should not be executed directly");
		}
    }
}
