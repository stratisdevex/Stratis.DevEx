namespace Stratis.CodeAnalysis.Cs
{
    using System;
    using System.Diagnostics;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Operations;

    using Stratis.DevEx;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SmartContractAnalyzer : DiagnosticAnalyzer
    {
        #region Overriden members
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = Validator.Diagnostics;
        
        public override void Initialize(AnalysisContext context)
        {
            Runtime.Initialize("Stratis.CodeAnalysis.Cs", "ROSLYN");
            attrCount = 0;
            if (!Debugger.IsAttached) context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            
            #pragma warning disable RS1012, RS1013
            context.RegisterCompilationStartAction(ctx =>
            {
                Runtime.Debug("Compilation start...");
                if (ctx.Options.AdditionalFiles != null && ctx.Options.AdditionalFiles.Any(f => f.Path == "stratisdev.cfg"))
                {
                    var cfgFile = ctx.Options.AdditionalFiles.First(f => f.Path == "stratisdev.cfg").Path;
                    Runtime.Info("Loading analyzer configuration from {0}...", cfgFile);
                    //var cfg = Runtime.LoadConfig(cfgFile);
                    //Runtime.BindConfig(Runtime.GlobalConfig, cfg);
                }
            });
            #pragma warning restore RS1012

            #region Smart contract validation
            context.RegisterCompilationAction(ctx => Validator.AnalyzeCompilation(ctx.Compilation).ForEach(d => ctx.ReportDiagnostic(d)));

            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeUsingDirective((UsingDirectiveSyntax)ctx.Node, ctx), SyntaxKind.UsingDirective);
            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeNamespaceDecl((NamespaceDeclarationSyntax)ctx.Node, ctx), SyntaxKind.NamespaceDeclaration);
            context.RegisterSyntaxNodeAction(ctx => AnalyzeClassDecl((ClassDeclarationSyntax)ctx.Node, ctx), SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeConstructorDecl((ConstructorDeclarationSyntax)ctx.Node, ctx), SyntaxKind.ConstructorDeclaration);
            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeFieldDecl((FieldDeclarationSyntax)ctx.Node, ctx), SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeMethodDecl((MethodDeclarationSyntax)ctx.Node, ctx), SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeDestructorDecl((DestructorDeclarationSyntax)ctx.Node, ctx), SyntaxKind.DestructorDeclaration);
            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeTryStmt((TryStatementSyntax)ctx.Node, ctx), SyntaxKind.TryStatement);
            
            context.RegisterOperationAction(ctx =>
            {
                switch (ctx.Operation)
                {
                    case IObjectCreationOperation objectCreation:
                        Validator.AnalyzeObjectCreation(objectCreation, ctx);
                        break;

                    case IPropertyReferenceOperation propReference:
                        Validator.AnalyzePropertyReference(propReference, ctx);
                        break;

                    case IInvocationOperation methodInvocation:
                        Validator.AnalyzeMethodInvocation(methodInvocation, ctx);
                        Validator.AnalyzeAssertConditionConstant(methodInvocation, ctx);
                        Validator.AnalyzeAssertMessageNotProvided(methodInvocation, ctx);
                        Validator.AnalyzeAssertMessageEmpty(methodInvocation, ctx);
                        break;
                        
                    case IVariableDeclaratorOperation variableDeclarator:
                        Validator.AnalyzeVariableDeclaration(variableDeclarator, ctx);
                        break;
                }
            }, OperationKind.ObjectCreation, OperationKind.Invocation, OperationKind.PropertyReference, OperationKind.VariableDeclarator);
        }
        #endregion

        #endregion

        #region Methods

        #region Syntactic analysis
        public void AnalyzeClassDecl(ClassDeclarationSyntax node, SyntaxNodeAnalysisContext ctx)
        {
            var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(node) as ITypeSymbol;
            var attrs = node.AttributeLists.Select(al => al.Attributes).SelectMany(x => x);
            Runtime.Debug("Class {0} with attributes {1} declared at {2}.", classSymbol.ToDisplayString(), attrs.Select(a => a.Name.ToString()).JoinWithSpaces(), node.GetLineLocation());
            foreach(var a in attrs)
            {
                if (a.Name.ToString() == "Deploy")
                {
                    if (attrCount++ > 0)
                    {
                        ctx.ReportDiagnostic(Validator.CreateDiagnostic("SC0018", a.GetLocation()));
                    }

                }
            }
            Validator.AnalyzeClassDecl(node, ctx);
        }
        #endregion

        #region Diagnostic utilities
        public static Diagnostic Try(Func<Diagnostic> d)
        {
            try
            {
                return d();
            }
            catch (Exception e)
            {
                Runtime.Error(e, "Exception thrown in analysis method.");
                return null;
            }
        }
        #endregion

        #endregion

        #region Fields
        internal int attrCount = 0;
        #endregion
    }
}
