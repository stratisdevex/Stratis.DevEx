using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
                    Nodes = graph.Nodes.Select(n => new NodeData() { Id = n.Id, Label = n.LabelText, Kind = n.Kind }).ToArray(),
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

        public static void SendSummaryGuiMessage(string cfgfile, Compilation c, string document, string summary, string[] classNames, 
            List<Dictionary<string, object>> implements, List<Dictionary<string, object>> invocations, List<Dictionary<string, object>> inherits,
            List<Dictionary<string, object>> classcreatedobjects, List<Dictionary<string, object>> methodcreatedobjects, PipeClient<MessagePack> pipeClient)
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
                    ClassNames = classNames,
                    Implements = implements,
                    Invocations = invocations,
                    Inherits = inherits,
                    ClassCreatedObjects = classcreatedobjects,
                    MethodCreatedObjects = methodcreatedobjects
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
            using (var op = Begin("Sending compilation message"))
            {
                try
                {
                    MemoryStream asm = new MemoryStream();
                    MemoryStream pdb = new MemoryStream();
                    
                    var r = c.Emit(asm, pdb, options: new Microsoft.CodeAnalysis.Emit.EmitOptions(
                        defaultSourceFileEncoding: Encoding.UTF8, 
                        fallbackSourceFileEncoding: Encoding.UTF8,
                        debugInformationFormat: Microsoft.CodeAnalysis.Emit.DebugInformationFormat.PortablePdb
                    ));
                    if (!r.Success)
                    {
                        foreach (var tree in c.SyntaxTrees)
                        {
                            var encoded = CSharpSyntaxTree.Create(tree.GetRoot() as CSharpSyntaxNode,
                                new CSharpParseOptions().WithLanguageVersion(LanguageVersion.CSharp8), tree.FilePath, Encoding.UTF8).GetRoot();
                            c = c.ReplaceSyntaxTree(tree, encoded.SyntaxTree);
                        }
                        r = c.Emit(asm, pdb, options: new Microsoft.CodeAnalysis.Emit.EmitOptions(
                            defaultSourceFileEncoding: Encoding.UTF8,
                            fallbackSourceFileEncoding: Encoding.UTF8,
                            debugInformationFormat: Microsoft.CodeAnalysis.Emit.DebugInformationFormat.PortablePdb
                        ));
                        if (!r.Success)
                        {
                            Error("Error emitting assembly: {err}", r.Diagnostics.Select(d => d.ToString()).JoinWithSpaces());
                            op.Abandon();
                            return;
                        }
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
