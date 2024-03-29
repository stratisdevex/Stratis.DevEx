﻿using System;
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
            stringBuilder.AppendLine($"<div id=\"{divId}\" style=\"height:100%; width:100%\"></div>");
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

        public static string DrawCallGraph(Graph graph)
        {
            var op = Begin("Drawing call graph of {0} nodes and {1} edges to HTML", graph.NodeCount, graph.EdgeCount);
            var network = VisJS.Draw(graph);
            var stringBuilder = new StringBuilder();
            var divId = Guid.NewGuid().ToString("N");
            stringBuilder.AppendLine("<html lang=\"en\"><head><script type=\"text/javascript\" src=\"https://unpkg.com/vis-network/standalone/umd/vis-network.min.js\"></script><title>Title</title><body>");
            stringBuilder.AppendLine($"<div id=\"{divId}\" style=\"height:100%; width:100%\"></div>");
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
        public static string DrawSummary(string summary)
        {
            var op = Begin("Drawing summary to HTML");
            var stringBuilder = new StringBuilder();
            var divId = Guid.NewGuid().ToString("N");
            stringBuilder.AppendLine("<html lang=\"en\"><head><script src=\"https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js\"></script><title>Title</title></head><body>");
            stringBuilder.AppendLine($"<pre id=\"{divId}\" class=\"mermaid\" style=\"height:100%; width:100%\">");
            stringBuilder.AppendLine(summary);
            stringBuilder.AppendLine("</pre>");
            stringBuilder.AppendLine("<script type=\"text/JavaScript\">");
            stringBuilder.AppendLine("mermaid.initialize({ startOnLoad: true });");
            stringBuilder.AppendLine("</script>");
            //stringBuilder.AppendLine("<script type=\"module\">");
            //stringBuilder.AppendLine("import mermaid from \"https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.esm.min.mjs\";");
            //stringBuilder.AppendLine("</script>");
            stringBuilder.AppendLine("</body></html>");
            op.Complete();
            return stringBuilder.ToString();
        }

        public static string DrawDisassembly(string il)
        {
            var op = Begin("Drawing disassembly");
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("<html lang=\"en\"><head><title>Title</title><link href=\"https://unpkg.com/prismjs@1.29.0/themes/prism.min.css\" rel=\"stylesheet\" /></head><body>");
            stringBuilder.AppendLine("<script src=\"https://unpkg.com/prismjs@1.29.0/components/prism-core.min.js\"></script>");
            stringBuilder.AppendLine("<script src=\"https://unpkg.com/prismjs@1.29.0/plugins/autoloader/prism-autoloader.min.js\"></script>");
            stringBuilder.AppendLine($"<pre><code class=\"language-cil\">{il}</code></pre>");
            stringBuilder.AppendLine("</body>");
            op.Complete();
            return stringBuilder.ToString();
        }
    }   
}

