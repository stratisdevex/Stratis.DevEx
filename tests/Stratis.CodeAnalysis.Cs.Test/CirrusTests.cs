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
    }
}
