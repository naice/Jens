using NETStandard.RestServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SampleServer
{
    class Program
    {
        private static IOContainer _container;
        private static RestServer _restServer;

        static void Main(string[] args)
        {
            Log.DefaultLogger = new ConsoleLogger();
            _container = new IOContainer();
            var config = _container.GetDependency(typeof(Configuration)) as Configuration;
            _restServer = new RestServer(new IPEndPoint(config.IPAddress, config.Port), _container, System.Reflection.Assembly.GetExecutingAssembly());
            _restServer.RegisterRouteHandler(new RestServerServiceFileRouteHandler("InetPub"));


            _restServer.Start();

            while (_restServer.IsRunning)
            {
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
