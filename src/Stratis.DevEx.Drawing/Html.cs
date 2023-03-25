using System;
using System.Text;
using Microsoft.Msagl.Drawing;

namespace Stratis.DevEx.Drawing
{
    public class Html : Runtime
    {
        public static string DrawControlFlowGraph(Graph graph)
        {
            var op = Begin("Drawing control-flow graph of {0} nodes and {1} edges to HTML", graph.NodeCount, graph.EdgeCount);
            var network = VisJS.Draw(graph);
            var stringBuilder = new StringBuilder();
            var divId = Guid.NewGuid().ToString("N");
            stringBuilder.AppendLine("<html lang=\"en\"><head><script type=\"text/javascript\" src=\"https://unpkg.com/vis-network/standalone/umd/vis-network.min.js\"></script><title>Title</title><body>");
            stringBuilder.AppendLine($"<div id=\"{divId}\" style=\"height:{network.Height}; width:{network.Width}\"></div>");
            stringBuilder.AppendLine("</div>");
            stringBuilder.AppendLine("<script type=\"text/javascript\">");
            stringBuilder.AppendLine($@"
        let container = document.getElementById('{divId}');
        let data = {{
                        nodes: new vis.DataSet({network.SerializeNodes()}), 
                        edges: new vis.DataSet({network.SerializeEdges()})
                    }};
        let options = {network.SerializeOptions()};
        let network = new vis.Network(container, data, options); 
        ");
            stringBuilder.AppendLine("</script>");
            stringBuilder.AppendLine("</body></html>");
            op.Complete();
            return stringBuilder.ToString();
        }
    }
}
