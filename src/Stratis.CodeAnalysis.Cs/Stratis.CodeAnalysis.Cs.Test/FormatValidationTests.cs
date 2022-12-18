using System.Collections.Immutable;
using System.Threading.Tasks;
using System.IO;

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
            var code = @"
            namespace ns1
            {
                using Stratis.SmartContracts;
                public class Player : SmartContract
                {
                    public Player(ISmartContractState state, Address player, Address opponent, string gameName)
                        : base(state)
                    {
           
                    }
                }
            }";
            //var expected = VerifyCS.Diagnostic("StratisCodeAnalysisCs").WithLocation(0).WithArguments("TypeName");
            var test = new VerifyCS.Test
            {
                TestCode = code,    
            };
            test.TestState.AdditionalReferences.Add(typeof(Stratis.SmartContracts.SmartContract).Assembly);
            var path = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            var exists = File.Exists(Path.Combine(path, "Stratis.SmartContracts.dll"));
            //test.ReferenceAssemblies = test.//ReferenceAssemblies.aAddAssemblies(ImmutableArray.Create<string>(Path.Combine(path, "Stratis.SmartContracts.dll") ));
            await test.RunAsync();
        }
    }
}
