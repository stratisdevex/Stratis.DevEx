using System.Collections.Immutable;
using System.Threading.Tasks;
using System.IO;

using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = Stratis.CodeAnalysis.Cs.Test.CSharpCodeFixVerifier<
    Stratis.CodeAnalysis.Cs.SmartContractAnalyzer,
    Stratis.CodeAnalysis.Cs.StratisCodeAnalysisCsCodeFixProvider>;

namespace Stratis.CodeAnalysis.Cs.Test
{
    [TestClass]
    public class FormatValidationTests
    {
        [TestMethod]
        public async Task NamespaceDeclNotAllowedTest()
        {
            var code =
@"  namespace ns1 {
        using Stratis.SmartContracts;
        public class Player : SmartContract
        {
            public Player(ISmartContractState state, Address player, Address opponent, string gameName)
                : base(state)
            {
           
            }
        }
    }
";
            await VerifyCS.VerifyAnalyzerAsync(code, VerifyCS.Diagnostic("SC0001").WithSpan(1, 13, 1, 16).WithArguments("ns1"));
        }

        [TestMethod]
        public async Task InvalidVarDeclsNotAllowedTest()
        {
            var code =
@"  using Stratis.SmartContracts;
    public class Player : SmartContract
    {
        public Player(ISmartContractState state, Address player, Address opponent, string gameName)
            : base(state)
        {
           byte[][] v1 = {};
        }
    }
";
            await VerifyCS.VerifyAnalyzerAsync(code, new DiagnosticResult[] { });
        }

        [TestMethod]
        public async Task FieldDeclsNotAllowedTest()
        {
            var code =
@"  using Stratis.SmartContracts;
    public class Player : SmartContract
    {
        public Player(ISmartContractState state, Address player, Address opponent, string gameName)
            : base(state)
        {
           
        }
        public string F;
    }
";
            await VerifyCS.VerifyAnalyzerAsync(code, VerifyCS.Diagnostic("SC0006").WithSpan(9, 9, 9, 25).WithArguments("Player"));
        }

        [TestMethod]
        public async Task InvalidMethodDeclNotAllowedTest()
        {
            var code =
@"  using Stratis.SmartContracts;
    public class Player : SmartContract
    {
        public Player(ISmartContractState state, Address player, Address opponent, string gameName)
            : base(state)
        {
           
        }
        public string[] M() => new string[] {};

        public byte[] B() => new byte[] {};

        protected Address Me() => Address.Zero;
    }
";
            await VerifyCS.VerifyAnalyzerAsync(code, VerifyCS.Diagnostic("SC0014").WithSpan(9, 16, 9, 24).WithArguments("string[]"));
        }


        [TestMethod]
        public async Task InvalidClassTypeDeclNotAllowedTest()
        {
            var code =
    @"  using Stratis.SmartContracts;
    public class Player 
    {
        public Player(ISmartContractState state, Address player, Address opponent, string gameName){}
    }

    public class Player2 : SmartContract
    {
        public Player2(ISmartContractState state, Address player, Address opponent, string gameName)
            : base(state) {}
    }

    public class Player3 : Player2
    {
        public Player3(ISmartContractState state, Address player, Address opponent, string gameName)
            : base(state, player, opponent, gameName) {}
    }
";
            await VerifyCS.VerifyAnalyzerAsync(code, VerifyCS.Diagnostic("SC0003").WithSpan(2, 18, 2, 24).WithArguments("Player"));
        }
    }
}

