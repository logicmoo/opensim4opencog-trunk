using System;
using System.Collections;
using System.Collections.Generic;
using cogbot.TheOpenSims;
using MushDLR223.ScriptEngines;
using MushDLR223.Utilities;
using OpenMetaverse;
using System.Reflection;
using Simulator = OpenMetaverse.Simulator;

namespace cogbot.Actions
{
    public enum CommandCategory : int
    {
        Parcel,
        Appearance,
        Movement,
        Simulator,
        Communication,
        Inventory,
        Objects,
        Voice,
        BotClient,
        Friends,
        Groups,
        Other,
        Unknown,
        Search,
        Money,
        Security
    }
    /// <summary>
    /// An interface for commands is only invoked on Region mastering bots
    /// Such as terrain uploads and simulator info (10 bots doing the command at once will create problems)
    /// Non region master bots are thinner clients and usually not fit for object tracking
    /// </summary>
    public interface RegionMasterCommand : BotCommand
    {
    }
    /// <summary>
    /// An interface for commands is only invoked on Grid mastering bots
    /// Such as Directory info requests (10 bots doing the command at once will create problems)   
    /// </summary>
    public interface GridMasterCommand : BotCommand
    {
    }
    /// <summary>
    /// An interface for commands that do not target any specific bots
    ///  Such as pathsystem maintainance or application commands
    ///  The gridClient used though will be GridMaster
    /// </summary>
    public interface SystemApplicationCommand : BotCommand
    {
    }

    /// <summary>
    /// An interface for commands that do not require a connected grid client
    /// such as Login or settings but still targets each bot individually
    /// </summary>
    public interface BotSystemCommand : BotCommand
    {
    }

    /// <summary>
    /// An interface for commands that DO REQUIRE a connected grid client
    /// such as say,jump,movement
    /// </summary>
    public interface BotPersonalCommand : BotCommand
    {
    }

    public interface BotCommand
    {
    }

    public abstract class Command : IComparable
    {
        private OutputDelegate _writeLine;
        public UUID CallerID = UUID.Zero;
        protected OutputDelegate WriteLine
        {
            get { return _writeLine; }
            set
            {
                if (value == null)
                {
                    _writeLine = StaticWriteLine;
                    return;
                }
                _writeLine = value;
            }
        }

        private int success = 0, failure = 0;

        public Command()
            : this(null)
        {
            
        } // constructor

        private void StaticWriteLine(string s, params object[] args)
        {
            if (_mClient != null) _mClient.WriteLine(Name + ": " + s, args);
        }

        public Command(BotClient bc)
        {
            WriteLine = StaticWriteLine;
            Name = GetType().Name.Replace("Command", "");
            _mClient = bc;
            if (!(this is BotCommand))
            {
                DLRConsole.DebugWriteLine("" + this + " is not a BotCommand?!");
            }
            if (this is BotPersonalCommand)
            {
                Parameters = new[] { new NamedParam(typeof(GridClient), null) };
                Category = CommandCategory.Other;
            }
            if (this is BotSystemCommand)
            {
                Parameters = new[] { new NamedParam(typeof(GridClient), null) };
                Category = CommandCategory.Simulator;
            }
            if (this is RegionMasterCommand)
            {
                Parameters = new[] { new NamedParam(typeof(Simulator), null) };
                Category = CommandCategory.Simulator;
            }
            if (this is SystemApplicationCommand)
            {
                Parameters = new[] { new NamedParam(typeof(GridClient), null) };
                Category = CommandCategory.BotClient;
            }
            if (this.GetType().Namespace.ToString() == "cogbot.Actions.Movement")
            {
                Category = CommandCategory.Movement;
            }
        } // constructor



        /// <summary>
        /// 
        /// </summary>
        /// <param name="verb"></param>
        /// <param name="args"></param>
        public virtual CmdResult acceptInput(string verb, Parser args, OutputDelegate WriteLine)
        {
            success = failure = 0;
            this.WriteLine = WriteLine;
            try
            {
                return Execute(args.tokens, CallerID, WriteLine);
            }
            catch (Exception e)
            {
                return Failure("" + e);
            }
        } // method: acceptInput


        public string Description
        {
            get
            {
                return GetDescription();
            }
            set
            {
                if (String.IsNullOrEmpty(value)) return;
                int half = value.ToLower().IndexOf("usage");
                if (half == -1)
                {
                    half = value.ToLower().IndexOf("use");
                }
                if (half==-1)
                {
                    helpString = value;
                    return;
                }
                Description = value.Substring(0, half).TrimEnd();
                Usage = value.Substring(half);

            }
        }

        public string Usage
        {
            get { return makeUsageString(); }
            set
            {
                usageString = value.Trim().Replace("Usage:", " ").Replace("usage:", " ").Replace("Use:", " ").Trim();
            }
        }

        public CommandCategory Category;
        /// <summary>
        /// When set to true, think will be called.
        /// </summary>
        public bool Active;
        public string Name { get; set; }
        protected string helpString;
        protected string usageString;
        /// <summary>
        /// Introspective Parameters for calling command from code
        /// </summary>
        public NamedParam[] Parameters;
        /// <summary>
        /// Called twice per second, when Command.Active is set to true.
        /// </summary>
        public virtual void Think()
        {
        }

        private BotClient _mClient = null;

        public BotClient Client
        {
            get
            {
                return TheBotClient;
            }
            set
            {
                TheBotClient = value;
            }
        }


        public BotClient TheBotClient
        {
            get
            {
                if (_mClient != null) return _mClient;
                return cogbot.Listeners.WorldObjects.GridMaster.client;
            }
            set
            {
                _mClient = value;
            }
        }

        public static ClientManager ClientManager
        {
            get
            {
                return ClientManager.SingleInstance;
            }
        }

        public cogbot.Listeners.WorldObjects WorldSystem
        {
            get
            {
                if (_mClient == null) return cogbot.Listeners.WorldObjects.GridMaster;
                return _mClient.WorldSystem;
            }
        }

        public SimActor TheSimAvatar
        {
            get
            {
                return _mClient.TheSimAvatar;
            }
        }

        public CmdResult acceptInputWrapper(string verb, string args,UUID callerID, OutputDelegate WriteLine)
        {
            CallerID = callerID;
            success = failure = 0;
            this.WriteLine = WriteLine;
            return acceptInput(verb, Parser.ParseArgs(args), WriteLine);
        }

        public virtual CmdResult Execute(string[] args, UUID fromAgentID, OutputDelegate WriteLine)
        {
            CallerID = fromAgentID;
            success = failure = 0;
            this.WriteLine = WriteLine;
            Parser p = Parser.ParseArgs(String.Join(" ", args));
            p.tokens = args;
            try
            {
                return acceptInput(Name, p, WriteLine);
            }
            finally
            {
                //??  WriteLine = StaticWriteLine;
            }
        }

        public virtual CmdResult ExecuteCmd(string[] args, UUID fromAgentID, OutputDelegate WriteLine)
        {
            CallerID = fromAgentID;
            success = failure = 0;
            this.WriteLine = WriteLine;
            Parser p = Parser.ParseArgs(String.Join(" ", args));
            p.tokens = args;
            try
            {
                return acceptInput(Name, p, WriteLine);
            }
            finally
            {
              //??  WriteLine = StaticWriteLine;
            }
        }


        public int CompareTo(object obj)
        {
            if (obj is Command)
            {
                Command c2 = (Command)obj;
                return Category.CompareTo(c2.Category);
            }
            else
                throw new ArgumentException("Object is not of type Command.");
        }
        public virtual string GetDescription()
        {
            if (!string.IsNullOrEmpty(helpString)) return helpString;
            return helpString + "  Usage: " + Usage;
        }


        public virtual string makeHelpString()
        {
            if (!string.IsNullOrEmpty(helpString)) return helpString;
            return helpString;
        }

        public virtual string makeUsageString()
        {
            if (!String.IsNullOrEmpty(usageString)) return usageString;
            return helpString;
        }
                
        // Helpers

        protected Vector3 GetSimPosition()
        {
            return TheSimAvatar.SimPosition;
        }

        public UUID UUIDParse(string p)
        {
            UUID uuid = UUID.Zero;
            int argsUsed;
            if (UUIDTryParse(new[] { p }, 0, out uuid,out argsUsed)) return uuid;
            return UUID.Parse(p);
        }

        public bool UUIDTryParse(string[] args, int start, out UUID target, out int argsUsed)
        {
            if (args==null|| args.Length==0)
            {
                target = UUID.Zero;
                argsUsed = 0;
                return false;
            }
            string p = args[0];
            if (p.Contains("-") && UUID.TryParse(p, out target))
            {
                argsUsed = 1;
                return true;
            }
            List<SimObject> OS = WorldSystem.GetPrimitives(args, out argsUsed);
            if (OS.Count == 1)
            {
                target = OS[0].ID;
                argsUsed = 1;
                return true;
            }
            target = WorldSystem.GetAssetUUID(p, AssetType.Unknown);
            if (target != UUID.Zero)
            {
                argsUsed = 1;
                return true;
            }
            argsUsed = 0;
            return false;
        }

        protected CmdResult Failure(string usage)
        {
            failure++;
            WriteLine(usage);
            DLRConsole.DebugWriteLine(usage);
            return Result(usage, false);
        }

        protected CmdResult Success(string usage)
        {
            success++;

            try
            {
                WriteLine(usage);
            }
            catch (Exception e)
            {

            }
            try
            {
                DLRConsole.DebugWriteLine(usage);
            }
            catch (Exception e)
            {
                DLRConsole.DebugWriteLine(e);
            }
            return Result("Success " + Name, true);
        }

        protected CmdResult Result(string usage, bool tf)
        {
            return new CmdResult(usage, tf);
        }
        protected CmdResult SuccessOrFailure()
        {
            return Result(Name + " " + failure + " failures and " + success + " successes", failure == 0);
        }


        /// <summary>
        /// Show commandusage
        /// </summary>
        /// <returns>CmdResult Failure with a string containing the parameter usage instructions</returns>
        public virtual CmdResult ShowUsage()
        {
            return ShowUsage(Usage);
        }
        public virtual CmdResult ShowUsage(string usg)
        {
            CmdResult res = Failure("Usage: //" +usg);
            res.InvalidArgs = true;
            return res;
        }

        protected bool TryEnumParse(Type type, string[] names, int argStart, out int argsUsed, out object value)
        {
            ulong d = 0;
            argsUsed = 0;
            for (int i = argStart; i < names.Length; i++)
            {
                var name = names[i];

                Object e = null;
                try
                {
                    e = Enum.Parse(type, name);
                }
                catch (ArgumentException)
                {

                }
                if (e != null)
                {
                    d += (ulong) e.GetHashCode();
                    argsUsed++;
                    continue;
                }
                try
                {
                    e = Enum.Parse(type, name, true);
                }
                catch (ArgumentException)
                {

                }

                if (e != null)
                {
                    d += (ulong) e.GetHashCode();
                    argsUsed++;
                    continue;
                }
                ulong numd;
                if (ulong.TryParse(name, out numd))
                {
                    d += numd;
                    argsUsed++;
                    continue;
                }
                break;
            }
            if (argsUsed == 0)
            {
                value = null;
                return false;
            }
            Type etype = Enum.GetUnderlyingType(type);
            if (typeof (IConvertible).IsAssignableFrom(etype))
            {
                MethodInfo mi = etype.GetMethod("Parse",new Type[]{typeof(string)});
                value = mi.Invoke(null, new object[] {d.ToString()});
                return argsUsed > 0;
            }
            value = d;
            return argsUsed > 0;
        }

        static public bool IsEmpty(ICollection enumerable)
        {
            return enumerable == null || enumerable.Count == 0;
        }

        protected Simulator TryGetSim(string[] args, out int argsUsed)
        {
            if (args.Length > 0)
            {
                string s = String.Join(" ", args);
                SimRegion R = SimRegion.GetRegion(s, Client);
                if (R == null)
                {
                    argsUsed = 0;
                    WriteLine("cant find sim " + s);
                    return null;
                }

                Simulator sim = R.TheSimulator;
                if (sim == null) WriteLine("not connect to sim" + R);
                argsUsed = args.Length;
                return sim;
            }
            argsUsed = 0;
            return Client.Network.CurrentSim;
        }
    }
}
