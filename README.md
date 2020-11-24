# consolidated-error-reports-logicapps
Consolidated error report generator Azure Function for Logic App action results.

Return an array with unified JSON exception messages collecting exceptions from different result bodies of different Azure Logic App actions.

This way you can consolidate different exception messages in a single format from of various Logic app actions and standardize the exception model. It helps if you want to log the errors in Logic apps from somewhere else, store them in a database, show them in a GUI or mail the errors from different sources to the user.

Version 1: works for errors from many standard logic app actions, exceptions coming from C# Azure Functions V1/V2/V3, and exceptions coming from other logic apps using this function. Support for more exception types will be added in future versions.

For usage of the see template logic app with logic app exception handling.

Response JSON exception format :


| Field  | Description |
| ------------- | ------------- |
| Code  | Code given for the exception (ex. 400,500 for http exceptions)  |
| BlockName  | Name of the logic app action which gives this exception  |
| Message  | Exception message  |
| StackTrace  | Stack trace (if applicable)  |
| Source  | Exception creating source (Exception class names in Azure function exceptions)  |
| ClientTrackingId  | Logic apps tracking ID  |
| StartTime  | Logic apps trigger execution datetime  |
