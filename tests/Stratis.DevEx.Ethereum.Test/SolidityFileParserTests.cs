using static Stratis.DevEx.Result;

namespace Stratis.DevEx.Ethereum.Test
{
    public class SolidityFileParserTests
    {
        static string file1 = Path.Combine("..", "..", "..", "..", "solidity", "test2", "3_Ballot.sol");

        [Fact]
        public void CanParseSolidityFile()
        {
            var parser = new SolidityFileParser(file1, false);            
            Assert.NotNull(parser);
            parser.Parse();
            Assert.NotEmpty(parser.contractNames);
            Assert.Contains("Ballot", parser.contractNames);
            Assert.Contains("proposalNames", parser.constructorParameters["Ballot"].Keys); 
            Assert.Equal("bytes32[]", parser.constructorParameters["Ballot"]["proposalNames"]);
        }
    }
}