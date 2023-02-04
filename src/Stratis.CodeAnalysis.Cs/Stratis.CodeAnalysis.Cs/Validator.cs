using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using Stratis.SmartContracts;
using Stratis.DevEx;

namespace Stratis.CodeAnalysis.Cs
{
    public class Validator : Runtime
    {
        #region Constructors
        static Validator()
        {
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
                { "SC0016", DiagnosticSeverity.Error }
            }.ToImmutableDictionary();
            Diagnostics = ImmutableArray.Create(DiagnosticIds.Select(i => GetDescriptor(i.Key, i.Value)).ToArray());
        }
        #endregion

        #region Methods

        #region Syntactic analysis
        // SC0001 Namespace declarations not allowed in smart contract code
        public static Diagnostic AnalyzeNamespaceDecl(NamespaceDeclarationSyntax node, SemanticModel model)
        {
            var ns = node.DescendantNodes().First();
            Debug("Namespace {0} declared at {1}.", ns.ToString(), ns.GetLineLocation());
            return CreateDiagnostic("SC0001", DiagnosticSeverity.Error, ns.GetLocation(), ns.ToString());
        }

        // SC0002 Only allow using Stratis.SmartContracts namespace in smart contract code
        public static Diagnostic AnalyzeUsingDirective(UsingDirectiveSyntax node, SemanticModel model)
        {
            var ns = node.DescendantNodes().OfType<NameSyntax>().FirstOrDefault();
            Debug("Using namespace {0} at {1}.", ns.ToString(), ns.GetLineLocation());
            if (ns != null && !WhitelistedNamespaces.Contains(ns.ToString()))
            {
                return CreateDiagnostic("SC0002", DiagnosticSeverity.Error, ns.GetLocation(), ns.ToFullString());
            }
            else
            {
                return NoDiagnostic;
            }
        }

        // SC0003 Declared classes must inherit from Stratis.SmartContracts.SmartContract
        public static Diagnostic AnalyzeClassDecl(ClassDeclarationSyntax node, SemanticModel model)
        {
            var classSymbol = model.GetDeclaredSymbol(node) as ITypeSymbol;
            Debug("Class {0} declared at {1}.", classSymbol.ToDisplayString(), node.GetLineLocation());
            if (classSymbol.BaseType is null || classSymbol.BaseType.ToDisplayString() != "Stratis.SmartContracts.SmartContract")
            {
                return CreateDiagnostic("SC0003", DiagnosticSeverity.Error, node.ChildTokens().First(t => t.IsKind(SyntaxKind.IdentifierToken)).GetLocation(), classSymbol.Name);
            }
            else
            {
                return NoDiagnostic;
            }
        }

        // SC0004 Class constructor must have ISmartContractState as the first parameter.
        public static Diagnostic AnalyzeConstructorDecl(ConstructorDeclarationSyntax node, SemanticModel model)
        {
            if (node.Parent is StructDeclarationSyntax) return NoDiagnostic;
            var parent = (ClassDeclarationSyntax)node.Parent;
            var parentSymbol = model.GetDeclaredSymbol(parent) as ITypeSymbol;
            Debug("Constructor for type {0} declared at {1}.", parentSymbol.ToDisplayString(), node.GetLineLocation());
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
                return CreateDiagnostic("SC0004", DiagnosticSeverity.Error, fpn.GetLocation(), fp.Identifier.Text);
            }
            else
            {
                return NoDiagnostic;
            }
        }

        // SC0005 Non-const field declarations outside structs not allowed in smart contract classes
        public static Diagnostic AnalyzeFieldDecl(FieldDeclarationSyntax node, SemanticModel model)
        {
            Debug("Field declaration of {0} at {1}.", node.Declaration.Variables.First().Identifier.Text, node.GetLineLocation());
            if (node.Parent.IsKind(SyntaxKind.StructDeclaration))
            {
                return NoDiagnostic;
            }
            else if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
            {

                return NoDiagnostic;
            }
            else
            {
                var parent = (ClassDeclarationSyntax) node.Parent;
                var type = model.GetDeclaredSymbol(parent) as ITypeSymbol;
                return CreateDiagnostic("SC0006", DiagnosticSeverity.Error, node.GetLocation(), type.ToDisplayString());
            }
        }

        // SC0014 This type cannot be used as a smart contract method return type or parameter type.
        // SC0015 A smart contract class cannnot declare a destructor or finalizer.
        public static Diagnostic AnalyzeMethodDecl(MethodDeclarationSyntax node, SemanticModel model)
        {
            var methodname = node.Identifier.Text;
            var type = (ITypeSymbol) model.GetSymbolInfo(node.ReturnType).Symbol;
            var typename = type.ToDisplayString();
            var parent = (ClassDeclarationSyntax)node.Parent;
            var parentSymbol = model.GetDeclaredSymbol(parent) as ITypeSymbol;
            Debug("Method {0}{1} of return type {2} in class {3} declared at {4}.", methodname, node.ParameterList, typename, parentSymbol.ToDisplayString(), node.GetLineLocation());

            if (methodname == "Finalize" && parentSymbol.IsSmartContract())
            {
                return CreateDiagnostic("SC0015", DiagnosticSeverity.Error, node.GetLocation(), parentSymbol.ToDisplayString());
            }
        
            foreach (var p in node.ParameterList.Parameters)
            {
                var pt = model.GetDeclaredSymbol(p).Type;
                if (pt.IsEnum() || pt.IsUserStruct() || IsPrimitiveType(pt) || IsSmartContractType(pt) || IsWhitelistedArrayType(pt))
                {
                    continue;
                }
                else
                {
                    return CreateDiagnostic("SC0014", DiagnosticSeverity.Error, p.GetLocation(), pt.ToDisplayString());
                }
            }

            if (type.IsEnum() || type.IsUserStruct() || IsPrimitiveType(type) || IsSmartContractType(type) || IsWhitelistedArrayType(type)) 
            {
                return NoDiagnostic;
            }
            else 
            {
                return CreateDiagnostic("SC0014", DiagnosticSeverity.Error, node.GetLocation(), typename);
            }
        }

        // SC0015 A smart contract class cannnot declare a destructor or finalizer.
        public static Diagnostic AnalyzeDestructorDecl(DestructorDeclarationSyntax node, SemanticModel model)
        {
            var parent = (ClassDeclarationSyntax) node.Parent;
            var type = model.GetDeclaredSymbol(parent) as ITypeSymbol;
            Debug("Destructor for type {0} declared at {1}.", type.ToDisplayString(), node.GetLineLocation());
            return CreateDiagnostic("SC0015", DiagnosticSeverity.Error, node.GetLocation(), type.ToDisplayString());
        }

        // SC0016 Exception handling with try/catch blocks not allowed in smart contract code.
        public static Diagnostic AnalyzeTryStmt(TryStatementSyntax node, SemanticModel model)
        {
            var parent = (ClassDeclarationSyntax) node.Ancestors().First(a => a.Kind() == SyntaxKind.ClassDeclaration);
            var type = model.GetDeclaredSymbol(parent) as ITypeSymbol;
            Debug("Try statement in class {0} at {1}.", type.ToDisplayString(), node.GetLineLocation());
            return CreateDiagnostic("SC0016", DiagnosticSeverity.Error, node.GetLocation());
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
            var typename = type.ToDisplayString();
            var elementtypename = elementtype?.ToDisplayString() ?? "";
        
            if (type.IsValueType || PrimitiveTypeNames.Contains(typename) || SmartContractTypeNames.Contains(typename) || SmartContractArrayTypeNames.Contains(typename) || (type.IsArrayTypeKind() && (elementtype.IsValueType || PrimitiveTypeNames.Contains(elementtypename) || SmartContractTypeNames.Contains(elementtypename))))
            {
                return NoDiagnostic;
            }
            else
            {
                return CreateDiagnostic("SC0005", DiagnosticSeverity.Error, objectCreation.Syntax.GetLocation(), type.ToDisplayString());
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
                v.ToDisplayString(), typename, variableDeclarator.Syntax.GetLineLocation(), basetype.ToDisplayString(), type.IsEnum(), type.IsUserStruct(), PrimitiveTypeNames.Contains(typename), type.IsArrayTypeKind(), elementtype?.ToDisplayString() ?? "(none)");
            
            if (type.IsEnum() || type.IsUserStruct() || IsPrimitiveType(type))
            {
                return NoDiagnostic;
            }
            else if (IsWhitelistedArrayType(type))
            {
                return NoDiagnostic;
            }
            else
            {
                return CreateDiagnostic("SC0007", DiagnosticSeverity.Error, variableDeclarator.Syntax.GetLocation(), typename);
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

            if (type.IsSmartContract() || type.IsEnum() || type.IsUserStruct() || PrimitiveTypeNames.Contains(typename) || SmartContractTypeNames.Contains(typename))
            {
                return NoDiagnostic;
            }
            else if (IsWhitelistedPropertyName(typename, propname))
            {
                return NoDiagnostic;
            }
            else
            {
                return CreateDiagnostic("SC0008", DiagnosticSeverity.Error, propReference.Syntax.GetLocation(), propname, type);
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

            for (int i = 0; i < method.Parameters.Length; i++)
            {
                var t = method.Parameters[i].Type;
                var tn = t.ToDisplayString();
                if (t.IsEnum() || t.IsUserStruct() || PrimitiveTypeNames.Contains(tn) || SmartContractTypeNames.Contains(tn) || PrimitiveArrayTypeNames.Contains(tn) || SmartContractArrayTypeNames.Contains(tn))
                {
                    continue;
                }
                else 
                {
                    return CreateDiagnostic("SC0013", DiagnosticSeverity.Error, method.Parameters[i].Locations.First(), method.Name, typename);
                }
            }

            if (type.IsSmartContract() || type.IsEnum() || type.IsUserStruct() || PrimitiveTypeNames.Contains(typename) || SmartContractTypeNames.Contains(typename))
            {
                return NoDiagnostic;
            }
            else if (IsWhitelistedMethodName(typename, methodname))
            {
                return NoDiagnostic;
            }
            else
            {
                return CreateDiagnostic("SC0009", DiagnosticSeverity.Error, node.GetLocation(), method.Name, typename);
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
            
            return CreateDiagnostic("SC0010", DiagnosticSeverity.Warning, location, value);
        }

        // SC0011 Assert should be called with message
        private static Diagnostic AnalyzeAssertMessageNotProvided(IInvocationOperation methodInvocation)
        {
            var method = methodInvocation.TargetMethod;

            if (method.ToString() != "Stratis.SmartContracts.SmartContract.Assert(bool, string)"
                || !methodInvocation.Arguments[1].IsImplicit) return NoDiagnostic;
            
            var location = methodInvocation.Syntax.GetLocation();
            return CreateDiagnostic("SC0011", DiagnosticSeverity.Info, location);
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
            return CreateDiagnostic("SC0012", DiagnosticSeverity.Info, location);
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

        public static Diagnostic CreateDiagnostic(string id, DiagnosticSeverity severity, Location location, params object[] args)
        {
            var d = Diagnostic.Create(GetDescriptor(id, severity), location, args);
            Info("Emitting diagnostic id: {0}; title: {1}; location: {2}.", d.Id, d.Descriptor.Title, d.Location.ToString());
            return d;
        }

        public static bool IsPrimitiveType(ITypeSymbol t) => PrimitiveTypeNames.Contains(t.ToDisplayString());

        public static bool IsSmartContractType(ITypeSymbol type) => SmartContractTypeNames.Contains(type.ToDisplayString());

        public static bool IsWhitelistedArrayType(ITypeSymbol type)
        {
            var elementtype = type.IsArrayTypeKind() ? ((IArrayTypeSymbol)type).ElementType : null;
            var elementtypename = elementtype?.ToDisplayString();
            return type.IsArrayTypeKind() && (elementtype.IsUserStruct() || elementtype.IsObject() || PrimitiveTypeNames.Contains(elementtypename) || SmartContractTypeNames.Contains(elementtypename) || (elementtype.IsArrayTypeKind() && IsWhitelistedArrayType(elementtype)));
        }

        public static bool IsWhitelistedMethodName(string typename, string methodname) => WhitelistedMethodNames.ContainsKey(typename) && WhitelistedMethodNames[typename].Contains(methodname);

        public static bool IsWhitelistedPropertyName(string typename, string propname) => WhitelistedPropertyNames.ContainsKey(typename) && WhitelistedPropertyNames[typename].Contains(propname);
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
        };

        internal static readonly string[] UnboxedPrimitiveTypeNames =
        {
            "void",
            "bool",
            "byte",
            "sbyte",
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
            typeof(sbyte[]),
            typeof(char[]),
            typeof(int[]),
            typeof(uint[]),
            typeof(long[]),
            typeof(ulong[]),
            typeof(UInt128[]),
            typeof(UInt256[]),
            typeof(string[])
        };

        internal static readonly string[] PrimitiveArrayTypeNames = UnboxedPrimitiveTypeNames.Skip(1).Select(t => t + "[]").Concat(BoxedPrimitiveArrayTypes.Select(t => t.FullName)).ToArray();

        internal static readonly Type[] SmartContractTypes =
        {
            typeof(UInt128),
            typeof(UInt256),
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
            typeof(UInt128[]),
            typeof(UInt256[]),
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
        internal static readonly string[] WhitelistedNamespaces = { "System", "Stratis.SmartContracts", "Stratis.SmartContracts.Standards" };
        #endregion
    }
}
