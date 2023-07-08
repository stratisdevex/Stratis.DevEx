using CSharpSourceEmitter;
using System.Drawing;

namespace Stratis.DevEx.CodeAnalysis.IL
{
    public interface IColorfulSourceEmitterOutput : ISourceEmitterOutput
    {
        void WriteLine(string str, bool fIndent, Color color);
        void WriteLine(string str, Color color);
        void Write(string str, bool fIndent, Color color);
        void Write(string str, Color color);
    }
}

