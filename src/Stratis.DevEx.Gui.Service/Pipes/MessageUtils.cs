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
            return $"{{{n}\tAssemblyName:{m.AssemblyName}{n}\tFiles:{m.Files.JoinWithSpaces()}{n}}}";
        }
    }
}
