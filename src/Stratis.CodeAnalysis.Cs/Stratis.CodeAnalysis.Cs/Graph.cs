using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.FlowAnalysis;

using Microsoft.Msagl.Drawing;

using Stratis.DevEx;

namespace Stratis.CodeAnalysis.Cs
{
    public class GraphAnalysis : Runtime
    {
        public Graph Analyze(ControlFlowGraph cfg, IMethodBodyOperation methodBody)
        {
            return new Graph();

            for (int i = 0; i < cfg.Blocks.Length; i++)
            {
                var bb = cfg.Blocks[i];
                //bb.
            }
        }
    }
}
