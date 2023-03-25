using System;
using System.IO;
using System.Text;
using Microsoft.Msagl.Drawing;

namespace Stratis.DevEx.Drawing
{
    public class Graphs : Runtime
    {
        public static void Draw(Graph graph, string filename, GraphFormat format, int width = 2000, int height = 2000, double rotateBy = 0.0)
        {
            WarnIfFileExists(filename);
            switch (format)
            {
                case GraphFormat.BMP:
                case GraphFormat.PNG:
                    Error("Writing PNG and BMP formats are not supported.");
                    return;

                case GraphFormat.DOT:
                    File.WriteAllText(filename, DOTWriter.Write(graph));
                    break;

                case GraphFormat.DGML:
                    using (var fdgml = new FileStream(filename, FileMode.Create))
                    {
                        DGMLWriter.Write(fdgml, graph);
                    }
                    break;

                case GraphFormat.SVG:
                    var svg = AGL.Drawing.Drawing.DrawSvg(graph, width, height);
                    File.WriteAllText(filename, svg);
                    break;

                case GraphFormat.XML:
                    using (var fxml = new FileStream(filename, FileMode.Create))
                    {
                        var xmlWriter = new GraphWriter(fxml, graph);
                        xmlWriter.Write();
                    }
                    break;
            }
            Info("Saved graph to {0} file {1}.", format, filename);
        }
    }
}
