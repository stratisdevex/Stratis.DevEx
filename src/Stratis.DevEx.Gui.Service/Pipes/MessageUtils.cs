using System;
using System.Collections.Generic;
using System.Text;

namespace Stratis.DevEx.Pipes
{
    public class MessageUtils
    {
        public static string PrettyPrint(Message m)
        {
            var n = Environment.NewLine;
            return $"{{{n}\tCompilation ID: {m.CompilationId}\n\tEditor Entry Assembly:{m.EditorEntryAssembly}\n\tAssemblyName:{m.AssemblyName}{n}\tDocuments:{m.Documents.JoinWithSpaces()}{n}}}";
        }
    }
}
