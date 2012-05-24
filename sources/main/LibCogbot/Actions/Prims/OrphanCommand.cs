﻿using System.Collections.Generic;
using cogbot.Listeners;
using cogbot.TheOpenSims;
using OpenMetaverse;

using MushDLR223.ScriptEngines;

namespace cogbot.Actions.SimExport
{
    public class OrphanCommand : cogbot.Actions.Command, RegionMasterCommand
    {
        public OrphanCommand(BotClient client)
        {
            Name = "orphans";
            Description = "Finds objects without locations [prim]";
            Category = cogbot.Actions.CommandCategory.Objects;
            Parameters = new[] { new NamedParam(typeof(SimObject), typeof(UUID)) };
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            int argsUsed;
            List<SimObject> objs = WorldSystem.GetPrimitives(args, out argsUsed);
            int detatched = 0;
            int orphans = 0;
            int missingSculpties = 0;
            foreach (SimObjectImpl o in objs)
            {
                if (!o.IsRoot)
                {
                    if (o.Parent == null)
                    {
                        orphans++;
                        WriteLine("Orphans: " + o);
                    }
                }
                if (!o.IsRegionAttached)
                {
                    detatched++;
                    WriteLine("Detatched " + o);
                }
                
                if (o.IsSculpted)
                {
                    Primitive p = o.Prim;
                    if (WorldSystem.StartTextureDownload(p.Sculpt.SculptTexture)==null)
                    {
                        missingSculpties++;
                        WriteLine("IsSculpted " + o);
                    }
                }
            }
            return Success("object examinined " + objs.Count + " detacted: " + detatched + " orphans: " + orphans + " missingScuplty: " + missingSculpties);
        }
    }
}