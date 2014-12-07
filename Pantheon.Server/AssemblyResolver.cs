using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pantheon.Server
{
    public static class AssemblyResolver
    {
        public static void Enable()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        }

        private static string Resolve(string directory, string assembly)
        {
            if (!Directory.Exists(Path.Combine(directory)))
            {
                Directory.CreateDirectory(Path.Combine(directory));
                return string.Empty;
            }

            string assemblyName;
            if (assembly.Contains(','))
            {
                assemblyName = assembly.Remove(assembly.IndexOf(','));
            }
            else
            {
                assemblyName = assembly;
            }

            var path = Path.Combine(directory, assemblyName);

            if (File.Exists(Path.ChangeExtension(path, "dll")))
            {
                path = Path.ChangeExtension(path, "dll");
            }
            else if (File.Exists(Path.ChangeExtension(path, "exe")))
            {
                path = Path.ChangeExtension(path, "exe");
            }
            else
            {
                path = string.Empty;
            }

            return path;
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            string path = Resolve("ServerModules", args.Name);
            if (path == string.Empty)
            {
                path = Resolve("Library", args.Name);
            }

            if (path == string.Empty)
            {
                return null;
            }

            return Assembly.LoadFrom(path);
        }
    }
}