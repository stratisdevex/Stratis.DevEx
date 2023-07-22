﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

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
    public class SummaryAnalysis : Runtime
    {
        public static void Analyze(string cfgFile, Configuration config, SemanticModel model)
        {
            using var top = Begin("Analyzing class hierarchy of source document {doc} using configuration {cfg}.", model.SyntaxTree.FilePath, config["General"]["ConfigFile"].StringValue);
            var projectDir = Path.GetDirectoryName(config["General"]["ConfigFile"].StringValue);
            ClassDeclarationSyntax[] classdecls = model.SyntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                                   .ToArray();
            StructDeclarationSyntax[] structdecls = model.SyntaxTree.GetRoot().DescendantNodes().OfType<StructDeclarationSyntax>()
                                   .ToArray();
            var builder = new StringBuilder();
            var classNames = new List<string>();
            builder.AppendLine("classDiagram");
            foreach (var c in classdecls)
            {
                var t = model.GetDeclaredSymbol(c);
                var className = t.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                builder.AppendLineFormat("class {0}", className);
                classNames.Add(className);
                Debug("{cls}", builder.Last());
                if (t.IsSmartContract()) builder.AppendLineFormat("<<contract>> {0}".Replace("<", "&lt;").Replace(">", "&gt;"), className);
                foreach (var method in t.GetMembers().Where(m => m.Kind == SymbolKind.Method && m.DeclaredAccessibility == Accessibility.Public).Cast<IMethodSymbol>())
                {
                    builder.AppendFormat("{0} : ", className);
                    builder.AppendFormat("+");
                    builder.AppendFormat(method.Name);
                    builder.Append("(");

                    foreach (var p in method.Parameters)
                    {
                        builder.AppendFormat("{0} {1}", p.Type.Name.Replace("<", "~").Replace(">", "~"), p.Name);
                        builder.Append(",");
                    }
                    if (method.Parameters.Count() > 0)
                    {
                        builder.Remove(builder.Length - 1, 1);
                    }
                    builder.Append(")");
                    builder.AppendFormat(Environment.NewLine);
                    Debug("{m}", builder.Last());
                }

                foreach (var method in t.GetMembers().Where(m => m.Kind == SymbolKind.Method && m.DeclaredAccessibility != Accessibility.Public).Cast<IMethodSymbol>())
                {
                    builder.AppendFormat("{0} : ", className);
                    switch (method.DeclaredAccessibility)
                    {
                        case Accessibility.Public:
                            builder.AppendFormat("+");
                            break;
                        case Accessibility.Private:
                            builder.AppendFormat("-");
                            break;
                        case Accessibility.ProtectedAndInternal:
                            builder.AppendFormat("#");
                            break;
                        //case Microsoft.Cci.TypeMemberVisibility.Assembly:
                        //    builder.AppendFormat("~");
                        //    break;
                        default:
                            builder.AppendFormat("-");
                            break;
                    }


                    builder.AppendFormat(method.Name);
                    builder.Append("(");
                    foreach (var p in method.Parameters)
                    {
                        builder.AppendFormat("{0} {1}", p.Type.Name, p.Name);
                        builder.Append(",");
                    }
                    if (method.Parameters.Count() > 0)
                    {
                        builder.Remove(builder.Length - 1, 1);
                    }
                    builder.Append(")");
                    builder.AppendFormat(Environment.NewLine);
                    Debug("{m}", builder.Last());
                }
            }

            foreach (var c in structdecls)
            {
                var t = model.GetDeclaredSymbol(c);
                
                var className = t.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                builder.AppendLineFormat("class {0}", className);
                Debug("{cls}", builder.Last());
                builder.AppendLineFormat("<<struct>> {0}".Replace("<", "&lt;").Replace(">", "&gt;"), className);
                foreach (var f in t.GetMembers().Where(m => m.Kind == SymbolKind.Field && m.DeclaredAccessibility == Accessibility.Public).Cast<IFieldSymbol>())
                {
                    builder.AppendFormat("{0} : ", className);
                    builder.AppendFormat("+{0} {1}", f.Type.Name, f.Name);
                    //builder.AppendFormat(f.Name);
                    builder.AppendFormat(Environment.NewLine);
                    Debug("{m}", builder.Last());
                }
                if (t.ContainingType != null)
                {
                    builder.AppendFormat("{0}*--{1}", className, t.ContainingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                    builder.AppendLine();
                }
            }
            top.Complete();
            var pipeClient = Gui.CreatePipeClient();
            Gui.SendSummaryGuiMessage(cfgFile, model.Compilation, model.SyntaxTree.FilePath, builder.ToString(), classNames.ToArray(), pipeClient);
            pipeClient.Dispose();
            //File.WriteAllText(projectDir.CombinePath(DateTime.Now.Millisecond.ToString() + ".html"), Stratis.DevEx.Drawing.Html.DrawSummary(builder.ToString()));
        }
    }
}
