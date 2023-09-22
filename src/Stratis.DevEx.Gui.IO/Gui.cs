using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using SharpConfig;

using Stratis.DevEx.Pipes;

namespace Stratis.DevEx.Gui.IO
{
    public class Gui : Runtime
    {
        public static bool GuiProcessRunning()
        {
            var f = StratisDevDir.CombinePath("Stratis.DevEx.Gui.run");
            if (!File.Exists(f))
            {
                Debug("{0} does not exist.", f);
                return false;
            }
            else
            {
                var c = Configuration.LoadFromFile(f);
                var pid = c["Process"]["ProcessId"].GetValueOrDefault(0);
                if (pid == 0)
                {
                    Error("Could not read process ID from Stratis.DevEx.Gui.run.");
                    return false;
                }
                else
                {
                    try
                    {
                        var p = Process.GetProcessById(pid);
                        if (p.ProcessName.Contains("Stratis.DevEx.Gui"))
                        {
                            return true;
                        }
                        else
                        {
                            Debug("Process {pid} is not Stratis.DevEx.Gui.", pid);
                            return false;
                        }
                    }
                    catch
                    {
                        Debug("Exception thrown getting process id {pid}.", pid);
                        return false;
                    }
                }
            }
        }

        public static PipeClient<MessagePack>? CreatePipeClient(string name)
        {
            using (var op = Begin("Creating GUI pipe client"))
            {
                try
                {
                    var pipeClient = new PipeClient<MessagePack>(name) { AutoReconnect = false };
                    op.Complete();
                    return pipeClient;
                }
                catch (Exception e)
                {
                    op.Abandon();
                    Error(e, "Error creating GUI pipe client.");
                    return null;
                }
            }
        }

        public static void ReconnectIfDisconnected(PipeClient<MessagePack> pipeClient)
        {
            if (!pipeClient.IsConnected)
            {
                pipeClient.ConnectAsync().Wait();
            }
        }
    }
}
