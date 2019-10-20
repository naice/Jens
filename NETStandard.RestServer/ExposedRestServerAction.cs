using System;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace NETStandard.RestServer
{
    internal class ExposedRestServerActionCompiledParameters
    {
        public string Param { get; set; }
        public int RouteIndex { get; set; }
        public MethodInfo Setter { get; set; }
        public Type Type { get; set; }
        public PropertyInfo Property { get; set; }

        public static void FillParameters(object destinationInstance, string route, IEnumerable<ExposedRestServerActionCompiledParameters> compiledParams)
        {
            var explodedRoute = route.Split('/');

            foreach (var compiledParam in compiledParams)
            {
                var value = explodedRoute[compiledParam.RouteIndex];
                compiledParam.Setter.Invoke(destinationInstance, new object[] { MakeDestinationType(compiledParam.Type, value, route, compiledParam) });
            }
        }

        private static object MakeDestinationType(Type type, string value, string route, ExposedRestServerActionCompiledParameters param)
        {
            try
            {
                return Convert.ChangeType(value, type);
            }
            catch (Exception ex)
            {
                throw new FormatException("Unable to convert " + param.Param + " '" + (value ?? "NULL") + "' to destination type " + param.Type.Name + " on Route '" + route + "'", ex);
            }
        }
    }
    internal class ExposedRestServerAction
    {
        public Type InputType { get; set; }
        public Type OutputType { get; set; }
        public bool IsBodyRequested { get; set; }
        public string Route { get; set; }
        public Regex RouteRegex { get; set; }
        public bool IsParameterized { get { return CompiledParameters != null && CompiledParameters.Length > 0; } }
        public ExposedRestServerActionCompiledParameters[] CompiledParameters { get; set; }
        public string[] Methods { get; set; }
        public ExposedRestServerService RestServerService { get; }

        private readonly MethodInfo _methodInfo;

        public ExposedRestServerAction(ExposedRestServerService restServerService, MethodInfo methodInfo)
        {
            RestServerService = restServerService;
            _methodInfo = methodInfo;
        }

        public void CompileInputParameters()
        {
            var explodedRoute = Route.Split('/');
            var routeParams = explodedRoute.Where((section) => section.StartsWith("{") && section.EndsWith("}")).ToList();
            var routeParamNames = routeParams.Select(section => section.Replace("}", "").Replace("{", "").ToLower()).ToList();
            var compiled = new List<ExposedRestServerActionCompiledParameters>();
            var properties = new List<PropertyInfo>(InputType?.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.CanWrite) ?? new PropertyInfo[0]);           

            for (int i = 0; i < routeParams.Count; i++)
            {
                var routeParam = routeParams[i];
                var routeParamName = routeParamNames[i];
                var cparam = new ExposedRestServerActionCompiledParameters();

                cparam.RouteIndex = Array.IndexOf(explodedRoute, routeParam);
                cparam.Param = routeParam;

                var property = properties.FirstOrDefault(prop => prop.Name.ToLower() == routeParamName);
                if (property == null) throw new InvalidOperationException("Route parameter " + routeParam + " defined but no parameter found on " + (InputType?.Name ?? "NO INPUT TYPE!") + ". Route='" + Route + "'");

                cparam.Setter = property.SetMethod;
                cparam.Property = property;
                cparam.Type = property.PropertyType;
                var typeCode = Type.GetTypeCode(cparam.Type);
                if (typeCode == TypeCode.Object) throw new InvalidOperationException("Route parameter " + routeParam + " defined but no primitive type. Route='" + Route + "'");

                compiled.Add(cparam);
            }

            List<string> regexRoute = new List<string>();
            for (int i = 0; i < explodedRoute.Length; i++)
            {
                var routeToken = explodedRoute[i];
                var cparam = compiled.FirstOrDefault(p => p.Param == routeToken);
                var token = Regex.Escape(routeToken);

                if (cparam != null)
                {
                    token = "[^/]*";
                }

                regexRoute.Add(token);
            }
            RouteRegex = new Regex("^" + string.Join("/", regexRoute) + "$", RegexOptions.Compiled);
            CompiledParameters = compiled.ToArray();
        }

        public async Task<object> Execute(HttpListenerContext context, object[] param)
        {

            // VOID
            if (OutputType == null && InputType == null)
            {
                ExecuteVoid(context);
                return null;
            }
            if (OutputType == null)
            {
                ExecuteVoid(context, param);
                return null;
            }
            if (OutputType == typeof(Task) && InputType == null)
            {
                await ExecuteVoidAsync(context);
                return null;
            }
            if (OutputType == typeof(Task))
            {
                await ExecuteVoidAsync(context, param);
                return null;
            }

            // RESULT
            if (IsGenericTaskType(OutputType) && InputType == null)
            {
                return await ExecuteAsync(context);
            }
            if (IsGenericTaskType(OutputType))
            {
                return await ExecuteAsync(context, param);
            }
            if (InputType == null)
            {
                return ExecuteInternal(context);
            }

            return ExecuteInternal(context, param);
        }

        private object ExecuteInternal(HttpListenerContext context, object[] param)
        {
            return _methodInfo.Invoke(RestServerService.GetInstance(context), param );
        }
        private object ExecuteInternal(HttpListenerContext context)
        {
            return _methodInfo.Invoke(RestServerService.GetInstance(context), new object[0]);
        }
        private async Task<object> ExecuteAsync(HttpListenerContext context, object[] param)
        {
            //return await (dynamic)_methodInfo.Invoke(_restServerService.GetInstance(context), new object[] { param });
            var task = (Task)_methodInfo.Invoke(RestServerService.GetInstance(context), param);
            var result = await Task.Run<object>(() => { return task.GetType().GetProperty("Result").GetValue(task); });

            return result;
        }
        private async Task<object> ExecuteAsync(HttpListenerContext context)
        {
            // dynamic fails with void, dont know why.
            var task = (Task)_methodInfo.Invoke(RestServerService.GetInstance(context), new object[0]);
            var result = await Task.Run<object>(() => { return task.GetType().GetProperty("Result").GetValue(task); });

            return result;
        }
        private async Task ExecuteVoidAsync(HttpListenerContext context, object[] param)
        {
            await (Task)_methodInfo.Invoke(RestServerService.GetInstance(context), param);
        }
        private async Task ExecuteVoidAsync(HttpListenerContext context)
        {
            await (Task)_methodInfo.Invoke(RestServerService.GetInstance(context), new object[0]);
        }
        private void ExecuteVoid(HttpListenerContext context, object[] param)
        {
            _methodInfo.Invoke(RestServerService.GetInstance(context), param);
        }
        private void ExecuteVoid(HttpListenerContext context)
        {
            _methodInfo.Invoke(RestServerService.GetInstance(context), new object[0]);
        }

        private static bool IsGenericTaskType(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Task<>);
        }
    }
}