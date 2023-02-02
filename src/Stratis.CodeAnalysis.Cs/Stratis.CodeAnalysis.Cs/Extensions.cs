using System;

using Microsoft.CodeAnalysis;
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

        public static bool IsSmartContract(this ITypeSymbol t) => t != null && t.ToDisplayString() == "Stratis.SmartContracts.SmartContract" || (t.BaseType != null && (t.BaseType.ToDisplayString() == "Stratis.SmartContracts.SmartContract"));

        public static FileLinePositionSpan GetLineLocation(this SyntaxNode s) => s.GetLocation().GetLineSpan();
    
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
