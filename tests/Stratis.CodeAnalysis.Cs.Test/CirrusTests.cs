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
    public class CirrusTests
    {
        [TestMethod]
        public async Task CanValidateAddressContract()
        {
            var code = File.ReadAllText(Path.Combine("..", "..", "..", "..", "..", "ext", 
                "CirrusSmartContracts", "Mainnet", "AddressMapper", "AddressMapper", "AddressMapper.cs"));
            await VerifyCS.VerifyAnalyzerAsync(code, VerifyCS.Diagnostic("SC0011").WithSpan(27, 9, 27, 69));
        }

        [TestMethod]
        public async Task CanValidateDAOContract()
        {
            var code = File.ReadAllText(Path.Combine("..", "..", "..", "..", "..", "ext",
                "CirrusSmartContracts", "Mainnet", "DAOContract", "DAOContract", "DAOContract.cs"));
            await VerifyCS.VerifyAnalyzerAsync(code);
        }

        [TestMethod]
        public async Task CanValidateIdentityContract()
        {
            var code = File.ReadAllText(Path.Combine("..", "..", "..", "..", "..", "ext",
                "CirrusSmartContracts", "Mainnet", "Identity", "IdentityContracts", "IdentityProvider.cs"));
           
            await VerifyCS.VerifyAnalyzerAsync(code);
        }

        [TestMethod]
        public async Task CanValidateInterFluxStandardTokenContract()
        {
            var code = File.ReadAllText(Path.Combine("..", "..", "..", "..", "..", "ext",
                "CirrusSmartContracts", "Mainnet", "InterFluxStandardToken", "InterFluxStandardToken", "InterFluxStandardToken.cs"));
            var sources = new[] 
            {
                Path.Combine("..", "..", "..", "..", "..", "ext",
                "CirrusSmartContracts", "Mainnet", "InterFluxStandardToken", "InterFluxStandardToken", "IBurnable.cs")
            };
            await VerifyCS.VerifyAnalyzerAsync(code,
                DiagnosticResult.CompilerError("CS0246").WithSpan(8, 73, 8, 81).WithArguments("IOwnable"),
                DiagnosticResult.CompilerError("CS0246").WithSpan(8, 83, 8, 92).WithArguments("IMintable"),
                DiagnosticResult.CompilerError("CS0246").WithSpan(8, 94, 8, 103).WithArguments("IBurnable"),
                DiagnosticResult.CompilerError("CS0246").WithSpan(8, 105, 8, 126).WithArguments("IBurnableWithMetadata"));
        }

        [TestMethod]
        public async Task CanValidateMultisigContract()
        {
            var code = File.ReadAllText(Path.Combine("..", "..", "..", "..", "..", "ext",
                "CirrusSmartContracts", "Mainnet", "Multisig", "Multisig", "MultisigContract.cs"));

            await VerifyCS.VerifyAnalyzerAsync(code, VerifyCS.Diagnostic("SC0010").WithSpan(258, 32, 258, 37).WithArguments("False"));
        }

        [TestMethod]
        public async Task CanValidateNFTAuctionStoreContract()
        {
            var code = File.ReadAllText(Path.Combine("..", "..", "..", "..", "..", "ext",
                "CirrusSmartContracts", "Mainnet", "NFTAuctionStore", "NFTAuctionStore", "NFTAuctionStore.cs"));

            await VerifyCS.VerifyAnalyzerAsync(code, VerifyCS.Diagnostic("SC0011").WithSpan(77, 9, 77, 27));
        }
    }
}
