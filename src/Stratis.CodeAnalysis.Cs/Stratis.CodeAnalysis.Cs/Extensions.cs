using System;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Stratis.CodeAnalysis.Cs
{
    public static class Extensions
    {
        public static bool IsObject(this ITypeSymbol t) => t != null && (t.ToDisplayString() == "System.Object" || t.ToDisplayString() == "object");
        
        public static bool IsArrayTypeSymbol(this ITypeSymbol t) => t != null && t.GetType().Name == "ArrayTypeSymbol";

        public static bool IsArrayTypeKind(this ITypeSymbol t) => t != null && t.TypeKind == TypeKind.Array;

        public static bool IsEnum(this ITypeSymbol t) => t != null && t.SpecialType == SpecialType.System_Enum;

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
