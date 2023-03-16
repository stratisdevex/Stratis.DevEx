using System.Collections.Generic;
using System.IO;

using CompactJson;

namespace Stratis.DevEx.Drawing.VisJS
{
    public class Network
    {
        [JsonProperty("options"), JsonSuppressDefaultValue]
        public NetworkOptions? Options { get; set; } = new NetworkOptions() { Width = "100%", Height = "600px" };

        [JsonProperty("nodes"), JsonSuppressDefaultValue]
        public List<NetworkNode>? Nodes { get; set; }

        [JsonProperty("edges"), JsonSuppressDefaultValue]
        public List<NetworkEdge>? Edges { get; set; }

        [JsonIgnoreMember]
        public string Width => Options?.Width ?? "100%";

        [JsonIgnoreMember]
        public string Height => Options?.Height ?? "600px";

        public static Network Load(string data) => Serializer.Parse<Network>(data)!;

        public static Network LoadFrom(string f) => Serializer.Parse<Network>(File.ReadAllText(f))!;
    }

    public class NetworkEdge
    {
        [JsonProperty("from"), JsonSuppressDefaultValue]
        public string? From { get; set; }

        [JsonProperty("to"), JsonSuppressDefaultValue]
        public string? To { get; set; }

        [JsonProperty("arrows"), JsonSuppressDefaultValue]
        public string? Arrows { get; set; }

        [JsonProperty("physics"), JsonSuppressDefaultValue]
        public bool? Physics { get; set; }

        [JsonProperty("smooth"), JsonSuppressDefaultValue]
        public NetworkSmooth? Smooth { get; set; }
    }

    public class NetworkFont
    {
        [JsonProperty("face"), JsonSuppressDefaultValue]
        public string? Face { get; set; }

        [JsonProperty("align"), JsonSuppressDefaultValue]
        public string? Align { get; set; }
    }

    public class NetworkHierarchical
    {
        [JsonProperty("enabled"), JsonSuppressDefaultValue]
        public bool? Enabled { get; set; }

        [JsonProperty("sortMethod"), JsonSuppressDefaultValue]
        public string? SortMethod { get; set; }

        [JsonProperty("direction"), JsonSuppressDefaultValue]
        public string? Direction { get; set; }

        [JsonProperty("nodeSpacing"), JsonSuppressDefaultValue]
        public int? NodeSpacing { get; set; }

        [JsonProperty("levelSeparation"), JsonSuppressDefaultValue]
        public int? LevelSeparation { get; set; }
    }

    public class NetworkHierarchicalRepulsion
    {
        [JsonProperty("nodeDistance"), JsonSuppressDefaultValue]
        public int? NodeDistance { get; set; }

        [JsonProperty("avoidOverlap"), JsonSuppressDefaultValue]
        public double? AvoidOverlap { get; set; }
    }

    public class NetworkLayout
    {
        [JsonProperty("improvedLayout"), JsonSuppressDefaultValue]
        public bool? ImprovedLayout { get; set; }

        [JsonProperty("hierarchical"), JsonSuppressDefaultValue]
        public NetworkHierarchical? Hierarchical { get; set; }
    }

    public class NetworkNode
    {
        [JsonProperty("id"), JsonSuppressDefaultValue]
        public string? Id { get; set; }

        [JsonProperty("size"), JsonSuppressDefaultValue]
        public int? Size { get; set; }

        [JsonProperty("label"), JsonSuppressDefaultValue]
        public string? Label { get; set; }

        [JsonProperty("title"), JsonSuppressDefaultValue]
        public string? Title { get; set; }

        [JsonProperty("level"), JsonSuppressDefaultValue]
        public int? Level { get; set; }

        [JsonProperty("color"), JsonSuppressDefaultValue]
        public NetworkColor? Color { get; set; }

        [JsonProperty("shape"), JsonSuppressDefaultValue]
        public string? Shape { get; set; }

        [JsonProperty("font"), JsonSuppressDefaultValue]
        public NetworkFont? Font { get; set; }
    }

    public class NetworkOptions
    {
        [JsonProperty("autoResize"), JsonSuppressDefaultValue]
        public bool? AutoResize { get; set; }

        [JsonProperty("manipulation"), JsonSuppressDefaultValue]
        public bool Manipulation { get; set; }

        [JsonProperty("width"), JsonSuppressDefaultValue]
        public string? Width { get; set; }

        [JsonProperty("height"), JsonSuppressDefaultValue]
        public string? Height { get; set; }

        [JsonProperty("layout"), JsonSuppressDefaultValue]
        public NetworkLayout? Layout { get; set; }

        [JsonProperty("physics"), JsonSuppressDefaultValue]
        public NetworkPhysics? Physics { get; set; }

        [JsonProperty("edges"), JsonSuppressDefaultValue]
        public NetworkEdgesOptions? Edges { get; set; }

        [JsonProperty("interaction"), JsonSuppressDefaultValue]
        public NetworkInteraction? Interaction { get; set; }
    }

    public class NetworkPhysics
    {
        [JsonProperty("hierarchicalRepulsion"), JsonSuppressDefaultValue]
        public NetworkHierarchicalRepulsion? HierarchicalRepulsion { get; set; }
    }

    public class NetworkSmooth
    {
        [JsonProperty("enabled"), JsonSuppressDefaultValue]
        public bool? Enabled { get; set; }

        [JsonProperty("type"), JsonSuppressDefaultValue]
        public string? Type { get; set; }
    }

    public class NetworkEdgeArrows
    {
        [JsonProperty("to"), JsonSuppressDefaultValue]
        public NetworkEdgeTo? To { get; set; }
    }

    public class NetworkEdgesOptions
    {
        [JsonProperty("arrows"), JsonSuppressDefaultValue]
        public NetworkEdgeArrows? Arrows { get; set; }

        [JsonProperty("smooth"), JsonSuppressDefaultValue]
        public NetworkSmooth? Smooth { get; set; }
    }


    public class NetworkEdgeTo
    {
        [JsonProperty("enabled"), JsonSuppressDefaultValue]
        public bool? Enabled { get; set; }
    }


    public class NetworkInteraction
    {
        [JsonProperty("navigationButtons"), JsonSuppressDefaultValue]
        public bool? NavigationButtons { get; set; }

        [JsonProperty("keyboard"), JsonSuppressDefaultValue]
        public NetworkInteractionKeyboard? Keyboard { get; set; }
    }

    public class NetworkInteractionKeyboard
    {
        [JsonProperty("enabled"), JsonSuppressDefaultValue]
        public bool? Enabled { get; set; }

        [JsonProperty("speed"), JsonSuppressDefaultValue]
        public NetworkInteractionKeyboardSpeed? Speed { get; set; }

        [JsonProperty("bindToWindow"), JsonSuppressDefaultValue]
        public bool? BindToWindow { get; set; }
    }


    public class NetworkInteractionKeyboardSpeed
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("zoom")]
        public double Zoom { get; set; }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class NetworkColorHighlight
    {
        [JsonProperty("border"), JsonSuppressDefaultValue]
        public string? Border { get; set; }

        [JsonProperty("background"), JsonSuppressDefaultValue]
        public string? Background { get; set; }
    }

    public class NetworkColorHover
    {
        [JsonProperty("border"), JsonSuppressDefaultValue]
        public string? Border { get; set; }

        [JsonProperty("background"), JsonSuppressDefaultValue]
        public string? Background { get; set; }
    }

    public class NetworkColor
    {
        [JsonProperty("border"), JsonSuppressDefaultValue]
        public string? Border { get; set; }

        [JsonProperty("background"), JsonSuppressDefaultValue]
        public string? Background { get; set; }

        [JsonProperty("highlight"), JsonSuppressDefaultValue]
        public NetworkColorHighlight? Highlight { get; set; }

        [JsonProperty("hover"), JsonSuppressDefaultValue]
        public NetworkColorHover? Hover { get; set; }
    }

}
/*
 * // these are all options in full.
var options = {
  nodes:{
    borderWidth: 1,
    borderWidthSelected: 2,
    brokenImage:undefined,
    chosen: true,
    color: {
      border: '#2B7CE9',
      background: '#97C2FC',
      highlight: {
        border: '#2B7CE9',
        background: '#D2E5FF'
      },
      hover: {
        border: '#2B7CE9',
        background: '#D2E5FF'
      }
    },
    opacity: 1,
    fixed: {
      x:false,
      y:false
    },
    font: {
      color: '#343434',
      size: 14, // px
      face: 'arial',
      background: 'none',
      strokeWidth: 0, // px
      strokeColor: '#ffffff',
      align: 'center',
      multi: false,
      vadjust: 0,
      bold: {
        color: '#343434',
        size: 14, // px
        face: 'arial',
        vadjust: 0,
        mod: 'bold'
      },
      ital: {
        color: '#343434',
        size: 14, // px
        face: 'arial',
        vadjust: 0,
        mod: 'italic',
      },
      boldital: {
        color: '#343434',
        size: 14, // px
        face: 'arial',
        vadjust: 0,
        mod: 'bold italic'
      },
      mono: {
        color: '#343434',
        size: 15, // px
        face: 'courier new',
        vadjust: 2,
        mod: ''
      }
    },
    group: undefined,
    heightConstraint: false,
    hidden: false,
    icon: {
      face: 'FontAwesome',
      code: undefined,
      weight: undefined,
      size: 50,  //50,
      color:'#2B7CE9'
    },
    image: undefined,
    imagePadding: {
      left: 0,
      top: 0,
      bottom: 0,
      right: 0
    },
    label: undefined,
    labelHighlightBold: true,
    level: undefined,
    mass: 1,
    physics: true,
    scaling: {
      min: 10,
      max: 30,
      label: {
        enabled: false,
        min: 14,
        max: 30,
        maxVisible: 30,
        drawThreshold: 5
      },
      customScalingFunction: function (min,max,total,value) {
        if (max === min) {
          return 0.5;
        }
        else {
          let scale = 1 / (max - min);
          return Math.max(0,(value - min)*scale);
        }
      }
    },
    shadow:{
      enabled: false,
      color: 'rgba(0,0,0,0.5)',
      size:10,
      x:5,
      y:5
    },
    shape: 'ellipse',
    shapeProperties: {
      borderDashes: false, // only for borders
      borderRadius: 6,     // only for box shape
      interpolation: false,  // only for image and circularImage shapes
      useImageSize: false,  // only for image and circularImage shapes
      useBorderWithImage: false,  // only for image shape
      coordinateOrigin: 'center'  // only for image and circularImage shapes
    }
    size: 25,
    title: undefined,
    value: undefined,
    widthConstraint: false,
    x: undefined,
    y: undefined
  }
}
*/
