using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;
using MushDLR223.ScriptEngines;

namespace Cogbot.Actions.Communication
{
    public class EchoMasterCommand : Command, BotSystemCommand, BotStatefullCommand, AsynchronousCommand
    {
        public EchoMasterCommand(BotClient testClient)
            : base(testClient)
        {
            Name = "echoMaster";
        }
        public override void MakeInfo()
        {
            Description = "Repeat everything that master says from open channel to open channel.";
            Details = AddUsage("echomaster", "toggles this commnand on/off");
            Category = CommandCategory.Communication;
            Parameters = CreateParams();
        }

        private bool Active;

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            if (!Active)
            {
                Active = true;
                Client.Self.ChatFromSimulator += Self_ChatFromSimulator;
                return Success("Echoing is now on.");
            }
            else
            {
                Active = false;
                Client.Self.ChatFromSimulator -= Self_ChatFromSimulator;
                return Success("Echoing is now off.");
            }
        }

        private void Self_ChatFromSimulator(object sender, ChatEventArgs e)
        {
            if (e.Message.Length > 0 && (Client.GetSecurityLevel(e.SourceID, e.FromName) & BotPermissions.IsMaster) != 0)
                Client.Self.Chat(e.Message, 0, ChatType.Normal);
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Active = false;
            Client.Self.ChatFromSimulator -= Self_ChatFromSimulator;
        }

        #endregion
    }
}