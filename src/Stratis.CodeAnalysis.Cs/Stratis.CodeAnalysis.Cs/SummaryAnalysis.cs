using System;
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
            
            var implementsList = new List<Dictionary<string, object>>(); //Method Implementations
            var invocationList = new List<Dictionary<string, object>>(); //Method Invocations
            var inheritsList = new List<Dictionary<string, object>>();//Class Heirarchy
            var classCreatedObjects = new List<Dictionary<string, object>>(); //Objects created by classes
            var methodCreatedObjects = new List<Dictionary<string, object>>();//Objects created by methods

            var builder = new StringBuilder();
            var classNames = new List<string>();
            builder.AppendLine("classDiagram");
            foreach (var c in classdecls)
            {
                var classSymbol = model.GetDeclaredSymbol(c);
                var classPath = classSymbol.Name;
                if (classSymbol.ContainingNamespace is not null && !string.IsNullOrEmpty(classSymbol.ContainingNamespace.Name))
                    classPath = classSymbol.ContainingNamespace.Name + '.' + classSymbol.Name;

                var classinfo = new Dictionary<string, object>();
                classinfo["name"] = classPath;
                classinfo["location"] = c.GetLocation().ToString();

                /*
                 * If this Class is a Subclass, Collet Inheritance Info
                 */
                if (c.BaseList != null)
                {
                    foreach (SimpleBaseTypeSyntax typ in c.BaseList.Types)
                    {
                        var symInfo = model.GetTypeInfo(typ.Type);

                        var baseClassPath = symInfo.Type.Name;
                        if (symInfo.Type.ContainingNamespace != null && !string.IsNullOrEmpty(symInfo.Type.ContainingNamespace.Name))
                            baseClassPath = symInfo.Type.ContainingNamespace.Name + '.' + symInfo.Type.Name;

                        var inheritInfo = new Dictionary<string, object>();
                        inheritInfo["class"] = classPath;
                        inheritInfo["base"] = baseClassPath;

                        inheritsList.Add(inheritInfo);
                    }
                }
                /*
                var methods = c.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var method in methods)
                {
                    var symbol = model.GetDeclaredSymbol(method);

                    //Collect Method Information
                    var methoddata = new Dictionary<string, object>();
                    methoddata["name"] = symbol.MetadataName;
                    if (symbol.ContainingNamespace != null && !string.IsNullOrEmpty(symbol.ContainingNamespace.Name))
                        methoddata["name"] = symbol.ContainingNamespace.Name + "." + symbol.MetadataName;
                    methoddata["location"] = c.GetLocation().ToString();
                    methoddata["class"] = classinfo["name"];

                    implementsList.Add(methoddata);

                    var invocations = method.SyntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>();

                    //For each invocation within our method, collect information
                    foreach (var invocation in invocations)
                    {
                        var invokedSymbol = model.GetSymbolInfo(invocation).Symbol;

                        if (invokedSymbol == null)
                            continue;

                        var invocationInfo = new Dictionary<string, object>();
                        invocationInfo["name"] = invokedSymbol.MetadataName;
                        if (symbol.ContainingNamespace != null && !string.IsNullOrEmpty(symbol.ContainingNamespace.Name))
                            invocationInfo["name"] = invokedSymbol.ContainingNamespace.Name + "." + invokedSymbol.MetadataName;
                        if (invokedSymbol.Locations.Length == 1)
                            invocationInfo["location"] = invocation.GetLocation().ToString();
                        invocationInfo["method"] = methoddata["name"];

                        invocationList.Add(invocationInfo);
                    }

                    //For each object creation within our method, collect information
                    var methodCreates = method.SyntaxTree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
                    foreach (var creation in methodCreates)
                    {
                        var typeInfo = model.GetTypeInfo(creation);
                        var createInfo = new Dictionary<string, object>();

                        var typeName = typeInfo.Type.Name;
                        if (typeInfo.Type.ContainingNamespace != null && !string.IsNullOrEmpty(typeInfo.Type.ContainingNamespace.Name))
                            typeName = typeInfo.Type.ContainingNamespace.Name + "." + typeInfo.Type.Name;

                        createInfo["method"] = methoddata["name"];
                        createInfo["creates"] = typeName;
                        createInfo["location"] = creation.GetLocation().ToString();

                        methodCreatedObjects.Add(createInfo);
                    }
                }
                */
                var t = model.GetDeclaredSymbol(c);
                var className = t.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                builder.AppendLineFormat("class {0}", className);
                classNames.Add(className);
                Debug("{cls}", builder.Last());
                if (t.IsSmartContract()) builder.AppendLineFormat("<<contract>> {0}".Replace("<", "&lt;").Replace(">", "&gt;"), className);
                
                foreach (var method in t.GetMembers().Where(m => m.Kind == SymbolKind.Method && m.DeclaredAccessibility == Accessibility.Public).Cast<IMethodSymbol>())
                {
                    // Collect Method Information
                    var methoddata = new Dictionary<string, object>();
                    methoddata["name"] = method.MetadataName;
                    if (method.ContainingNamespace != null && !string.IsNullOrEmpty(method.ContainingNamespace.Name))
                        methoddata["name"] = method.ContainingNamespace.Name + "." + method.MetadataName;
                    methoddata["location"] = c.GetLocation().ToString();
                    methoddata["class"] = classinfo["name"];
                    var invocations = method.DeclaringSyntaxReferences.First().GetSyntax().DescendantNodes().OfType<InvocationExpressionSyntax>();
                    //For each invocation within our method, collect information
                    foreach (var invocation in invocations)
                    {
                        var invokedSymbol = model.GetSymbolInfo(invocation).Symbol;

                        if (invokedSymbol == null)
                            continue;

                        var invocationInfo = new Dictionary<string, object>();
                        invocationInfo["name"] = invokedSymbol.MetadataName;
                        if (method.ContainingNamespace != null && !string.IsNullOrEmpty(method.ContainingNamespace.Name))
                            invocationInfo["name"] = invokedSymbol.ContainingNamespace.Name + "." + invokedSymbol.MetadataName;
                        if (invokedSymbol.Locations.Length == 1)
                            invocationInfo["location"] = invocation.GetLocation().ToString();
                        invocationInfo["method"] = methoddata["name"];

                        invocationList.Add(invocationInfo);
                    }

                    //For each object creation within our method, collect information
                    var methodCreates = method.DeclaringSyntaxReferences.First().GetSyntax().DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
                    foreach (var creation in methodCreates)
                    {
                        var typeInfo = model.GetTypeInfo(creation);
                        var createInfo = new Dictionary<string, object>();

                        var typeName = typeInfo.Type.Name;
                        if (typeInfo.Type.ContainingNamespace != null && !string.IsNullOrEmpty(typeInfo.Type.ContainingNamespace.Name))
                            typeName = typeInfo.Type.ContainingNamespace.Name + "." + typeInfo.Type.Name;

                        createInfo["method"] = methoddata["name"];
                        createInfo["creates"] = typeName;
                        createInfo["location"] = creation.GetLocation().ToString();

                        methodCreatedObjects.Add(createInfo);
                    }


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

                //For each object created within the class, collect information
                var creates = c.SyntaxTree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
                foreach (var creation in creates)
                {
                    var typeInfo = model.GetTypeInfo(creation);
                    var createInfo = new Dictionary<string, object>();

                    var typeName = typeInfo.Type.Name;
                    if (typeInfo.Type.ContainingNamespace != null && !string.IsNullOrEmpty(typeInfo.Type.ContainingNamespace.Name))
                        typeName = typeInfo.Type.ContainingNamespace.Name + "." + typeInfo.Type.Name;

                    createInfo["class"] = classPath;
                    createInfo["creates"] = typeName;
                    createInfo["location"] = creation.GetLocation().ToString();
                    classCreatedObjects.Add(createInfo);
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
            Gui.SendSummaryGuiMessage(cfgFile, model.Compilation, model.SyntaxTree.FilePath, builder.ToString(), classNames.ToArray(), 
                implementsList, invocationList, inheritsList, classCreatedObjects, methodCreatedObjects, 
                pipeClient);
            pipeClient.Dispose();
            //File.WriteAllText(projectDir.CombinePath(DateTime.Now.Millisecond.ToString() + ".html"), Stratis.DevEx.Drawing.Html.DrawSummary(builder.ToString()));
        }
    }
}
