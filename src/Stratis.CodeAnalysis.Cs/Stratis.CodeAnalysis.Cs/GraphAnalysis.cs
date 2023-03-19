using System;
using System.Collections.Generic;
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
            Info("Analyzing control-flow of method: {ident} in class {c}...", identifier, classtype.ToDisplayString());
            var cfg = ControlFlowGraph.Create(methodBody);
            Debug("{ident} has {len} basic blocks.", identifier, cfg.Blocks.Length);
            return;
        }
    }
}
