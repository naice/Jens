using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jens.RestServer
{
    /// <summary>
    /// Simple dependency resolver for your <see cref="RestServerService"/> implementations.
    /// </summary>
    public interface IRestServerDependencyResolver
    {
        /// <summary>
        /// Activates the dependency matching the given <see cref="Type"/>.
        /// </summary>
        object Activate(Type type);
    }
}
