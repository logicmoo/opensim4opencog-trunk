using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using OpenMetaverse;
using OpenMetaverse.Packets;

using MushDLR223.ScriptEngines;

namespace cogbot.Actions.System
{
    public class LoadCommand : Command, BotSystemCommand
    {
        public LoadCommand(BotClient testClient)
		{
			Name = "load";
			Description = "Loads commands from a dll. (Usage: load AssemblyNameWithoutExtension)";
            Category = CommandCategory.BotClient;
		}

		public override CmdResult Execute(string[] args, UUID fromAgentID, OutputDelegate WriteLine)
		{
			if (args.Length < 1)
				return ShowUsage();// " load AssemblyNameWithoutExtension";

            BotClient Client = TheBotClient;

			string filename = AppDomain.CurrentDomain.BaseDirectory + args[0];
		    string loadfilename = filename;
            if (!filename.EndsWith(".dll") && !filename.EndsWith(".exe"))
            {
                if (!File.Exists(loadfilename + "."))
                {
                    foreach (var s in new[] {"dll", "exe", "jar", "lib", "dynlib"})
                    {
                       if(File.Exists(loadfilename + "." + s))
                       {
                           loadfilename += ("." + s);
                           break;
                       }

                    }
                }
            }
            try
            {
                Assembly assembly = Assembly.LoadFile(loadfilename);
                ClientManager.SingleInstance.RegisterAssembly(assembly);
                Client.LoadAssembly(assembly);
                return Success("Assembly " + filename + " loaded.");
            }
            catch (Exception e)
            {                
                   return Failure("failed: load " + filename + " " +e);
            }
		}
    }
}
