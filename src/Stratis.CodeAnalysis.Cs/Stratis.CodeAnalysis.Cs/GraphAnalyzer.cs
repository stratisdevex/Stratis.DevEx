using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.FlowAnalysis;

using Microsoft.Msagl.Drawing;

using SharpConfig;

using Stratis.DevEx;
using Stratis.DevEx.Drawing;
using Stratis.DevEx.Pipes;

namespace Stratis.CodeAnalysis.Cs
{
    public class GraphAnalyzer : Runtime
    {
        public static void AnalyzeControlFlow(string cfgFile, Configuration config, SemanticModel model)
        {
            using var top = Begin("Analyzing control-flow of source document {doc} using configuration {cfg}.", model.SyntaxTree.FilePath, config["General"]["ConfigFile"].StringValue);
            var projectDir = Path.GetDirectoryName(config["General"]["ConfigFile"].StringValue);
            var graph = new Graph();
            SyntaxNode[] methods = model.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                                   .Cast<SyntaxNode>()
                                   .Concat(model.SyntaxTree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Cast<SyntaxNode>())
                                   .ToArray();
            for (int i = 0; i < methods.Length; i++)
            {
                var m = methods[i];
                var method = m switch
                {
                    MethodDeclarationSyntax mds => new
                    {
                        Identifier = mds.GetDeclaringType(model).ToDisplayString() + "::" + mds.Identifier.Text,
                        IsSmartContractMember = mds.IsSmartContractMethod(model),
                        Type = model.GetSymbolInfo(mds.ReturnType).Symbol.ToDisplayString(),
                        ClassType = mds.GetDeclaringType(model)
                    },
                    AccessorDeclarationSyntax ads => new
                    {
                        Identifier = ads.GetDeclaringType(model).ToDisplayString() + "::" + ads.Parent.Parent.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)).Text + ((ads.Kind() == SyntaxKind.SetAccessorDeclaration) ? "_Set" : "_Get"),
                        IsSmartContractMember = ads.IsSmartContractProperty(model),
                        Type = ((PropertyDeclarationSyntax)ads.Parent.Parent).Type.ToString(),
                        ClassType = ads.GetDeclaringType(model)
                    },
                    _ => throw new Exception()
                };

                Info("Analyzing control-flow of method: {ident}...", method.Identifier);
                /*
                var op = model.GetOperation(m);
                if (op is null || op is not IMethodBodyOperation)
                {
                    Error("Could not get semantic model operation for method {ident}.", method.Identifier);
                    continue;
                }
                
                var mbop = op;
                */
                var cfg = ControlFlowGraph.Create(m, model);
                Debug("{ident} has {len} basic block(s).", method.Identifier, cfg.Blocks.Where(bb => bb.Kind == BasicBlockKind.Block).Count());
                for (int j = 0; j < cfg.Blocks.Length; j++)
                {
                    var bb = cfg.Blocks[j];
                    if (bb.Kind == BasicBlockKind.Exit) continue;
                    var nid = method.Identifier + "::" + method.Type + "_" + bb.Ordinal.ToString();
                    var node = graph.FindNode(nid);
                    if (node is null)
                    {
                        node = new Node(nid);
                        var src = bb.Operations.Select(o => o?.Syntax.ToString() ?? "").JoinWith(Environment.NewLine);
                        node.LabelText =
                            bb.Kind == BasicBlockKind.Entry ? method.Identifier + "::" + method.Type + Environment.NewLine + src : src;
                        graph.AddNode(node);
                    }
                    for (int k = 0; k < bb.Predecessors.Length; k++)
                    {
                        var pb = bb.Predecessors[k];
                        var pid = method.Identifier + "::" + method.Type + "_" + pb.Source.Ordinal.ToString();
                        var pred = graph.FindNode(pid);
                        graph.AddEdge(pred.Id, node.Id);
                        
                    }
                    if (bb.ConditionalSuccessor is not null)
                    {
                        nid = method.Identifier + "::" + method.Type + "_" + bb.ConditionalSuccessor.Destination.Ordinal.ToString();
                        var succ = graph.FindNode(nid);
                        if (succ is null)
                        {
                            succ = new Node(nid);
                            var src = bb.ConditionalSuccessor.Destination.Operations.Select(o => o?.Syntax.ToString() ?? "").JoinWith(Environment.NewLine);
                            succ.LabelText =
                                bb.ConditionalSuccessor.Destination.Kind == BasicBlockKind.Entry ? method.Identifier + "::" + method.Type + Environment.NewLine + src : src;
                            graph.AddNode(succ);
                        }
                        graph.AddEdge(node.Id, succ.Id);
                    }

                    /*
                    if (bb.FallThroughSuccessor is not null && bb.FallThroughSuccessor.Destination != bb.ConditionalSuccessor.Destination)
                    {
                        nid = method.Identifier + "::" + method.Type + "_" + bb.FallThroughSuccessor.Destination.Ordinal.ToString();
                        var succ = graph.FindNode(nid);
                        if (succ is null)
                        {
                            succ = new Node(nid);
                            var src = bb.FallThroughSuccessor.Destination.Operations.Select(o => o?.Syntax.ToString() ?? "").JoinWith(Environment.NewLine);
                            succ.LabelText =
                                bb.FallThroughSuccessor.Destination.Kind == BasicBlockKind.Entry ? method.Identifier + "::" + method.Type + Environment.NewLine + src : src;
                            graph.AddNode(succ);
                        }
                        graph.AddEdge(node.Id, succ.Id);
                    }
                    */
                }
            }
            top.Complete();
            Debug("Control-flow graph of source document {doc} has {n} nodes, {e} edges.", model.SyntaxTree.FilePath, graph.NodeCount, graph.EdgeCount);
            if (config["Gui"]["Enabled"].BoolValue)
            {
                var pipeClient = Gui.CreatePipeClient();
                Gui.SendGuiMessage(cfgFile, model.Compilation, model.SyntaxTree.FilePath, graph, pipeClient);
                pipeClient.Dispose();
                //File.WriteAllText(projectDir.CombinePath(DateTime.Now.Millisecond.ToString() + ".html"), Html.DrawControlFlowGraph(graph));
            }
        }
    }
}
