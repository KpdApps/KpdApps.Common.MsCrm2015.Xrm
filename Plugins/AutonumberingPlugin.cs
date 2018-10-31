using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using KpdApps.Common.MsCrm2015.Extensions;

namespace KpdApps.Common.MsCrm2015.Xrm.Plugins
{
    public class AutonumberingPlugin : AutogeneratingBasePlugin
    {
        private int Counter;

        public override void ExecuteInternal(PluginState state)
        {
            InitTargetAndService(state);
            Entity autonumbering = GetAutonumberingByEntityName(TargetEntityName, state.Service);

            if (autonumbering == null)
                return;

            AddPredefinedFunction("number", new Func<string, string>(PredefinedFunctionGetCounterString));

            string mask = autonumbering.Attributes.GetStringValue(Schema.Autonumbering.Mask);
            Counter = autonumbering.Attributes.GetNumberValue(Schema.Autonumbering.Counter);
            string generatedValue = GenerateValueByMask(mask);

            string fieldName = autonumbering.Attributes.GetStringValue(Schema.Autonumbering.FieldName);
            TargetEntity.Attributes.SetStringValue(fieldName, generatedValue);

            Counter++;
            autonumbering.Attributes.SetNumberValue(Schema.Autonumbering.Counter, Counter);
            Service.Update(autonumbering);
        }

        private static Entity GetAutonumberingByEntityName(string entityName, IOrganizationService service)
        {
            QueryExpression query = new QueryExpression
            {
                EntityName = Schema.Autonumbering.LogicalName,
                NoLock = true,
                Distinct = true,
                ColumnSet = new ColumnSet(Schema.Autonumbering.Id, Schema.Autonumbering.FieldName, Schema.Autonumbering.Mask, Schema.Autonumbering.Counter),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(Schema.Autonumbering.EntityName, ConditionOperator.Equal, entityName)
                    }
                }
            };

            EntityCollection result = service.RetrieveMultiple(query);

            if (result.Entities.Any())
                return result.Entities[0];

            return null;
        }

        protected override string ProcessField(string resultMatch)
        {
            throw new InvalidPluginExecutionException($"В маске автонумерации указано поле {resultMatch} для сущности {TargetEntityName}");
        }

        private string PredefinedFunctionGetCounterString(string format)
        {
            return Counter.ToString(format);
        }
    }
}