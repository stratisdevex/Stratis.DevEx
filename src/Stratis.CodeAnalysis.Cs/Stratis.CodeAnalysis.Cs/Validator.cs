﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using SharpConfig;

using Stratis.SmartContracts;
using Stratis.DevEx;

namespace Stratis.CodeAnalysis.Cs
{
    public class Validator : Runtime
    {
        #region Constructors
        static Validator()
        {
            AllowedAssemblyReferencesNames = allowedAssemblyReferencesStr.Split(' ');
            DiagnosticIds = new Dictionary<string, DiagnosticSeverity>
            {
                { "SC0001", DiagnosticSeverity.Error },
                { "SC0002", DiagnosticSeverity.Error },
                { "SC0003", DiagnosticSeverity.Error },
                { "SC0004", DiagnosticSeverity.Error },
                { "SC0005", DiagnosticSeverity.Error },
                { "SC0006", DiagnosticSeverity.Error },
                { "SC0007", DiagnosticSeverity.Error },
                { "SC0008", DiagnosticSeverity.Error },
                { "SC0009", DiagnosticSeverity.Error },
                { "SC0010", DiagnosticSeverity.Warning },
                { "SC0011", DiagnosticSeverity.Info },
                { "SC0012", DiagnosticSeverity.Info },
                { "SC0013", DiagnosticSeverity.Error },
                { "SC0014", DiagnosticSeverity.Error },
                { "SC0015", DiagnosticSeverity.Error },
                { "SC0016", DiagnosticSeverity.Error },
                { "SC0017", DiagnosticSeverity.Error },
                { "SC0018", DiagnosticSeverity.Error },
                { "SC0019", DiagnosticSeverity.Error },
                { "SC0020", DiagnosticSeverity.Error },
                { "SC0021", DiagnosticSeverity.Error },
                { "SC0022", DiagnosticSeverity.Error },
            }.ToImmutableDictionary();
            Diagnostics = ImmutableArray.Create(DiagnosticIds.Select(i => GetDescriptor(i.Key, i.Value)).ToArray());
        }
        #endregion

        #region Methods

        #region Compilation analysis
        public static List<Diagnostic> AnalyzeCompilation(Compilation c)
        {
            var refs = c.ReferencedAssemblyNames.Select(a => a.Name);
            var d = new List<Diagnostic>();
            //Debug("Compilation assembly references: {0}.", refs.JoinWithSpaces());
            foreach (var r in refs)
            {
                if (!AllowedAssemblyReferencesNames.Contains(r))
                {
                    d.Add(CreateDiagnostic("SC0017", Location.None, r));
                }
            }
            return d;
        }
        #endregion
        
        #region Syntactic analysis
        // SC0001 Namespace declarations not allowed in smart contract code
        public static Diagnostic AnalyzeNamespaceDecl(NamespaceDeclarationSyntax node, SemanticModel model)
        {
            var ns = node.DescendantNodes().First();
            Debug("Namespace {0} declared at {1}.", ns.ToString(), ns.GetLineLocation());
            return CreateDiagnostic("SC0001", ns.GetLocation(), ns.ToString());
        }

        // SC0002 Only allow using Stratis.SmartContracts namespace in smart contract code
        public static Diagnostic AnalyzeUsingDirective(UsingDirectiveSyntax node, SemanticModel model)
        {
            var ns = node.DescendantNodes().OfType<NameSyntax>().FirstOrDefault();
            Debug("Using namespace {0} at {1}.", ns.ToString(), ns.GetLineLocation());
            if (ns != null && !WhitelistedNamespaces.Contains(ns.ToString()))
            {
                return CreateDiagnostic("SC0002", ns.GetLocation(), ns.ToFullString());
            }
            else
            {
                return NoDiagnostic;
            }
        }

        // SC0003 Declared classes must inherit from Stratis.SmartContracts.SmartContract
        // SC0020 A smart contract class or struct cannot contain nested types.
        // SC0022 A smart contract class cannot declare more than one constructor.
        public static Diagnostic AnalyzeClassDecl(ClassDeclarationSyntax node, SemanticModel model)
        {
            var type = model.GetDeclaredSymbol(node) as ITypeSymbol;
            var identifier = node.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken));
            Debug("Class {0} declared at {1}.", type.ToDisplayString(), node.GetLineLocation());
            var cdecls = node.ChildNodes().OfType<ConstructorDeclarationSyntax>();
            if (cdecls.Count() > 1)
            {
                return CreateDiagnostic("SC0022", node.GetLocation(), identifier.Text);
            }
            var parent = node.Parent;
            if (parent is ClassDeclarationSyntax clp)
            {
                return CreateDiagnostic("SC0020", node.GetLocation(), "class", clp.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)), identifier.Text);
            }
            else if (parent is StructDeclarationSyntax sp)
            {
                return CreateDiagnostic("SC0020", node.GetLocation(), "struct", sp.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)), identifier.Text);
            }
            else if (!type.IsSmartContract())
            {
                return CreateDiagnostic("SC0003", node.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)).GetLocation(), type.Name);
            }
            else
            {
                return NoDiagnostic;
            }
        }

        // SC0020 A smart contract class or struct cannot contain nested types.
        public static Diagnostic AnalyzeStructDecl(StructDeclarationSyntax node, SemanticModel model)
        {
            var structSymbol = model.GetDeclaredSymbol(node) as ITypeSymbol;
            var identifier = node.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken));
            Debug("Struct {0} declared at {1}.", structSymbol.ToDisplayString(), node.GetLineLocation());
            var parent = node.Parent;
            if (parent is StructDeclarationSyntax sp)
            {
                return CreateDiagnostic("SC0020", node.GetLocation(), "struct", sp.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)), identifier.Text);
            }
            return NoDiagnostic;
        }

        // SC0004 Class constructor must have ISmartContractState as the first parameter.
        // SC0019 A smart contract class cannot declare a static constructor or property or field.
        public static Diagnostic AnalyzeConstructorDecl(ConstructorDeclarationSyntax node, SemanticModel model)
        {
            if (node.Parent is StructDeclarationSyntax) return NoDiagnostic;
            var parent = node.Parent;
            var type = model.GetDeclaredSymbol(parent) as ITypeSymbol;
            Debug("Constructor for type {0} declared at {1}.", type.ToDisplayString(), node.GetLineLocation());
            if (node.Modifiers.Any(m => m.Kind() == SyntaxKind.StaticKeyword))
            {
                return CreateDiagnostic("SC0019", node.GetLocation(), parent.TypeDeclLabel(), type.ToDisplayString());
            }
            var fp = node
                .DescendantNodes()
                .OfType<ParameterListSyntax>()
                .FirstOrDefault()?
                .DescendantNodes()
                .OfType<ParameterSyntax>()
                .FirstOrDefault();

            if (fp == null) return NoDiagnostic;

            var fpt = fp.Type;

            var fpn = fp
                .ChildTokens()
                .First(t => t.IsKind(SyntaxKind.IdentifierToken));
            var m = model.GetSymbolInfo(fpt).Symbol;
            var classSymbol = model.GetSymbolInfo(fpt).Symbol as ITypeSymbol;
            if (classSymbol.ToDisplayString() != "Stratis.SmartContracts.ISmartContractState")
            {
                return CreateDiagnostic("SC0004", fpn.GetLocation(), fp.Identifier.Text);
            }
            else
            {
                return NoDiagnostic;
            }
        }

        // SC0005 Non-const field declarations outside structs not allowed in smart contract classes.
        public static Diagnostic AnalyzeFieldDecl(FieldDeclarationSyntax node, SemanticModel model)
        {
            var type = model.GetDeclaredSymbol(node.Parent) as ITypeSymbol;
            Debug("Field declaration of {0} in {1} at {2}.", node.Declaration.Variables.First().Identifier.Text, type.ToDisplayString(), node.GetLineLocation());
            if (node.Parent.IsKind(SyntaxKind.StructDeclaration))
            {
                if(node.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
                {
                    return CreateDiagnostic("SC0019", node.GetLocation(), node.Parent.TypeDeclLabel(), type.ToDisplayString());
                }
                else
                {
                    return NoDiagnostic;
                }
                
            }
            else if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
            {
                return NoDiagnostic;
            }
            else
            {
                return CreateDiagnostic("SC0006", node.GetLocation(), type.ToDisplayString());
            }
        }

        // SC0014 This type cannot be used as a smart contract method return type or parameter type.
        // SC0015 A smart contract class cannnot declare a destructor or finalizer.
        public static Diagnostic AnalyzeMethodDecl(MethodDeclarationSyntax node, SemanticModel model)
        {
            var methodname = node.Identifier.Text;
            var type = (ITypeSymbol)model.GetSymbolInfo(node.ReturnType).Symbol;
            var typename = type.ToDisplayString();
            var parent = node.Parent;

            var parentSymbol = model.GetDeclaredSymbol(parent) as ITypeSymbol;
            Debug("Method {0}{1} of return type {2} in {3} {4} declared at {5}.", methodname, node.ParameterList, typename, parent.TypeDeclLabel(), parentSymbol.ToDisplayString(), node.ReturnType.GetLineLocation());


            if (methodname == "Finalize" && parentSymbol.IsSmartContract())
            {
                return CreateDiagnostic("SC0015", node.Identifier.GetLocation(), parentSymbol.ToDisplayString());
            }
            else if (parent is StructDeclarationSyntax sd)
            {
                return CreateDiagnostic("SC0021", node.GetLocation(), parentSymbol.ToDisplayString(), methodname);
            }

            Func<ITypeSymbol, bool> typetest = node.Modifiers.Any(m => m.Kind() == SyntaxKind.PublicKeyword) ?
                t => t.IsWhitelistedMethodReturnType() : t => t.IsWhitelistedMethodReturnType() || t.IsValueType || t.IsWhitelistedArrayType();

            foreach (var p in node.ParameterList.Parameters)
            {
                var pt = model.GetDeclaredSymbol(p).Type;
                if (typetest(pt))
                {
                    continue;
                }
                else
                {
                    return CreateDiagnostic("SC0014", p.Type.GetLocation(), pt.ToDisplayString());
                }
            }


            if (typetest(type))
            {
                return NoDiagnostic;
            }
            else if (type.IsValueType && type.Locations.All(l => l.SourceTree.FilePath == node.GetLocation().SourceTree.FilePath))
            {
                Info("Method {0} has struct return type {1} declared in this file.", methodname, type);
                return NoDiagnostic;
            }
            else
            {
                return CreateDiagnostic("SC0014", node.ReturnType.GetLocation(), typename);
            }
        }
       
        // SC0015 A smart contract class cannnot declare a destructor or finalizer.
        public static Diagnostic AnalyzeDestructorDecl(DestructorDeclarationSyntax node, SemanticModel model)
        {
            var parent = (ClassDeclarationSyntax) node.Parent;
            var type = model.GetDeclaredSymbol(parent) as ITypeSymbol;
            Debug("Destructor for type {0} declared at {1}.", type.ToDisplayString(), node.GetLineLocation());
            return CreateDiagnostic("SC0015", node.GetLocation(), type.ToDisplayString());
        }

        // SC0016 Exception handling with try/catch blocks not allowed in smart contract code.
        public static Diagnostic AnalyzeTryStmt(TryStatementSyntax node, SemanticModel model)
        {
            var parent = (ClassDeclarationSyntax) node.Ancestors().First(a => a.Kind() == SyntaxKind.ClassDeclaration);
            var type = model.GetDeclaredSymbol(parent) as ITypeSymbol;
            Debug("Try statement in class {0} at {1}.", type.ToDisplayString(), node.GetLineLocation());
            return CreateDiagnostic("SC0016", node.GetLocation());
        }

        
        #endregion

        #region Semantic analysis
        // SC0006 New object creation not allowed except for structs and arrays of primitive types and structs
        public static Diagnostic AnalyzeObjectCreation(IObjectCreationOperation objectCreation)
        {
            var type = objectCreation.Type;
            var elementtype = type.IsArrayTypeKind() ? ((IArrayTypeSymbol)type).ElementType : null;
            if (elementtype is not null)
            {
                Debug("New array of type {0}[] created at {1}.", elementtype.ToDisplayString(), objectCreation.Syntax.GetLineLocation());
            }
            else
            {
                Debug("New object of type {0} created at {1}.", type.ToDisplayString(), objectCreation.Syntax.GetLineLocation());
            }
           
        
            if (type.IsValueType || type.IsAttribute() || type.IsPrimitiveType() || type.IsStratisType() || type.IsWhitelistedArrayType())
            {
                return NoDiagnostic;
            }
            else
            {
                return CreateDiagnostic("SC0006", objectCreation.Syntax.GetLocation(), type.ToDisplayString());
            }
        }

        // SC0007 Only certain variable types can be used in smart contract code
        public static Diagnostic AnalyzeVariableDeclaration(IVariableDeclaratorOperation variableDeclarator)
        {
            var v = variableDeclarator.Symbol;
            var type = variableDeclarator.Symbol.Type;
            var elementtype = type.IsArrayTypeKind() ? ((IArrayTypeSymbol)type).ElementType : null;
            var basetype = type.BaseType;
            var typename = type.ToDisplayString();
            Debug("Variable {0} of type {1} declared at {2}. Base type: {3}. Enum type: {4}. User struct: {5}. Primitive type: {6}., Array type: {7}. Element type: {8}.", 
                v.ToDisplayString(), typename, variableDeclarator.Syntax.GetLineLocation(), basetype?.ToDisplayString() ?? "(none)", type.IsEnum(), type.IsUserStruct(), type.IsPrimitiveType(), type.IsArrayTypeKind(), elementtype?.ToDisplayString() ?? "(none)");
            
            if (type.IsEnum() || type.IsObject() || type.IsUserStruct() || type.IsPrimitiveType() || type.IsStratisType() || type.IsWhitelistedArrayType())
            {
                return NoDiagnostic;
            }
            else
            {
                return CreateDiagnostic("SC0007", variableDeclarator.Syntax.GetLocation(), typename);
            }
        }

        // SC0008 Only struct properties or whitelisted properties of reference types can be accessed
        public static Diagnostic AnalyzePropertyReference(IPropertyReferenceOperation propReference)
        {
            var member = propReference.Member;
            string propname = member.Name;
            var type = member.ContainingType;
            var typename = type.ToDisplayString();
            Debug("Property reference {0} at {1}", member.ToDisplayString(), propReference.Syntax.GetLineLocation());

            if (type.IsEnum() || type.IsPrimitiveType() || type.IsSmartContract() || type.IsStratisType() || type.IsUserStruct() || type.TypeKind == TypeKind.Interface)
            {
                return NoDiagnostic;
            }
            else if (type.IsWhitelistedPropertyName(propname))
            {
                return NoDiagnostic;
            }
            else
            {
                return CreateDiagnostic("SC0008", propReference.Syntax.GetLocation(), propname, type);
            }
        }

        // SC0009 This method cannot be used in smart contract code.
        // SC0013 This type cannot be used in smart contract code.
        public static Diagnostic AnalyzeMethodInvocation(IInvocationOperation methodInvocation)
        {
            var node = methodInvocation.Syntax;
            var method = methodInvocation.TargetMethod;
            string methodname = method.Name;
            var type = method.ContainingType;
            var basetype = type.BaseType;
            var typename = type.ToDisplayString();
            Debug("Method invocation {0} at {1}", method.ToDisplayString(), node.GetLineLocation());
            if (method.MethodKind == MethodKind.Constructor)
            {
                return NoDiagnostic;
            }
            else if (type.IsSmartContract() || PrimitiveTypeNames.Contains(typename) || SmartContractTypeNames.Contains(typename))
            {
                return NoDiagnostic;
            }
            else if (type.IsWhitelistedMethodName(methodname))
            {
                return NoDiagnostic;
            }
            else
            {
                return CreateDiagnostic("SC0009", node.GetLocation(), method.Name, typename);
            }
        }

        // SC0010 Assert should not test constant value boolean condition
        private static Diagnostic AnalyzeAssertConditionConstant(IInvocationOperation methodInvocation)
        {
            if (methodInvocation.Arguments.Length == 0) return NoDiagnostic;
            
            var method = methodInvocation.TargetMethod;

            if (method.Name != "Assert" || !methodInvocation.Arguments[0].Value.ConstantValue.HasValue) return NoDiagnostic;
            
            var location = methodInvocation.Arguments[0].Syntax.GetLocation();
            var value = (bool)methodInvocation.Arguments[0].Value.ConstantValue.Value;
            
            return CreateDiagnostic("SC0010", location, value);
        }

        // SC0011 Assert should be called with message
        private static Diagnostic AnalyzeAssertMessageNotProvided(IInvocationOperation methodInvocation)
        {
            var method = methodInvocation.TargetMethod;

            if (method.ToString() != "Stratis.SmartContracts.SmartContract.Assert(bool, string)"
                || !methodInvocation.Arguments[1].IsImplicit) return NoDiagnostic;
            
            var location = methodInvocation.Syntax.GetLocation();
            return CreateDiagnostic("SC0011", location);
        }

        // SC0012 Assert message should not be empty
        private static Diagnostic AnalyzeAssertMessageEmpty(IInvocationOperation methodInvocation)
        {
            var method = methodInvocation.TargetMethod;
            
            if (method.ToString() != "Stratis.SmartContracts.SmartContract.Assert(bool, string)"
                || methodInvocation.Arguments[1].IsImplicit) return NoDiagnostic;

            var assertMessageSyntax = methodInvocation.Arguments[1].Syntax.ToString();
            if (assertMessageSyntax != "\"\"" && assertMessageSyntax != "string.Empty") return NoDiagnostic;
            
            var location = methodInvocation.Arguments[1].Syntax.GetLocation();
            return CreateDiagnostic("SC0012", location);
        }
        #endregion

        #region Overloads
       
        #region Syntactic analysis
        public static Diagnostic AnalyzeUsingDirective(UsingDirectiveSyntax node, SyntaxNodeAnalysisContext ctx) =>
           AnalyzeUsingDirective(node, ctx.SemanticModel)?.Report(ctx);
        public static Diagnostic AnalyzeNamespaceDecl(NamespaceDeclarationSyntax node, SyntaxNodeAnalysisContext ctx) =>
            AnalyzeNamespaceDecl(node, ctx.SemanticModel)?.Report(ctx);

        public static Diagnostic AnalyzeClassDecl(ClassDeclarationSyntax node, SyntaxNodeAnalysisContext ctx) =>
           AnalyzeClassDecl(node, ctx.SemanticModel)?.Report(ctx);

        public static Diagnostic AnalyzeStructDecl(StructDeclarationSyntax node, SyntaxNodeAnalysisContext ctx) =>
          AnalyzeStructDecl(node, ctx.SemanticModel)?.Report(ctx);

        public static Diagnostic AnalyzeConstructorDecl(ConstructorDeclarationSyntax node, SyntaxNodeAnalysisContext ctx) =>
            AnalyzeConstructorDecl(node, ctx.SemanticModel)?.Report(ctx);

        public static Diagnostic AnalyzeFieldDecl(FieldDeclarationSyntax node, SyntaxNodeAnalysisContext ctx) =>
           AnalyzeFieldDecl(node, ctx.SemanticModel)?.Report(ctx);

        public static Diagnostic AnalyzeMethodDecl(MethodDeclarationSyntax node, SyntaxNodeAnalysisContext ctx) =>
            AnalyzeMethodDecl(node, ctx.SemanticModel)?.Report(ctx);

        public static Diagnostic AnalyzeDestructorDecl(DestructorDeclarationSyntax node, SyntaxNodeAnalysisContext ctx) =>
            AnalyzeDestructorDecl(node, ctx.SemanticModel)?.Report(ctx);

        public static Diagnostic AnalyzeTryStmt(TryStatementSyntax node, SyntaxNodeAnalysisContext ctx) =>
            AnalyzeTryStmt(node, ctx.SemanticModel)?.Report(ctx);

        #endregion

        #region Semantic analysis
        public static Diagnostic AnalyzeObjectCreation(IObjectCreationOperation objectCreation, OperationAnalysisContext ctx) =>
            AnalyzeObjectCreation(objectCreation).Report(ctx);

        public static Diagnostic AnalyzePropertyReference(IPropertyReferenceOperation propertyReference, OperationAnalysisContext ctx) =>
            AnalyzePropertyReference(propertyReference).Report(ctx);

        public static Diagnostic AnalyzeMethodInvocation(IInvocationOperation methodInvocation, OperationAnalysisContext ctx) =>
            AnalyzeMethodInvocation(methodInvocation).Report(ctx);

        public static Diagnostic AnalyzeVariableDeclaration(IVariableDeclaratorOperation variableDeclarator, OperationAnalysisContext ctx) =>
            AnalyzeVariableDeclaration(variableDeclarator).Report(ctx);
        
        public static Diagnostic AnalyzeAssertConditionConstant(IInvocationOperation methodInvocation, OperationAnalysisContext ctx) =>
            AnalyzeAssertConditionConstant(methodInvocation).Report(ctx);
        
        public static Diagnostic AnalyzeAssertMessageNotProvided(IInvocationOperation methodInvocation, OperationAnalysisContext ctx) =>
            AnalyzeAssertMessageNotProvided(methodInvocation).Report(ctx);
        
        public static Diagnostic AnalyzeAssertMessageEmpty(IInvocationOperation methodInvocation, OperationAnalysisContext ctx) =>
            AnalyzeAssertMessageEmpty(methodInvocation).Report(ctx);

        #endregion

        #endregion

        #region Diagnostic utilities
        public static DiagnosticDescriptor GetDescriptor(string id, DiagnosticSeverity severity) =>
            new DiagnosticDescriptor(id, RM.GetString($"{id}_Title"), RM.GetString($"{id}_MessageFormat"), Category,
                severity, true, RM.GetString($"{id}_Description"));

        public static Diagnostic CreateDiagnostic(string id, Location location, params object[] args)
        {
            var d = Diagnostic.Create(GetDescriptor(id, DiagnosticIds[id]), location, args);
            Info("Emitting diagnostic id: {0}; title: {1}; location: {2}.", d.Id, d.Descriptor.Title, d.Location.ToString());
            return d;
        }
        #endregion

        #endregion

        #region Fields
        internal const string Category = "Smart Contract";
        internal const Diagnostic NoDiagnostic = null;
        internal static readonly ImmutableArray<DiagnosticDescriptor> Diagnostics;
        internal static readonly ImmutableDictionary<string, DiagnosticSeverity> DiagnosticIds;
        internal static readonly System.Resources.ResourceManager RM = Resources.ResourceManager;

        internal static readonly Type[] BoxedPrimitiveTypes =
        {
            typeof(void),
            typeof(bool),
            typeof(byte),
            typeof(char),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(string),
            typeof(UInt128),
            typeof(UInt256),
        };

        internal static readonly string[] UnboxedPrimitiveTypeNames =
        {
            "void",
            "bool",
            "byte",
            "char",
            "int",
            "uint",
            "long",
            "ulong",
            "string",
        };

        internal static readonly string[] PrimitiveTypeNames = UnboxedPrimitiveTypeNames.Concat(BoxedPrimitiveTypes.Select(t => t.FullName)).ToArray();

        internal static readonly Type[] BoxedPrimitiveArrayTypes =
        {
            typeof(bool[]),
            typeof(byte[]),
            typeof(char[]),
            typeof(int[]),
            typeof(uint[]),
            typeof(long[]),
            typeof(ulong[]),
            typeof(string[]),
            typeof(UInt128[]),
            typeof(UInt256[]),
        };

        internal static readonly string[] PrimitiveArrayTypeNames = UnboxedPrimitiveTypeNames.Skip(1).Select(t => t + "[]").Concat(BoxedPrimitiveArrayTypes.Select(t => t.FullName)).ToArray();

        internal static readonly Type[] SmartContractTypes =
        {
            typeof(Address),
            typeof(Block),
            typeof(IBlock),
            typeof(IContractLogger),
            typeof(ICreateResult),
            typeof(IMessage),
            typeof(IPersistentState),
            typeof(ISmartContractState),
            typeof(ISerializer),
            typeof(ITransferResult),
            typeof(Message),
        };

        internal static readonly Type[] SmartContractArrayTypes =
        {
            typeof(Address[]),
            typeof(Block[]),
            typeof(IBlock[]),
            typeof(IContractLogger[]),
            typeof(ICreateResult[]),
            typeof(IMessage[]),
            typeof(IPersistentState[]),
            typeof(ISmartContractState[]),
            typeof(ITransferResult[]),
            typeof(Message[]),
        };

        internal static readonly Type[] SmartContractAttributeTypes =
        {
            typeof(DeployAttribute),
            typeof(IndexAttribute)
        };

        internal static readonly string[] SmartContractTypeNames = SmartContractTypes.Select(t => t.FullName).ToArray();

        internal static readonly string[] SmartContractArrayTypeNames = SmartContractArrayTypes.Select(t => t.FullName).ToArray();

        internal static readonly string[] SmartContractAttributeTypeNames = SmartContractAttributeTypes.Select(t => t.FullName).ToArray();

        internal static readonly Dictionary<string, string[]> WhitelistedPropertyNames = new Dictionary<string, string[]>()
        {
            { "System.Array", new string[] {"Length" } }
        };

        internal static readonly string[] WhitelistedArrayMethodNames = { "GetLength", "Copy", "GetValue", "SetValue", "Resize" };

        internal static readonly Dictionary<string, string[]> WhitelistedMethodNames = new Dictionary<string, string[]>() 
        {
            { "object", new string [] { "ToString" } },
            { "System.Object", new string [] { "ToString" } },
            { "System.Array", WhitelistedArrayMethodNames}
        };

        internal static string[] WhitelistedMethodReturnTypeNames = PrimitiveTypeNames.Concat(new[] { typeof(byte[]).FullName, "byte[]", typeof(object[]).FullName, "object[]", typeof(UInt128[]).FullName, typeof(UInt256[]).FullName, typeof(Address).FullName }).ToArray();

        internal static readonly string[] WhitelistedNamespaces = { "System", "Stratis.SmartContracts", "Stratis.SmartContracts.Standards" };

        internal static string allowedAssemblyReferencesStr = "Microsoft.CSharp Microsoft.VisualBasic.Core Microsoft.VisualBasic Microsoft.Win32.Primitives mscorlib netstandard System.AppContext System.Buffers System.Collections.Concurrent System.Collections System.Collections.Immutable System.Collections.NonGeneric System.Collections.Specialized System.ComponentModel.Annotations System.ComponentModel.DataAnnotations System.ComponentModel System.ComponentModel.EventBasedAsync System.ComponentModel.Primitives System.ComponentModel.TypeConverter System.Configuration System.Console System.Core System.Data.Common System.Data.DataSetExtensions System.Data System.Diagnostics.Contracts System.Diagnostics.Debug System.Diagnostics.DiagnosticSource System.Diagnostics.FileVersionInfo System.Diagnostics.Process System.Diagnostics.StackTrace System.Diagnostics.TextWriterTraceListener System.Diagnostics.Tools System.Diagnostics.TraceSource System.Diagnostics.Tracing System System.Drawing System.Drawing.Primitives System.Dynamic.Runtime System.Globalization.Calendars System.Globalization System.Globalization.Extensions System.IO.Compression.Brotli System.IO.Compression System.IO.Compression.FileSystem System.IO.Compression.ZipFile System.IO System.IO.FileSystem System.IO.FileSystem.DriveInfo System.IO.FileSystem.Primitives System.IO.FileSystem.Watcher System.IO.IsolatedStorage System.IO.MemoryMappedFiles System.IO.Pipes System.IO.UnmanagedMemoryStream System.Linq System.Linq.Expressions System.Linq.Parallel System.Linq.Queryable System.Memory System.Net System.Net.Http System.Net.HttpListener System.Net.Mail System.Net.NameResolution System.Net.NetworkInformation System.Net.Ping System.Net.Primitives System.Net.Requests System.Net.Security System.Net.ServicePoint System.Net.Sockets System.Net.WebClient System.Net.WebHeaderCollection System.Net.WebProxy System.Net.WebSockets.Client System.Net.WebSockets System.Numerics System.Numerics.Vectors System.ObjectModel System.Reflection.DispatchProxy System.Reflection System.Reflection.Emit System.Reflection.Emit.ILGeneration System.Reflection.Emit.Lightweight System.Reflection.Extensions System.Reflection.Metadata System.Reflection.Primitives System.Reflection.TypeExtensions System.Resources.Reader System.Resources.ResourceManager System.Resources.Writer System.Runtime.CompilerServices.Unsafe System.Runtime.CompilerServices.VisualC System.Runtime System.Runtime.Extensions System.Runtime.Handles System.Runtime.InteropServices System.Runtime.InteropServices.RuntimeInformation System.Runtime.InteropServices.WindowsRuntime System.Runtime.Intrinsics System.Runtime.Loader System.Runtime.Numerics System.Runtime.Serialization System.Runtime.Serialization.Formatters System.Runtime.Serialization.Json System.Runtime.Serialization.Primitives System.Runtime.Serialization.Xml System.Security.Claims System.Security.Cryptography.Algorithms System.Security.Cryptography.Csp System.Security.Cryptography.Encoding System.Security.Cryptography.Primitives System.Security.Cryptography.X509Certificates System.Security System.Security.Principal System.Security.SecureString System.ServiceModel.Web System.ServiceProcess System.Text.Encoding.CodePages System.Text.Encoding System.Text.Encoding.Extensions System.Text.Encodings.Web System.Text.Json System.Text.RegularExpressions System.Threading.Channels System.Threading System.Threading.Overlapped System.Threading.Tasks.Dataflow System.Threading.Tasks System.Threading.Tasks.Extensions System.Threading.Tasks.Parallel System.Threading.Thread System.Threading.ThreadPool System.Threading.Timer System.Transactions System.Transactions.Local System.ValueTuple System.Web System.Web.HttpUtility System.Windows System.Xml System.Xml.Linq System.Xml.ReaderWriter System.Xml.Serialization System.Xml.XDocument System.Xml.XmlDocument System.Xml.XmlSerializer System.Xml.XPath System.Xml.XPath.XDocument WindowsBase Stratis.SmartContracts Stratis.CodeAnalysis";

        internal static string[] AllowedAssemblyReferencesNames;

        /// <summary>
        /// The set of Assemblies that a <see cref="SmartContract"/> is required to reference
        /// </summary>
        //internal static HashSet<Assembly> AllowedSCAssemblies = new HashSet<Assembly> {
        //        Assembly.Load("System.Runtime"),
        //        typeof(object).Assembly,
        //        typeof(SmartContract).Assembly,
        //        typeof(Enumerable).Assembly,
                //typeof(IStandardToken).Assembly
        //    };
        #endregion
    }
}
