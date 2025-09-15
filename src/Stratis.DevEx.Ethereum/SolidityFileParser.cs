using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Stratis.DevEx.Ethereum
{
    public partial class SolidityFileParser : SolidityBaseListener
    {
        public SolidityFileParser(string filepath) {
            AntlrInputStream inputStream = new AntlrInputStream(File.ReadAllText(filepath));
            SolidityLexer speakLexer = new SolidityLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(speakLexer);
            parser = new SolidityParser(commonTokenStream);
            parser.AddParseListener(this);
            
        }

        public void Parse()
        {
            var walker = new Antlr4.Runtime.Tree.ParseTreeWalker();
            walker.Walk(this, parser.sourceUnit());
           
        }
      
        public override void ExitContractDefinition([NotNull] SolidityParser.ContractDefinitionContext context)
        {
            base.ExitContractDefinition(context);
            contractNames.Add(context.identifier().GetText());  
        }

        public List<string> contractNames = new List<string>();
        protected SolidityParser parser;
    }
}
