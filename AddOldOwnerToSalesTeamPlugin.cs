using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;

public class AddOldOwnerToSalesTeamPlugin : IPlugin
{
    private const string OpportunityLogicalName = "opportunity";
    private const string OpportunityTeamName = "Opportunity Sales Team";

    public void Execute(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
        var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        var service = factory.CreateOrganizationService(context.UserId);

        try
        {
            if (context.MessageName != "Update" || !context.InputParameters.Contains("Target"))
            {
                return;
            }

            if (!(context.InputParameters["Target"] is Entity target) || target.LogicalName != OpportunityLogicalName)
            {
                return;
            }

            if (!target.Contains("ownerid"))
            {
                return;
            }

            if (!context.PreEntityImages.TryGetValue("PreImage", out var preEntity))
            {
                return;
            }

            var preImage = (Entity)preEntity;
            if (!preImage.Contains("ownerid"))
            {
                return;
            }

            var oldOwner = preImage.GetAttributeValue<EntityReference>("ownerid");
            var newOwner = target.GetAttributeValue<EntityReference>("ownerid");

            if (oldOwner == null || newOwner == null || oldOwner.Id == newOwner.Id)
            {
                return;
            }

            var templateQuery = new QueryExpression("teamtemplate")
            {
                ColumnSet = new ColumnSet("teamtemplateid")
            };
            templateQuery.Criteria.AddCondition("objecttypecode", ConditionOperator.Equal, OpportunityLogicalName);
            templateQuery.Criteria.AddCondition("name", ConditionOperator.Equal, OpportunityTeamName);

            var template = service.RetrieveMultiple(templateQuery).Entities.FirstOrDefault();
            if (template == null)
            {
                tracing.Trace("Opportunity Sales Team template not found.");
                throw new InvalidPluginExecutionException("Opportunity Sales Team template not found.");
            }

            var templateId = template.GetAttributeValue<Guid>("teamtemplateid");

            var addRequest = new AddUserToRecordTeamRequest
            {
                Record = new EntityReference(OpportunityLogicalName, target.Id),
                SystemUserId = oldOwner.Id,
                TeamTemplateId = templateId
            };

            service.Execute(addRequest);
        }
        catch (Exception ex)
        {
            tracing.Trace($"AddOldOwnerToSalesTeamPlugin: {ex}");
            throw;
        }
    }
}
