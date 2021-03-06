using System;
using System.Collections.Generic;
using System.Text;
using Cogbot.World;
using OpenMetaverse;
using System.Threading;
using PathSystem3D.Navigation; //using libsecondlife;
// older LibOMV
//using TeleportFlags = OpenMetaverse.AgentManager.TeleportFlags;
//using TeleportStatus = OpenMetaverse.AgentManager.TeleportStatus;
using MushDLR223.ScriptEngines;

namespace Cogbot.Actions.Agent
{
#pragma warning disable 0168
    internal class Teleport : Command, BotPersonalCommand
    {
        private ManualResetEvent TeleportFinished = new ManualResetEvent(false);

        public Teleport(BotClient testClient)
            : base(testClient)
        {
            TheBotClient = testClient;
            Category = CommandCategory.Movement;
        }

        public override void MakeInfo()
        {
            Description = "Teleport to a location defined by an avatar, object, or position";
            Details =
                @"<p>teleport  &lt;location&gt;</p>
<p>example: teleport Zindra/112.3/114.4/23</p>
<p>example: teleport Fluffybunny Resident</p>
<p>example: teleport nth 3 Ship <i>teleports to 3rd nearest object named Ship</i></p>";
            Parameters = CreateParams("to", typeof (SimPosition),
                                      "Location to TP to. Can be an avatar, object, or position. See <a href='wiki/BotCommands#Location'>Locations</a>");
            ResultMap = CreateParams(
                "message", typeof (string), "if we could not teleport, the reason why",
                "success", typeof (bool), "true if the teleport succeeded");
        }

        //string message, TeleportStatus status, TeleportFlags flags
        public void On_Teleport(object sender, TeleportEventArgs e)
        {
            BotClient Client = TheBotClient;
            WriteLine(e + " " + e.Status);
            if (e.Status == TeleportStatus.Finished)
            {
                Client.Self.TeleportProgress -= On_Teleport;
                TeleportFinished.Set();
            }
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            string verb = args.CmdName;
            acceptInput0(verb, args);
            return Success(verb + " complete");
        }

        private void acceptInput0(string verb, Parser parser)
        {
            String[] args = parser.GetProperty("to");
            string ToS = Parser.Rejoin(args, 0);
            if (String.IsNullOrEmpty(ToS))
            {
                ToS = parser.str;
            }
            int argUsed;
            if (ToS == "home")
            {
                Client.Self.GoHome();
                AddSuccess("teleporting home");
                return;
            }
            SimPosition pos = WorldSystem.GetVector(args, out argUsed);
            if (argUsed > 0)
            {
                Vector3d global = pos.GlobalPosition;
                WriteLine("Teleporting to " + pos + "...");
                float x, y;
                TheSimAvatar.StopMoving();
                bool res =
                    Client.Self.Teleport(
                        SimRegion.GlobalPosToRegionHandle((float) global.X, (float) global.Y, out x, out y),
                        pos.SimPosition, pos.SimPosition);
                if (res)
                {
                    AddSuccess("Teleported to " + pos);
                }
                else
                {
                    Failure("Teleport Failed to " + pos);
                }
                return;
            }
            char[] splitchar = null;
            if (ToS.Contains("/"))
            {
                splitchar = new char[] {'/'};
            }
            string[] tokens = ToS.Split(splitchar);
            if (tokens.Length == 0)
            {
                WriteLine("Provide somewhere to teleport to.");
            }
            else
            {
                Vector3 coords = new Vector3(128, 128, 40);
                string simName = ""; //CurSim.Name;

                bool ifCoordinates = false;

                if (tokens.Length >= 3)
                {
                    try
                    {
                        coords.X = float.Parse(tokens[tokens.Length - 3]);
                        coords.Y = float.Parse(tokens[tokens.Length - 2]);
                        coords.Z = float.Parse(tokens[tokens.Length - 1]);
                        ifCoordinates = true;
                    }
                    catch (Exception e)
                    {
                    }
                }

                if (!ifCoordinates)
                {
                    for (int i = 0; i < tokens.Length; i++)
                        simName += tokens[i] + " ";
                    simName = simName.Trim();
                }
                else
                {
                    for (int i = 0; i < tokens.Length - 3; i++)
                        simName += tokens[i] + " ";
                    simName = simName.Trim();
                }
                {
                    if (String.IsNullOrEmpty(simName)) simName = Client.Network.CurrentSim.Name;
                    TeleportFinished.Reset();
                    Client.Self.TeleportProgress += On_Teleport;
                    WriteLine("Trying to teleport to " + simName + " " + coords);
                    Client.Self.Teleport(simName, coords);
                    // wait 30 seconds
                    if (!TeleportFinished.WaitOne(30000, false))
                    {
                        Client.Self.TeleportProgress -= On_Teleport;
                        WriteLine("Timeout on teleport to " + simName + " " + coords);
                    }
                }
            }
        }
    }
#pragma warning restore 0168
}