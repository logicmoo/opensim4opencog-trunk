using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using AIMLbot;
using LAIR.ResourceAPIs.WordNet;
using MushDLR223.ScriptEngines;
using MushDLR223.Utilities;
using MushDLR223.Virtualization;
using RTParser.AIMLTagHandlers;
using RTParser.Database;
using RTParser.Normalize;
using RTParser.Utils;
using RTParser.Variables;
using UPath = RTParser.Unifiable;
using UList = System.Collections.Generic.List<RTParser.Utils.TemplateInfo>;


namespace RTParser
{
    interface IChatterBot
    {
        SystemExecHandler ChatWithHandler(string userName);
    }

    public partial class RTPBot
    {
        public bool AlwaysUseImmediateAimlInImput = true;
        public static bool RotateUsedTemplate = true;
        public bool DontUseSplitters = true;

        public readonly TaskQueueHandler HeardSelfSayQueue = new TaskQueueHandler("AIMLBot HeardSelf",
                                                                                   TimeSpan.FromMilliseconds(10),
                                                                                   TimeSpan.FromSeconds(10), true);
        private readonly Object chatTraceO = new object();
        public List<Thread> ThreadList { get { return HeardSelfSayQueue.InteruptableThreads; } }
        public bool chatTrace = true;
        private StreamWriter chatTraceW;
        private JoinedTextBuffer currentEar = new JoinedTextBuffer();
        private int streamDepth;
        //public Unifiable responderJustSaid;

        // last user talking to bot besides itself
        private User _lastUser;
        public User LastUser
        {
            get
            {
                if (BotAsUser != null)
                {
                    var LR = BotAsUser.LastResponder;
                    if (LR != null) return LR;
                }
                if (IsInteractiveUser(_lastUser)) return _lastUser;
                User LU = _lastResult != null ? _lastResult.Requester : null;
                if (IsInteractiveUser(LU)) return LU;
                return null;
            }
            set
            {
                if (BotAsUser != null)
                {
                    if (value == BotAsUser) return;
                    BotAsUser.LastResponder = value;
                }
                User LU = LastUser;
                if (!IsInteractiveUser(LU) || IsInteractiveUser(value))
                {
                    _lastUser = value;
                }
            }
        }

        // last result from the last talking to bot besides itself
        private Result _lastResult;
        public Result LastResult
        {
            get
            {
                if (_lastResult != null)
                {
                    if (IsInteractiveUser(_lastResult.Requester)) return _lastResult;
                }
                else
                {
                    User LU = LastUser;
                    if (IsInteractiveUser(LU)) return LU.LastResult;
                }
                return _lastResult;
            }
            set
            {
                Result LR = LastResult;
                if (value == null) return;
                if (LR == null)
                {
                    _lastResult = value;
                }
                LastUser = value.Requester;

                if (LR != null && LR.Requester != BotAsUser)
                {
                    if (value.Requester != LR.Requester)
                    {
                        _lastUser = value.Requester ?? _lastUser;
                    }
                }
            }
        }

        /// <summary>
        /// The directory to look in for the WordNet3 files
        /// </summary>
        public string PathToWordNet
        {
            get { return GetPathSetting("wordnetdirectory", "wordnet30"); }
        }

        /// <summary>
        /// The directory to look in for the Lucene Index files
        /// </summary>
        public string PathToLucene
        {
            get { return GetPathSetting("lucenedirectory", "lucenedb"); }
        }

        /// <summary>
        /// The number of categories this Proccessor has in its graphmaster "brain"
        /// </summary>
        public int Size
        {
            get { return GraphMaster.Size + HeardSelfSayGraph.Size; }
        }

        private GraphMaster _g;
        private GraphMaster _h;
        private static GraphMaster TheUserListernerGraph;

        /// <summary>
        /// The "brain" of the Proccessor
        /// </summary>
        public GraphMaster GraphMaster
        {
            get
            {
                if (_g != null) return _g;
                if (String.IsNullOrEmpty(NamePath))
                {
                    writeToLog("No graphmapster!");
                    return null;
                }
                return GetGraph(NamePath, _g);
            }
        }

        public GraphMaster HeardSelfSayGraph
        {
            get
            {
                if (_h != null) return _h;
                if (String.IsNullOrEmpty(NamePath))
                {
                    writeToLog("No HeardSelfSayGraph!");
                    return null;
                }
                return GetGraph(NamePath + "_heardselfsay", _h);
            }
        }


        /// <summary>
        /// The Markovian "brain" of the Proccessor for generation
        /// </summary>
        public MBrain MBrain
        {
            get { return mbrain; }
        }

        private readonly MBrain mbrain = new MBrain();

        public MBrain STM_Brain
        {
            get { return stm_brain; }
        }

        private readonly MBrain stm_brain = new MBrain();

        /// <summary>
        /// Proccessor for phonetic HMM
        /// </summary>
        // emission and transition stored as double hash tables
        public PhoneticHmm pHMM
        {
            get { return phoneHMM; }
        }

        private readonly PhoneticHmm phoneHMM = new PhoneticHmm();

        /// <summary>
        /// Proccessor for action Markov State Machine
        /// </summary>
        // emission and transition stored as double hash tables
        public actMSM pMSM
        {
            get { return botMSM; }
        }

        private readonly actMSM botMSM = new actMSM();
        //public string lastDefMSM;
        //public string lastDefState;

        public Stack<string> conversationStack = new Stack<string>();
        public Hashtable wordAttributeHash = new Hashtable();

        public WordNetEngine wordNetEngine;
        // = new WordNetEngine(HostSystem.Combine(Environment.CurrentDirectory, this.GlobalSettings.grabSetting("wordnetdirectory")), true);

        //public string indexDir = @"C:\dev\Lucene\";
        public string fieldName = "TEXT_MATTER";
        public IEnglishFactiodEngine LuceneIndexer;
        public ITripleStore TripleStore;


        #region Conversation methods

        private Result GlobalChatWithUser(string input, string user, string otherName, OutputDelegate traceConsole, bool saveResults)
        {
            User targetUser = BotAsUser;
            string youser = input;
            int lastIndex = input.IndexOfAny("-,:".ToCharArray());
            User targ = null;
            if (lastIndex > 0)
            {
                youser = input.Substring(0, lastIndex);
                targ = FindUser(youser);
                if (targ != null) targetUser = targ;
            }
            if (otherName != null)
                if (targ == null)
                {
                    targ = FindUser(otherName);
                    if (targ != null) targetUser = targ;
                }

            traceConsole = traceConsole ?? writeDebugLine;
            User CurrentUser = FindOrCreateUser(user) ?? LastUser;
            var varMSM = this.pMSM;
            varMSM.clearEvidence();
            varMSM.clearNextStateValues();
            //  myUser.TopicSetting = "collectevidencepatterns";
            RequestImpl r = GetRequest(input, CurrentUser);
            r.IsTraced = true;
            r.writeToLog = traceConsole;
            r.Responder = targetUser;
            Predicates.IsTraced = false;
            Result res = r.CreateResult(r);
            ChatLabel label = r.PushScope;
            try
            {
                ChatWithUser(r, r.Requester, targetUser, r.Graph);
            }
            catch (ChatSignal e)
            {
                if (label.IsSignal(e)) return (AIMLbot.MasterResult)r.CurrentResult;
                throw;
            }
            catch (Exception exception)
            {
                traceConsole("" + exception);
            }
            finally
            {
                label.PopScope();
            }
            string useOut = null;
            //string useOut = resOutput;
            if (!res.IsEmpty)
            {
                useOut = res.Output;
                CurrentUser = res.Requester;
                string oTest = ToEnglish(useOut);
                if (oTest != null && oTest.Length > 2)
                {
                    useOut = oTest;
                }
            }
            if (string.IsNullOrEmpty(useOut))
            {
                useOut = "Interesting.";
                res.TemplateRating = Math.Max(res.Score, 0.5d);
            }
            if (saveResults)
            {
                LastResult = res;
                LastUser = CurrentUser;
            }

            traceConsole(useOut);
            return res;
        }

        public RequestImpl GetRequest(string rawInput, string username)
        {
            return GetRequest(rawInput, FindOrCreateUser(username));
        }
        public RequestImpl GetRequest(string rawInput, User findOrCreateUser)
        {
            AIMLbot.MasterRequest r = findOrCreateUser.CreateRequest(rawInput, null); 
            findOrCreateUser.CurrentRequest = r;
            r.depth = 0;
            r.IsTraced = findOrCreateUser.IsTraced;
            return r;
        }

        /// <summary> 
        /// Given some raw input string username/unique ID creates a response for the user
        /// </summary>
        /// <param name="rawInput">the raw input</param>
        /// <param name="UserGUID">a usersname</param>
        /// <returns>the result to be output to the user</returns>        
        public string ChatString(string rawInput, string username)
        {
            RequestImpl r = GetRequest(rawInput, username);
            r.IsTraced = this.IsTraced;
            return Chat(r).Output;
        }

        /// <summary>
        /// Given some raw input and a unique ID creates a response for a new user
        /// </summary>
        /// <param name="rawInput">the raw input</param>
        /// <param name="UserGUID">an ID for the new user (referenced in the result object)</param>
        /// <returns>the result to be output to the user</returns>
        public Result Chat(string rawInput, string UserGUID)
        {
            Request request = GetRequest(rawInput, UserGUID);
            request.IsTraced = this.IsTraced;
            return Chat(request);
        }
        /// <summary>
        /// Given a request containing user input, produces a result from the Proccessor
        /// </summary>
        /// <param name="request">the request from the user</param>
        /// <returns>the result to be output to the user</returns>
        /// 
        public AIMLbot.MasterResult Chat(Request request)
        {
           try
            {
                return Chat(request, request.Graph ?? GraphMaster);
            }
            finally
            {
                AddHeardPreds(request.rawInput, HeardPredicates);
            }
        }

        public AIMLbot.MasterResult Chat(Request request, GraphMaster G)
        {
            GraphMaster prev = request.Graph;
            request.Graph = G;
            try
            {
                AIMLbot.MasterResult v = ChatWithUser(request, request.Requester, request.Responder, G);
                return v;
            }
            finally
            {
                request.Graph = prev;
            }
        }

        public AIMLbot.MasterResult ChatWithUser(Request request, User user, User target, GraphMaster G)
        {
            var originalRequestor = request.Requester;
            bool isTraced = request.IsTraced || G == null;
            user = user ?? request.Requester ?? LastUser;
            UndoStack undoStack = UndoStack.GetStackFor(request);
            Unifiable requestrawInput = request.rawInput;
            undoStack.pushValues(user.Predicates, "i", user.UserName);
            undoStack.pushValues(user.Predicates, "rawinput", requestrawInput);
            undoStack.pushValues(user.Predicates, "input", requestrawInput);
            if (target != null && target.UserName != null)
            {
                undoStack.pushValues(user.Predicates, "you", target.UserName);
            }
            AIMLbot.MasterResult res = request.CreateResult(request);
            //lock (user.QueryLock)
            {
                ChatLabel label = request.PushScope;
                try
                {
                    res = ChatWithRequest4(request, user, target, G);
                    /*
                    // ReSharper disable ConditionIsAlwaysTrueOrFalse
                    if (res.OutputSentenceCount == 0 && false)
                    // ReSharper restore ConditionIsAlwaysTrueOrFalse
                    {
                        request.UndoAll();
                        request.IncreaseLimits(1);
                         res = ChatWithRequest4(request, user, target, G);
                    }
                     */
                    if (request.IsToplevelRequest)
                    {
                        AddSideEffectHook(request, originalRequestor, res);
                        user.JustSaid = requestrawInput;
                        if (target != null)
                        {
                            target.JustSaid = user.ResponderJustSaid; //.Output;
                        }
                    }
                    return res;
                }
                catch (ChatSignalOverBudget e)
                {
                    request.UndoAll();
                    writeToLog("ChatWithUser ChatSignalOverBudget: ( request.UndoAll() )" + request + " " + e);
                    return (AIMLbot.MasterResult)e.request;
                }
                catch (ChatSignal e)
                {
                    if (label.IsSignal(e)) return (AIMLbot.MasterResult)request.CurrentResult;
                    throw;
                }
                finally
                {
                    label.PopScope();
                    undoStack.UndoAll();
                    request.UndoAll();
                    request.Commit();
                    request.Requester = originalRequestor;
                }
            }
        }

        private void AddSideEffectHook(Request request, User originalRequestor, AIMLbot.MasterResult res)
        {
            request.AddSideEffect("Populate the Result object",
                                  () =>
                                  {
                                      PopulateUserWithResult(originalRequestor, request, res);
                                  });
        }

        public AIMLbot.MasterResult ChatWithRequest4(Request request, User user, User target, GraphMaster G)
        {
            var originalRequestor = request.Requester;
            var originalTargetUser = request.Responder;
            ChatLabel label = request.PushScope;
            try
            {
                if (request.ParentMostRequest.DisallowedGraphs.Contains(G) || request.depth > 4)
                    return (AIMLbot.MasterResult)request.CurrentResult;
                request.Requester = user;
                Result result = ChatWithRequest44(request, user, target, G);
                if (result.OutputSentences.Count != 0)
                {
                    result.RotateUsedTemplates();
                }
                return (AIMLbot.MasterResult)result;
            }
            catch (ChatSignalOverBudget e)
            {
                writeToLog("ChatWithRequest4 ChatSignalOverBudget: " + request + " " + e);
                return (AIMLbot.MasterResult)request.CurrentResult;
            }
            catch (ChatSignal e)
            {
                if (label.IsSignal(e)) return (AIMLbot.MasterResult)request.CurrentResult;
                throw;
            }
            finally
            {
                label.PopScope();
                request.Requester = originalRequestor;
            }
        }

        public AIMLbot.MasterResult ChatWithRequest44(Request request, User user, User target, GraphMaster G)
        {
            User originalRequestor = request.Requester;
            //LastUser = user; 
            //AIMLbot.Result result;
            bool isTraced = request.IsTraced || G == null;

            OutputDelegate writeToLog = this.writeToLog;
            var result = request.CreateResult(request);
            string rr = request.rawInput;
            if (rr.StartsWith("@") || (rr.IndexOf("<") + 1 < rr.IndexOf(">")))
            {
                result = ChatWithNonGraphmaster(request, G, isTraced, writeToLog);
            }
            else if (request.GraphsAcceptingUserInput)
            {
                result = ChatUsingGraphMaster(request, G, isTraced, writeToLog);
            }
            else
            {
                string nai = NotAcceptingUserInputMessage;
                if (isTraced) this.writeToLog("ERROR {0} getting back {1}", request, nai);
                request.AddOutputSentences(null, nai, result);
            }
            User popu = originalRequestor ?? request.Requester ?? result.Requester;
            result.Durration = DateTime.Now - request.StartedOn;
            result.IsComplete = true;
            popu.addResultTemplates(request);
            if (streamDepth > 0) streamDepth--;
            return result;
        }
        private AIMLbot.MasterResult ChatWithNonGraphmaster(Request request, GraphMaster G, bool isTraced, OutputDelegate writeToLog)
        {
            AIMLbot.MasterResult result;
            writeToLog = writeToLog ?? DEVNULL;
            isTraced = request.IsTraced || G == null;
            //chatTrace = null;

            streamDepth++;

            string rawInputString = request.rawInput.AsString();

            if (rawInputString.StartsWith("@"))
            {
                result = request.CreateResult(request);
                if (chatTrace) result.IsTraced = isTraced;
                StringWriter sw = new StringWriter();

                bool myBotBotDirective = BotDirective(request, rawInputString, sw.WriteLine);
                string swToString = sw.ToString();
                if (writeToLog != null) writeToLog("ChatWithNonGraphmaster: " + swToString);
                else
                {
                    writeToLog = DEVNULL;
                }
                if (myBotBotDirective)
                {
                    Result requestCurrentResult = result ?? request.CurrentResult;
                    if (requestCurrentResult != null)
                    {
                        requestCurrentResult.SetOutput = swToString;
                    }
                    return result;
                }
                writeToLog("ERROR: cannot find command " + request.rawInput);
                return null;
            }
            ChatLabel label = request.PushScope;
            var orig = request.ResponderOutputs;
            if (AlwaysUseImmediateAimlInImput && ContainsAiml(rawInputString))
            {
                try
                {
                    result = ImmediateAiml(StaticAIMLUtils.getTemplateNode(rawInputString), request, Loader, null);
                    //request.rawInput = result.Output;
                    return result;
                }
                catch (ChatSignal e)
                {
                    if (label.IsSignal(e)) return e.result;
                    throw;
                }
                catch (Exception e)
                {
                    isTraced = true;
                    this.writeToLog(e);
                    writeToLog("ImmediateAiml: ERROR: " + e);
                    label.PopScope();
                    return null;
                }
            }
            return null;
        }

        private AIMLbot.MasterResult ChatUsingGraphMaster(Request request, GraphMaster G, bool isTraced, OutputDelegate writeToLog)
        {
            //writeToLog = writeToLog ?? DEVNULL;
            AIMLbot.MasterResult result;
            {
                ParsedSentences parsedSentences = ParsedSentences.GetParsedSentences(request, isTraced, writeToLog);

                bool printedSQs = false;
                G = G ?? GraphMaster;

                // grab the templates for the various sentences from the graphmaster
                request.IsTraced = isTraced;
                result = request.CreateResult(request);

                // load the queries
                List<GraphQuery> AllQueries = new List<GraphQuery>();

                bool topleveRequest = request.IsToplevelRequest;

                int UNLIMITED = 10000;
                request.MaxOutputs = UNLIMITED;
                request.MaxPatterns = UNLIMITED;
                request.MaxTemplates = UNLIMITED;

                // Gathers the Pattern SubQueries!
                foreach (Unifiable userSentence in parsedSentences.NormalizedPaths)
                {
                    AllQueries.Add(G.gatherQueriesFromGraph(userSentence, request, MatchState.UserInput));
                }
                try
                {
                    // gather the templates and patterns
                    foreach (var ql in AllQueries)
                    {
                        if (topleveRequest)
                        {
                            ql.TheRequest.SuspendSearchLimits = true;
                            ql.NoMoreResults = false;
                            ql.MaxTemplates = UNLIMITED;
                            ql.MaxPatterns = UNLIMITED;
                            ql.MaxOutputs = UNLIMITED;
                        }
                        G.RunGraphQuery(ql);
                        //if (request.IsComplete(result)) return result;
                    }
                    foreach (var ql in AllQueries)
                    {
                        request.TopLevel = ql;
                        if (chatTrace) result.IsTraced = isTraced;
                        if (ql.PatternCount > 0)
                        {
                            request.TopLevel = ql;
                            // if (ql.TemplateCount > 0)
                            {
                                request.TopLevel = ql;
                                result.AddSubqueries(ql);
                            }
                            var kept = ql.PreprocessSubQueries(request, result.SubQueries, isTraced, ref printedSQs,
                                                               writeToLog);
                        }
                        // give a 20 second blessing
                        if (false && result.SubQueries.Count > 0 && !request.SraiDepth.IsOverMax)
                        {
                            writeToLog("Extending time");
                            request.TimeOutFromNow = TimeSpan.FromSeconds(2);
                        }
                        //ProcessSubQueriesAndIncreasLimits(request, result, ref isTraced, printedSQs, writeToLog);
                    }
                    int solutions;
                    bool hasMoreSolutions;
                    CheckResult(request, result, out solutions, out hasMoreSolutions);
                }
                catch (ChatSignal exception)
                {
                    writeToLog("ChatSignalOverBudget: " + exception.Message);
                }
            }
            return result;
        }

        private void ProcessSubQueriesAndIncreasLimits(Request request, Result result, ref bool isTraced, bool printedSQs, OutputDelegate writeToLog)
        {
            writeToLog = writeToLog ?? DEVNULL;
            int sqc = result.SubQueries.Count;
            int solutions;
            bool hasMoreSolutions;
            CheckResult(request, result, out solutions, out hasMoreSolutions);
            return;
            if (result.OutputSentenceCount == 0 || sqc == 0)
            {
                return;
                //  string oldSets = QuerySettings.ToSettingsString(request.GetQuerySettings());
                isTraced = true;
                //todo pick and chose the queries
                int n = 0;
                while (result.OutputSentenceCount == 0 && n < 3 && request.depth < 3)
                {
                    n++;
                    request.UndoAll();
                    request.IncreaseLimits(1);
                    CheckResult(request, result, out solutions, out hasMoreSolutions);
                    if (result.OutputSentenceCount != 0 || sqc != 0)
                    {
                    }
                    // string newSets = QuerySettings.ToSettingsString(request.GetQuerySettings());
                    //writeToLog("AIMLTRACE: bumped up limits " + n + " times for " + request + "\n --from\n" + oldSets + "\n --to \n" +
                    //         newSets);
                }
            }
            // process the templates into appropriate output
            PostProcessSubqueries(request, result, isTraced, writeToLog);
        }
        private void PostProcessSubqueries(Request request, Result result, bool isTraced, OutputDelegate writeToLog)
        {
            writeToLog = writeToLog ?? DEVNULL;
            {
                if (isTraced)
                {
                    if (result.OutputSentenceCount != 1 && !request.Graph.UnTraced)
                    {
                        DLRConsole.SystemFlush();
                        string s = "AIMLTRACE: result.OutputSentenceCount = " + result.OutputSentenceCount;
                        foreach (string path in result.OutputSentences)
                        {
                            s += Environment.NewLine;
                            s += "  " + Unifiable.ToVMString(path);
                        }
                        s += Environment.NewLine;
                        writeToLog(s);
                        DLRConsole.SystemFlush();
                    }

                    foreach (SubQuery path in result.SubQueries)
                    {
                        //string s = "AIMLTRACE QUERY:  " + path.FullPath;

                        //writeToLog("\r\n tt: " + path.Request.Graph);
                        if (chatTrace)
                        {
                            //bot.writeChatTrace("\"L{0}\" -> \"{1}\" ;\n", result.SubQueries.Count, path.FullPath.ToString());
                            writeChatTrace("\"L{0}\" -> \"L{1}\" ;\n", result.SubQueries.Count - 1,
                                           result.SubQueries.Count);
                            writeChatTrace("\"L{0}\" -> \"REQ:{1}\" ;\n", result.SubQueries.Count, request.ToString());
                            writeChatTrace("\"REQ:{0}\" -> \"PATH:{1}\" [label=\"{2}\"];\n", request.ToString(),
                                           result.SubQueries.Count, path.FullPath);
                            writeChatTrace("\"REQ:{0}\" -> \"RPY:{1}\" ;\n", request.ToString(), result.RawOutput);
                        }
                    }
                }
            }
        }
        internal void PopulateUserWithResult(User user, Request request, Result result)
        {
            User popu = user ?? request.Requester ?? result.Requester;
            // only the toplevle query popuklates the user object
            if (result.ParentResult == null)
            {
                // toplevel result
                popu.addResult(result);
                if (RotateUsedTemplate)
                {
                    result.RotateUsedTemplates();
                }
            }
        }

        public string EnsureEnglish(string arg)
        {
            // ReSharper disable ConvertToConstant.Local
            bool DoOutputSubst = false;
            // ReSharper restore ConvertToConstant.Local
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (DoOutputSubst)
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            {
                string sentence = ApplySubstitutions.Substitute(OutputSubstitutions, arg);
                sentence = TextPatternUtils.ReTrimAndspace(sentence);
                if (TextPatternUtils.DifferentBesidesCase(arg, sentence))
                {
                    writeDebugLine("OutputSubst: " + arg + " -> " + sentence);
                    arg = sentence;
                }
            }
            return arg;

            return ToEnglish(arg);
        }

        public delegate void InputParser(Request request, IEnumerable<Unifiable> rawSentences);

        private string swapPerson(string inputString)
        {
            if (Loader == null)
            {
                Loader = new AIMLLoader(this, GetBotRequest("swapPerson " + inputString));
            }
            string temp = Loader.Normalize(inputString, true);
            //temp = ApplySubstitutions.Substitute(this, this.PersonSubstitutions, temp);
            temp = ApplySubstitutions.Substitute(Person2Substitutions, temp);
            return temp;
        }

        public Unifiable CleanupCyc(string text)
        {
            if (text == null) return null;
            if (text == "")
            {
                return "";
            }
            text = text.Trim();
            if (text == "")
            {
                writeToLog(" had white string ");
                return "";
            }
            if (TheCyc != null)
            {
                text = TheCyc.CleanupCyc(text);
            }
            return text.Replace("#$", " ").Replace("  ", " ").Trim();
        }


        private void CheckResult(Request request, Result result, out int solutions, out bool hasMoreSolutions)
        {
            var isTraced = request.IsTraced || result.IsTraced;
            hasMoreSolutions = false;
            solutions = 0;
            // checks that we have nothing to do
            if (result == null || result.SubQueries == null)
            {
                if (isTraced)
                {
                    writeToLog("NO QUERIES FOR " + request);
                }
                return;
            }


            List<SubQuery> resultSubQueries = result.SubQueries;

            List<SubQuery> AllQueries = new List<SubQuery>();
            List<TemplateInfo> AllTemplates = new UList();
            bool found1 = false;
            lock (resultSubQueries)
            {
                foreach (SubQuery query in resultSubQueries)
                {
                    var queryTemplates = query.Templates;
                    if (queryTemplates == null || queryTemplates.Count == 0) continue;
                    AllQueries.Add(query);
                    if (!found1) found1 = true;
                    lock (queryTemplates)
                        AllTemplates.AddRange(query.Templates);
                }
            }

            if (!found1)
            {
                solutions = 0;
                hasMoreSolutions = false;
                result.TemplatesSucceeded = 0;
                result.OutputsCreated = 0;
                if (isTraced)
                {
                    writeToLog("NO TEMPLATES FOR " + request);
                }
                return;
            }

            hasMoreSolutions = true;
            List<SubQuery> sortMe = new List<SubQuery>(AllQueries);

            sortMe.Sort();

            bool countChanged = sortMe.Count != AllQueries.Count;
            for (int index = 0; index < sortMe.Count; index++)
            {
                var subQuery = sortMe[index];
                var allQ = AllQueries[index];
                if (subQuery.EqualsMeaning(allQ))
                {
                    continue;
                }
                countChanged = true;
            }
            if (isTraced || countChanged)
            {
                string cc = countChanged ? "QUERY SAME " : "QUERY SORT ";
                writeToLog("--------------------------------------------");
                if (countChanged)
                {

                    int sqNum = 0;
                    writeToLog("AllQueries.Count = " + sortMe.Count + " was " + AllQueries);
                    if (false)
                        foreach (SubQuery query in AllQueries)
                        {
                            writeToLog("---BEFORE QUERY " + sqNum + ": " + query.Pattern + " " + query.Pattern.Graph);
                            sqNum++;
                        }
                }
                if (isTraced)
                {
                    int sqNum = 0;
                    foreach (SubQuery query in sortMe)
                    {
                        writeToLog("--- " + cc + sqNum + ": " + query.Pattern + " " + query.Pattern.Graph);
                        sqNum++;
                    }
                }
                writeToLog("--------------------------------------------");
            }

            foreach (SubQuery query in sortMe)
            {
                foreach (TemplateInfo s in query.Templates)
                {
                    hasMoreSolutions = true;
                    try
                    {
                        result.CurrentQuery = query;
                        // Start each the same
                        var lastHandler = ProcessQueryTemplate(request, s.Query, s, result, request.LastHandler,
                                                               ref solutions,
                                                               out hasMoreSolutions);                      
                    }
                    catch (ChatSignal)
                    {
                        if (!result.IsToplevelRequest) throw;
                    }
                }
            }
            hasMoreSolutions = false;
            result.CurrentQuery = null;
        }

        private AIMLTagHandler ProcessQueryTemplate(Request request, SubQuery query, TemplateInfo s, Result result, AIMLTagHandler lastHandler, ref int solutions, out bool hasMoreSolutions)
        {
            AIMLTagHandler childHandler = null;
            s.Rating = 1.0;
            hasMoreSolutions = false;
            try
            {
                s.Query = query;
                query.CurrentTemplate = s;
                bool createdOutput;
                bool templateSucceeded;
                XmlNode sOutput = s.ClonedOutput;
                childHandler = proccessResponse(query, request, result, sOutput, s.Guard, out createdOutput,
                                               out templateSucceeded, lastHandler, s, false, false);
                solutions++;
                request.LastHandler = lastHandler;
                if (templateSucceeded)
                {
                    result.TemplatesSucceeded++;
                }
                if (createdOutput)
                {
                    result.OutputsCreated++;
                    hasMoreSolutions = true;
                    //break; // KHC: single vs. Multiple
                    if (((QuerySettingsReadOnly)request).ProcessMultipleTemplates == false)
                    {
                        if (request.IsComplete(result))
                        {
                            hasMoreSolutions = false;
                            return lastHandler;
                        }
                    }
                }
                return childHandler;
            }
            catch (ChatSignal e)
            {
                throw;
            }
            catch (Exception e)
            {
                writeToLog(e);
                if (WillCallHome)
                {
                    phoneHome(e.Message, request);
                }
                writeToLog("WARNING! A problem was encountered when trying to process the input: " +
                           request.rawInput + " with the template: \"" + s + "\"");
                hasMoreSolutions = false;
            }
            return childHandler;
        }

        public void writeChatTrace(string message, params object[] args)
        {
            if (!chatTrace) return;
            try
            {
                lock (chatTraceO)
                {
                    if (chatTraceW == null)
                    {
                        chatTraceW = new StreamWriter("bgm\\chatTrace.dot");
                        chatTraceW.WriteLine("digraph G {");
                        streamDepth = 1;
                    }
                    if (streamDepth < 0) streamDepth = 0;
                    int w = streamDepth;
                    while (w-- < 0)
                    {
                        chatTraceW.Write("  ");
                    }
                    if (args != null && args.Length != 0) message = String.Format(message, args);
                    chatTraceW.WriteLine(message);
                    //writeDebugLine(message);
                    if (streamDepth <= 0 && chatTraceW != null)
                    {
                        chatTraceW.WriteLine("}");
                        chatTraceW.Close();
                        streamDepth = 0;
                        chatTraceW = null;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public AIMLbot.MasterResult ImmediateAiml(XmlNode templateNode, Request request0,
                                            AIMLLoader loader, AIMLTagHandler handler)
        {
            bool fastCall = handler == null;
            MasterResult masterResult = request0.CreateResult(request0);
            bool prev = request0.GraphsAcceptingUserInput;
            try
            {
                request0.GraphsAcceptingUserInput = true;
                var mr = ImmediateAIML0(request0, templateNode, handler, fastCall);
                return mr;
            }
            catch (ChatSignal ex)
            {
                writeToLog(ex);
                return masterResult;
            }
            catch (Exception ex)
            {
                writeToLog(ex);
                return masterResult;
            }
            finally
            {
                request0.GraphsAcceptingUserInput = prev;
            }
        }

        private AIMLbot.MasterResult ImmediateAIML0(Request parentRequest, XmlNode templateNode, AIMLTagHandler handler, bool isFastAIML)
        {
            if (isFastAIML)
            {

            }

            string requestName = ToTemplateXML(templateNode);

            RTPBot request0Proccessor = this;
            GuardInfo sGuard = null;
            Request request = null;
            User user = BotAsUser;

            if (parentRequest != null)
            {
                user = parentRequest.Requester;
                requestName = parentRequest.rawInput + " " + requestName;
            }

            //  if (request == null)

            request = parentRequest;
            // new AIMLbot.Request(requestName, user, request0Proccessor, (AIMLbot.Request)parentRequest);

            if (parentRequest != null)
            {
                if (parentRequest != request) request.Graph = parentRequest.Graph;
                //request.depth = parentRequest.depth + 1;
            }

            AIMLbot.MasterResult result = request.CreateResult(request);
            //request.CurrentResult = result;
            if (request.CurrentResult != result)
            {
                writeToLog("ERROR did not set the result!");
            }
            if (request.Graph != result.Graph)
            {
                writeToLog("ERROR did not set the Graph!");
            }
            TemplateInfo templateInfo = null; //
            if (false)
            {
                templateInfo = new TemplateInfo(templateNode, null, null, null);
            }
            bool templateSucceeded;
            bool createdOutput;
            SubQuery query = request.CurrentQuery;
            bool doUndos = false;
            bool copyChild = false;
            if (query == null)
            {
                query = new SubQuery(requestName, result, request);
                request.IsTraced = true;
                doUndos = true;
                copyChild = true;
            }
            if (copyChild)
            {
                if (templateInfo != null)
                    templateNode = templateInfo.ClonedOutput;
                copyChild = false;
            }
            var lastHandler = proccessResponse(query, request, result, templateNode, sGuard, out createdOutput, out templateSucceeded,
                             handler, templateInfo, copyChild, false); //not sure if should copy parent
            if (doUndos) query.UndoAll();
            request.LastHandler = lastHandler;
            return result;
        }

        public AIMLTagHandler proccessResponse(SubQuery query,
                                             Request request, Result result,
                                             XmlNode templateNode, GuardInfo sGuard,
                                             out bool createdOutput, out bool templateSucceeded,
                                             AIMLTagHandler handler, TemplateInfo templateInfo,
                                             bool copyChild, bool copyParent)
        {
            //request.CurrentResult = result;
            query = query ?? request.CurrentQuery;
            templateInfo = templateInfo ?? query.CurrentTemplate;
            //request.CurrentQuery = query;
            if (!request.CanUseTemplate(templateInfo, result))
            {
                templateSucceeded = false;
                createdOutput = false;
                return null;
            }
            UndoStack undoStack = UndoStack.GetStackFor(query);
            try
            {
                return proccessTemplate(query, request, result, templateNode, sGuard,
                                           out createdOutput, out templateSucceeded,
                                           handler, templateInfo, copyChild, copyParent);
            }
            finally
            {
                undoStack.UndoAll();
            }
        }

        private AIMLTagHandler proccessTemplate(SubQuery query, Request request, Result result,
                                                XmlNode templateNode, GuardInfo sGuard,
                                                out bool createdOutput, out bool templateSucceeded,
                                                AIMLTagHandler handler, TemplateInfo templateInfo,
                                                bool copyChild, bool copyParent)
        {
            ChatLabel label = request.PushScope;
            var prevTraced = request.IsTraced;
            var untraced = request.Graph.UnTraced;
            var superTrace = templateInfo != null && templateInfo.IsTraced;
            try
            {
                if (superTrace)
                {
                    request.IsTraced = true;
                    request.Graph.UnTraced = false;
                }

                var th = proccessResponse000(query, request, result, templateNode, sGuard,
                                           out createdOutput, out templateSucceeded,
                                           handler, templateInfo, copyChild, copyParent);

                if (superTrace)
                {
                    writeDebugLine("SuperTrace=" + templateSucceeded + ": " + templateInfo);
                }
                return th;
            }
            catch (ChatSignalOverBudget ex)
            {
                throw;
            }
            catch (ChatSignal ex)
            {
                if (label.IsSignal(ex))
                {
                    // if (ex.SubQuery != query) throw;
                    if (ex.NeedsAdding)
                    {
                        request.AddOutputSentences(templateInfo, ex.TemplateOutput, result);
                    }
                    templateSucceeded = ex.TemplateSucceeded;
                    createdOutput = ex.CreatedOutput;
                    return ex.TagHandler;
                }
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                request.IsTraced = prevTraced;
                request.Graph.UnTraced = untraced;
                label.PopScope();
            }
        }

        public AIMLTagHandler proccessResponse000(SubQuery query, Request request, Result result,
                                                XmlNode sOutput, GuardInfo sGuard,
                                                out bool createdOutput, out bool templateSucceeded,
                                                AIMLTagHandler handler, TemplateInfo templateInfo,
                                                bool copyChild, bool copyParent)
        {
            query.LastTagHandler = handler;
            bool isTraced = request.IsTraced || result.IsTraced || !request.GraphsAcceptingUserInput ||
                            (templateInfo != null && templateInfo.IsTraced);
            //XmlNode guardNode = AIMLTagHandler.getNode(s.Guard.InnerXml);
            bool usedGuard = sGuard != null && sGuard.Output != null;
            sOutput = sOutput ?? templateInfo.ClonedOutput;
            string output = sOutput.OuterXml;
            XmlNode templateNode = sOutput;
            bool childOriginal = true;
            result.Started = true;
            if (usedGuard)
            {
                string guardStr = "<template>" + sGuard.Output.InnerXml + " GUARDBOM " + sOutput.OuterXml +
                                  "</template>";
                templateNode = getNode(guardStr, sOutput);
                childOriginal = false;
            }

            bool protectChild = copyChild || childOriginal;
            AIMLTagHandler tagHandler;
            string outputSentence = processNode(templateNode, query,
                                                request, result, request.Requester, handler,
                                                protectChild, copyParent, out tagHandler);
            if (tagHandler == null)
            {
                writeToLog("tagHandler = null " + output);
            }
            templateSucceeded = !IsFalse(outputSentence);

            int f = outputSentence.IndexOf("GUARDBOM");
            if (f < 0)
            {
                string o = ToEnglish(outputSentence);
                if (IsOutputSentence(o))
                {
                    if (isTraced)
                    {
                        string aIMLLoaderParentTextAndSourceInfo = ParentTextAndSourceInfo(templateNode);
                        if (aIMLLoaderParentTextAndSourceInfo.Length > 300)
                        {
                            aIMLLoaderParentTextAndSourceInfo = TextFilter.ClipString(
                                aIMLLoaderParentTextAndSourceInfo, 300);
                        }
                        writeToLog("AIMLTRACE '{0}' IsOutputSentence={1}", o, aIMLLoaderParentTextAndSourceInfo);
                    }
                    createdOutput = true;
                    templateSucceeded = true;
                    request.AddOutputSentences(templateInfo, o, result);
                }
                else
                {
                    createdOutput = false;
                }
                if (!createdOutput && isTraced && request.GraphsAcceptingUserInput)
                {
                    if (templateInfo != null)
                    {
                        string fromStr = " from " + templateInfo.Graph;
                        if (!StaticAIMLUtils.IsSilentTag(templateNode))
                        {
                            writeToLog("SILENT '{0}' TEMPLATE={1}", o, ParentTextAndSourceInfo(templateNode) + fromStr);
                        }
                        templateInfo.IsDisabled = true;
                        request.AddUndo(() =>
                        {
                            templateInfo.IsDisabled = false;
                        });
                    }
                    else
                    {
                        writeToLog("UNUSED '{0}' TEMPLATE={1}", o, ParentTextAndSourceInfo(templateNode));
                    }

                }

                return tagHandler;
            }
            try
            {
                string left = outputSentence.Substring(0, f).Trim();
                templateSucceeded = !IsFalse(left);
                if (!templateSucceeded)
                {
                    createdOutput = false;
                    return tagHandler;
                }
                string lang = GetAttribValue(sGuard.Output, "lang", "cycl").ToLower();

                try
                {
                    Unifiable ss = SystemExecute(left, lang, request);
                    if (IsFalse(ss) || IsNullOrEmpty(ss))
                    {
                        if (isTraced)
                            writeToLog("GUARD FALSE '{0}' TEMPLATE={1}", request,
                                       ParentTextAndSourceInfo(templateNode));
                        templateSucceeded = false;
                        createdOutput = false;
                        return tagHandler;
                    }
                    else
                    {
                        templateSucceeded = true;
                    }
                }
                catch (ChatSignal e)
                {
                    throw;
                }
                catch (Exception e)
                {
                    writeToLog(e);
                    templateSucceeded = false;
                    createdOutput = false;
                    return tagHandler;
                }

                //part the BOM
                outputSentence = outputSentence.Substring(f + 9);
                string o = ToEnglish(outputSentence);
                if (IsOutputSentence(o))
                {
                    if (isTraced)
                        writeToLog(query.Graph + ": GUARD SUCCESS '{0}' TEMPLATE={1}", o,
                                   ParentTextAndSourceInfo(templateNode));
                    templateSucceeded = true;
                    createdOutput = true;
                    request.AddOutputSentences(templateInfo, o, result);
                    return tagHandler;
                }
                else
                {
                    writeToLog("GUARD SKIP '{0}' TEMPLATE={1}", outputSentence,
                               ParentTextAndSourceInfo(templateNode));
                }
                templateSucceeded = false;
                createdOutput = false;
                return tagHandler;
            }
            catch (ChatSignal e)
            {
                throw;
            }
            catch (Exception ex)
            {
                writeToLog(ex);
                templateSucceeded = false;
                createdOutput = false;
                return tagHandler;
            }
        }

        private bool IsOutputSentence(string sentence)
        {
            if (sentence == null) return false;
            string o = ToEnglish(sentence);
            if (o == null) return false;
            return o.Length > 0;
        }

        public string ToHeard(string message)
        {
            if (message == null) return null;
            if (message.Trim().Length == 0) return "";
            if (message.StartsWith("  "))
            {
                return message;
            }
            message = message.Trim();
            if (message == "") return "";
            if (false && message.Contains("<"))
            {
                string messageIn = message;
                message = ForInputTemplate(message);
                //if (messageIn != message) writeDebugLine("heardSelfSay - ForInputTemplate: " + messageIn + " -> " + message);
            }

            if (message == "") return "";
            //if (message.Contains("<"))
            {
                string messageIn = message;
                message = ToEnglish(message);
                if (messageIn != message) writeDebugLine("heardSelfSay - ToEnglish: " + messageIn + " -> " + message);
            }

            if (message == "" || message.Contains("<"))
            {
                writeDebugLine("heardSelfSay - ERROR: heard ='" + message + "'");
                return message;
            }
            return "  " + message;
        }

        static char[] toCharArray = "@#$%^&*()_+<>,/{}[]\\\";'~~".ToCharArray();
        public string ToEnglish(string sentenceIn)
        {
            if (sentenceIn == null)
            {
                return null;
            }
            sentenceIn = ReTrimAndspace(sentenceIn);
            if (sentenceIn == "")
            {
                return "";
            }

            string sentence = VisibleRendering(StaticAIMLUtils.getTemplateNode(sentenceIn).ChildNodes,
                                               PatternSideRendering);

            sentence = ReTrimAndspace(sentence);
            if (DifferentBesidesCase(sentenceIn, sentence))
            {
                writeToLog("VisibleRendering: " + sentenceIn + " -> " + sentence);
                sentenceIn = sentence;
            }

            if (sentence == "")
            {
                return "";
            }

            const bool DoInputSubsts = false;
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (DoInputSubsts)
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            {
                sentenceIn = ToInputSubsts(sentenceIn);
            }

            sentence = CleanupCyc(sentenceIn);
            sentence = ReTrimAndspace(sentence);
            if (DifferentBesidesCase(sentenceIn, sentence))
            {
                writeToLog("CleanupCyc: " + sentenceIn + " -> " + sentence);
                sentenceIn = sentence;
            }

            sentence = ApplySubstitutions.Substitute(OutputSubstitutions, sentenceIn);
            sentence = ReTrimAndspace(sentence);
            if (DifferentBesidesCase(sentenceIn, sentence))
            {
                writeToLog("OutputSubst: " + sentenceIn + " -> " + sentence);
                sentenceIn = sentence;
            }

            if (!checkEndsAsSentence(sentenceIn))
            {
                sentenceIn += ".";
            }

            return sentenceIn;
        }

        public string ToInputSubsts(string sentenceIn)
        {
            string sentence;
            sentence = ApplySubstitutions.Substitute(InputSubstitutions, sentenceIn);
            //sentence = string.Join(" ", sentence.Split(toCharArray, StringSplitOptions.RemoveEmptyEntries));
            sentence = ReTrimAndspace(sentence);
            if (DifferentBesidesCase(sentenceIn, sentence))
            {
                writeToLog("InputSubst: " + sentenceIn + " -> " + sentence);
                sentenceIn = sentence;
            }
            return sentenceIn;
        }


        /// <summary>
        /// Recursively evaluates the template nodes returned from the Proccessor
        /// </summary>
        /// <param name="node">the node to evaluate</param>
        /// <param name="query">the query that produced this node</param>
        /// <param name="request">the request from the user</param>
        /// <param name="result">the result to be sent to the user</param>
        /// <param name="user">the user who originated the request</param>
        /// <returns>the output Unifiable</returns>
        public string processNodeDebug(XmlNode childNode, SubQuery query,
                                  Request request, Result result, User user,
                                  AIMLTagHandler parent, bool copyChild, bool copyParent,
                                  out AIMLTagHandler tagHandlerChild)
        {
            var wasSuspendRestrati = result.SuspendSearchLimits;
            try
            {
                result.SuspendSearchLimits = true;
                return processNode(childNode, query,
                             request, result, user,
                             parent, copyChild, copyParent,
                             out tagHandlerChild);
            }
            finally
            {
                result.SuspendSearchLimits = wasSuspendRestrati;
            }
        }

        /// <summary>
        /// Recursively evaluates the template nodes returned from the Proccessor
        /// </summary>
        /// <param name="node">the node to evaluate</param>
        /// <param name="query">the query that produced this node</param>
        /// <param name="request">the request from the user</param>
        /// <param name="result">the result to be sent to the user</param>
        /// <param name="user">the user who originated the request</param>
        /// <returns>the output Unifiable</returns>
        public string processNode(XmlNode node, SubQuery query,
                                  Request request, Result result, User user,
                                  AIMLTagHandler parent, bool protectChild, bool copyParent,
                                  out AIMLTagHandler tagHandler)
        {
            RequestImpl originalSalientRequest = RequestImpl.GetOriginalSalientRequest(request);
            var sraiMark = originalSalientRequest.CreateSRAIMark();
            string outputSentence = processNodeVV(node, query,
                                                  request, result, user, parent,
                                                  protectChild, copyParent, out tagHandler);
            originalSalientRequest.ResetSRAIResults(sraiMark);
            if (!Unifiable.IsNullOrEmpty(outputSentence)||IsSilentTag(node))
            {
                return outputSentence;
            }
            if (tagHandler.RecurseResultValid) return tagHandler.RecurseResult;
            if (Unifiable.IsNull(outputSentence))
            {
                outputSentence = tagHandler.GetTemplateNodeInnerText();
                return outputSentence;
            }
            return outputSentence;
        }

        /// <summary>
        /// Recursively evaluates the template nodes returned from the Proccessor
        /// </summary>
        /// <param name="node">the node to evaluate</param>
        /// <param name="query">the query that produced this node</param>
        /// <param name="request">the request from the user</param>
        /// <param name="result">the result to be sent to the user</param>
        /// <param name="user">the user who originated the request</param>
        /// <returns>the output Unifiable</returns>
        public string processNodeVV(XmlNode node, SubQuery query,
                                  Request request, Result result, User user,
                                  AIMLTagHandler parent, bool protectChild, bool copyParent,
                                  out AIMLTagHandler tagHandler)
        {
            if (node != null && node.NodeType == XmlNodeType.Text)
            {
                tagHandler = null;
                string s = node.InnerText.Trim();
                if (!String.IsNullOrEmpty(s))
                {
                    return ValueText(s);
                }
                //return s;
            }
            bool isTraced = request.IsTraced || result.IsTraced || !request.GraphsAcceptingUserInput ||
                (query != null && query.IsTraced);
            // check for timeout (to avoid infinite loops)
            bool overBudget = false;
            if (request.IsComplete(result))
            {
                object gn = request.Graph;
                if (query != null) gn = query.Graph;
                string s = string.Format("WARNING! Request " + request.WhyComplete +
                                         ". User: {0} raw input: {3} \"{1}\" processing {2} templates: \"{4}\"",
                                         request.Requester.UserID, request.rawInput,
                                         (query == null ? "-NOQUERY-" : query.Templates.Count.ToString()), gn, node);

                if (isTraced)
                    request.writeToLog(s);
                overBudget = true;
                if (!request.IsToplevelRequest)
                {
                    throw new ChatSignalOverBudget(s);
                }
            }

            XmlNode oldNode = node;
            // copy the node!?!
            if (protectChild)
            {
                copyParent = true;
                LineInfoElementImpl newnode = CopyNode(node, copyParent);
                newnode.ReadOnly = false;
                node = newnode;
            }

            // process the node
            tagHandler = GetTagHandler(user, query, request, result, node, parent);
            if (ReferenceEquals(null, tagHandler))
            {
                if (node.NodeType == XmlNodeType.Comment)
                {
                    return Unifiable.Empty;
                }
                if (node.NodeType == XmlNodeType.Text)
                {
                    string s = node.InnerText.Trim();
                    if (String.IsNullOrEmpty(s))
                    {
                        return Unifiable.Empty;
                    }
                    return s;
                }
// ReSharper disable ConditionIsAlwaysTrueOrFalse
                OutputDelegate del = (request != null) ? request.writeToLog : writeToLog;
// ReSharper restore ConditionIsAlwaysTrueOrFalse
                if (overBudget)
                {
                    return Unifiable.Empty;
                }
                EvalAiml(node, request, del ?? DEVNULL);
                return node.InnerXml;
            }

            if (overBudget)
            {
                tagHandler.Dispose();
                tagHandler = null;
                return Unifiable.Empty;
            }
            
            tagHandler.SetParent(parent);
            //if (parent!=null) parent.AddChild(tagHandler);

            Unifiable cp = tagHandler.CompleteAimlProcess();
            if (Unifiable.IsNullOrEmpty(cp) && (!tagHandler.QueryHasSuceeded || tagHandler.QueryHasFailed))
            {
                bool needsOneMoreTry = !request.SuspendSearchLimits &&
                                       (request.IsToplevelRequest /*|| result.ParentRequest.IsToplevelRequest*/);
                if (isTraced || needsOneMoreTry)
                {
                    //writeDebugLine("ERROR: Try Again since NULL " + tagHandler);
                    bool wsl = request.SuspendSearchLimits;
                    try
                    {
                        request.SuspendSearchLimits = true;
                        cp = tagHandler.CompleteAimlProcess();
                        if (Unifiable.IsNull(cp))
                        {
                            return tagHandler.GetTemplateNodeInnerText();
                        }
                        if (Unifiable.IsNullOrEmpty(cp))
                        {
                            // trace the next line to see why
                            AIMLTagHandler handler = tagHandler;
                            TraceTest("ERROR: Try Again since NULL " + handler,
                                () => { cp = handler.CompleteAimlProcess(); });
                        }
                    }
                    finally
                    {
                        request.SuspendSearchLimits = wsl;

                    }
                }
            }
            if (tagHandler.QueryHasFailed)
            {
                return Unifiable.Empty;
            }
            if (!Unifiable.IsNullOrEmpty(cp) || IsSilentTag(node))
            {
                return cp;
            }
            if (Unifiable.IsNull(cp))
            {
                cp = tagHandler.GetTemplateNodeInnerText();
                return cp;
            }
            return cp;
        }
        #endregion

        private void SetupConveration()
        {
            HeardSelfSayQueue.Start();
            AddBotCommand("do16", () =>
                                      {
                                          Enqueue(() => Sleep16Seconds(10));
                                          Enqueue(() => Sleep16Seconds(1));
                                          Enqueue(() => Sleep16Seconds(30));
                                          Enqueue(() => Sleep16Seconds(5));
                                          Enqueue(() => Sleep16Seconds(5));
                                          Enqueue(() => Sleep16Seconds(45));
                                      });

            string names_str = "markovx.trn 5ngram.ngm";
            var nameset = names_str.Split(' ');
            foreach (string name in nameset)
            {
                int loadcount = 0;
                string file = HostSystem.Combine("trn", name);
                if (HostSystem.FileExists(file))
                {
                    StreamReader sr = new StreamReader(file);
                    writeToLog(" **** Markovian Brain LoadMarkovLTM: '{0}'****", file);
                    MBrain.Learn(sr);
                    sr.Close();
                    writeToLog(" **** Markovian Brain initialized.: '{0}' **** ", file);
                    loadcount++;
                }

                file = HostSystem.Combine("ngm", name);
                if (HostSystem.FileExists(file))
                {
                    StreamReader sr = new StreamReader(file);
                    writeToLog(" **** Markovian Brain LoadMarkovLTM: '{0}'**** ", file);
                    MBrain.LearnNgram(sr);
                    sr.Close();
                    writeToLog(" **** Markovian Brain N-Gram initialized '{0}'. **** ", file);
                    loadcount++;
                }

                if (loadcount == 0)
                {
                    writeToLog(
                        " **** WARNING: No Markovian Brain Training nor N-Gram file found for '{0}' . **** ", name);
                }
            }

            if (pHMM.hmmCorpusLoaded == 0)
            {
                string file = HostSystem.Combine("bgm", "corpus.txt");
                //if (HostSystem.DirExists(file))
                if (HostSystem.FileExists(file))
                {
                    writeToLog("Load Corpus Bigrams: '{0}'", file);
                    StreamReader sr = new StreamReader(file);
                    pHMM.LearnBigramFile(sr);
                    sr.Close();
                    pHMM.hmmCorpusLoaded++;
                    writeToLog("Loaded Corpus Bigrams: '{0}'", file);
                }
            }
            Console.WriteLine("*** Start WN-Load ***");
            wordNetEngine = new WordNetEngine(PathToWordNet, true);
            Console.WriteLine("*** DONE WN-Load ***");

            Console.WriteLine("*** Start Lucene ***");
            var myLuceneIndexer = new MyLuceneIndexer(PathToLucene, fieldName, this, wordNetEngine);
            this.LuceneIndexer = myLuceneIndexer;
            myLuceneIndexer.TheBot = this;
            TripleStore = myLuceneIndexer.TripleStoreProxy;
            myLuceneIndexer.wordNetEngine = wordNetEngine;
            Console.WriteLine("*** DONE Lucene ***");
        }

        private int napNum = 0;

        private void Sleep16Seconds(int secs)
        {
            napNum++;
            DateTime start = DateTime.Now;
            var errOutput = DLRConsole.SYSTEM_ERR_WRITELINE;
            string thisTime = " #" + napNum;
            try
            {
                errOutput("START Sleep" + secs + "Seconds " + thisTime + " \ntime=" + DateTime.Now.Subtract(start).TotalMilliseconds);
                Thread.Sleep(TimeSpan.FromSeconds(secs));
                errOutput("COMPLETE Sleep" + secs + "Seconds " + thisTime + " \ntime=" + DateTime.Now.Subtract(start).TotalSeconds);
                Enqueue("ENQUE Sleep" + secs + "Seconds #" + thisTime, () => Sleep16Seconds(secs));

            }
            catch (ThreadAbortException e)
            {
                errOutput("ThreadAbortException Sleep" + secs + "Seconds " + e + " " + thisTime + " \ntime=" + DateTime.Now.Subtract(start).TotalSeconds);
            }
            catch (ThreadInterruptedException e)
            {
                errOutput("ThreadInterruptedException Sleep" + secs + "Seconds " + e + " " + thisTime + " \ntime=" + DateTime.Now.Subtract(start).TotalSeconds);
            }
            catch (Exception e)
            {
                errOutput("Exception Sleep" + secs + "Seconds " + e + " " + thisTime + " \ntime=" + DateTime.Now.Subtract(start).TotalSeconds);
                throw;
            }
            finally
            {
                errOutput("Finanaly Sleep" + secs + "Seconds #" + thisTime + " \ntime=" + DateTime.Now.Subtract(start).TotalSeconds);
            }
        }


        internal void Enqueue(ThreadStart action)
        {
            HeardSelfSayQueue.Enqueue(HeardSelfSayQueue.NamedTask("Enqueue_" + napNum, action));
        }
        internal void Enqueue(string named, ThreadStart action)
        {
            HeardSelfSayQueue.Enqueue(HeardSelfSayQueue.NamedTask(named, action));
        }

        private object ChatWithThisBot(string cmd, Request request)
        {
            Request req = request.CreateSubRequest(cmd);
            req.Responder = BotAsUser;
            req.IsToplevelRequest = request.IsToplevelRequest;
            return LightWeigthBotDirective(cmd, req);
        }
    }
}