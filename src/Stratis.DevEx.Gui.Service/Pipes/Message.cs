using System;

namespace Stratis.DevEx.Pipes
{
    [Serializable]
    public struct Message
    {
        public long CompilationId { get; set; }
        public string EditorEntryAssembly { get; set; }
        public string AssemblyName { get; set; }
        public string[] Documents { get; set; }
    }
}
