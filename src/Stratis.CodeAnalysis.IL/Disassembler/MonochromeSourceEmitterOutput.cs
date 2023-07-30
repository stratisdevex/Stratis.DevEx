using System.Drawing;
using CSharpSourceEmitter;

namespace Stratis.DevEx.CodeAnalysis.IL
{
    public class MonochromeSourceEmitterOutput : SourceEmitterOutputString, IColorfulSourceEmitterOutput
    {
        #region Constructors
        public MonochromeSourceEmitterOutput(int indentSize) : base(indentSize) { }

        public MonochromeSourceEmitterOutput() : this(4) { }
        #endregion

        #region Methods
        public virtual void WriteLine(string str, bool fIndent, Color color) => base.WriteLine(str, fIndent);

        public virtual void WriteLine(string str, Color color) => this.WriteLine(str, false, color);

        public virtual void Write(string str, bool fIndent, Color color) => base.Write(str, fIndent);

        public virtual void Write(string str, Color color) => this.Write(str, false, color);
        #endregion

        #region Overriden members
        public override void WriteLine(string str, bool fIndent) => this.WriteLine(str, fIndent, Color.White);

        public override void WriteLine(string str) => this.WriteLine(str, false);

        public override void Write(string str, bool fIndent) => this.Write(str, fIndent, Color.White);

        public override void Write(string str) => this.Write(str, false);
        #endregion
    }
}







