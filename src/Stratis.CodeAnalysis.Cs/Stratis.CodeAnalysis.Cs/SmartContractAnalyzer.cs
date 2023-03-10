namespace Stratis.CodeAnalysis.Cs
{
    using System;
    using System.Diagnostics;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Operations;

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
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
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
                Validator.CompilationConfiguration.AddOrUpdate(ctx.Compilation, cfg, (_,_) => cfg);
                if (AnalyzerSetting(ctx.Compilation, "Analyzer", "Enabled", true))
                {
                    if (AnalyzerSetting(ctx.Compilation, "Gui", "Enabled", false))
                    {
                        Runtime.Info("GUI enabled for compilation, registering action to send compilation message...");
                        ctx.RegisterCompilationEndAction(cctx => SendGuiMessage(cctx.Compilation));
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

        #region Gui
        protected void SendGuiMessage(Compilation c)
        {
            if (!GuiProcessRunning())
            {
                Runtime.Error("Did not detect GUI process running, not sending message.");
                return;
            }
            if (this.pipeClient is null)
            {
                using (var op = Runtime.Begin("Creating GUI pipe client"))
                {
                    try
                    {
                        pipeClient = new PipeClient<Message>("stratis_devexgui") { AutoReconnect = false };
                        op.Complete();
                    }
                    catch (Exception e)
                    {
                        op.Abandon();
                        Runtime.Error(e, "Error creating GUI pipe client.");
                        return;
                    }
                }
            }

            using (var op = Runtime.Begin("Sending compilation message"))
            {
                try
                {
                    var m = new Message()
                    {
                        AssemblyName = c.AssemblyName,
                        Files = c.SyntaxTrees.Select(st => st.FilePath).ToArray()
                    };
                    if (GuiProcessRunning() && !pipeClient.IsConnected)
                    {
                        pipeClient.ConnectAsync().Wait();
                    }
                    if (GuiProcessRunning() && pipeClient.IsConnected)
                    {
                        pipeClient.WriteAsync(m).Wait();
                        op.Complete();
                    }
                    else
                    {
                        op.Abandon();
                        Runtime.Error("GUI is not running or pipe client disconnected. Error sending compilation message to GUI.");
                    }
                }
                catch (Exception e)
                {
                    op.Abandon();
                    Runtime.Error(e, "Error sending compilation message to GUI.");
                }
            }
        }

        protected static bool GuiProcessRunning()
        {
            var f = Runtime.StratisDevDir.CombinePath("Stratis.DevEx.Gui.run");
            if (!File.Exists(f))
            {
                Runtime.Debug("{0} does not exist.", f);
                return false;
            }
            else
            {
                var c = Configuration.LoadFromFile(f);
                var pid = c["Process"]["ProcessId"].GetValueOrDefault(0);
                if (pid == 0)
                {
                    Runtime.Error("Could not read process ID from Stratis.DevEx.Gui.run.");
                    return false;
                }
                else
                {
                    try
                    {
                        var p = Process.GetProcessById(pid);
                        if (p.ProcessName.Contains("Stratis.DevEx.Gui"))
                        {
                            return true;
                        }
                        else
                        {
                            Runtime.Debug("Process {pid} is not Stratis.DevEx.Gui.", pid);
                            return false;
                        }
                    }
                    catch 
                    {
                        Runtime.Debug("Exception thrown getting process id {pid}.", pid);
                        return false;
                    }
                }
            }
        }
        #endregion
        
        #endregion

        #region Fields
        internal int attrCount = 0;
        protected PipeClient<Message> pipeClient;
        #endregion
    }
}
