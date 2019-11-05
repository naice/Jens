using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Jens.RestServer
{
    internal class RestServerServiceRouteHandler : IRestServerRouteHandler
    {
        private readonly IPEndPoint _endPoint;
        private readonly List<ExposedRestServerService> _exposedRestServerServices;
        private readonly IRestServerDependencyResolver _restServerDependencyResolver;
        private readonly Assembly[] _assemblys;
        private readonly ILogger _logger;
        private readonly Type _restServerServiceBaseType = typeof(IRestServerService);

        public RestServerServiceRouteHandler(IPEndPoint endPoint, IRestServerDependencyResolver restServerDependencyResolver, ILogger logger, params Assembly[] assemblys)
        {
            _exposedRestServerServices = new List<ExposedRestServerService>();
            _assemblys = assemblys ?? throw new ArgumentNullException(nameof(assemblys));
            _restServerDependencyResolver = restServerDependencyResolver ?? throw new ArgumentNullException(nameof(restServerDependencyResolver));
            _endPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            RegisterExposedServices();
        }

        private void RegisterExposedServices()
        {
            foreach (var assembly in _assemblys)
            {
                foreach (var restServerServiceType in assembly.GetTypes()
                    .Where(type => type != _restServerServiceBaseType && _restServerServiceBaseType.IsAssignableFrom(type) && type.IsClass))
                {
                    var instanceType = RestServerServiceInstanceType.Instance;
                    var attrib = restServerServiceType.GetTypeInfo().GetCustomAttribute<RestServerServiceInstanceAttribute>();
                    if (attrib != null)
                    {
                        instanceType = attrib.RestServerServiceInstanceType;
                    }

                    TryExposeRestServerService(restServerServiceType, instanceType);
                }
            }
        }

        private Type FindTopLevelInterface(Type type)
        {
            var intface = type.GetInterfaces().Where(intf => _restServerServiceBaseType.IsAssignableFrom(intf)).FirstOrDefault();
            if (intface == null)
                return FindTopLevelInterface(type.BaseType);

            if (intface == _restServerServiceBaseType)
                return null;

            return intface;
        }

        private bool IsRouteAlreadyRegistered(string route)
        {
            return GetActionForRoute(route) != null;
        }
        private ExposedRestServerAction GetActionForRoute(string route)
        {
            var defRoute = _exposedRestServerServices
                .SelectMany(service => service.Routes.Where(rroute => !rroute.IsParameterized))
                .Where(rroute =>  string.Compare(rroute.Route, route, true) == 0)
                .FirstOrDefault();

            if (defRoute != null)
                return defRoute;

            return _exposedRestServerServices
                .SelectMany(service => service.Routes.Where(rroute => rroute.IsParameterized))
                .Where(rroute => rroute.RouteRegex.IsMatch(route))
                .FirstOrDefault();
        }
        private void TryExposeRestServerService(Type restServerServiceType, RestServerServiceInstanceType instanceType)
        {
            ExposedRestServerService exposedRestServerService = new ExposedRestServerService(_restServerDependencyResolver)
            {
                ServiceType = restServerServiceType,
                InstanceType = instanceType,
                TopLevelInterfaceType = FindTopLevelInterface(restServerServiceType),
            };

            InterfaceMapping interfaceMapping;
            if (exposedRestServerService.TopLevelInterfaceType != null)
                interfaceMapping = restServerServiceType.GetInterfaceMap(exposedRestServerService.TopLevelInterfaceType);
            else
            {
                interfaceMapping = new InterfaceMapping() {
                    TargetMethods = null,
                    InterfaceMethods = null,
                    InterfaceType = null,
                    TargetType = null,
                };
            }

            foreach (var methodImpl in restServerServiceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var methodAttribute = methodImpl.GetCustomAttribute<RestServerServiceCallAttribute>();
                if (methodAttribute == null)
                {
                    continue;
                }
                if (string.IsNullOrEmpty(methodAttribute.Route))
                {
                    throw new ArgumentException($"Invalid route given: {methodAttribute.Route}", nameof(RestServerServiceCallAttribute.Route));
                }


                var routeStr = MakeRoute(methodAttribute.Route);

                if (IsRouteAlreadyRegistered(routeStr))
                {
                    throw new ArgumentException($"Route already registered. {routeStr}", nameof(RestServerServiceCallAttribute.Route));
                }

                MethodInfo methodToCall = methodImpl;
                if (interfaceMapping.TargetMethods != null)
                {
                    var implFoundOnIfaceMap = FindIndexByImpl(interfaceMapping.TargetMethods, methodImpl);
                    if (implFoundOnIfaceMap == null)
                    {
                        _logger.Warn($"{nameof(RestServerServiceRouteHandler)}: Implementation skipped {routeStr}, not defined in interface {exposedRestServerService.TopLevelInterfaceType.FullName} ");
                        continue;
                    }

                    methodToCall = interfaceMapping.InterfaceMethods[implFoundOnIfaceMap.Value];
                }

                Type inputType = null;
                bool isBodyRequested = false;
                var parameters = methodImpl.GetParameters();

                if (parameters != null && parameters.Length > 2)
                {
                    _logger.Warn($"{nameof(RestServerServiceRouteHandler)}: Method Parameter missmatch. Too many parameters. {routeStr}");
                    continue;
                }

                if (parameters != null && parameters.Length > 0)
                {
                    var parameterInfo = parameters[0];
                    var parameterTypeInfo = parameterInfo.ParameterType.GetTypeInfo();
                    if (!parameterTypeInfo.IsClass)
                    {
                        _logger.Warn($"{nameof(RestServerServiceRouteHandler)}: Method Parameter missmatch. Parameter is no class! {routeStr}");
                        continue;
                    }

                    inputType = parameterInfo.ParameterType;
                    if (inputType == typeof(string))
                        isBodyRequested = true;
                }

                if (parameters != null && parameters.Length == 2)
                {
                    if (parameters[1].ParameterType != typeof(string))
                    {
                        _logger.Warn($"{nameof(RestServerServiceRouteHandler)}: Method Parameter missmatch. Two parameters found but the last one is no string. {routeStr}");
                        continue;
                    }
                    isBodyRequested = true;
                }

                var exposedRestServerAction = new ExposedRestServerAction(exposedRestServerService, methodToCall)
                {
                    IsBodyRequested = isBodyRequested,
                    OutputType = methodImpl.ReturnType,
                    InputType = inputType,
                    Route = routeStr,
                    Methods = methodAttribute.Methods?.Split(',').Select(str => str.Trim().ToUpper()).ToArray() ?? new string[0]
                };
                exposedRestServerAction.CompileInputParameters();
                exposedRestServerService.Routes.Add(exposedRestServerAction);

                _logger.Info($"{nameof(RestServerServiceRouteHandler)}: \"{restServerServiceType.FullName}\" exposed API method \"{exposedRestServerAction.Route.Replace("{", "{{").Replace("}", "}}")}\".");
            }

            // We got any routes exposed? 
            if (exposedRestServerService.Routes.Count > 0)
            {
                if (exposedRestServerService.InstanceType == RestServerServiceInstanceType.SingletonStrict)
                {
                    exposedRestServerService.GetInstance(null);
                }

                _exposedRestServerServices.Add(exposedRestServerService);
            }
        }

        private int? FindIndexByImpl(MethodInfo[] targetMethods, MethodInfo methodImpl)
        {
            for (int i = 0; i < targetMethods.Length; i++)
            {
                if (targetMethods[i].Equals(methodImpl))
                    return i;
            }

            return null;
        }

        private static string MakeRoute(string route)
        {
            while (route.StartsWith("/"))
            {
                route = route.Substring(1);
            }
            while (route.EndsWith("/"))
            {
                route = route.Substring(0, route.Length - 1);
            }

            return route;
        }
        private string StripPort(string absPath)
        {
            string portStr = _endPoint.Port.ToString();

            if (absPath.StartsWith(portStr))
            {
                absPath = absPath.Substring(portStr.Length);
            }

            return absPath;
        }

        public async Task<bool> HandleRouteAsync(System.Net.Http.HttpListenerContext context)
        {
            string correlationId = Guid.NewGuid().ToString();
            string route = MakeRoute(StripPort(context.Request.Url.AbsolutePath));
            var restServerAction = GetActionForRoute(route);
            if (restServerAction == null)
            {
                return false;
            }

            _logger.Info($"Handling route {route} - Correlation: {correlationId}");

            object[] inputParameter = null;

            try
            {
                if (restServerAction.InputType != null)
                {
                    string requestString = await context.Request.ReadContentAsStringAsync();
                    if (restServerAction.IsBodyRequested && restServerAction.IsParameterized)
                        inputParameter = new object[2] { null, requestString };
                    else
                        inputParameter = new object[1];

                    if (!string.IsNullOrEmpty(requestString))
                    {
                        if (restServerAction.InputType == typeof(string))
                            inputParameter[0] = requestString;
                        else if (!restServerAction.IsBodyRequested)
                            inputParameter[0] = JsonConvert.DeserializeObject(requestString, restServerAction.InputType);
                    }

                    if (restServerAction.IsParameterized)
                    {
                        if (inputParameter[0] == null)
                            inputParameter[0] = Activator.CreateInstance(restServerAction.InputType);

                        ExposedRestServerActionCompiledParameters.FillParameters(inputParameter[0], route, restServerAction.CompiledParameters);
                    }
                }
            }
            catch (FormatException ex) // <-- CHANGE EXCEPTION TYPE!!!!1!11
            {
                _logger.Info($"{correlationId} Routing failed, Bad Request.");
                context.Response.ReasonPhrase = "Bad Request - " + ex.Message;
                context.Response.StatusCode = 400;
                context.Response.Close();
                return true;
            }
            catch (JsonException ex)
            {
                _logger.Info($"{correlationId} Routing failed, Bad Request.");
                context.Response.ReasonPhrase = "Bad Request - " + ex.Message;
                context.Response.StatusCode = 400;
                context.Response.Close();
                return true;
            }

            object result = null;            
            // make json response. 
            context.Response.Headers.ContentType.Clear();
            context.Response.Headers.ContentType.Add("application/json");
            try
            {
                result = await restServerAction.Execute(context, inputParameter);
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException targetInvocationException)
                {
                    if (ex.InnerException is RestServerServiceCallException restServerServiceCallException)
                    {
                        _logger.Info($"{correlationId} Routing failed, on expected exception {ex.GetType().FullName}, {route} action failed. Code: {restServerServiceCallException.HttpStatusCode} Phrase: {restServerServiceCallException.HttpReasonPhrase}  Message: {ex.Message}");
                        context.Response.ReasonPhrase = restServerServiceCallException.HttpReasonPhrase;
                        context.Response.StatusCode = restServerServiceCallException.HttpStatusCode;
                        context.Response.Close();
                        return true;
                    }
                }

                _logger.Info($"{correlationId} Routing failed, {route} action failed. Exception: {ex.ToString()}");
                context.Response.InternalServerError();
                context.Response.Close();
                return true;
            }

            if (result != null)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(result);
                    await context.Response.WriteContentAsync(json);
                }
                catch (JsonException ex)
                {
                    _logger.Info($"{correlationId} Routing failed, {route} result serialization failed Type={result?.GetType().FullName ?? "NULL"}. Message: {ex.Message}");
                    context.Response.InternalServerError();
                    context.Response.Close();
                    return true;
                }
            }

            _logger.Info($"{correlationId} Routing ended gracefully. {route}");
            return true;
        }
    }
}
