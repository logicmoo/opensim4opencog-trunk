using System;
using System.Xml;
using System.Text;
using System.IO;

namespace RTParser.AIMLTagHandlers
{
    /// <summary>
    /// The learn element instructs the AIML interpreter to retrieve a resource specified by a URI, 
    /// and to process its AIML object contents.
    /// </summary>
    public class learn : RTParser.Utils.AIMLTagHandler
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="bot">The bot involved in this request</param>
        /// <param name="user">The user making the request</param>
        /// <param name="query">The query that originated this node</param>
        /// <param name="request">The request inputted into the system</param>
        /// <param name="result">The result to be passed to the user</param>
        /// <param name="templateNode">The node to be processed</param>
        public learn(RTParser.RTPBot bot,
                        RTParser.User user,
                        RTParser.Utils.SubQuery query,
                        RTParser.Request request,
                        RTParser.Result result,
                        XmlNode templateNode)
            : base(bot, user, query, request, result, templateNode)
        {
        }

        protected override string ProcessChange()
        {
            if (this.templateNode.Name.ToLower() == "learn")
            {
                // currently only AIML files in the local filesystem can be referenced
                // ToDo: Network HTTP and web service based learning
                if (templateNodeInnerText.Length > 0)
                {
                    string path = templateNodeInnerText;
                    FileInfo fi = new FileInfo(path);
                    if (fi.Exists)
                    {
                        XmlDocument doc = new XmlDocument();
                        try
                        {
                            doc.Load(path);
                            this.Proc.loadAIMLFromXML(doc, path);
                        }
                        catch
                        {
                            this.Proc.writeToLog("ERROR! Attempted (but failed) to <learn> some new AIML from the following URI: " + path);
                        }
                    }
                }
            }
            return string.Empty;
        }
    }
}
