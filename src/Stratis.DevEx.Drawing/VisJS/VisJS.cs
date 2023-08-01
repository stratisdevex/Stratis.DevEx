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
                            LevelSeparation = 50,
                        }
                        */
                    };
                    var physics = new NetworkPhysics()
                    {
                        
                        Enabled = true,
                        HierarchicalRepulsion = new NetworkHierarchicalRepulsion()
                        {
                            AvoidOverlap = 2.0,
                            NodeDistance = 250,
                        },
                        TimeStep = 0.5
                        
                    };
                    var edgesOptions = new NetworkEdgesOptions()
                    {
                        Arrows = new NetworkEdgeArrows() { To = new NetworkEdgeTo() { Enabled = true } },
                        Smooth = new NetworkSmooth() { Enabled = true, Type = "continuous" }
                    };
                    var interaction = new NetworkInteraction()
                    {
                        NavigationButtons = true
                    };
                    options.Layout = layout;
                    options.Physics = physics;
                    options.Edges = edgesOptions;
                    options.Interaction = interaction;
                    options.Nodes = new NetworkNodeOptions()
                    {
                        ShapeProperties = new NetworkShapeProperties()
                        {
                            Interpolation = false
                        }
                    };
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
                    Color = graph.Kind switch 
                            {
                                "cg" => node.Attr.FillColor != Color.Transparent || node.Attr.Color != Color.White ? 
                                        new NetworkColor()
                                        {
                                            Background = node.Attr.FillColor.ToString().Trim('"')
                                        } : null,
                                "cfg" => GetCFGNodeColor(node),
                                _ => null
                            },
                    Mass = graph.Kind switch
                    {
                        "cfg" => node.edgeSourceCount + node.edgeTargetCount switch 
                        {
                            var c when c <= 2 => 1.5,
                            var c when c > 2 && c <= 4 => 2.0, 
                            var c when c > 4 && c <= 6 => 3.0,
                            _ => 4.0
                        },
                        "cg" => node.edgeSourceCount + node.edgeTargetCount switch
                        {
                            var c when c <= 2 => 1.5,
                            var c when c > 2 && c <= 4 => 2.0,
                            var c when c > 4 && c <= 6 => 3.0,
                            _ => 4.0
                        },
                        _ => 1.0
                    }
                }); 
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

        protected static NetworkColor GetCFGNodeColor(Node node)
        {
            var bkgn = (node.Kind) switch
            {
                "entry" => "LightYellow",
                "branch" => "LightBlue",
                _ => "White"
            };
            //if (node.LabelText.StartsWith("SmartContract::") || node.LabelText.StartsWith("IPersistentState::") || node.LabelText.StartsWith("Assert"))
            //{
            //    bkgn = "LightPink";
            //}
            return new NetworkColor()
            {
                Background = bkgn,
                
            };
        }
        #endregion
    }
}
