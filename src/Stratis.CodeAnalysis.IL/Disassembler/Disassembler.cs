using System.Collections.Generic;
using System.IO;

using System.Linq;

using CSharpSourceEmitter;
using Microsoft.Cci;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.ILToCodeModel;
using Microsoft.Cci.MetadataReader;

public enum DisassemblerLang
{
    CIL,
    TAC,
    CSHARP,
    BOOGIE,
}

namespace Stratis.DevEx.CodeAnalysis.IL
{

    public class Disassembler : Runtime
    {
        public static void Run(string fileName, SmartContractSourceEmitterOutput output, bool noIL = false, string? classPattern = null, string? methodPattern = null)
        {
            using var host = new PeReader.DefaultHost();
            IModule? module = host.LoadUnitFrom(FailIfFileNotFound(fileName)) as IModule;
            if (module is null || module is Dummy)
            {
                Error("{0} is not a PE file containing a CLR module or assembly.", fileName);
                return;
            }
            string pdbFile = Path.ChangeExtension(module.Location, "pdb");
            using var pdbReader = new PdbReader(fileName, pdbFile, host, true);
            foreach(var ar in module.AssemblyReferences.Where(ar => ar.ResolvedAssembly == Dummy.Assembly))
            {
                var rd = System.Environment.OSVersion.Platform == System.PlatformID.Win32NT ? 
                    AssemblyMetadata.TryResolve(ar, Path.Combine(System.Environment.GetEnvironmentVariable("ProgramFiles"), "dotnet\\packs\\Microsoft.NETCore.App.Ref\\3.1.0\\ref\\netcoreapp3.1")) : AssemblyMetadata.TryResolve(ar);
                if (rd is null)
                {
                    Error("Could not resolve assembly reference {ar} using NuGet resolver. Exiting.", ar.ToString());
                    return;
                }
                else
                {
                    if (!File.Exists(rd.File.FullName))
                    {
                        Error("Could not find the assembly file {f} for assembly reference {ar}. Exiting.", rd.File.FullName, ar.ToString());
                        return;
                    }
                    var mr = host.LoadUnitFrom(rd.File.FullName);
                    if (module is null || module is Dummy)
                    {
                        Error("{0} is not a PE file containing a CLR module or assembly. Could not load assembly reference {ar}. Exiting.", rd.File.FullName, ar.ToString());
                        return;
                    }
                }
            }
            var options = DecompilerOptions.AnonymousDelegates | DecompilerOptions.Iterators | DecompilerOptions.Loops;
            module = Decompiler.GetCodeModelFromMetadataModel(host, module, pdbReader, options);
            var sourceEmitter = new SmartContractSourceEmitter(output, host, pdbReader, true, noIL, classPattern, methodPattern);
            sourceEmitter.Traverse(module);

        }
    }
}

