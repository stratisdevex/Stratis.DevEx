using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.FlowAnalysis;

using Microsoft.Msagl.Drawing;

using SharpConfig;

using Stratis.DevEx;
namespace Stratis.CodeAnalysis.Cs
{
    public class Disasembler : Runtime
    {
        public static string Disassemble(string cfgFile, Configuration config, SemanticModel model)
        {
            return "";
        }
    }
}