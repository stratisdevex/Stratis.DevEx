using Eto.Drawing;
using Eto.Forms;
using System;
using System.Threading.Tasks;
using H.Pipes;
namespace Stratis.DevEx.Gui
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            
            new Application(Eto.Platform.Detect).Run(new MainForm());
        }

        static async Task Foo()
        {
            await using var server = new PipeServer<string>("ll");
        }
    }
}
