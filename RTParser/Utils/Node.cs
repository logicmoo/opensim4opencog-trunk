using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace RTParser.Utils
{
    /// <summary>
    /// Encapsulates a node in the graphmaster tree structure
    /// </summary>
    [Serializable]
    public class Node
    {
        private Node Parent;
        public Node(Node P)
        {
            Parent = P;            
        }
        #region Attributes

        /// <summary>
        /// Contains the child nodes of this node
        /// </summary>
        private Dictionary<Unifiable, Node> children = new Dictionary<Unifiable, Node>();

        /// <summary>
        /// The number of direct children (non-recursive) of this node
        /// </summary>
        public int NumberOfChildNodes
        {
            get
            {
                return this.children.Count;
            }
        }

        /// <summary>
        /// The template (if any) associated with this node
        /// </summary>
        public List<Template> template = null;//Unifiable.Empty;

        /// <summary>
        /// The AIML source for the category that defines the template
        /// </summary>
        public Unifiable filename = Unifiable.Empty;

        /// <summary>
        /// The word that identifies this node to it's parent node
        /// </summary>
        public Unifiable word=Unifiable.Empty;

        //private XmlNode GuardText;

        #endregion

        #region Methods

        #region Add category

        /// <summary>
        /// Adds a category to the node
        /// </summary>
        /// <param name="path">the path for the category</param>
        /// <param name="template">the template to find at the end of the path</param>
        /// <param name="filename">the file that was the source of this category</param>
        public void addCategoryTag(Unifiable path, XmlNode template, XmlNode guard, Unifiable filename)
        {
            if (template==null)
            {
                throw new XmlException("The category with a pattern: " + path + " found in file: " + filename + " has an empty template tag. ABORTING");
            }

            // check we're not at the leaf node
            if (path.Trim().Length == 0)
            {
                if (this.template == null) this.template = new List<Template>();
                this.template.Insert(0, new Template(template, guard));
                //this.GuardText = guard;
                this.filename = filename;
                return;
            }

            // otherwise, this sentence requires further child nodemappers in order to
            // be fully mapped within the GraphMaster structure.

            // split the input into its component words
            Unifiable[] words = path.Trim().Split(" ".ToCharArray());

            if (words[0].Contains("COMEHERE"))
            {
                if (!words[0].Contains("<t"))
                {

                }
            }
            // get the first word (to form the key for the child nodemapper)
            Unifiable firstWord = Normalize.MakeCaseInsensitive.TransformInput(words[0]);

            // concatenate the rest of the sentence into a suffix (to act as the
            // path argument in the child nodemapper)
            Unifiable newPath = path.Substring(firstWord.Length, path.Length - firstWord.Length).Trim();

            // o.k. check we don't already have a child with the key from this sentence
            // if we do then pass the handling of this sentence down the branch to the 
            // child nodemapper otherwise the child nodemapper doesn't yet exist, so create a new one
            if (this.children.ContainsKey(firstWord))
            {
                Node childNode = this.children[firstWord];
                childNode.addCategoryTag(newPath, template, guard, filename);
            }
            else
            {
                Node childNode = new Node(this);
                childNode.word = firstWord;
                childNode.addCategoryTag(newPath, template, guard, filename);
                this.children.Add(childNode.word, childNode);
            }
        }

        #endregion

        public static bool AlwaysFail = true;
        #region Evaluate Node

        public List<Template> evaluate(Unifiable path, SubQuery query, Request request, MatchState matchstate, StringBuilder wildcard)
        {
            //// are we in failure?
            //// first call the pre-existing evaluate  that was renamed to evaluate0
            //// String temp = evaluate0(path, query, request, matchstate, wildcard);
            //if (this.GuardText!=null)
            //{
            //    Unifiable preserve = query.Template;
            //    try
            //    {
	                
            //        Result result = new Result(request.user, request.Proccessor, request);
            //        XmlNode templateNode = AIMLTagHandler.getNode(GuardText.OuterXml);
            //        query.Template = GuardText.OuterXml;
            //        Unifiable outputSentence = request.Proccessor.processNode(templateNode, query, request, result, request.user);
            //        bool failed = outputSentence == null || outputSentence == "NIL";
            //        if (failed)
            //        {
            //            Console.WriteLine("failing guard " + GuardText.InnerXml + " for " + this);
            //            Node next = GetNextNode();
            //            if (next!=null)
            //            {
            //                // return next.evaluate(path, query, request, matchstate, wildcard);
            //            }
            //            return String.Empty;
            //        }
            //        // success
            //        return evaluate0(path, query, request, matchstate, wildcard); ;
            //    }
            //    catch (System.Exception ex)
            //    {
            //        Console.WriteLine("" + ex);
            //        return null;// String.Empty; //fail
            //    }
            //    finally
            //    {
            //        query.Template = preserve;
            //    }
            //}
            return evaluate0(path, query, request, matchstate, wildcard); ;
        }

        public Node GetNextNode()
        {
            if (Parent==null) return null;
            bool useNext = false;
            foreach (KeyValuePair<Unifiable, Node> v in Parent.children)
            {
                if (useNext) return v.Value;
                if (v.Value==this)
                {
                    useNext = true;
                }
            }
            if (useNext)
            {
           //     Console.WriteLine(String.Format("Last key {0}", ToString()));
                return Parent.GetNextNode();
            }
            return null;
        }

        public override string ToString()
        {
            if (Parent != null) return String.Format("{0} {1}", Parent, word);
            return word;
        }


        /// <summary>
        /// Navigates this node (and recursively into child nodes) for a match to the path passed as an argument
        /// whilst processing the referenced request
        /// </summary>
        /// <param name="path">The normalized path derived from the user's input</param>
        /// <param name="query">The query that this search is for</param>
        /// <param name="request">An encapsulation of the request from the user</param>
        /// <param name="matchstate">The part of the input path the node represents</param>
        /// <param name="wildcard">The contents of the user input absorbed by the AIML wildcards "_" and "*"</param>
        /// <returns>The template to process to generate the output</returns>
        private List<Template> evaluate0(Unifiable path, SubQuery query, Request request, MatchState matchstate, StringBuilder wildcard)
        {
            // check for timeout
            if (request.StartedOn.AddMilliseconds(request.Proccessor.TimeOut) < DateTime.Now)
            {
                request.Proccessor.writeToLog("WARNING! Request timeout. User: " + request.user.UserID + " raw input: \"" + request.rawInput + "\"");
                request.hasTimedOut = true;
                return null;// Unifiable.Empty;
            }

            // so we still have time!
            path = path.Trim();

            // check if this is the end of a branch in the GraphMaster 
            // return the cCategory for this node
            if (this.children.Count==0)
            {
                if (path.Length > 0)
                {
                    // if we get here it means that there is a wildcard in the user input part of the
                    // path.
                    this.storeWildCard(path, wildcard);
                }
                return this.template;
            }

            // if we've matched all the words in the input sentence and this is the end
            // of the line then return the cCategory for this node
            if (path.Length == 0)
            {
                return this.template;
            }

            // otherwise split the input into it's component words
            Unifiable[] splitPath = path.Split(" \r\n\t".ToCharArray());

            // get the first word of the sentence
            Unifiable firstWord = Normalize.MakeCaseInsensitive.TransformInput(splitPath[0]);

            // and concatenate the rest of the input into a new path for child nodes
            Unifiable newPath = path.Substring(firstWord.Length, path.Length - firstWord.Length);

            // first option is to see if this node has a child denoted by the "_" 
            // wildcard. "_" comes first in precedence in the AIML alphabet
            if (this.children.ContainsKey("_"))
            {
                Node childNode = (Node)this.children["_"];

                // add the next word to the wildcard match 
                StringBuilder newWildcard = new StringBuilder();
                this.storeWildCard(splitPath[0],newWildcard);
                
                // move down into the identified branch of the GraphMaster structure
                List<Template> result = childNode.evaluate(newPath, query, request, matchstate, newWildcard);

                // and if we get a result from the branch process the wildcard matches and return 
                // the result
                if (result!=null && result.Count>0)
                {
                    if (newWildcard.Length > 0)
                    {
                        // capture and push the star content appropriate to the current matchstate
                        switch (matchstate)
                        {
                            case MatchState.UserInput:
                                query.InputStar.Add(newWildcard.ToString());
                                // added due to this match being the end of the line
                                newWildcard.Remove(0, newWildcard.Length);
                                break;
                            case MatchState.That:
                                query.ThatStar.Add(newWildcard.ToString());
                                break;
                            case MatchState.Topic:
                                query.TopicStar.Add(newWildcard.ToString());
                                break;
                        }
                    }
                    return result;
                }
            }

            // second option - the nodemapper may have contained a "_" child, but led to no match
            // or it didn't contain a "_" child at all. So get the child nodemapper from this 
            // nodemapper that matches the first word of the input sentence.
            if (this.children.ContainsKey(firstWord))
            {
                // process the matchstate - this might not make sense but the matchstate is working
                // with a "backwards" path: "topic <topic> that <that> user input"
                // the "classic" path looks like this: "user input <that> that <topic> topic"
                // but having it backwards is more efficient for searching purposes
                MatchState newMatchstate = matchstate;
                if (firstWord == "<THAT>")
                {
                    newMatchstate = MatchState.That;
                }
                else if (firstWord == "<TOPIC>")
                {
                    newMatchstate = MatchState.Topic;
                }

                Node childNode = (Node)this.children[firstWord];
                // move down into the identified branch of the GraphMaster structure using the new
                // matchstate
                StringBuilder newWildcard = new StringBuilder();
                List<Template> result = childNode.evaluate(newPath, query, request, newMatchstate, newWildcard);
                // and if we get a result from the child return it
                if (result != null && result.Count > 0)
                {
                    if (newWildcard.Length > 0)
                    {
                        // capture and push the star content appropriate to the matchstate if it exists
                        // and then clear it for subsequent wildcards
                        switch (matchstate)
                        {
                            case MatchState.UserInput:
                                query.InputStar.Add(newWildcard.ToString());
                                newWildcard.Remove(0, newWildcard.Length);
                                break;
                            case MatchState.That:
                                query.ThatStar.Add(newWildcard.ToString());
                                newWildcard.Remove(0, newWildcard.Length);
                                break;
                            case MatchState.Topic:
                                query.TopicStar.Add(newWildcard.ToString());
                                newWildcard.Remove(0, newWildcard.Length);
                                break;
                        }
                    }
                    return result;
                }
            }

            // third option - the input part of the path might have been matched so far but hasn't
            // returned a match, so check to see it contains the "*" wildcard. "*" comes last in
            // precedence in the AIML alphabet.
            if (this.children.ContainsKey("*"))
            {
                // o.k. look for the path in the child node denoted by "*"
                Node childNode = (Node)this.children["*"];

                // add the next word to the wildcard match 
                StringBuilder newWildcard = new StringBuilder();
                this.storeWildCard(splitPath[0], newWildcard);

                List<Template> result = childNode.evaluate(newPath, query, request, matchstate, newWildcard);
                // and if we get a result from the branch process and return it
                if (result!=null && result.Count > 0)
                {
                    if (newWildcard.Length > 0)
                    {
                        // capture and push the star content appropriate to the current matchstate
                        switch (matchstate)
                        {
                            case MatchState.UserInput:
                                query.InputStar.Add(newWildcard.ToString());
                                // added due to this match being the end of the line
                                newWildcard.Remove(0, newWildcard.Length);
                                break;
                            case MatchState.That:
                                query.ThatStar.Add(newWildcard.ToString());
                                break;
                            case MatchState.Topic:
                                query.TopicStar.Add(newWildcard.ToString());
                                break;
                        }
                    }
                    return result;
                }
            }

            // o.k. if the nodemapper has failed to match at all: the input contains neither 
            // a "_", the sFirstWord text, or "*" as a means of denoting a child node. However, 
            // if this node is itself representing a wildcard then the search continues to be
            // valid if we proceed with the tail.
            if ((this.word == "_") || (this.word == "*"))
            {
                this.storeWildCard(splitPath[0], wildcard);
                return this.evaluate(newPath, query, request, matchstate, wildcard);
            }

            // If we get here then we're at a dead end so return an empty Unifiable. Hopefully, if the
            // AIML files have been set up to include a "* <that> * <topic> *" catch-all this
            // state won't be reached. Remember to empty the surplus to requirements wildcard matches
            wildcard = new StringBuilder();
            return null;// Unifiable.Empty;
        }

        /// <summary>
        /// Correctly stores a word in the wildcard slot
        /// </summary>
        /// <param name="word">The word matched by the wildcard</param>
        /// <param name="wildcard">The contents of the user input absorbed by the AIML wildcards "_" and "*"</param>
        private void storeWildCard(Unifiable word, StringBuilder wildcard)
        {
            if (wildcard.Length > 0)
            {
                wildcard.Append(" ");
            }
            wildcard.Append(word);
        }
        #endregion

        #endregion
    }
}