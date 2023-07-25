using System;
using System.Collections.Generic;
using System.Linq;
using Stratis.DevEx.Pipes.Formatters;

namespace Stratis.DevEx.Pipes
{
    [Serializable]
    public enum MessageType
    {
        COMPILATION,
        CONTROL_FLOW_GRAPH,
        SUMMARY,
        CALL_GRAPH
    }
    
    [Serializable]
    public struct MessagePack
    {
        public MessageType Type;
        public byte[] MessageBytes;
    }

    [Serializable]
    public class Message
    {

        public string ConfigFile { get; set; } = "";
        public long CompilationId { get; set; }
        public string EditorEntryAssembly { get; set; } = "";
        public string AssemblyName { get; set; } = "";
    }

    [Serializable]
    public class CompilationMessage : Message
    {
        public string[] Documents { get; set; } = Array.Empty<string>();

        public byte[] Assembly { get; set; } = Array.Empty<byte>();

        public byte[] Pdb { get; set; } = Array.Empty<byte>();
    }

    [Serializable]
    public struct NodeData
    {
        public string Id { get; set; }

        public string Label { get; set; }
    }

    [Serializable]
    public struct EdgeData
    {
        public string SourceId { get; set; }

        public string TargetId { get; set; }

        public string Label { get; set; }
    }
    
    [Serializable]
    public class ControlFlowGraphMessage : Message
    {
        public string Document { get; set; } = string.Empty;

        public NodeData[] Nodes { get; set; } = Array.Empty<NodeData>();

        public EdgeData[] Edges { get; set; } = Array.Empty<EdgeData>();
    }

    [Serializable]
    public class SummaryMessage : Message
    {
        public string Document { get; set; } = string.Empty;

        public List<Dictionary<string, object>> Implements { get; set; } = new();

        public List<Dictionary<string, object>> Invocations { get; set; } = new();

        public List<Dictionary<string, object>> Inherits { get; set; } = new();

        public List<Dictionary<string, object>> ClassCreatedObjects { get; set; } = new();

        public List<Dictionary<string, object>> MethodCreatedObjects { get; set; } = new();

        public string Summary { get; set; } = string.Empty;

        public string[] ClassNames = Array.Empty<string>();
    }

    [Serializable]
    public class CallGraphMessage : Message
    {
        public string Document { get; set; } = string.Empty;

        public NodeData[] Nodes { get; set; } = Array.Empty<NodeData>();

        public EdgeData[] Edges { get; set; } = Array.Empty<EdgeData>();
    }

    [Serializable]
    public class DisassemblyMessage : Message
    {
        public string Document { get; set; } = string.Empty;

        public string[] ClassNames = Array.Empty<string>();
    }

    public class MessageUtils
    {
        public static BinaryFormatter Formatter { get; } = new BinaryFormatter();

        public static byte[] Serialize(Message m) => Formatter.Serialize(m);

        public static T Deserialize<T>(byte[] b) => Formatter.Deserialize<T>(b) ?? throw new Exception($"Could not deserialize message of length {b.Length}.");

        public static MessagePack Pack(CompilationMessage m) => new MessagePack()
        {
            Type = MessageType.COMPILATION,
            MessageBytes = Serialize(m)
        };
        
        public static MessagePack Pack(ControlFlowGraphMessage m) => new MessagePack()
        {
            Type = MessageType.CONTROL_FLOW_GRAPH,
            MessageBytes = Serialize(m)
        };

        public static MessagePack Pack(SummaryMessage m) => new MessagePack()
        {
            Type = MessageType.SUMMARY,
            MessageBytes = Serialize(m)
        };

        public static MessagePack Pack(CallGraphMessage m) => new MessagePack()
        {
            Type = MessageType.CALL_GRAPH,
            MessageBytes = Serialize(m)
        };

        public static string PrettyPrint(CompilationMessage m)
        {
            var n = Environment.NewLine;
            return $"{{{n}\tCompilation ID: {m.CompilationId}\n\tEditor Entry Assembly: {m.EditorEntryAssembly}\n\tAssemblyName: {m.AssemblyName}{n}\tDocuments: {m.Documents.JoinWithSpaces()}{n}\tAssembly: {m.Assembly.Length} bytes{n}\tPdb: {m.Pdb.Length} bytes{n}}}";
        }

        public static string PrettyPrint(SummaryMessage m)
        {
            var n = Environment.NewLine;
            return $"{{{n}\tCompilation ID: {m.CompilationId}{n}\tEditor Entry Assembly: {m.EditorEntryAssembly}{n}" +
                $"\tAssemblyName: {m.AssemblyName}{n}\tDocument: {m.Document}{n}\tSummary: {m.Summary}{n}\tClasses: {m.ClassNames.JoinWithSpaces()}{n}" +
                $"\tInherits: {"{" + m.Inherits.Select(d => (string)d["class"] + "<:" + (string)d["base"]).JoinWith(", ") + "}"}{n}" +
                $"\tInvocations: {"{" + m.Invocations.Select(d =>(string) d["name"] + " in " + (string)d["method"]).JoinWith(", ") + "}"}{n}" +
                $"\tClass Created Objects: {"{" + m.ClassCreatedObjects.Select(d => (string)d["class"] + " creates " + (string)d["creates"]).JoinWith(", ") + "}"}{n}" +
                $"}}";
        }

        public static string PrettyPrint(ControlFlowGraphMessage m)
        {
            var n = Environment.NewLine;
            return $"{{{n}\tCompilation ID: {m.CompilationId}{n}\tEditor Entry Assembly: {m.EditorEntryAssembly}{n}\tAssemblyName: {m.AssemblyName}{n}\tDocument: {m.Document}{n}\tControl-Flow Graph: {m.Nodes.Length} nodes, {m.Edges.Length} edges{n}}}";
        }

        public static string PrettyPrint(CallGraphMessage m)
        {
            var n = Environment.NewLine;
            return $"{{{n}\tCompilation ID: {m.CompilationId}{n}\tEditor Entry Assembly: {m.EditorEntryAssembly}{n}\tAssemblyName: {m.AssemblyName}{n}\tDocument: {m.Document}{n}\tCall Graph: {m.Nodes.Length} nodes, {m.Edges.Length} edges{n}}}";
        }
    }
}
