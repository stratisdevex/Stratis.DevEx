﻿using System;
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
            }.ToImmutableDictionary();
            Diagnostics = ImmutableArray.Create(DiagnosticIds.Select(i => GetDescriptor(i.Key, i.Value)).ToArray());
        }
        #endregion

        #region Methods

        #region Syntax analysis
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
            Debug("Using namespace {0} declared at {1}.", ns.ToString(), ns.GetLineLocation());
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

        // SC0004 Class constructor must have a ISmartContractState as first parameter.
        public static Diagnostic AnalyzeConstructorDecl(ConstructorDeclarationSyntax node, SemanticModel model)
        {
            if (node.Parent is StructDeclarationSyntax) return NoDiagnostic;
            var parent = (ClassDeclarationSyntax) node.Parent;
            var parentSymbol = model.GetDeclaredSymbol(parent) as ITypeSymbol;

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
                return CreateDiagnostic("SC0006", DiagnosticSeverity.Error, node.GetLocation());
            }
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
                Debug("New array of type {0}[] created at location {1}.", elementtype.ToDisplayString(), objectCreation.Syntax.GetLocation());
            }
            else
            {
                Debug("New object of type {0} created at location {1}.", type.ToDisplayString(), objectCreation.Syntax.GetLocation());
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
            Debug("Variable {0} of type {1} declared at {2}. Base type: {3}. Array type: {4}. Element type: {5}.", v.ToDisplayString(), type.ToDisplayString(), variableDeclarator.Syntax.GetLineLocation(), basetype.ToDisplayString(), type.IsArrayTypeKind(), elementtype?.ToDisplayString() ?? "");
            
            var typename = type.ToDisplayString();
            var elementtypename = elementtype?.ToDisplayString() ?? string.Empty;
            var basetypename = basetype?.ToDisplayString() ?? string.Empty;

            if (PrimitiveTypeNames.Contains(typename) || SmartContractTypeNames.Contains(typename) || SmartContractTypeNames.Contains(basetypename))
            {
                return NoDiagnostic;
            }
            else if (type.IsValueType || type.IsObject() || type.IsEnum() || (type.IsArrayTypeKind() && (elementtype.IsValueType || elementtype.IsObject() || PrimitiveTypeNames.Contains(elementtypename) || SmartContractTypeNames.Contains(elementtypename) || (elementtype.IsArrayTypeKind() && ((IArrayTypeSymbol)elementtype).ElementType.IsValueType))))
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
            
            if(type.IsSmartContract() || type.IsEnum() || type.IsUserStruct() || PrimitiveTypeNames.Contains(typename) || SmartContractTypeNames.Contains(typename))
            {
                return NoDiagnostic;
            }
            else if (member.ContainingType.ToDisplayString() == "System.Array" && WhitelistedArrayPropertyNames.Contains(propname))
            {
                return NoDiagnostic;
            }
            else
            {
                return CreateDiagnostic("SC0008", DiagnosticSeverity.Error, propReference.Syntax.GetLocation(), propname, type);
            }
        }

        // Only whitelisted methods can be accessed
        public static Diagnostic AnalyzeMethodInvocation(IInvocationOperation methodInvocation)
        {
            var node = methodInvocation.Syntax;
            var method = methodInvocation.TargetMethod;
            var type = method.ContainingType;
            var basetype = type.BaseType;
            var typename = type.ToDisplayString();
            Debug("Method invocation {0} at {1}", method.ToDisplayString(), node.GetLineLocation());

            if (type.IsSmartContract() || type.IsEnum() || type.IsUserStruct() || PrimitiveTypeNames.Contains(typename) || SmartContractTypeNames.Contains(typename))
            {
                return NoDiagnostic;
            }
            else if (IsWhitelistedMethodName(typename, method.Name))
            {
                return NoDiagnostic;
            }
            else
            {
                return CreateDiagnostic("SC0009", DiagnosticSeverity.Error, node.GetLocation(), method.Name, typename);
            }
        }

        // Assert should not test constant value boolean condition
        private static Diagnostic AnalyzeAssertConditionConstant(IInvocationOperation methodInvocation)
        {
            if (methodInvocation.Arguments.Length == 0) return NoDiagnostic;
            
            var method = methodInvocation.TargetMethod;

            if (method.Name != "Assert" || !methodInvocation.Arguments[0].Value.ConstantValue.HasValue) return NoDiagnostic;
            
            var location = methodInvocation.Arguments[0].Syntax.GetLocation();
            var value = (bool)methodInvocation.Arguments[0].Value.ConstantValue.Value;
            
            return CreateDiagnostic("SC0010", DiagnosticSeverity.Warning, location, value);
        }

        // Assert should be called with message
        private static Diagnostic AnalyzeAssertMessageNotProvided(IInvocationOperation methodInvocation)
        {
            var method = methodInvocation.TargetMethod;

            if (method.ToString() != "Stratis.SmartContracts.SmartContract.Assert(bool, string)"
                || !methodInvocation.Arguments[1].IsImplicit) return NoDiagnostic;
            
            var location = methodInvocation.Syntax.GetLocation();
            return CreateDiagnostic("SC0011", DiagnosticSeverity.Info, location);
        }

        // Assert message should not be empty
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

        #region Syntax analysis
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

        public static bool IsWhitelistedMethodName(string typename, string methodname) => WhitelistedMethodNames.ContainsKey(typename) && WhitelistedArrayMethodNames.Contains(methodname);
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

        internal static readonly string[] WhitelistedArrayPropertyNames = { "Length" };

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
