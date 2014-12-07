using System;
using System.Collections.Generic;
using System.Linq;

namespace Pantheon.Common
{
    public abstract class PantheonInitializer
    {
        private static bool _Initialized;

        public static void InitializeAll()
        {
            if (_Initialized)
            {
                return;
            }
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            IEnumerable<Type> types = assemblies
                                     .SelectMany(a => a.GetTypes())
                                     .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(PantheonInitializer)));
            foreach (var item in types)
            {
                var obj = (PantheonInitializer)Activator.CreateInstance(item);
                obj.Initialize();
            }

            _Initialized = true;
        }

        public abstract void Initialize();
    }
}