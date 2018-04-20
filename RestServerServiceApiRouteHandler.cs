using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace NETStandard.RestServer
{
    internal class RestServerServiceRouteHandler : IRestServerRouteHandler
    {
        private readonly IPEndPoint _endPoint;
        private readonly List<ExposedRestServerService> _exposedRestServerServices;
        private readonly IRestServerServiceDependencyResolver _restServerDependencyResolver;
        private readonly Assembly[] _assemblys;

        public RestServerServiceRouteHandler(IPEndPoint endPoint, IRestServerServiceDependencyResolver restServerDependencyResolver, params Assembly[] assemblys)
        {
            _exposedRestServerServices = new List<ExposedRestServerService>();
            _assemblys = assemblys;
            _restServerDependencyResolver = restServerDependencyResolver;
            _endPoint = endPoint;

            RegisterExposedServices();
        }

        private void RegisterExposedServices()
        {
            var restServerServiceBaseType = typeof(RestServerService);
            foreach (var assembly in _assemblys)
            {
                var test = assembly.GetTypes();


                foreach (var RestServerServiceType in assembly.GetTypes()
                    .Where(type => type != restServerServiceBaseType && type.GetTypeInfo().BaseType == restServerServiceBaseType))
                {
                    var instanceType = RestServerServiceInstanceType.Instance;
                    var attrib = RestServerServiceType.GetTypeInfo().GetCustomAttribute<RestServerServiceInstanceAttribute>();
                    if (attrib != null)
                    {
                        instanceType = attrib.RestServerServiceInstanceType;
                    }

                    TryExposeRestServerService(RestServerServiceType, instanceType);
                }
            }
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
        private void TryExposeRestServerService(Type RestServerServiceType, RestServerServiceInstanceType instanceType)
        {
            ExposedRestServerService exposedRestServerService = new ExposedRestServerService(_restServerDependencyResolver)
            {
                ServiceType = RestServerServiceType,
                InstanceType = instanceType,
            };

            foreach (var method in RestServerServiceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var methodAttribute = method.GetCustomAttribute<RestServerServiceCallAttribute>();
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

                Type inputType = null;
                var parameters = method.GetParameters();
                if (parameters != null && parameters.Length > 1)
                {
                    Log.w($"{nameof(RestServerServiceRouteHandler)}: Method Parameter missmatch. Too many parameters. {routeStr}");
                    continue;
                }
                if (parameters != null && parameters.Length == 1)
                {
                    var parameterInfo = parameters[0];
                    var parameterTypeInfo = parameterInfo.ParameterType.GetTypeInfo();
                    if (!parameterTypeInfo.IsClass)
                    {
                        Log.w($"{nameof(RestServerServiceRouteHandler)}: Method Parameter missmatch. Parameter is no class! {routeStr}");
                        continue;
                    }

                    inputType = parameterInfo.ParameterType;
                }

                var exposedRestServerAction = new ExposedRestServerAction(exposedRestServerService, method)
                {
                    OutputType = method.ReturnType,
                    InputType = inputType,
                    Route = routeStr,
                    Methods = methodAttribute.Methods?.Split(',').Select(str => str.Trim().ToUpper()).ToArray() ?? new string[0]
                };
                exposedRestServerAction.CompileInputParameters();
                exposedRestServerService.Routes.Add(exposedRestServerAction);

                Log.i($"{nameof(RestServerServiceRouteHandler)}: \"{RestServerServiceType.FullName}\" exposed API method \"{exposedRestServerAction.Route.Replace("{", "{{").Replace("}", "}}")}\".");
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

            Log.i($"Handling route {route} - Correlation: {correlationId}");

            object inputParameter = null;

            try
            {
                if (restServerAction.InputType != null)
                {
                    string requestString = await context.Request.ReadContentAsStringAsync();
                    if (!string.IsNullOrEmpty(requestString))
                    {
                        inputParameter = JsonConvert.DeserializeObject(requestString, restServerAction.InputType);
                    }

                    if (restServerAction.IsParameterized)
                    {
                        if (inputParameter == null)
                            inputParameter = Activator.CreateInstance(restServerAction.InputType);

                        ExposedRestServerActionCompiledParameters.FillParameters(inputParameter, route, restServerAction.CompiledParameters);
                    }
                }
            }
            catch (FormatException ex) // <-- CHANGE EXCEPTION TYPE!!!!1!11
            {
                Log.i($"{correlationId} Routing failed, Bad Request.");
                context.Response.ReasonPhrase = "Bad Request - " + ex.Message;
                context.Response.StatusCode = 400;
                context.Response.Close();
                return true;
            }
            catch (JsonException ex)
            {
                Log.i($"{correlationId} Routing failed, Bad Request.");
                context.Response.ReasonPhrase = "Bad Request - " + ex.Message;
                context.Response.StatusCode = 400;
                context.Response.Close();
                return true;
            }

            object result = null;
            try
            {
                result = await restServerAction.Execute(context, inputParameter);
            }
            catch (Exception ex)
            {
                Log.i($"{correlationId} Routing failed, {route} action failed. Message: {ex.Message}");
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
                    Log.i($"{correlationId} Routing failed, {route} result serialization failed Type={result?.GetType().FullName ?? "NULL"}. Message: {ex.Message}");
                    context.Response.InternalServerError();
                    context.Response.Close();
                    return true;
                }
            }

            Log.i($"{correlationId} Routing ended gracefully. {route}");
            return true;
        }
    }
}
