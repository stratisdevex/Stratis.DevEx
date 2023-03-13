using System;

namespace Stratis.DevEx.Pipes
{
    [Serializable]
    public struct Message
    {
        public string AssemblyName { get; set; }
        public string[] Files { get; set; }
    }
}
