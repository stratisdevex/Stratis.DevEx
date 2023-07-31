using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Msagl.Drawing;

using SharpConfig;

using Stratis.DevEx.Pipes;

namespace Stratis.DevEx.Gui
{
    class Program : Runtime
    {
        #region Methods

        #region Entry-point
        [STAThread]
        static void Main(string[] args)
        {
            Initialize("Stratis.DevEx.Gui", "APP", true);
            if ((args.Contains("--debug") || args.Contains("-d")) && !GlobalSetting("General", "Debug", false))
            {
                GlobalConfig["General"]["Debug"].SetValue(true);
                Logger.SetLogLevelDebug();
                Info("Debug mode enabled.");                
            }
            if (!assemblyCacheDir.Exists)
            {
                assemblyCacheDir.Create();
            }
            PipeServer = new PipeServer<MessagePack>("stratis_devexgui") { WaitFreePipe = true };
            PipeServer.ClientConnected += (sender, e) => Info("Client connected...");
            PipeServer.ExceptionOccurred += (sender, e) => Error(e.Exception, "Exception occurred in pipe server.");

            //ParserResult<Options> result = new Parser().ParseArguments<Options>(args);
            if (!args.Contains("--no-gui"))
            {
                Info("Starting GUI...");
                GuiApp = new GuiApp(Eto.Platform.Detect);
                GuiApp.Initialized += (sender, e) => GuiApp.InvokeAsync(async () => await PipeServer.StartAsync());
                GuiApp.Terminating += (sender, e) => Shutdown();
                PipeServer.MessageReceived += (sender, e) => GuiApp.AsyncInvoke(() => ReadMessage(e.Message));
                WriteRunFile();
                GuiApp.Run(new MainForm());
            }
            else
            {
                Info("Not starting GUI...");
                PipeServer.StartAsync().Wait();
                PipeServer.MessageReceived += (sender, e) => ReadMessage(e.Message);
                WriteRunFile();
                Console.CancelKeyPress += (sender, e) => Shutdown();
                Info("Press Ctrl-C to exit...");
                while (true)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
        #endregion

        public static void WriteRunFile()
        {
            var cfg = new Configuration();
            cfg.Add(new Section("Process"));
            cfg["Process"].Add(new Setting("ProcessId", System.Diagnostics.Process.GetCurrentProcess().Id));
            cfg.SaveToFile(RunFile);
            Info("Wrote run file {runfile}.", RunFile);
        }

        public static void ReadMessage(MessagePack m, Action<CompilationMessage>? cma = null, Action<ControlFlowGraphMessage>? cfgma = null, Action<SummaryMessage>? sma = null)
        {
            switch (m.Type)
            {
                case MessageType.COMPILATION:
                    var cm = MessageUtils.Deserialize<CompilationMessage>(m.MessageBytes);
                    Info("Compilation message received: \n{msg}", MessageUtils.PrettyPrint(cm));
                    CompilationMessages.Push(cm);
                    cma?.Invoke(cm);
                    break;

                case MessageType.CONTROL_FLOW_GRAPH:
                    var cfgm = MessageUtils.Deserialize<ControlFlowGraphMessage>(m.MessageBytes);
                    Info("Control-flow message received: \n{msg}", MessageUtils.PrettyPrint(cfgm));
                    ControlFlowGraphMessages.Push(cfgm);
                    cfgma?.Invoke(cfgm);
                    break;

                case MessageType.SUMMARY:
                    var sm = MessageUtils.Deserialize<SummaryMessage>(m.MessageBytes);
                    Info("Summary message received: \n{msg}", MessageUtils.PrettyPrint(sm));
                    SummaryMessages.Push(sm);
                    sma?.Invoke(sm);
                    break;
            }
        }
        public static void Shutdown()
        {
            Info("Shutting down...");
            if (PipeServer is not null)
            {
                PipeServer.Dispose();
            }
            if (File.Exists(RunFile))
            {
                File.Delete(RunFile);
            }
            Environment.Exit(0);
        }

        public static Graph CreateGraph(ControlFlowGraphMessage m)
        {
            var graph = new Graph();
            graph.Kind = "cfg";
            foreach (var node in m.Nodes)
            {
                graph.AddNode(new Node(node.Id) { LabelText = node.Label, Kind = node.Kind });
                
            }
            foreach (var edge in m.Edges)
            {
                graph.FindNode(edge.SourceId).edgeSourceCount++;
                graph.FindNode(edge.TargetId).edgeTargetCount++;
                graph.AddEdge(edge.SourceId, edge.Label, edge.TargetId);   
            }
            return graph;
        }

        public static Graph CreateCallGraph(List<Dictionary<string, object>> methods, List<Dictionary<string, object>> invocations)
        {
            Graph graph = new Graph();
            graph.Kind = "cg";
            foreach(var m in methods)
            {
                var nid = (string)m["name"];
                if (graph.FindNode(nid) == null)
                {
                    var node = new Node(nid);
                    var s = (string)m["signature"];
                    node.LabelText = nid.Replace(",", ",\n");
                    if (nid.Contains("::.ctor"))
                    {
                        node.Attr.FillColor = Color.LightYellow;
                    }
                    else if (nid.Contains("::get_"))
                    {
                        node.Attr.FillColor = Color.LightGreen;
                    }
                    else if (nid.Contains("::set_"))
                    {
                        node.Attr.FillColor = Color.LightBlue;
                    }
                    else if (nid.Contains("IPersistentState::"))
                    {
                        node.Attr.FillColor = Color.Pink;
                    }
                    else if (nid.Contains("SmartContract::"))
                    {
                        node.Attr.FillColor = Color.Pink;
                    }
                    graph.AddNode(node);
                }
            }
            foreach(var i in invocations)
            {
                graph.FindNode((string)i["method"]).edgeSourceCount++;
                var x = graph.FindNode((string)i["name"]);
                if (x is not null) x.edgeTargetCount++;
                graph.AddEdge((string)i["method"], (string)i["name"]);
            }
            return graph;
        }
        #endregion

        #region Properties
        public static GuiApp? GuiApp { get; private set; }
        public static PipeServer<MessagePack>? PipeServer { get; private set; }

        public static DirectoryInfo assemblyCacheDir = new DirectoryInfo(Path.Combine(Runtime.StratisDevDir, "asmcache"));

        public static Stack<CompilationMessage> CompilationMessages { get; private set; } = new Stack<CompilationMessage>();
        public static Stack<SummaryMessage> SummaryMessages { get; private set; } = new Stack<SummaryMessage>();
        public static Stack<ControlFlowGraphMessage> ControlFlowGraphMessages { get; private set; } = new Stack<ControlFlowGraphMessage>();
        #endregion
    }
}
