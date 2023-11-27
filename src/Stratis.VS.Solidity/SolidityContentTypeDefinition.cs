using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace MockLanguageExtension
{
#pragma warning disable 649
    public class SolidityContentDefinition
    {
        [Export]
        [Name("solidity")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition SolidityContentTypeDefinition;


        [Export]
        [FileExtension(".sol")]
        [ContentType("solidity")]
        internal static FileExtensionToContentTypeDefinition SolidityFileExtensionDefinition;
    }
#pragma warning restore 649
}
