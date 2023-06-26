﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Threading;
using System.Threading.Tasks;

namespace Stratis.CodeAnalysis.Cs.Test
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic()"/>
        public static DiagnosticResult Diagnostic()
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, MSTestVerifier>.Diagnostic();

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(string)"/>
        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, MSTestVerifier>.Diagnostic(diagnosticId);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(DiagnosticDescriptor)"/>
        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, MSTestVerifier>.Diagnostic(descriptor);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
        public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestCode = source,
            };
            test.TestState.AdditionalReferences.Add(typeof(Stratis.SmartContracts.SmartContract).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Stratis.SmartContracts.Standards.IStandardToken).Assembly);
            //test.TestState.AdditionalReferences.Add(typeof(Stratis.SmartContracts.Standards.IStandardToken256).Assembly);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        public static async Task VerifyAnalyzerAsync(string source, System.Reflection.Assembly[] references, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestCode = source,
            };
            test.TestState.AdditionalReferences.Add(typeof(Stratis.SmartContracts.SmartContract).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Stratis.SmartContracts.Standards.IStandardToken).Assembly);
            foreach (var r in references)
            {
                test.TestState.AdditionalReferences.Add(r);
            }
            //test.TestState.AdditionalReferences.Add(typeof(Stratis.SmartContracts.Standards.IStandardToken256).Assembly);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        public static async Task VerifyAnalyzerAsync(string source, string[] sources, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestCode = source,
            };
            test.TestState.AdditionalReferences.Add(typeof(Stratis.SmartContracts.SmartContract).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Stratis.SmartContracts.Standards.IStandardToken).Assembly);
            foreach (var s in sources)
            {
                test.TestState.Sources.Add(s);
            }
            //test.TestState.AdditionalReferences.Add(typeof(Stratis.SmartContracts.Standards.IStandardToken256).Assembly);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, string)"/>
        public static async Task VerifyCodeFixAsync(string source, string fixedSource)
            => await VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult, string)"/>
        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
            => await VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult[], string)"/>
        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
        {
            var test = new Test
            {
                TestCode = source,
                FixedCode = fixedSource,
            };
            test.TestState.AdditionalReferences.Add(typeof(Stratis.SmartContracts.SmartContract).Assembly);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }
    }
}
