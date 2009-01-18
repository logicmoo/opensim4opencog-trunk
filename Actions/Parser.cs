using System;
using System.Collections.Generic;
using System.Text;

namespace cogbot.Actions
{
    public class Parser
    {
        static string[] preps = { "of", "to", "in", "for", "with", "as", "by", "at", "from", "on", "is" };

        public Dictionary<string, string> prepPhrases;
        public string objectPhrase;
        public string str;
        public string[] tokens;

        public Parser(string _str)
        {
            str = _str;
            prepPhrases = new Dictionary<string, string>();
            foreach (string prep in preps)
                prepPhrases[prep] = "";
            objectPhrase = "";

            string currentPrep = "";
            tokens = str.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            bool firstTok = true;

            for (int i = 0; i < tokens.Length; ++i)
            {
                if (prepPhrases.ContainsKey(tokens[i]))
                {
                    currentPrep = tokens[i];
                    firstTok = true;
                }
                else
                {
                    if (currentPrep == "")
                    {
                        if (!firstTok)
                            objectPhrase += " ";
                        objectPhrase += tokens[i];
                        firstTok = false;
                    }
                    else
                    {
                        if (!firstTok)
                            prepPhrases[currentPrep] += " ";
                        prepPhrases[currentPrep] += tokens[i];
                        firstTok = false;
                    }
                }
            }
        }
    }
}
