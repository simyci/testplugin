using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;

public class AddOldOwnerToSalesTeamPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        if (context.MessageName != "Update" || !context.InputParameters.Contains("Target"))
        {
            return;
        }

        if (!(context.InputParameters["Target"] is Entity target) || target.LogicalName != "opportunity")
        {
            return;
        }

        // Only run if owner is being changed
        if (!target.Attributes.Contains("ownerid"))
        {
            return;
        }

        Entity preImage = null;
        if (context.PreEntityImages.TryGetValue("PreImage", out var pre))
        {
            preImage = (Entity)pre;
        }

        if (preImage == null || !preImage.Attributes.Contains("ownerid"))
        {
            return;
        }

        var oldOwner = preImage.GetAttributeValue<EntityReference>("ownerid");
        var newOwner = target.GetAttributeValue<EntityReference>("ownerid");

        // Ignore if owner not changed
        if (oldOwner == null || newOwner == null || oldOwner.Id == newOwner.Id)
        {
            return;
        }

        var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        var service = factory.CreateOrganizationService(context.UserId);

        // Retrieve access team template for opportunity sales team
        var templateQuery = new QueryExpression("teamtemplate")
        {
            ColumnSet = new ColumnSet("teamtemplateid"),
        };
        templateQuery.Criteria.AddCondition("objecttypecode", ConditionOperator.Equal, "opportunity");
        templateQuery.Criteria.AddCondition("name", ConditionOperator.Equal, "Opportunity Sales Team");

        var template = service.RetrieveMultiple(templateQuery).Entities.FirstOrDefault();
        if (template == null)
        {
            throw new InvalidPluginExecutionException("Opportunity Sales Team template not found.");
        }

        var templateId = template.GetAttributeValue<Guid>("teamtemplateid");

        // Add the previous owner to the opportunity access team
        var addRequest = new AddUserToRecordTeamRequest
        {
            Record = new EntityReference("opportunity", target.Id),
            SystemUserId = oldOwner.Id,
            TeamTemplateId = templateId
        };

        service.Execute(addRequest);
    }
}
