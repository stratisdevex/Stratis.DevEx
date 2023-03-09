namespace Stratis.CodeAnalysis.Cs
{
    using System;
    using System.Diagnostics;
    using System.Collections.Immutable;
    using System.Linq;
    
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Operations;

    using SharpConfig;

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
            attrCount = 0;
            context.RegisterCompilationStartAction(ctx =>
            {
                Runtime.Debug("Compilation start...");
                var cfg = CreateDefaultAnalyzerConfig();
                if (ctx.Options.AdditionalFiles != null && ctx.Options.AdditionalFiles.Any(f => f.Path.EndsWith("stratisdev.cfg")))
                {
                    var cfgFile = ctx.Options.AdditionalFiles.First(f => f.Path.EndsWith("stratisdev.cfg")).Path;
                    Runtime.Info("Loading analyzer configuration from {0}...", cfgFile);
                    cfg = Runtime.LoadConfig(cfgFile);
                }
                else
                {
                    Runtime.Info("No analyzer configuration file found, using default configuration...");
                }
                if (!Validator.CompilationConfiguration.ContainsKey(ctx.Compilation))
                {
                    Validator.CompilationConfiguration.Add(ctx.Compilation, cfg);
                }
                else
                {
                    Validator.CompilationConfiguration[ctx.Compilation] = cfg;
                }

                if (AnalyzerSetting(ctx.Compilation, "Analyzer", "Enabled", true))
                {
                    #region Smart contract validation
                    //ctx.RegisterCompilationAction(ctx => Validator.AnalyzeCompilation(ctx.Compilation).ForEach(d => ctx.ReportDiagnostic(d)));
                    ctx.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeUsingDirective((UsingDirectiveSyntax)ctx.Node, ctx), SyntaxKind.UsingDirective);
                    ctx.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeNamespaceDecl((NamespaceDeclarationSyntax)ctx.Node, ctx), SyntaxKind.NamespaceDeclaration);
                    ctx.RegisterSyntaxNodeAction(ctx => AnalyzeClassDecl((ClassDeclarationSyntax)ctx.Node, ctx), SyntaxKind.ClassDeclaration);
                    ctx.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeStructDecl((StructDeclarationSyntax)ctx.Node, ctx), SyntaxKind.StructDeclaration);
                    ctx.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeConstructorDecl((ConstructorDeclarationSyntax)ctx.Node, ctx), SyntaxKind.ConstructorDeclaration);
                    ctx.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeFieldDecl((FieldDeclarationSyntax)ctx.Node, ctx), SyntaxKind.FieldDeclaration);
                    ctx.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeMethodDecl((MethodDeclarationSyntax)ctx.Node, ctx), SyntaxKind.MethodDeclaration);
                    ctx.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeDestructorDecl((DestructorDeclarationSyntax)ctx.Node, ctx), SyntaxKind.DestructorDeclaration);
                    ctx.RegisterSyntaxNodeAction(ctx => Validator.AnalyzeTryStmt((TryStatementSyntax)ctx.Node, ctx), SyntaxKind.TryStatement);

                    ctx.RegisterOperationAction(cctx =>
                    {
                        switch (cctx.Operation)
                        {
                            case IObjectCreationOperation objectCreation:
                                Validator.AnalyzeObjectCreation(objectCreation, cctx);
                                break;

                            case IPropertyReferenceOperation propReference:
                                Validator.AnalyzePropertyReference(propReference, cctx);
                                break;

                            case IInvocationOperation methodInvocation:
                                Validator.AnalyzeMethodInvocation(methodInvocation, cctx);
                                Validator.AnalyzeAssertConditionConstant(methodInvocation, cctx);
                                Validator.AnalyzeAssertMessageNotProvided(methodInvocation, cctx);
                                Validator.AnalyzeAssertMessageEmpty(methodInvocation, cctx);
                                break;

                            case IVariableDeclaratorOperation variableDeclarator:
                                Validator.AnalyzeVariableDeclaration(variableDeclarator, cctx);
                                break;
                        }
                    }, OperationKind.ObjectCreation, OperationKind.Invocation, OperationKind.PropertyReference, OperationKind.VariableDeclarator);
                    #endregion
                }
                else
                {
                    Runtime.Info("Analyzer disabled in configuration file...not registering analyzer actions.");
                }
            });
        }
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

        #region Configuration
        public static Configuration CreateDefaultAnalyzerConfig()
        {
            var cfg = new Configuration();
            var analyzer = cfg.Add("Analyzer");
            analyzer.Add("Debug", false);
            return cfg;
        }

        public static T AnalyzerSetting<T>(Compilation c, string section, string setting, T defaultval, bool setDefault = false)
        {
            var cfg = Validator.CompilationConfiguration[c];
            return cfg[section][setting].IsEmpty ? defaultval! : cfg[section][setting].GetValueOrDefault(defaultval!, setDefault);
        }
        #endregion
        #endregion

        #region Fields
        internal int attrCount = 0;
        #endregion
    }
}
