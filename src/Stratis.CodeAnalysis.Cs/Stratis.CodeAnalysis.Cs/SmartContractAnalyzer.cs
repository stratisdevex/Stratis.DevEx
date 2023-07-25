namespace Stratis.CodeAnalysis.Cs
{
    using System;
    using System.Diagnostics;
    using System.Collections.Concurrent;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Emit;
    using Microsoft.CodeAnalysis.Operations;
    using Microsoft.CodeAnalysis.FlowAnalysis;

    using SharpConfig;

    using Stratis.DevEx;
    using Stratis.DevEx.Pipes;

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
            context.RegisterCompilationAction(ctx =>
            {
                Debug("Compilation end.");
                var cfg = CreateDefaultAnalyzerConfig();
                string cfgFile = "";
                if (ctx.Options.AdditionalFiles != null && ctx.Options.AdditionalFiles.Any(f => f.Path.EndsWith("stratisdev.cfg")))
                {
                    cfgFile = ctx.Options.AdditionalFiles.First(f => f.Path.EndsWith("stratisdev.cfg")).Path;
                    cfg = Runtime.LoadConfig(cfgFile);
                    cfg["General"]["ConfigFile"].SetValue(cfgFile);
                }
                if (!cfg["Analyzer"]["Enabled"].GetValueOrDefault(true))
                {
                    return;
                }
                if (!ctx.Compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    var pipeClient = Gui.CreatePipeClient();
                    Gui.SendCompilationMessage(ctx.Compilation, pipeClient);
                    pipeClient.Dispose();
                }
            });

            context.RegisterCompilationStartAction(ctx =>
            {
                Debug("Compilation start...");
                var cfg = CreateDefaultAnalyzerConfig();
                string cfgFile = "";
                if (ctx.Options.AdditionalFiles != null && ctx.Options.AdditionalFiles.Any(f => f.Path.EndsWith("stratisdev.cfg")))
                {
                    cfgFile = ctx.Options.AdditionalFiles.First(f => f.Path.EndsWith("stratisdev.cfg")).Path;
                    Info("Loading analyzer configuration from {0}...", cfgFile);
                    cfg = Runtime.LoadConfig(cfgFile);
                    cfg["General"]["ConfigFile"].SetValue(cfgFile);
                }
                else
                {
                    Info("No analyzer configuration file found, using default configuration...");
                }

                if (!cfg["Analyzer"]["Enabled"].GetValueOrDefault(true))
                {
                    Runtime.Info("Analyzer disabled in configuration file...not registering analyzer actions.");
                    return;
                }

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

                #region Class analysis
                ctx.RegisterSemanticModelAction(sma =>
                {
                    if (!sma.SemanticModel.Compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        ModelAnalyzer.Analyze(cfgFile, cfg, sma.SemanticModel);
                    }
                });
                #endregion

                #region Control-flow analysis;
                if (cfg["ControlFlowAnalysis"]["Enabled"].BoolValue)
                {
                    ctx.RegisterSemanticModelAction(sma =>
                    {
                        if (!sma.SemanticModel.Compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                        {
                            GraphAnalyzer.AnalyzeControlFlow(cfgFile, cfg, sma.SemanticModel);
                        }
                        else
                        {
                            Debug("Compilation has errors, not running graph analysis.");
                        }
                    });
                }
                else
                {
                    Info("Control-flow analysis not enabled in analyzer configuration.");
                }

                #endregion
            });
        }
        #endregion

        #region Methods

        #region Logging
        [DebuggerStepThrough]
        public static void Info(string messageTemplate, params object[] args) => Runtime.Info(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Debug(string messageTemplate, params object[] args) => Runtime.Debug(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Error(string messageTemplate, params object[] args) => Runtime.Error(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Error(Exception ex, string messageTemplate, params object[] args) => Runtime.Error(ex, messageTemplate, args);

        [DebuggerStepThrough]
        public static void Warn(string messageTemplate, params object[] args) => Runtime.Warn(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Fatal(string messageTemplate, params object[] args) => Runtime.Fatal(messageTemplate, args);

        [DebuggerStepThrough]
        public static Logger.Op Begin(string messageTemplate, params object[] args) => Runtime.Begin(messageTemplate, args);
        #endregion

        #region Syntactic analysis
        public void AnalyzeClassDecl(ClassDeclarationSyntax node, SyntaxNodeAnalysisContext ctx)
        {
            var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(node) as ITypeSymbol;
            var attrs = node.AttributeLists.Select(al => al.Attributes).SelectMany(x => x);
            Debug("Class {0} with attributes {1} declared at {2}.", classSymbol.ToDisplayString(), attrs.Select(a => a.Name.ToString()).JoinWithSpaces(), node.GetLineLocation());
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
            analyzer.Add("Enabled", true);
            analyzer.Add("Debug", false);
            return cfg;
        }

        public static T AnalyzerSetting<T>(Compilation c, string section, string setting, T defaultval, bool setDefault = false)
        {
            var cfg = CompilationConfiguration[c.GetHashCode()];
            return cfg[section][setting].IsEmpty ? defaultval! : cfg[section][setting].GetValueOrDefault(defaultval!, setDefault);
        }

        public static T AnalyzerSetting<T>(Compilation c, string section, string setting) => AnalyzerSetting<T>(c, section, setting, default(T));
        #endregion

        #endregion

        #region Fields
        internal int attrCount = 0;
        protected PipeClient<MessagePack> pipeClient;
        internal static ConcurrentDictionary<int, Configuration> CompilationConfiguration = new();
        #endregion
    }
}
