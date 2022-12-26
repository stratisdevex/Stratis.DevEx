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
    using System.IO;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SmartContractAnalyzer : DiagnosticAnalyzer
    {
        #region Overriden members
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = Validator.Diagnostics;
        
        public override void Initialize(AnalysisContext context)
        {
            Runtime.Initialize("ROSLYN", Runtime.StratisDevDir.CombinePath("Stratis.CodeAnalysis.Cs.log"));
            Runtime.Info("Stratis.CodeAnalysis analyzer initializing...");

            if (!Debugger.IsAttached) context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(ctx =>
            {
                Runtime.Info("Compilation start...");
                /*
                var workspace = Runtime.GetProp(ctx.Compilation.Options, "Workspace");
                if (workspace != null) 
                {
                    var sln = Runtime.GetProp(workspace, "CurrentSolution");
                    var fpath = (string) Runtime.GetProp(sln, "FilePath");
                    Runtime.Info("Solution path is {0}.", fpath);
                    
                }
                else
                {
                    Runtime.Info("Could not get Workspace property");
                }
                */
                Runtime.Debug("Project additional files: {0}.", ctx.Options.AdditionalFiles.Select(f => f.Path));
                if (ctx.Options.AdditionalFiles != null && ctx.Options.AdditionalFiles.Any(f => f.Path == "stratisdev.cfg"))
                {
                    var cfgFile = ctx.Options.AdditionalFiles.First(f => f.Path == "stratisdev.cfg").Path;
                    Runtime.Info("Loading analyzer configuration from {0}...", cfgFile);
                    var cfg = Runtime.LoadConfig(cfgFile);
                    Runtime.BindConfig(Runtime.GlobalConfig, cfg);
                }
                
            });
            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeUsingDirective((UsingDirectiveSyntax)ctx.Node, ctx), SyntaxKind.UsingDirective);
            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeNamespaceDecl((NamespaceDeclarationSyntax)ctx.Node, ctx), SyntaxKind.NamespaceDeclaration);
            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeClassDecl((ClassDeclarationSyntax)ctx.Node, ctx), SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeConstructorDecl((ConstructorDeclarationSyntax)ctx.Node, ctx), SyntaxKind.ConstructorDeclaration);
            context.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeFieldDecl((FieldDeclarationSyntax)ctx.Node, ctx), SyntaxKind.FieldDeclaration);
            
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
