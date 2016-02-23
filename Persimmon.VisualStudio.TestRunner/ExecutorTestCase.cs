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
        public readonly string AssemblyPath;

        public ExecutorTestCase(
            string fullyQualifiedTestName,
            string typeName,
            string assemblyPath)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(fullyQualifiedTestName));
            Debug.Assert(!string.IsNullOrWhiteSpace(typeName));
            Debug.Assert(!string.IsNullOrWhiteSpace(assemblyPath));

            this.FullyQualifiedTestName = fullyQualifiedTestName;
            this.TypeName = typeName;
            this.AssemblyPath = assemblyPath;
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
