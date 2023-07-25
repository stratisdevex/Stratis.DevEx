using System;
using System.Collections.Generic;
using System.IO;

using CompactJson;
using DotLiquid;

using Microsoft.Msagl.Drawing;

namespace Stratis.DevEx.Drawing
{
    public class VisJS
    {
        #region Properties
        public static Template HtmlPageTemplate = Template.Parse("<html>\n<head>\n<title>\n{{title}}\n</title>\n<script type=\"text/javascript\" src=\"https://unpkg.com/vis-network/standalone/umd/vis-network.min.js\"></script>\n</head>\n<body>\n</body>\n{{body}}</html>)");
        #endregion

        #region Methods
        public static Network Draw(Graph graph, string width="100%", string height="100%")
        {
            var network = new Network();
            var options = new NetworkOptions();
            options.Width = width;
            options.Height = height;
            string? nodeshape = null;
            int? nodesize = null;
            
            switch (graph.Kind)
            {
                case "cfg":
                case "cg":
                    options.AutoResize = false;
                    var layout = new NetworkLayout()
                    {
                        ImprovedLayout = true,
                        /*
                        Hierarchical = new NetworkHierarchical()
                        {
                            Enabled = true,
                            SortMethod = "directed",
                            Direction = "UD",
                            NodeSpacing = 150,
                            LevelSeparation = 150,
                        }*/
                    };
                    var physics = new NetworkPhysics()
                    {
                        
                        HierarchicalRepulsion = new NetworkHierarchicalRepulsion()
                        {
                            AvoidOverlap = 2.0,
                            NodeDistance = 250,
                        }
                        
                    };
                    var edgesOptions = new NetworkEdgesOptions()
                    {
                        Arrows = new NetworkEdgeArrows() { To = new NetworkEdgeTo() { Enabled = true } },
                        Smooth = new NetworkSmooth() { Enabled = true }
                    };
                    var interaction = new NetworkInteraction()
                    {
                        NavigationButtons = true
                    };
                    options.Layout = layout;
                    options.Physics = physics;
                    options.Edges = edgesOptions;
                    options.Interaction = interaction;
                    nodeshape = "box";
                    nodesize = 300;
                    break;
            }

            List<NetworkNode> nodes = new List<NetworkNode>();
            List<NetworkEdge> edges = new List<NetworkEdge>();
            foreach (var node in graph.Nodes)
            {
                nodes.Add(new NetworkNode()
                {
                    Id = node.Id,
                    Label = node.LabelText,
                    Font = new NetworkFont() { Face = "monospace", Align = "left" },
                    Shape = nodeshape,
                    Size = nodesize,
                    Color = node.Attr.FillColor != Color.Transparent || node.Attr.Color != Color.White ? 
                        new NetworkColor()
                        {
                            Background = node.Attr.FillColor.ToString().Trim('"')
                        } : null
                }); ;
            }
            
            foreach(var edge in graph.Edges)
            {
                edges.Add(new NetworkEdge()
                {
                    From = edge.Source,
                    To = edge.Target,
                });
            }

            network.Nodes = nodes;
            network.Edges = edges;
            network.Options = options;
            return network;
        }

        #endregion
    }
}
