using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Msagl.Drawing;

using SharpConfig;

using Stratis.DevEx;
using Stratis.DevEx.Pipes;

namespace Stratis.CodeAnalysis.Cs
{
    public class Gui : Runtime
    {
        public static bool GuiProcessRunning()
        {
            var f = Runtime.StratisDevDir.CombinePath("Stratis.DevEx.Gui.run");
            if (!File.Exists(f))
            {
                Runtime.Debug("{0} does not exist.", f);
                return false;
            }
            else
            {
                var c = Configuration.LoadFromFile(f);
                var pid = c["Process"]["ProcessId"].GetValueOrDefault(0);
                if (pid == 0)
                {
                    Runtime.Error("Could not read process ID from Stratis.DevEx.Gui.run.");
                    return false;
                }
                else
                {
                    try
                    {
                        var p = Process.GetProcessById(pid);
                        if (p.ProcessName.Contains("Stratis.DevEx.Gui"))
                        {
                            return true;
                        }
                        else
                        {
                            Runtime.Debug("Process {pid} is not Stratis.DevEx.Gui.", pid);
                            return false;
                        }
                    }
                    catch
                    {
                        Runtime.Debug("Exception thrown getting process id {pid}.", pid);
                        return false;
                    }
                }
            }
        }

        public static PipeClient<MessagePack> CreatePipeClient()
        {
            using (var op = Begin("Creating GUI pipe client"))
            {
                try
                {
                    var pipeClient = new PipeClient<MessagePack>("stratis_devexgui") { AutoReconnect = false };
                    op.Complete();
                    return pipeClient;
                }
                catch (Exception e)
                {
                    op.Abandon();
                    Error(e, "Error creating GUI pipe client.");
                    return null;
                }
            }
        }
        
        public static void SendGuiMessage(Compilation c, PipeClient<MessagePack> pipeClient)
        {
            if (!GuiProcessRunning())
            {
                Runtime.Error("Did not detect GUI process running, not sending message.");
                return;
            }

            using (var op = Runtime.Begin("Sending compilation message"))
            {
                try
                {
                    var m = new CompilationMessage()
                    {
                        CompilationId = c.GetHashCode(),
                        EditorEntryAssembly = Runtime.EntryAssembly?.FullName ?? "(none)",
                        AssemblyName = c.AssemblyName,
                        Documents = c.SyntaxTrees.Select(st => st.FilePath).ToArray()
                    };
                    if (GuiProcessRunning() && !pipeClient.IsConnected)
                    {
                        Runtime.Debug("Pipe client disconnected, attempting to reconnect...");
                        pipeClient.ConnectAsync().Wait();
                    }
                    if (GuiProcessRunning() && pipeClient.IsConnected)
                    {
                        var mp = MessageUtils.Pack(m);
                        pipeClient.WriteAsync(mp).Wait();
                        op.Complete();
                    }
                    else
                    {
                        op.Abandon();
                        Runtime.Error("GUI is not running or pipe client disconnected. Error sending compilation message to GUI.");
                    }
                }
                catch (Exception e)
                {
                    op.Abandon();
                    Runtime.Error(e, "Error sending compilation message to GUI.");
                }
            }
        }

        public static void SendGuiMessage(string cfgfile, Compilation c, string document, Graph graph, PipeClient<MessagePack> pipeClient)
        {
            if (!GuiProcessRunning())
            {
                Error("Did not detect GUI process running, not sending message.");
                return;
            }

            using var op = Begin("Sending control-flow graph message");
            try
            {
                var m = new ControlFlowGraphMessage()
                {
                    ConfigFile = cfgfile,
                    CompilationId = c.GetHashCode(),
                    EditorEntryAssembly = EntryAssembly?.FullName ?? "(none)",
                    AssemblyName = c.AssemblyName,
                    Document = document,
                    Nodes = graph.Nodes.Select(n => new NodeData() { Id = n.Id, Label = n.LabelText }).ToArray(),
                    Edges = graph.Edges.Select(e => new EdgeData() { SourceId = e.Source, TargetId = e.Target, Label = e.LabelText}).ToArray()
                };
                if (GuiProcessRunning() && !pipeClient.IsConnected)
                {
                    Debug("Pipe client disconnected, attempting to reconnect...");
                    pipeClient.ConnectAsync().Wait();
                }
                if (GuiProcessRunning() && pipeClient.IsConnected)
                {
                    var mp = MessageUtils.Pack(m);
                    pipeClient.WriteAsync(mp).Wait();
                    op.Complete();
                }
                else
                {
                    op.Abandon();
                    Error("GUI is not running or pipe client disconnected. Error sending control-flow graph message to GUI.");
                }
            }
            catch (Exception e)
            {
                op.Abandon();
                Error(e, "Error sending control-flow graph message to GUI.");
            }
            
        }

        public static void SendSummaryGuiMessage(string cfgfile, Compilation c, string document, string summary, PipeClient<MessagePack> pipeClient)
        {
            if (!GuiProcessRunning())
            {
                Error("Did not detect GUI process running, not sending message.");
                return;
            }

            using var op = Begin("Sending summary message");
            try
            {
                var m = new SummaryMessage()
                {
                    ConfigFile = cfgfile,
                    CompilationId = c.GetHashCode(),
                    EditorEntryAssembly = EntryAssembly?.FullName ?? "(none)",
                    AssemblyName = c.AssemblyName,
                    Document = document,
                    Summary = summary,
                };
                if (GuiProcessRunning() && !pipeClient.IsConnected)
                {
                    Debug("Pipe client disconnected, attempting to reconnect...");
                    pipeClient.ConnectAsync().Wait();
                }
                if (GuiProcessRunning() && pipeClient.IsConnected)
                {
                    var mp = MessageUtils.Pack(m);
                    pipeClient.WriteAsync(mp).Wait();
                    op.Complete();
                }
                else
                {
                    op.Abandon();
                    Error("GUI is not running or pipe client disconnected. Error sending control-flow graph message to GUI.");
                }
            }
            catch (Exception e)
            {
                op.Abandon();
                Error(e, "Error sending control-flow graph message to GUI.");
            }
        }

        public static void SendCallGraphGuiMessage(string cfgfile, Compilation c, string document, Graph graph, PipeClient<MessagePack> pipeClient)
        {
            if (!GuiProcessRunning())
            {
                Error("Did not detect GUI process running, not sending message.");
                return;
            }

            using var op = Begin("Sending call graph message");
            try
            {
                var m = new CallGraphMessage()
                {
                    ConfigFile = cfgfile,
                    CompilationId = c.GetHashCode(),
                    EditorEntryAssembly = EntryAssembly?.FullName ?? "(none)",
                    AssemblyName = c.AssemblyName,
                    Document = document,
                    Nodes = graph.Nodes.Select(n => new NodeData() { Id = n.Id, Label = n.LabelText }).ToArray(),
                    Edges = graph.Edges.Select(e => new EdgeData() { SourceId = e.Source, TargetId = e.Target, Label = e.LabelText }).ToArray()
                };
                if (GuiProcessRunning() && !pipeClient.IsConnected)
                {
                    Debug("Pipe client disconnected, attempting to reconnect...");
                    pipeClient.ConnectAsync().Wait();
                }
                if (GuiProcessRunning() && pipeClient.IsConnected)
                {
                    var mp = MessageUtils.Pack(m);
                    pipeClient.WriteAsync(mp).Wait();
                    op.Complete();
                }
                else
                {
                    op.Abandon();
                    Error("GUI is not running or pipe client disconnected. Error sending control-flow graph message to GUI.");
                }
            }
            catch (Exception e)
            {
                op.Abandon();
                Error(e, "Error sending call graph message to GUI.");
            }

        }

        public static void SendCompilationMessage(Compilation c, PipeClient<MessagePack> pipeClient)
        {
            if (!GuiProcessRunning())
            {
                Error("Did not detect GUI process running, not sending message.");
                return;
            }
            using (var op = Runtime.Begin("Sending compilation message"))
            {
                try
                {
                    MemoryStream asm = new MemoryStream();
                    MemoryStream pdb = new MemoryStream();
                    var r = c.Emit(asm, pdb);
                    if (!r.Success)
                    {
                        Error("Error emitting assembly: {err}", r.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Descriptor.Description));
                        op.Abandon();
                        return;
                    }
                    var m = new CompilationMessage()
                    {
                        CompilationId = c.GetHashCode(),
                        EditorEntryAssembly = Runtime.EntryAssembly?.FullName ?? "(none)",
                        AssemblyName = c.AssemblyName,
                        Documents = c.SyntaxTrees.Select(st => st.FilePath).ToArray(),
                        Assembly = asm.ToArray(),
                        Pdb = pdb.ToArray()
                    };
                    if (GuiProcessRunning() && !pipeClient.IsConnected)
                    {
                        Debug("Pipe client disconnected, attempting to reconnect...");
                        pipeClient.ConnectAsync().Wait();
                    }
                    if (GuiProcessRunning() && pipeClient.IsConnected)
                    {
                        var mp = MessageUtils.Pack(m);
                        pipeClient.WriteAsync(mp).Wait();
                        op.Complete();
                    }
                    else
                    {
                        op.Abandon();
                        Error("GUI is not running or pipe client disconnected. Error sending compilation message to GUI.");
                    }
                }
                catch (Exception e)
                {
                    op.Abandon();
                    Error(e, "Error sending compilation message to GUI.");
                }
            }
        }
    }
}
