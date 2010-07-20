﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
using HttpServer;
using HttpServer.FormDecoders;
using MushDLR223.ScriptEngines;


namespace MushDLR223.Utilities
{
    //.. tonight i am writing them a webserver in .net 
    public class WriteLineToResponse
    {
        public IHttpResponse response;
        private ClientManagerHttpServer Server;
        public WriteLineToResponse(ClientManagerHttpServer server, IHttpResponse r)
        {
            response = r;
            Server = server;
        }
        public void WriteLine(string str, object[] args)
        {
            try
            {

                string s = string.Format(str, args);
                if (response != null)
                {
                    response.AddToBody(s + Environment.NewLine);
                }
                else
                {
                    Server.LogInfo("no respnse object for " + s);
                }
            } catch(Exception e)
            {
                Console.WriteLine("" + e);
                Server.LogInfo("WriteLine exception" + e);
            }
        }
    }

    public class ClientManagerHttpServer : ILogWriter
    {
        private ScriptExecutorGetter getter;
        HttpServer.HttpListener _listener;
        private int _port;
        private ScriptExecutorGetter clientManager;
        private string defaultUser = "UNKNOWN_PARTNER";

        public ClientManagerHttpServer(ScriptExecutorGetter bc, int port)
        {
            clientManager = bc;
            _port = port;
            Init();
        }
        public void Init()
        {
            _listener = HttpServer.HttpListener.Create(this, IPAddress.Any, _port);
            _listener.Accepted += _listener_Accepted;
            _listener.Set404Handler(_listener_404);
            workArroundReuse();
            try
            {
                _listener.Start(10);
                LogInfo("Ready for HTTPD port " + _port);
                WriteLine("Ready for HTTPD port " + _port);
            }
            catch (Exception e)
            {
                WriteLine("NOT OK for HTTPD port " + _port + "\n" + e);
            }
        }

        private void WriteLine(string s)
        {
            clientManager.WriteLine(s);
        }

        private void workArroundReuse()
        {
            try
            {
                TcpClient client = new TcpClient();
                client.Connect("localhost", _port);
            }
            catch (Exception exception)
            {
                LogInfo("Listener workarround: " + exception.Message);
            }
        }

        readonly public static object HttpLock = new object ();
        private void _listener_404(IHttpClientContext context, IHttpRequest request, IHttpResponse response)
        {
            // 10 second wait
            if (Monitor.TryEnter(HttpLock, 10000))
            {
                _listener_4040(context, request, response);
                Monitor.Exit(HttpLock);
            }
            else
            {
                _listener_4040(context, request, response);
            } 
        }
        private void _listener_4040(IHttpClientContext context, IHttpRequest request, IHttpResponse response)
        {
            try
            {
                _listener_4040_errable(context, request, response);
            }
            catch (Exception exception)
            {
                LogInfo("Listener exception: " + exception);
            }            
        }

        private void _listener_4040_errable(IHttpClientContext context, IHttpRequest request, IHttpResponse response)
        {
          //  UUID capsID;
            bool success;

            string path = request.Uri.PathAndQuery;//.TrimEnd('/');
            string pathd = HttpUtility.UrlDecode(request.Uri.PathAndQuery);//.TrimEnd('/');
            LogInfo("_listener " + path + " from " + request.RemoteEndPoint);
            if (request.UriPath.EndsWith(".ico"))
            {
                response.Status = HttpStatusCode.NotFound;
                response.Send();
            }
            var wrresp = new WriteLineToResponse(this, response);

            string botname = GetVariable(request, "bot", GetVariable(request, "botid", null));

            ScriptExecutor _botClient = clientManager.GetScriptExecuter(botname);
            if (_botClient==null){
                response.Status = HttpStatusCode.ServiceUnavailable;
                response.Send();
                return;
            }

            // Micro-posterboard
            if (pathd.StartsWith("/posterboard"))
            {
                string slot = path;
                string value = "";
                value = _botClient.getPosterBoard(slot) as string;
                if (value!=null)                    
                    if (value.Length > 0) { LogInfo(String.Format(" board response: {0} = {1}", slot, value)); }
                AddToBody(response, "<xml>");
                AddToBody(response, "<slot>");
                AddToBody(response, "<path>" + path + "</path>");
                AddToBody(response, "<value>" + (value ?? "") + "</value>");
                AddToBody(response, "</slot>");
                AddToBody(response, "</xml>");

                wrresp.response = null;
                response.Status = HttpStatusCode.OK;
                response.Send();
                return;
            }

            bool useHtml = false;
            if (request.Method == "POST")
            {
                var fdp = new FormDecoderProvider();
                fdp.Add(new MultipartDecoder());
                fdp.Add(new UrlDecoder());
                request.DecodeBody(fdp);
            }

            if (path.StartsWith("/?") || path.StartsWith("/test"))
            {
                useHtml = true;
            }
            try
            {
                if (useHtml)
                {
                    AddToBody(response, "<html>");
                    AddToBody(response, "<head>");
                    botname = GetVariable(request, "bot", _botClient.GetName());

                    AddToBody(response, "<title>" + botname + "</title>");
                    AddToBody(response, "</head>");
                    AddToBody(response, "<body>");
                    AddToBody(response, "<pre>");
                    foreach (var p in request.Param.AsEnumerable())
                    {
                        foreach (var item in p.Values)
                        {
                            AddToBody(response, "" + p.Name + " = " + item);
                        }
                    }
                    AddToBody(response, "</pre>");
                    AddToBody(response, "<a href='" + request.Uri.PathAndQuery + "'>"
                                        + request.Uri.PathAndQuery + "</a>");
                    AddToBody(response, "<pre>");
                }


                string cmd = GetVariable(request, "cmd", "MeNe");

                CmdResult res;
                // this is our default handler
                if (cmd != "MeNe")
                {
                    res = _botClient.ExecuteXmlCommand(cmd  + " " + GetVariable(request, "args", ""), wrresp.WriteLine);

                }
                else
                {
                    AddToBody(response, "<xml>");
                    AddToBody(response, "\n<!-- Begin Response !-->");
                    // this is our MeNe handler
                    string username = GetVariable(request, "username", GetVariable(request, "ident", null));
                    string saytext = GetVariable(request, "saytext", "missed the post");
                    string text = GetVariable(request, "text", GetVariable(request, "entry", pathd.TrimStart('/')));
                    if (text.Contains("<sapi>"))
                    {
                        // example fragment
                        // <sapi> <silence msec="100" /> <bookmark mark="anim:hello.csv"/> Hi there </sapi>
                        text = text.Replace("<sapi>", "");
                        text = text.Replace("</sapi>", "");
                        while (text.Contains("<"))
                        {
                            int p1 = text.IndexOf("<");
                            int p2 = text.IndexOf(">",p1);
                            if (p2>p1) 
                            {
                                string fragment = text.Substring(p1, (p2+1) - p1);
                                text = text.Replace(fragment, " ");
                            }
                        }

                    }

                    if (String.IsNullOrEmpty(username))
                    {
                        //res = _botClient.ExecuteCommand(cmd + " " + text, wrresp.WriteLine);
                        res = _botClient.ExecuteCommand("aiml @ " + defaultUser + " - " + text, wrresp.WriteLine);
                    }
                    else
                    {
                        res = _botClient.ExecuteCommand("aiml @ " + username + " - " + text, wrresp.WriteLine);
                    }
                    AddToBody(response, "");
                    AddToBody(response, "\n<!-- End Response !-->");
                    AddToBody(response, "</xml>");
                }
                if (useHtml)
                {
                    AddToBody(response, "</pre>");
                    AddToBody(response, "</body>");
                    AddToBody(response, "</html>");
                }
            }
            finally
            {
                wrresp.response = null;
                response.Status = HttpStatusCode.OK;
                try
                {
                    response.Send();
                } catch(Exception e)
                {
                    LogInfo("Exception sening respose: " + e);
                }
            }
        }

        public void LogInfo(string s)
        {
            // Console.WriteLine("[HTTP SERVER] " + s);
        }

        static public void AddToBody(IHttpResponse response, string text)
        {
            response.AddToBody(text + Environment.NewLine);
        }

        static public string GetVariable(IHttpRequest request, string varName, string defaultValue)
        {
            if (request.Param.Contains(varName))
            {
                var single = request.Param[varName].Value;
                if (!String.IsNullOrEmpty(single)) return single;
                var values = request.Param[varName].Values;
                if (values.Count > 0) return values[0];
            }
            if (request.QueryString.Contains(varName))
            {
                return HttpUtility.UrlDecode(request.QueryString[varName].Value);
            }
            return HttpUtility.UrlDecode(defaultValue);
        }

        private void _listener_Accepted(object sender, ClientAcceptedEventArgs e)
        {
            LogInfo("_listener_Accepted " + e.Socket);
        }

        #region Implementation of ILogWriter

        public void Write(object source, LogPrio priority, string message)
        {
            WriteLine(priority + " " + message);
        }

        #endregion
    }

    public interface ScriptExecutorGetter
    {
        ScriptExecutor GetScriptExecuter(object o);
        void WriteLine(string s, params object[] args);
    }

    public interface ScriptExecutor
    {
        CmdResult ExecuteCommand(string s, OutputDelegate outputDelegate);
        CmdResult ExecuteXmlCommand(string s, OutputDelegate outputDelegate);
        string GetName();
        object getPosterBoard(object slot);
    }
}
