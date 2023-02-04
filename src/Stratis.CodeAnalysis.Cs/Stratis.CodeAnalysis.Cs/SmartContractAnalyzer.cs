namespace Stratis.CodeAnalysis.Cs
{
    using System;
    using System.Diagnostics;
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
            
            if (!Debugger.IsAttached) context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            
            #pragma warning disable RS1012
            context.RegisterCompilationStartAction(ctx =>
            {
                Runtime.Debug("Compilation start..."); 
                //Runtime.Debug("Project additional files: {0}.", ctx.Options.AdditionalFiles.Select(f => f.Path));
                if (ctx.Options.AdditionalFiles != null && ctx.Options.AdditionalFiles.Any(f => f.Path == "stratisdev.cfg"))
                {
                    var cfgFile = ctx.Options.AdditionalFiles.First(f => f.Path == "stratisdev.cfg").Path;
                    //Runtime.Info("Loading analyzer configuration from {0}...", cfgFile);
                    //var cfg = Runtime.LoadConfig(cfgFile);
                    //Runtime.BindConfig(Runtime.GlobalConfig, cfg);
                }
                
            });
            #pragma warning restore RS1012
            
            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeUsingDirective((UsingDirectiveSyntax)ctx.Node, ctx), SyntaxKind.UsingDirective);
            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeNamespaceDecl((NamespaceDeclarationSyntax)ctx.Node, ctx), SyntaxKind.NamespaceDeclaration);
            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeClassDecl((ClassDeclarationSyntax)ctx.Node, ctx), SyntaxKind.ClassDeclaration);
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
    }
}
