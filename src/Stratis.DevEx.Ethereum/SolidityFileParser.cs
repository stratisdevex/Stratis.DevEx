using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Stratis.DevEx.Ethereum
{
    public class SolidityFileParser : SolidityBaseListener
    {
        public SolidityFileParser(string filepath, bool parse = true) {
            AntlrInputStream inputStream = new AntlrInputStream(File.ReadAllText(filepath));
            SolidityLexer speakLexer = new SolidityLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(speakLexer);
            parser = new SolidityParser(commonTokenStream);
            if (parse) Parse();
        }

        public void Parse() => Antlr4.Runtime.Tree.ParseTreeWalker.Default.Walk(this, parser.sourceUnit());
              
        public override void ExitContractDefinition([NotNull] SolidityParser.ContractDefinitionContext context)
        {
            contractNames.Add(context.identifier().GetText());  
        }

        public List<string> contractNames = new List<string>();
        public List<RecognitionException> exceptions = new List<RecognitionException>();
        protected SolidityParser parser;
    }
}
