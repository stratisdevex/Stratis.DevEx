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

namespace Stratis.CodeAnalysis.Cs
{
    public class GraphAnalysis : Runtime
    {
        public static void Analyze(Configuration config, Compilation compilation, IMethodBodyOperation methodBody)
        {
            string identifier = methodBody.Syntax switch
            {
                MethodDeclarationSyntax mds => mds.Identifier.Text,
                AccessorDeclarationSyntax ads => ads.Parent.Parent.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)).Text + ((ads.Kind() == SyntaxKind.SetAccessorDeclaration) ? "_Set" : "_Get"),
                _ => ""
            };
            if (identifier.IsEmpty())
            {
                Error("Unknown method-body syntax: {kind}. Aborting control-flow analysis.", methodBody.Syntax.Kind());
                return;
            }

            var classtype = methodBody.Syntax switch
            {
                MethodDeclarationSyntax mds => mds.GetDeclaringType(methodBody.SemanticModel),
                AccessorDeclarationSyntax ads => ads.GetDeclaringType(methodBody.SemanticModel),
                _ => throw new Exception("Unknown syntax node kind")
            };
            Info("Analyzing control-flow of method: {ident} in class {c} using config {config}...", identifier, classtype.ToDisplayString(), config["General"]["ConfigFile"].StringValue);
            var projectDir = Path.GetDirectoryName(config["General"]["ConfigFile"].StringValue);
            var cfg = ControlFlowGraph.Create(methodBody);
            Debug("{ident} has {len} basic blocks.", identifier, cfg.Blocks.Length);
            var graph = new Graph();
            Debug("Created graph...");
            
            for(int i = 0; i < cfg.Blocks.Length; i++)
            {
                var bb = cfg.Blocks[i];
                if (cfg.Blocks[i].Kind == BasicBlockKind.Exit) continue;
                if (graph.FindNode(bb.Ordinal.ToString()) == null)
                {
                    var node = new Node(bb.Ordinal.ToString());
                    node.LabelText = bb.Operations.Select(o => o?.ToString() ?? "").JoinWith(Environment.NewLine) ?? "";
                    graph.AddNode(node);
                }
            }
            Debug("Finished creating graph.");
            //var ss = projectDir.CombinePath(identifier + "_" + DateTime.Now.Millisecond + ".svg");
            //Debug("Finished creating graph {ss}.", ss);
            //try
            //{
            //    Drawing.Draw(graph, ss, GraphFormat.SVG);
            //}
            //catch (Exception e)
            //{
            //    Error(e, "Drawing error");
            //}
            //Debug("Wrote graph {ss}.", ss);
            //graph.AddNode()
            return;
        }

        public static void AnalyzeControlFlow(Configuration config, SemanticModel model)
        {
            using var top = Begin("Analyzing control-flow of source document {doc} using configuration {cfg}.", model.SyntaxTree.FilePath, config["General"]["ConfigFile"].StringValue);
            var projectDir = Path.GetDirectoryName(config["General"]["ConfigFile"].StringValue);
            var graph = new Graph();
            SyntaxNode[] methods = model.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                                   .Cast<SyntaxNode>()
                                   .Concat(model.SyntaxTree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Cast<SyntaxNode>())
                                   .ToArray();
            for (int i = 0;  i < methods.Length; i++)
            {
                var m = methods[i];
                var method = m switch
                {
                    MethodDeclarationSyntax mds => new
                    {
                        Identifier = mds.Identifier.Text,
                        IsSmartContractMember = mds.IsSmartContractMethod(model),
                        Type = model.GetSymbolInfo(mds.ReturnType).Symbol.ToDisplayString(),
                        ClassType = mds.GetDeclaringType(model)
                    },
                    AccessorDeclarationSyntax ads => new
                    {
                        Identifier = ads.Parent.Parent.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)).Text + ((ads.Kind() == SyntaxKind.SetAccessorDeclaration) ? "_Set" : "_Get"),
                        IsSmartContractMember = ads.IsSmartContractProperty(model),
                        Type = ((PropertyDeclarationSyntax)ads.Parent.Parent).Type.ToString(),
                        ClassType = ads.GetDeclaringType(model)
                    },
                    _ => throw new Exception()
                };
               
                Info("Analyzing control-flow of method: {ident} in class {c}...", method.Identifier, method.ClassType.ToDisplayString());
                var op = model.GetOperation(m);
                if (op is null || op is not IMethodBodyOperation)
                {
                    Error("Could not get semantic model operation for method {ident}.", method.Identifier);
                    continue;
                }
                var mbop = (IMethodBodyOperation)op;
                var cfg = ControlFlowGraph.Create(mbop);
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
                        node.LabelText = 
                            bb.Kind == BasicBlockKind.Entry ? method.Identifier + "::" + method.Type : bb.Operations.Select(o => o?.Syntax.ToString() ?? "").JoinWith(Environment.NewLine) ?? "";
                        graph.AddNode(node);
                    }
                    for (int k = 0; k < bb.Predecessors.Length; k++)
                    {
                        var pb = bb.Predecessors[k];
                        var pid  = method.Identifier + "::" + method.Type + "_" + pb.Source.Ordinal.ToString();
                        var pred = graph.FindNode(pid);
                        graph.AddEdge(pred.Id, node.Id);
                    }
                }
            }
            top.Complete();
            Debug("Control-flow graph of source document {doc} has {n} nodes, {e} edges.", model.SyntaxTree.FilePath, graph.NodeCount, graph.EdgeCount);
            var ss = projectDir.CombinePath(DateTime.Now.Millisecond + ".dgml");
            Debug("Finished creating graph {ss}.", ss);
            try
            {
                Drawing.Draw(graph, ss, GraphFormat.DGML);
            }
            catch (Exception e)
            {
                Error(e, "Drawing error");
            }
            Debug("Wrote graph {ss}.", ss);
        }
    }
}
