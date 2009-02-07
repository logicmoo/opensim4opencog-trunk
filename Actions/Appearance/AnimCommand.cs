using System;
using OpenMetaverse;
using System.Collections.Generic;
using System.Threading;

namespace cogbot.Actions
{
    public class AnimCommand : Command
    {
        public AnimCommand(BotClient testClient)
        {
            Client = testClient;
            Name = "anim";
            Description = "Do a amination or gesture.  Usage:  anim [1-10] aminname";
            Category = CommandCategory.Appearance;
        }
       
        public override string Execute(string[] args, UUID fromAgentID)
        {
            if (args.Length < 1)
            {
                ICollection<string> list = Listeners.WorldObjects.GetAnimationList();
               WriteLine(Client.argsListString(list));
               return "Usage:  anim [seconds] HOVER [seconds] 23423423423-4234234234-234234234-23423423  +CLAP -JUMP STAND";
           }
            int time = 1300; //should be long enough for most animations
            List<KeyValuePair<UUID, int>> amins = new List<KeyValuePair<UUID, int>>();
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (String.IsNullOrEmpty(a)) continue;
                if (time < 1) time = 1300;
                try
                {
                    float ia;
                    if (float.TryParse(a, out ia))
                    {
                        if (ia > 0.0)
                        {
                            time = (int)(ia * 1000);
                            continue;
                        }
                    }
                }
                catch (Exception) { }
                char c = a.ToCharArray()[0];
                if (c == '-')
                {
                    time = -1;
                    a = a.Substring(1);
                }
                else if (c == '+')
                {
                    time = 0;
                    a = a.Substring(1);
                }
                UUID anim = Listeners.WorldObjects.GetAnimationUUID(a);

                if (anim == UUID.Zero)
                {
                    try
                    {
                        if (a.Substring(2).Contains("-"))
                            anim = UUIDParse(a);
                    }
                    catch (Exception) { }
                }
                if (anim == UUID.Zero)
                {
                    WriteLine("unknown animation " + a);
                    continue;
                }
                amins.Add(new KeyValuePair<UUID,int>(anim,time));
            }
            foreach(KeyValuePair<UUID,int> anim in amins) {
                int val = anim.Value;
                switch (val)
                {
                    case -1:
                        WriteLine("Stop anim " + Listeners.WorldObjects.GetAnimationName(anim.Key));
                        Client.Self.AnimationStop(anim.Key, true);
                        continue;
                    case 0:
                        WriteLine("Start anim " + Listeners.WorldObjects.GetAnimationName(anim.Key));
                        Client.Self.AnimationStart(anim.Key, true);
                        continue;
                    default:
                        Client.Self.AnimationStart(anim.Key, true);
                        WriteLine("Run anim " + Listeners.WorldObjects.GetAnimationName(anim.Key) + " for " + val / 1000 + " seconds.");
                        Thread.Sleep(val);
                        Client.Self.AnimationStop(anim.Key, true);
                        continue;
                }
            }
            return "Ran "+amins.Count+" amins";
        }
    }
}
