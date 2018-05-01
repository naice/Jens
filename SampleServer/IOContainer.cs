using NETStandard.RestServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleServer
{
    internal class IOContainer : IRestServerServiceDependencyResolver
    {
        private readonly SimpleInjector.Container _container;

        public IOContainer()
        {
            _container = new SimpleInjector.Container();
            _container.Register<Configuration>(SimpleInjector.Lifestyle.Singleton);
        }

        public object[] GetDependecys(Type[] dependencyTypes)
        {
            return dependencyTypes.Select(type => GetDependency(type)).ToArray();
        }

        public object GetDependency(Type dependencyType)
        {
            return _container.GetInstance(dependencyType);
        }
    }
}
