using static Stratis.DevEx.Result;

namespace Stratis.DevEx.Ethereum.Test
{
    public class SolidityFileParserTests
    {
        static string file1 = Path.Combine("..", "..", "..", "..", "solidity", "test2", "3_Ballot.sol");

        [Fact]
        public void CanParseContractNames()
        {
            var parser = new SolidityFileParser(file1);
            
            Assert.NotNull(parser);
            parser.Parse();
            Assert.NotEmpty(parser.contractNames);
            Assert.Contains("Ballot", parser.contractNames);
        }
    }
}