using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using KpdApps.Common.MsCrm2015.Extensions;
using KpdApps.Common.MsCrm2015.Helpers;

namespace KpdApps.Common.MsCrm2015.Xrm.Plugins
{
    public class AutonamingPlugin : AutogeneratingBasePlugin
    {
        public override void ExecuteInternal(PluginState state)
        {
            InitTargetAndService(state);
            Entity autonaming = GetAutonamingByEntityName(TargetEntityName, Service);

            if (autonaming == null)
                return;

            string mask = autonaming.Attributes.GetStringValue(Schema.Autonaming.Mask);
            string generatedValue = GenerateValueByMask(mask);

            string fieldName = autonaming.Attributes.GetStringValue(Schema.Autonaming.FieldName);
            TargetEntity.Attributes.SetStringValue(fieldName, generatedValue);
        }

        private static Entity GetAutonamingByEntityName(string entityName, IOrganizationService service)
        {
            QueryExpression query = new QueryExpression
            {
                EntityName = Schema.Autonaming.LogicalName,
                NoLock = true,
                Distinct = true,
                ColumnSet = new ColumnSet(Schema.Autonaming.Id, Schema.Autonaming.FieldName, Schema.Autonaming.Mask),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(Schema.Autonaming.EntityName, ConditionOperator.Equal, entityName)
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
            string fieldAddress = resultMatch;

            object value = CrmFieldHelper.GetFieldValueByFieldAddress(TargetEntity, fieldAddress, Service);

            return value.ToString();
        }
    }
}