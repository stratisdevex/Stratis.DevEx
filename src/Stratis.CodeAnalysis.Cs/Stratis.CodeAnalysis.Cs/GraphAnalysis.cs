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
            var ss = projectDir.CombinePath(identifier + "_" + DateTime.Now.Millisecond + ".svg");
            Debug("Finished creating graph {ss}.", ss);
            try
            {
                Drawing.Draw(graph, ss, GraphFormat.SVG);
            }
            catch (Exception e)
            {
                Error(e, "Drawing error");
            }
            Debug("Wrote graph {ss}.", ss);
            //graph.AddNode()
            return;
        }
    }
}
