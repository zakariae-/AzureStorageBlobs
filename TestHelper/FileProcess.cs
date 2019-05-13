using System;
using System.IO;
using System.Reflection;

namespace TestHelper
{
    public static class FileProcess
    {
        public static string GetLoadedAssemblyDirectory(Assembly loadedAssembly = null)
        {
            var binDirectory = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";
            var loadedAssemblyLocation = (loadedAssembly ?? Assembly.GetCallingAssembly()).Location;

            var indexOf = loadedAssemblyLocation.IndexOf(binDirectory, StringComparison.OrdinalIgnoreCase);
            if (indexOf <= 0)
                throw new Exception(
                    $"Directory {binDirectory} not find in the assembly, " +
                    	"you need to provide the loaded assembly !!");

            return loadedAssemblyLocation.Substring(0, indexOf);
        }
    }
}
