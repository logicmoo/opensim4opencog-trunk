/******************************************************************************************
  Cogbot -- Copyright (c) 2008-2012, Douglas Miles, Kino Coursey, Daxtron Labs, Logicmoo
      and the Cogbot Development Team.
   
  Major contributions from (and special thanks to):
      Latif Kalif, Anne Ogborn and Openmeteverse Foundation

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute,
sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
**************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using Cogbot;
using Cogbot.World;
using MushDLR223.Utilities;
using OpenMetaverse;
using OpenMetaverse.Packets;

using MushDLR223.ScriptEngines;

namespace Cogbot.Actions.Agent
{
    public class WhoCommand: Command, RegionMasterCommand, FFIComplete
    {
        public WhoCommand(BotClient testClient)
        {
            Name = "Who";
            if (Reloading(testClient)) return;
            Description = "Lists seen avatars.";
            Category = CommandCategory.Other;
            AddVersion(CreateParams(Optional("--presence", typeof(bool), "Use Presence List")), Description);
            ResultMap = CreateParams(
                "avatarList", typeof(List<SimAvatar>), "list of present avatars",
                "presenceList", typeof(List<SimAvatar>), "list of present avatars",
                "message", typeof(string), "if success was false, the reason why",
                "success", typeof (bool), "true if command was successful");
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            bool writeInfo = !args.IsFFI;
            if (args.ContainsFlag("--presence"))
            {
                foreach (var A in WorldObjects.SimAvatars)
                {
                    AppendMap(Results, "presenceList", A);
                    if (writeInfo) WriteLine(A.ToString() + " local=" + A.IsLocal);
                }
            }
            else
            {
                foreach (Simulator sim in LockInfo.CopyOf(Client.Network.Simulators))
                {
                    if (sim.ObjectsAvatars.Count == 0) continue;
                    if (writeInfo) WriteLine("");
                    if (writeInfo) WriteLine("Region: " + sim);
                    sim.ObjectsAvatars.ForEach(
                        delegate(Avatar av)
                            {
                                AppendMap(Results, "avatarList", av);
                                if (string.IsNullOrEmpty(av.Name))
                                {
                                    Client.Objects.SelectObjects(sim, new uint[] {av.LocalID}, true);
                                }
                                if (writeInfo) WriteLine("");
                                if (writeInfo)
                                    WriteLine(" {0} (Group: {1}, Location: {2}, UUID: {3})",
                                              av.Name, av.GroupName, av.Position, av.ID.ToString());
                            }
                        );
                }
            }
            return SuccessOrFailure();
        }
    }
}
