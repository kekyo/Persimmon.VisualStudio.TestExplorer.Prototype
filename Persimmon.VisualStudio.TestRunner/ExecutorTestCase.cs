using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Persimmon.VisualStudio.TestRunner
{
    [Serializable]
    public sealed class ExecutorTestCase
    {
        public readonly string FullyQualifiedTestName;
        public readonly string TypeName;
        public readonly AssemblyName Source;

        public ExecutorTestCase(
            string fullyQualifiedTestName,
            string typeName,
            AssemblyName source)
        {
            Debug.Assert(fullyQualifiedTestName != null);
            Debug.Assert(source != null);

            FullyQualifiedTestName = fullyQualifiedTestName;
            TypeName = typeName;
            Source = source;
        }

        public override string ToString()
        {
            return string.Join(
                ", ",
                this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).
                    Where(pi => pi.IsInitOnly).
                    Select(pi => string.Format("{0}=\"{1}\"", pi.Name, pi.GetValue(this))));
        }
    }
}
