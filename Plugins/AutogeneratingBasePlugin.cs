using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;

namespace KpdApps.Common.MsCrm2015.Xrm.Plugins
{
    abstract public class AutogeneratingBasePlugin : BasePlugin
    {
        protected Entity TargetEntity;
        protected string TargetEntityName;
        protected IOrganizationService Service;


        protected void InitTargetAndService(PluginState state)
        {
            TargetEntity = state.GetTarget<Entity>();
            TargetEntityName = TargetEntity.LogicalName;
            Service = state.Service;
        }

        // Возвращаемое значение делигата должно быть типа стринг.
        private readonly Dictionary<string, Delegate> predefinedFunctions = new Dictionary<string, Delegate>
        {
            { "date", new Func<string, string>(GetCurrentDateString) }
        };

        protected string GenerateValueByMask(string mask)
        {
            MatchCollection matches = Regex.Matches(mask, @"\{(.+?)\}");

            List<string> resultMatches = (from Match m in matches select m.Groups[1].ToString()).ToList();

            var results = new Dictionary<string, string>();

            foreach (string resultMatch in resultMatches)
            {
                var resultValue = string.Empty;
                if (resultMatch.IndexOf(':') > -1)
                {
                    string[] parts = resultMatch.Split(':');

                    if (!predefinedFunctions.ContainsKey(parts[0]))
                        throw new InvalidPluginExecutionException($"Попытка вызова неопределенной функции {parts[0]} для сущности {TargetEntityName}");

                    resultValue = (string)predefinedFunctions[parts[0]].DynamicInvoke(parts[1]);
                }
                else
                {
                    resultValue = ProcessField(resultMatch);
                }
                results.Add("{" + resultMatch + "}", resultValue.ToString());
            }

            var resultString = mask;
            foreach (var result in results)
            {
                resultString = resultString.Replace(result.Key, result.Value);
            }

            return resultString;
        }

        protected void AddPredefinedFunction(string functionName, Delegate function)
        {
            predefinedFunctions.Add(functionName, function);
        }

        private static string GetCurrentDateString(string format)
        {
            return DateTime.Now.ToString(format);
        }

        abstract protected string ProcessField(string resultMatch);
    }
}