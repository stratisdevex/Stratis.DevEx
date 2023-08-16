using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Stratis.DevEx;
using Stratis.DevEx.Pipes;

namespace Stratis.CodeAnalysis.Cs
{
    public class TypeAnalyzer : Runtime
    {
        public ClassInfo AnalyzeClass(SemanticModel model, ClassDeclarationSyntax c)
        {
            var classInfo = new ClassInfo()
            {
                BaseTypeNames = new List<string>(),
            };
            var classSymbol = model.GetDeclaredSymbol(c);
            var classPath = classSymbol.Name;
            classInfo.Name = classPath;
            if (classSymbol.ContainingNamespace is not null && !string.IsNullOrEmpty(classSymbol.ContainingNamespace.Name))
                classPath = classSymbol.ContainingNamespace.Name + '.' + classSymbol.Name;

            if (c.BaseList != null)
            {
                foreach (SimpleBaseTypeSyntax typ in c.BaseList.Types)
                {
                    var symInfo = model.GetTypeInfo(typ.Type);

                    var baseClassPath = symInfo.Type.Name;
                    if (symInfo.Type.ContainingNamespace != null && !string.IsNullOrEmpty(symInfo.Type.ContainingNamespace.Name))
                        baseClassPath = symInfo.Type.ContainingNamespace.Name + '.' + symInfo.Type.Name;

                    classInfo.BaseTypeNames.Add(baseClassPath);
                }
            }
            return classInfo;
        }
    }
}
