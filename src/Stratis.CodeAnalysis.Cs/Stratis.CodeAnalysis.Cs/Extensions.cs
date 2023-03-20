using System;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Stratis.CodeAnalysis.Cs
{
    public static class Extensions
    {
        public static bool IsObject(this ITypeSymbol t) => t != null && (t.ToDisplayString() == "System.Object" || t.ToDisplayString() == "object");

        public static bool IsFloat(this ITypeSymbol t) => t != null && (t.ToDisplayString() == "System.Single" || t.ToDisplayString() == "System.Double" || t.ToDisplayString() == "float" || t.ToDisplayString() == "double");

        public static bool IsArrayTypeSymbol(this ITypeSymbol t) => t != null && t.GetType().Name == "ArrayTypeSymbol";

        public static bool IsArrayTypeKind(this ITypeSymbol t) => t != null && t.TypeKind == TypeKind.Array;

        public static bool IsEnum(this ITypeSymbol t) => t != null && t.SpecialType == SpecialType.System_Enum;

        public static bool IsUserStruct(this ITypeSymbol t) => t != null && t.SpecialType == SpecialType.None && t.IsValueType;

        public static ITypeSymbol GetRootType(this ITypeSymbol t) => t.BaseType == null || t.BaseType.IsObject() ? t : GetRootType(t.BaseType);

        public static ITypeSymbol GetDeclaringType(this MethodDeclarationSyntax node, SemanticModel model) => model.GetDeclaredSymbol(node.Parent) as ITypeSymbol;

        public static ITypeSymbol GetDeclaringType(this AccessorDeclarationSyntax node, SemanticModel model) => model.GetDeclaredSymbol(node.Parent.Parent.Parent) as ITypeSymbol;

        public static bool IsAttribute(this ITypeSymbol t) => t != null && (t.ToDisplayString() == "System.Attribute" || t.GetRootType().ToDisplayString() == "System.Attribute");

        public static bool IsSmartContract(this ITypeSymbol t) => t != null && (t.ToDisplayString() == "Stratis.SmartContracts.SmartContract" || (t.GetRootType().ToDisplayString() == "Stratis.SmartContracts.SmartContract"));

        public static bool IsPrimitiveType(this ITypeSymbol t) => Validator.PrimitiveTypeNames.Contains(t.ToDisplayString());

        public static bool IsStratisType(this ITypeSymbol t) => Validator.SmartContractTypeNames.Contains(t.ToDisplayString());

        public static bool IsWhitelistedMethodReturnType(this ITypeSymbol t) => Validator.WhitelistedMethodReturnTypeNames.Contains(t.ToDisplayString());

        public static bool IsWhitelistedArrayType(this ITypeSymbol t)
        {
            var elementtype = t.IsArrayTypeKind() ? ((IArrayTypeSymbol) t).ElementType : null;
            return t.IsArrayTypeKind() && (elementtype.IsUserStruct() || elementtype.IsObject() || elementtype.IsPrimitiveType() || elementtype.IsStratisType() || (elementtype.IsArrayTypeKind() && IsWhitelistedArrayType(elementtype)));
        }

        public static bool IsWhitelistedMethodName(this ITypeSymbol t, string methodname) => 
            Validator.WhitelistedMethodNames.ContainsKey(t.ToDisplayString()) && Validator.WhitelistedMethodNames[t.ToDisplayString()].Contains(methodname);

        public static bool IsWhitelistedPropertyName(this ITypeSymbol t, string propname) => 
            Validator.WhitelistedPropertyNames.ContainsKey(t.ToDisplayString()) && Validator.WhitelistedPropertyNames[t.ToDisplayString()].Contains(propname);

        public static bool IsSmartContractMethod(this MethodDeclarationSyntax node, SemanticModel model) => node.GetDeclaringType(model).IsSmartContract();

        public static bool IsSmartContractProperty(this AccessorDeclarationSyntax node, SemanticModel model) => node.GetDeclaringType(model).IsSmartContract();

        public static FileLinePositionSpan GetLineLocation(this SyntaxNode s) => s.GetLocation().GetLineSpan();

        public static string TypeDeclLabel(this SyntaxNode node)
        {
            if (node.Kind() == SyntaxKind.ClassDeclaration)
            {
                return "class";
            }
            else if (node.Kind() == SyntaxKind.StructDeclaration)
            {
                return "struct";
            }
            else if (node.Kind() == SyntaxKind.InterfaceDeclaration)
            {
                return "interface";
            }
            else throw new ArgumentException($"The node has kind {node.Kind()} and is not a class declaration or struct declaration or interface declaration.");
        }

        public static Diagnostic Report(this Diagnostic diagnostic, SyntaxNodeAnalysisContext ctx)
        {
            if (diagnostic != null) ctx.ReportDiagnostic(diagnostic);
            return diagnostic;
        }

        public static Diagnostic Report(this Diagnostic diagnostic, OperationAnalysisContext ctx)
        {
            if (diagnostic != null) ctx.ReportDiagnostic(diagnostic);
            return diagnostic;
        }
    }
}
