using Jens.InversionOfControl;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jens.RestServer.Sample
{
    /// <summary>
    /// Wrapper for inversion of control container.
    /// </summary>
    public class RestServerContainer : Container, IRestServerDependencyResolver
    {
    }
}
