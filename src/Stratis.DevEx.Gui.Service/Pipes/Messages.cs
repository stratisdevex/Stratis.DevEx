using System;

using Microsoft.Msagl.Drawing;

using Stratis.DevEx.Pipes.Formatters;

namespace Stratis.DevEx.Pipes
{
    [Serializable]
    public enum MessageType
    {
        COMPILATION_MESSAGE,
        CONTROL_FLOW_GRAPH_MESSAGE
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
        public long CompilationId { get; set; }
        public string EditorEntryAssembly { get; set; } = "";
        public string AssemblyName { get; set; } = "";
    }

    [Serializable]
    public class CompilationMessage : Message
    {
        public string[] Documents { get; set; } = Array.Empty<string>();
    }

    [Serializable]
    public class ControlFlowGraphMessage : Message
    {
        public string Document { get; set; } = string.Empty;

        public Graph CFG { get; set; } = new Graph();
    }

    public class MessageUtils
    {
        public static BinaryFormatter Formatter { get; } = new BinaryFormatter();

        public static byte[] Serialize(Message m) => Formatter.Serialize(m);

        public static T Deserialize<T>(byte[] b) => Formatter.Deserialize<T>(b) ?? throw new Exception($"Could not deserialize message of length {b.Length}.");

        public static MessagePack Pack(CompilationMessage m) => new MessagePack()
        {
            Type = MessageType.COMPILATION_MESSAGE,
            MessageBytes = Serialize(m)
        };
        
        public static MessagePack Pack(ControlFlowGraphMessage m) => new MessagePack()
        {
            Type = MessageType.CONTROL_FLOW_GRAPH_MESSAGE,
            MessageBytes = Serialize(m)
        };

        public static string PrettyPrint(CompilationMessage m)
        {
            var n = Environment.NewLine;
            return $"{{{n}\tCompilation ID: {m.CompilationId}\n\tEditor Entry Assembly:{m.EditorEntryAssembly}\n\tAssemblyName:{m.AssemblyName}{n}\tDocuments:{m.Documents.JoinWithSpaces()}{n}}}";
        }

        public static string PrettyPrint(ControlFlowGraphMessage m)
        {
            var n = Environment.NewLine;
            return $"{{{n}\tCompilation ID: {m.CompilationId}\n\tEditor Entry Assembly:{m.EditorEntryAssembly}\n\tAssemblyName:{m.AssemblyName}{n}}}";
        }
    }
}
