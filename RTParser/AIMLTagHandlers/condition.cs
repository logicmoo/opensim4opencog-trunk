using System;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;

namespace RTParser.AIMLTagHandlers
{
    /// <summary>
    /// The condition element instructs the AIML interpreter to return specified contents depending 
    /// upon the results of matching a predicate against a pattern. 
    /// 
    /// NB: The condition element has three different types. The three different types specified 
    /// here are distinguished by an xsi:type attribute, which permits a validating XML Schema 
    /// processor to validate them. Two of the types may contain li elements, of which there are 
    /// three different types, whose validity is determined by the type of enclosing condition. In 
    /// practice, an AIML interpreter may allow the omission of the xsi:type attribute and may instead 
    /// heuristically determine which type of condition (and hence li) is in use. 
    /// 
    /// Block Condition 
    /// ---------------
    /// 
    /// The blockCondition type of condition has a required attribute "name", which specifies an AIML 
    /// predicate, and a required attribute "value", which contains a simple pattern expression. 
    ///
    /// If the contents of the value attribute match the value of the predicate specified by name, then 
    /// the AIML interpreter should return the contents of the condition. If not, the empty Unifiable "" 
    /// should be returned.
    /// 
    /// Single-predicate Condition 
    /// --------------------------
    /// 
    /// The singlePredicateCondition type of condition has a required attribute "name", which specifies 
    /// an AIML predicate. This form of condition must contain at least one li element. Zero or more of 
    /// these li elements may be of the valueOnlyListItem type. Zero or one of these li elements may be 
    /// of the defaultListItem type.
    /// 
    /// The singlePredicateCondition type of condition is processed as follows: 
    ///
    /// Reading each contained li in order: 
    ///
    /// 1. If the li is a valueOnlyListItem type, then compare the contents of the value attribute of 
    /// the li with the value of the predicate specified by the name attribute of the enclosing 
    /// condition. 
    ///     a. If they match, then return the contents of the li and stop processing this condition. 
    ///     b. If they do not match, continue processing the condition. 
    /// 2. If the li is a defaultListItem type, then return the contents of the li and stop processing
    /// this condition.
    /// 
    /// Multi-predicate Condition 
    /// -------------------------
    /// 
    /// The multiPredicateCondition type of condition has no attributes. This form of condition must 
    /// contain at least one li element. Zero or more of these li elements may be of the 
    /// nameValueListItem type. Zero or one of these li elements may be of the defaultListItem type.
    /// 
    /// The multiPredicateCondition type of condition is processed as follows: 
    ///
    /// Reading each contained li in order: 
    ///
    /// 1. If the li is a nameValueListItem type, then compare the contents of the value attribute of 
    /// the li with the value of the predicate specified by the name attribute of the li. 
    ///     a. If they match, then return the contents of the li and stop processing this condition. 
    ///     b. If they do not match, continue processing the condition. 
    /// 2. If the li is a defaultListItem type, then return the contents of the li and stop processing 
    /// this condition. 
    /// 
    /// ****************
    /// 
    /// Condition List Items
    /// 
    /// As described above, two types of condition may contain li elements. There are three types of 
    /// li elements. The type of li element allowed in a given condition depends upon the type of that 
    /// condition, as described above. 
    /// 
    /// Default List Items 
    /// ------------------
    /// 
    /// An li element of the type defaultListItem has no attributes. It may contain any AIML template 
    /// elements. 
    ///
    /// Value-only List Items
    /// ---------------------
    /// 
    /// An li element of the type valueOnlyListItem has a required attribute value, which must contain 
    /// a simple pattern expression. The element may contain any AIML template elements.
    /// 
    /// Name and Value List Items
    /// -------------------------
    /// 
    /// An li element of the type nameValueListItem has a required attribute name, which specifies an 
    /// AIML predicate, and a required attribute value, which contains a simple pattern expression. The 
    /// element may contain any AIML template elements. 
    /// </summary>
    public class condition : RTParser.Utils.AIMLTagHandler
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
        public condition(RTParser.RTPBot bot,
                        RTParser.User user,
                        RTParser.Utils.SubQuery query,
                        RTParser.Request request,
                        RTParser.Result result,
                        XmlNode templateNode)
            : base(bot, user, query, request, result, templateNode)
        {
            this.isRecursive = false;
        }

        protected override Unifiable ProcessChange()
        {
            if (query.CurrentTemplate != null) query.CurrentTemplate.Rating *= 1.5;
            if (this.templateNode.Name.ToLower() == "condition")
            {
                // heuristically work out the type of condition being processed

                if (this.templateNode.Attributes.Count == 2) // block
                {
                    string name = GetAttribValue("name", String.Empty);
                    Unifiable value = GetAttribValue("value", String.Empty);
                    if ((name.Length > 0) & (!value.IsEmpty))
                    {
                        Unifiable actualValue = this.query.grabSetting(name);
                        if (IsPredMatch(value, actualValue))
                        {
                            return Unifiable.InnerXmlText(templateNode);
                        }
                        return Unifiable.Empty;
                    }
                    UnknownCondition();
                }
                else if (this.templateNode.Attributes.Count == 1) // single predicate
                {
                    if (this.templateNode.Attributes[0].Name == "name")
                    {
                        string name = GetAttribValue("name", String.Empty);

                        foreach (XmlNode childLINode in this.templateNode.ChildNodes)
                        {
                            if (childLINode.Name.ToLower() == "li")
                            {
                                if (childLINode.Attributes.Count == 1)
                                {
                                    if (childLINode.Attributes[0].Name.ToLower() == "value")
                                    {
                                        Unifiable actualValue = this.query.grabSetting(name);
                                        Unifiable value = GetAttribValue(childLINode, "value", Unifiable.Empty);
                                        if (IsPredMatch(value, actualValue))
                                        {
                                            return Unifiable.InnerXmlText(childLINode);
                                        }
                                    }
                                }
                                else if (childLINode.Attributes.Count == 0)
                                {
                                    return Unifiable.InnerXmlText(childLINode);
                                }
                            }
                        }
                    }
                }
                else if (this.templateNode.Attributes.Count == 0) // multi-predicate
                {
                    foreach (XmlNode childLINode in this.templateNode.ChildNodes)
                    {
                        if (childLINode.Name.ToLower() == "li")
                        {
                            if (childLINode.Attributes.Count == 2)
                            {
                                string name = GetAttribValue(childLINode, "name", string.Empty);
                                Unifiable value = GetAttribValue(childLINode, "value", string.Empty);
                                if ((name.Length > 0) & (!value.IsEmpty))
                                {
                                    Unifiable actualValue = this.query.grabSetting(name);
                                    if (IsPredMatch(value, actualValue))
                                    {
                                        return Unifiable.InnerXmlText(childLINode);
                                    }
                                }
                            }
                            if (childLINode.Attributes.Count == 1)
                            {
                                string name = GetAttribValue(childLINode, "name", string.Empty);
                                if ((name.Length > 0) && this.query.containsSettingCalled(name))
                                {
                                        return Unifiable.InnerXmlText(childLINode);
                                }
                            }
                            else if (childLINode.Attributes.Count == 0)
                            {
                                return Unifiable.InnerXmlText(childLINode);
                            }
                        }
                    }
                }
            }
            return Unifiable.Empty;
        }

        public void UnknownCondition()
        {
            Console.WriteLine("Unknown conditions " + LineNumberTextInfo());
        }
    }
}
