# Dynamics 365 Opportunity Plugin

This repository contains a sample plug-in that automatically adds the previous owner of an opportunity to the opportunity's "Opportunity Sales Team" access team whenever the record is reassigned.

## Summary

When the owner of an opportunity is changed, the `AddOldOwnerToSalesTeamPlugin` runs. It uses the standard **AddUserToRecordTeamRequest** CRM message to add the previous owner to the "Opportunity Sales Team" access team. If the user is already a member of the team, the request does nothing, so duplicates are avoided.

## Registration

Register the plug-in on the **Update** message of the `opportunity` entity and include a pre-image named `PreImage` that contains the `ownerid` attribute. Make sure an Access Team Template exists for Opportunity with the name **"Opportunity Sales Team"**.

## Building

Compile the project using the CRM SDK assemblies targeting .NET Framework 4.6.2 or later. Only the plug-in class file is provided; you may include it in your existing plug-in assembly project.
