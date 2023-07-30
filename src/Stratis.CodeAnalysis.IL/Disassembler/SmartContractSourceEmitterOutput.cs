using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Stratis.DevEx.CodeAnalysis.IL
{
    public class SmartContractSourceEmitterOutput : MonochromeSourceEmitterOutput
    {
        #region Constructors
        public SmartContractSourceEmitterOutput(int indentSize) : base(indentSize) { }

        public SmartContractSourceEmitterOutput() : this(4) { }
        #endregion

        #region Fields
        protected Dictionary<string, StringBuilder> classOutput = new Dictionary<string, StringBuilder>();
        public string currentClass = "";

        #endregion

        #region Methods
        public void SetCurrentClass(string name)
        {
            if (!this.classOutput.ContainsKey(name))
            {
                this.classOutput[name] = new StringBuilder();
            }
            currentClass = name;
        }
        
        #endregion

        #region Overriden methods
        public override void Write(string str, bool fIndent, Color color)
        {
            base.Write(str, fIndent, color);
            if (currentClass != string.Empty)
            {
                if (fIndent)
                {
                    classOutput[currentClass].Append(strIndent);
                }
                classOutput[currentClass].Append(str);
            }
        }

        public override void Write(string str, Color color)
        {
            this.Write(str, false, color);
        }

        public override void WriteLine(string str, bool fIndent, Color color)
        {
            base.WriteLine(str, fIndent, color);
            if (currentClass != string.Empty)
            {
                if (fIndent)
                {
                    classOutput[currentClass].AppendLine(strIndent);
                }
                classOutput[currentClass].AppendLine(str);
            }
        }

        public override void WriteLine(string str, Color color)
        {
            this.WriteLine(str, false, color);
        }


        #endregion


    }
}
