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
        public SolidityFileParser(string filepath, bool parse = true)
        {
            AntlrInputStream inputStream = new AntlrInputStream(File.ReadAllText(filepath));
            SolidityLexer speakLexer = new SolidityLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(speakLexer);
            parser = new SolidityParser(commonTokenStream);
            if (parse) Parse();
        }

        public void Parse() => Antlr4.Runtime.Tree.ParseTreeWalker.Default.Walk(this, parser.sourceUnit());

        public override void EnterContractDefinition([NotNull] SolidityParser.ContractDefinitionContext context)
        { 
            var contractName = context.identifier().GetText();
            contractNames.Add(contractName);
            currentContract = contractName;
            base.EnterContractDefinition(context);
        }
        
        public override void EnterFunctionDefinition([NotNull] SolidityParser.FunctionDefinitionContext context)
        {
            // Check if this is a constructor
            // Solidity 0.4.x: constructor is named as contract
            // Solidity 0.5.x+: uses 'constructor' keyword
            var isConstructor = context.GetText().StartsWith("constructor") || context.GetText().StartsWith(currentContract);
                                   
            if (isConstructor)
            {
                var paramList = context.parameterList();
                if (paramList != null)
                {
                    var parameters = new Dictionary<string, string>();
                    foreach (var param in paramList.parameter())
                    {
                        var type = param.typeName()?.GetText() ?? "";
                        var name = param.identifier()?.GetText() ?? "";
                        parameters.Add(name, type);
                    }
                    constructorParameters[currentContract] = parameters;
                }
                else
                {
                    constructorParameters[currentContract] = new Dictionary<string, string>();
                }
            }

            base.EnterFunctionDefinition(context);
        }

       
        public List<string> contractNames = new List<string>();
        public List<RecognitionException> exceptions = new List<RecognitionException>();
        protected SolidityParser parser;

        // Dictionary: contract name -> list of (type, name) tuples for constructor parameters
        public Dictionary<string, Dictionary<string, string>> constructorParameters = new Dictionary<string, Dictionary<string, string>>();

        // Track the current contract being parsed
        private string currentContract = null;
    }
}
