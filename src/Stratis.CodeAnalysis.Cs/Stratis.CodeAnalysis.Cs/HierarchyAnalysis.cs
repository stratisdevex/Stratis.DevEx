using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.FlowAnalysis;

using SharpConfig;

using Stratis.DevEx;
namespace Stratis.CodeAnalysis.Cs
{
    public class HierarchyAnalysis : Runtime
    {
        public static void AnalyzeControlFlow(string cfgFile, Configuration config, SemanticModel model)
        {
            using var top = Begin("Analyzing class hierarchy of source document {doc} using configuration {cfg}.", model.SyntaxTree.FilePath, config["General"]["ConfigFile"].StringValue);
            var projectDir = Path.GetDirectoryName(config["General"]["ConfigFile"].StringValue);
            //var graph = new Graph();
            ClassDeclarationSyntax[] classdecls = model.SyntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                                   .ToArray();
            foreach(var c in classdecls)
            {
                var t = model.GetDeclaredSymbol(c);
                //t.
            }
        }
    }
}
