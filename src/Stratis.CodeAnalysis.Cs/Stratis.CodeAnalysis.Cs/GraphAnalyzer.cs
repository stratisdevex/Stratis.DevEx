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
                                   .Concat(model.SyntaxTree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Cast<SyntaxNode>())
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
                    ConstructorDeclarationSyntax cds => new
                    {
                        Identifier = cds.Identifier.ToFullString() + "::" + "_ctor",
                        IsSmartContractMember = model.GetTypeInfo((ClassDeclarationSyntax)cds.Parent).Type.IsSmartContract(),
                        Type = "void",
                        ClassType = model.GetTypeInfo((ClassDeclarationSyntax)cds.Parent).Type
                    },
                    _ => throw new Exception()
                };

                Info("Analyzing control-flow of method: {ident}...", method.Identifier);
                var cfg = ControlFlowGraph.Create(m, model);
                Debug("{ident} has {len} basic block(s).", method.Identifier, cfg.Blocks.Where(bb => bb.Kind == BasicBlockKind.Block).Count());
                for (int j = 0; j < cfg.Blocks.Length; j++)
                {
                    var bb = cfg.Blocks[j];
                    if (bb.Kind == BasicBlockKind.Exit) continue;
                    var node = CreateNodeFromBasicBlock(bb, method.Identifier, method.Type, graph);
                    if (bb.ConditionalSuccessor is not null && bb.ConditionalSuccessor.Destination.Kind != BasicBlockKind.Exit)
                    {
                        var csnode = CreateNodeFromBasicBlock(bb.ConditionalSuccessor.Destination, method.Identifier, method.Type, graph);
                        if (bb.ConditionalSuccessor.Source != bb)
                        {
                            var cssnode = CreateNodeFromBasicBlock(bb.ConditionalSuccessor.Source, method.Identifier, method.Type, graph);
                            graph.AddEdge(node.Id, cssnode.Id);
                            graph.AddEdge(cssnode.Id, csnode.Id);
                        }
                        else
                        {
                            graph.AddEdge(node.Id, csnode.Id);
                        }
                    }

                    if (bb.FallThroughSuccessor is not null && bb.FallThroughSuccessor.Destination.Kind != BasicBlockKind.Exit)
                    {
                        var ftnid = method.Identifier + "::" + method.Type + "_" + bb.FallThroughSuccessor.Destination.Ordinal.ToString();
                        var ftnode = graph.FindNode(ftnid);
                        if (ftnode is null)
                        {
                            ftnode = new Node(ftnid);
                            ftnode.LabelText = bb.FallThroughSuccessor.Destination.Operations.Select(o => o?.Syntax.ToString() ?? "").JoinWith(Environment.NewLine) + Environment.NewLine + (bb.FallThroughSuccessor.Destination.BranchValue?.Syntax.ToString() ?? "");
                            ftnode.Kind = bb.FallThroughSuccessor.Destination.Kind == BasicBlockKind.Entry ? "entry" : bb.FallThroughSuccessor.Destination.ConditionalSuccessor is not null ? "branch" : "block";
                            graph.AddNode(ftnode);
                        }
                        
                        if (bb.FallThroughSuccessor.Source != bb)
                        {
                            var ftsnode = CreateNodeFromBasicBlock(bb.FallThroughSuccessor.Source, method.Identifier, method.Type, graph);
                            graph.AddEdge(node.Id, ftsnode.Id);
                            graph.AddEdge(ftsnode.Id, ftnid);
                        }
                        else
                        {
                            graph.AddEdge(node.Id, ftnid);
                        }
                    }

                    /*
                    for (int k = 0; k < bb.Predecessors.Length; k++)
                    {
                        var pb = bb.Predecessors[k];
                        var pid = method.Identifier + "::" + method.Type + "_" + pb.Source.Ordinal.ToString();
                        var pred = graph.FindNode(pid);
                        graph.AddEdge(pred.Id, node.Id);

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

        protected static Node CreateNodeFromBasicBlock(BasicBlock bb, string methodIdentifier, string methodType, Graph graph)
        {
            var nid = methodIdentifier + "::" + methodType + "_" + bb.Ordinal.ToString();
            var node = graph.FindNode(nid);
            if (node is null)
            {
                node = new Node(nid);
                var src = bb.Operations.Select(o => o?.Syntax.ToString() ?? "").JoinWith(Environment.NewLine) + Environment.NewLine + (bb.BranchValue?.Syntax.ToString() ?? "");
                node.LabelText =
                    bb.Kind == BasicBlockKind.Entry ? methodIdentifier + "::" + methodType + Environment.NewLine + src : src;
                node.Kind = bb.Kind == BasicBlockKind.Entry ? "entry" : bb.BranchValue is not null && bb.ConditionalSuccessor is not null  ? "branch" : "block";
                graph.AddNode(node);
            }
            return node;
        }
    }
}
