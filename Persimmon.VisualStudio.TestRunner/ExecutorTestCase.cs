using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Persimmon.VisualStudio.TestRunner
{
    [Serializable]
    public sealed class ExecutorTestCase
    {
        public readonly string FullyQualifiedName;
        public readonly string ClassName;
        public readonly string Source;
        public readonly string DisplayName;

        public ExecutorTestCase(
            string fullyQualifiedName,
            string className,
            string source,
            string displayName)
        {
            Debug.Assert(fullyQualifiedName != null);
            Debug.Assert(source != null);
            Debug.Assert(displayName != null);

            FullyQualifiedName = fullyQualifiedName;
            ClassName = className;
            Source = source;
            DisplayName = displayName;
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
