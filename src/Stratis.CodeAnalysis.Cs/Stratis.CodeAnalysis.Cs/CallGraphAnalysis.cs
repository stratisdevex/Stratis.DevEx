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
using Stratis.DevEx.Drawing;

namespace Stratis.CodeAnalysis.Cs
{
    class CallGraphAnalysis : Runtime
    {
        public static void Analyze(string cfgFile, Configuration config, SemanticModel sem)
        {
            using var top = Begin("Analyzing call graph of source document {doc} using configuration {cfg}.", sem.SyntaxTree.FilePath, config["General"]["ConfigFile"].StringValue);
            var projectDir = Path.GetDirectoryName(config["General"]["ConfigFile"].StringValue);
            ClassDeclarationSyntax[] classDeclarations = sem.SyntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                                   .ToArray();
            Graph graph = new Graph();
            graph.Kind = "cg";
            foreach (var classDeclaration in classDeclarations)
            {
                var methods = classDeclaration.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var method in methods)
                {
                    var invokingSymbol = sem.GetSymbolInfo(method).Symbol;
                    Debug("Analyzing method {0} invocations....", invokingSymbol.Name);
                    var invokingnid = invokingSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                    var invokingNode = graph.FindNode(invokingnid);
                    if (invokingNode is null)
                    {
                        invokingNode = new Node(invokingnid);
                        graph.AddNode(invokingNode);
                    }
                    var invocations = method.SyntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>();
                    foreach (var invocation in invocations)
                    {
                        var invokedSymbol = sem.GetSymbolInfo(invocation).Symbol;
                        if (invokedSymbol is null)
                            continue;
                        var invokednid = invokedSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                        var invokedNode = graph.FindNode(invokednid);
                        if (invokedNode is null)
                        {
                            invokedNode = new Node(invokednid);
                            graph.AddNode(invokedNode);
                        }
                        graph.AddEdge(invokingnid, invokednid);
                    }
                }
            }
            top.Complete();
            Debug("Call graph of source document {doc} has {n} nodes, {e} edges.", sem.SyntaxTree.FilePath, graph.NodeCount, graph.EdgeCount);
            if (config["Gui"]["Enabled"].BoolValue)
            {
                var pipeClient = Gui.CreatePipeClient();
                Gui.SendCallGraphGuiMessage(cfgFile, sem.Compilation, sem.SyntaxTree.FilePath, graph, pipeClient);
                pipeClient.Dispose();
                File.WriteAllText(projectDir.CombinePath(DateTime.Now.Millisecond.ToString() + ".html"), Html.DrawCallGraph(graph));
            }
        }
        public static void Analyze2(string cfgFile, Configuration config, SemanticModel sem)
        {
            using var top = Begin("Analyzing call graph of source document {doc} using configuration {cfg}.", sem.SyntaxTree.FilePath, config["General"]["ConfigFile"].StringValue);
            var projectDir = Path.GetDirectoryName(config["General"]["ConfigFile"].StringValue);
            ClassDeclarationSyntax[] classDeclarations = sem.SyntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                                   .ToArray();
            Dictionary<string, object> para;
            Graph graph = new Graph();
            var implementsList = new List<object>(); //Method Implementations
            var invocationList = new List<object>(); //Method Invocations
            var inheritsList = new List<object>();//Class Heirarchy
            var classCreatedObjects = new List<object>(); //Objects created by classes
            var methodCreatedObjects = new List<object>();//Objects created by methods

            foreach (ClassDeclarationSyntax classDeclaration in classDeclarations)
            {
                var classSymbol = sem.GetDeclaredSymbol(classDeclaration);
                var classPath = classSymbol.Name;
                if (classSymbol.ContainingNamespace != null)
                    classPath = classSymbol.ContainingNamespace.Name + '.' + classSymbol.Name;

                var classinfo = new Dictionary<string, object>();
                classinfo["name"] = classPath;
                classinfo["location"] = classDeclaration.GetLocation().ToString();

                /*
                 * If this Class is a Subclass, Collet Inheritance Info
                 */
                if (classDeclaration.BaseList != null)
                {
                    foreach (SimpleBaseTypeSyntax typ in classDeclaration.BaseList.Types)
                    {
                        var symInfo = sem.GetTypeInfo(typ.Type);

                        var baseClassPath = symInfo.Type.Name;
                        if (symInfo.Type.ContainingNamespace != null)
                            baseClassPath = symInfo.Type.ContainingNamespace.Name + '.' + symInfo.Type.Name;

                        var inheritInfo = new Dictionary<string, object>();
                        inheritInfo["class"] = classPath;
                        inheritInfo["base"] = baseClassPath;

                        inheritsList.Add(inheritInfo);
                    }
                }

                /*
                 * Insert Class into the Graph
                 */
                para = new Dictionary<string, object>();
                para["obj"] = classinfo;
                var nid = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var node = graph.FindNode(nid);
                if (node is null)
                {
                    node = new Node(nid);
                    node.UserData = classinfo;
                    graph.AddNode(node);
                }
               

                /*
                 * For each method within the class
                 */
                var methods = classDeclaration.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var method in methods)
                {
                    var symbol = sem.GetDeclaredSymbol(method);

                    //Collect Method Information
                    var methoddata = new Dictionary<string, object>();
                    methoddata["name"] = symbol.MetadataName;
                    if (symbol.ContainingNamespace != null)
                        methoddata["name"] = symbol.ContainingNamespace.Name + "." + symbol.MetadataName;
                    methoddata["location"] = classDeclaration.GetLocation().ToString();
                    methoddata["class"] = classinfo["name"];

                    implementsList.Add(methoddata);

                    var invocations = method.SyntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>();

                    //For each invocation within our method, collect information
                    foreach (var invocation in invocations)
                    {
                        var invokedSymbol = sem.GetSymbolInfo(invocation).Symbol;

                        if (invokedSymbol == null)
                            continue;

                        var invocationInfo = new Dictionary<string, object>();
                        invocationInfo["name"] = invokedSymbol.MetadataName;
                        if (symbol.ContainingNamespace != null)
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
                        var typeInfo = sem.GetTypeInfo(creation);
                        var createInfo = new Dictionary<string, object>();

                        var typeName = typeInfo.Type.Name;
                        if (typeInfo.Type.ContainingNamespace != null)
                            typeName = typeInfo.Type.ContainingNamespace.Name + "." + typeInfo.Type.Name;

                        createInfo["method"] = methoddata["name"];
                        createInfo["creates"] = typeName;
                        createInfo["location"] = creation.GetLocation().ToString();

                        methodCreatedObjects.Add(createInfo);
                    }
                }

                //For each object created within the class, collect information
                var creates = classDeclaration.SyntaxTree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
                foreach (var creation in creates)
                {
                    var typeInfo = sem.GetTypeInfo(creation);
                    var createInfo = new Dictionary<string, object>();

                    var typeName = typeInfo.Type.Name;
                    if (typeInfo.Type.ContainingNamespace != null)
                        typeName = typeInfo.Type.ContainingNamespace.Name + "." + typeInfo.Type.Name;

                    createInfo["class"] = classPath;
                    createInfo["creates"] = typeName;
                    createInfo["location"] = creation.GetLocation().ToString();
                    classCreatedObjects.Add(createInfo);
                }
            }
            /*
             * Insert Methods into Graph
             */
            para = new Dictionary<string, object>();
            para["methods"] = implementsList;

            /*
            session.WriteTransaction(tx =>
            {
                var txresult = tx.Run(@"UNWIND {methods} AS implements
                                                MATCH (c:Class{name:implements.class})
                                                MERGE (m:method{name:implements.name})
                                                ON CREATE SET m.location = implements.location
                                                MERGE (c)-[:ImplementsMethod]->(m)", para);
            });
            */

            /*
             * Insert Invocations into Graph
             */
            para = new Dictionary<string, object>();
            para["invocations"] = invocationList;

            /*
            session.WriteTransaction(tx =>
            {
                var txresult = tx.Run(@"UNWIND {invocations} AS invocation
                                                            MATCH (i:method{name:invocation.name})
                                                            MATCH (m:method{name:invocation.method})
                                                            CREATE UNIQUE (m)-[:InvokesMethod]->(i)", para);
            });
            */

            /*
             * Insert Class Inheritance into Graph
             */
            para = new Dictionary<string, object>();
            para["inherits"] = inheritsList;

            /*
            session.WriteTransaction(tx =>
            {
                var txresult = tx.Run(@"UNWIND {inherits} AS inherit
                                                MERGE (bc:Class{name:inherit.base})
                                                MERGE (c:Class{name:inherit.class})
                                                MERGE (c)-[:InheritsClass]->(bc)", para);
            });
            */

            /*
             * Insert Method Creations into Graph 
             */
            para["created"] = methodCreatedObjects;

            /*
            session.WriteTransaction(tx =>
            {
                var txresult = tx.Run(@"UNWIND {created} AS creation
                                                MATCH (m:method{name:creation.method})
                                                MATCH (cr:Class{name:creation.creates})
                                                CREATE UNIQUE (m)-[:CreatesObject]->(cr)", para);
            });
            */
            /*
             * Insert Class Creations into Graph 
             */
            para = new Dictionary<string, object>();
            para["created"] = classCreatedObjects;

            /*
            session.WriteTransaction(tx =>
            {
                var txresult = tx.Run(@"UNWIND {created} AS creation
                                                MATCH (c:Class{name:creation.class})
                                                MATCH (cr:Class{name:creation.creates})
                                                CREATE UNIQUE (c)-[:CreatesObject]->(cr)", para);
            });
            */

        }

    }

}
    
