using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.Cci;
using Microsoft.Cci.MutableContracts;

using Nuclear.Assemblies;
using Nuclear.Assemblies.Resolvers;
using Nuclear.Assemblies.ResolverData;
using Nuclear.Assemblies.Resolvers.Internal;

namespace Stratis.DevEx.CodeAnalysis.IL
{
    public struct AssemblyReference
    {    
        AssemblyName Name;
        IAssemblyResolverData? ResolverData;
        public AssemblyReference(AssemblyName name, IAssemblyResolverData? rd)
        {
            Name = name;
            ResolverData = rd;
        }
    }

    public class AssemblyMetadata : Runtime
    {
        #region Constructors
        public AssemblyMetadata(string assemblyPath)
        {
            Directory = new FileInfo(assemblyPath).Directory!;
            Host = new PeReader.DefaultHost();
            Module = (IModule)Host.LoadUnitFrom(assemblyPath);
            References = Module.AssemblyReferences.Select(a => new AssemblyReference(GetAssemblyName(a), TryResolve(a, Directory.FullName)));
        }
        #endregion

        #region Properties
        public DirectoryInfo Directory { get; protected set; }
        public PeReader.DefaultHost Host { get; protected set; }
        public IModule Module { get; protected set; }
        public IEnumerable<AssemblyReference> References { get; protected set; }

        internal static DefaultResolver DefaultResolver { get; } = new DefaultResolver(VersionMatchingStrategies.Strict, SearchOption.AllDirectories);
        internal static NugetResolver NugetResolver { get; } = new NugetResolver(VersionMatchingStrategies.SemVer, VersionMatchingStrategies.SemVer);
        #endregion

        #region Methods
        public static AssemblyName GetAssemblyName(IAssemblyReference r)
        {
            var name = new AssemblyName(r.Name.Value)
            {
                Version = r.Version,
                CultureName = r.Culture,
            };
            name.SetPublicKey(r.PublicKey.Any() ? r.PublicKey.ToArray() : null);
            name.SetPublicKeyToken(r.PublicKeyToken.Any() ? r.PublicKeyToken.ToArray() : null);
            return name;
        }

        public static IAssemblyResolverData? TryResolve(AssemblyName name, string? searchPath = null)
        {
            var defaultResolverData = searchPath is not null && System.IO.Directory.Exists(searchPath) ? DefaultResolver.CoreResolver.Resolve(name, new DirectoryInfo(searchPath), SearchOption.AllDirectories, VersionMatchingStrategies.Strict) : null;
            if (defaultResolverData is not null && defaultResolverData.Any())
            {
                Debug("Resolved assembly {0} using default resolver.", name);
                return defaultResolverData.First();
            }
            else
            {
                NugetResolver.TryResolve(name, out var nugetResolverData);
                if (nugetResolverData is not null && nugetResolverData.Any())
                {
                    Debug("Resolved assembly {0} using NuGet resolver.", name);
                    return nugetResolverData.First();
                }
                else
                {
                    return null;
                }
            }
        }

        public static IAssemblyResolverData? TryResolve(IAssemblyReference r, string? searchPath = null) => TryResolve(GetAssemblyName(r), searchPath);
        #endregion
    }
}

