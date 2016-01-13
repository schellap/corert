using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILCompiler.DependencyAnalysis
{
    interface INodeWithNonRelocDependencies
    {
        void AddDependencies(IEnumerable<Object> dependencies, string reason);
    }
}
