using Eto.Drawing;
using Eto.Forms;
using System;
using System.Threading.Tasks;
namespace Stratis.DevEx.Gui
{
    class Program : Runtime
    {
        [STAThread]
        static void Main(string[] args)
        {
            
            var app = new Application(Eto.Platform.Detect);
            app.Initialized += (sender, e) => app.AsyncInvoke(async () => await Foo());
            app.Run(new MainForm());
        }

        static async Task Foo()
        {
            await Task.CompletedTask;
        }
    }
}
