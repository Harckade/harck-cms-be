using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Reflection;

namespace Harckade.CMS.JwtAuthorization.Middleware
{
    /// <summary>
    /// https://github.com/Azure/azure-functions-dotnet-worker/issues/414
    /// solution by: https://github.com/david-peden-q2
    /// </summary>
    internal static class FunctionUtilities
    {
        private const string _bindingsFeature = "IFunctionBindingsFeature";
        private const string _invocationResult = "InvocationResult";
        private const string _inputData = "InputData";
        internal static HttpRequestData GetHttpRequestData(this FunctionContext context)
        {
            var keyValuePair = context.Features.SingleOrDefault(f => f.Key.Name == _bindingsFeature);
            var functionBindingsFeature = keyValuePair.Value;
            var type = functionBindingsFeature.GetType();
            var inputData = type.GetProperties().Single(p => p.Name == _inputData).GetValue(functionBindingsFeature) as IReadOnlyDictionary<string, object>;
            return inputData?.Values.SingleOrDefault(o => o is HttpRequestData) as HttpRequestData;
        }

        internal static void SetStatusCode(this FunctionContext context, HttpStatusCode code)
        {
            var req = context.GetHttpRequestData();
            var response = req.CreateResponse();
            response.StatusCode = code;
            var keyValuePair = context.Features.SingleOrDefault(f => f.Key.Name == _bindingsFeature);
            var functionBindingsFeature = keyValuePair.Value;
            var type = functionBindingsFeature.GetType();
            var result = type.GetProperties().Single(p => p.Name == _invocationResult);
            result.SetValue(functionBindingsFeature, response);
        }

        internal static MethodInfo GetTargetFunctionMethod(this FunctionContext context)
        {
            // This contains the fully qualified name of the method
            // E.g. IsolatedFunctionAuth.TestFunctions.ScopesAndAppRoles
            var entryPoint = context.FunctionDefinition.EntryPoint;

            var assemblyPath = context.FunctionDefinition.PathToAssembly;
            var assembly = Assembly.LoadFrom(assemblyPath);
            var typeName = entryPoint.Substring(0, entryPoint.LastIndexOf('.'));
            var type = assembly.GetType(typeName);
            var methodName = entryPoint.Substring(entryPoint.LastIndexOf('.') + 1);
            var method = type.GetMethod(methodName);
            return method;
        }
    }
}
