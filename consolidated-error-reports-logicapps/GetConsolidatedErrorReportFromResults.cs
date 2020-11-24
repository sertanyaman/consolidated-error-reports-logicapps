//*****************************************************************************************************************
// Consolidated error report generator azure function for logic app action results
// Author : Tayfun Sertan Yaman
//https://github.com/sertanyaman/consolidated-error-reports-logicapps
// MIT License
//*****************************************************************************************************************

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace consolidated_error_reports_logicapps
{
    /// <summary>
    /// Consolidated error report generator azure function for logic app action results
    /// Author : Tayfun Sertan Yaman
    /// https://github.com/sertanyaman/consolidated-error-reports-logicapps
    /// MIT License
    /// </summary>
    public static class GetConsolidatedErrorReportFromResults
    {
        [FunctionName("GetConsolidatedErrorReportFromResults")]
        public static async Task<IActionResult> Run(
                                    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            const string defaultFaultMessage = "An internal error has occured, unable to process the request";
            const string defaultSource = "Logic app";

            System.Collections.Generic.List<Models.LogicAppsError> logicAppsErrors = new System.Collections.Generic.List<Models.LogicAppsError>();

            log.LogInformation("Calling - GetConsolidatedErrorReportFromResults");

            dynamic results = JsonConvert.DeserializeObject<dynamic>(await req.ReadAsStringAsync());

            foreach (dynamic result in results)
            {
                if (result.status != null && (result.outputs != null || result.error != null))
                {
                    if (result.status == "Failed")
                    {
                        bool parsedOk = false;

                        //pre init error message
                        Models.LogicAppsError logicAppsError = new Models.LogicAppsError()
                        {
                            BlockName = result.name,
                            StartTime = result.startTime,
                            ClientTrackingId = result.clientTrackingId
                        };

                        //Initialize generic values
                        logicAppsError.Code = (result.outputs?.statusCode != null ? $"{result.outputs.statusCode}" : (result.code != null ? result.code : "Error"));
                        logicAppsError.StackTrace = "";
                        logicAppsError.Source = !String.IsNullOrEmpty(logicAppsError.BlockName) ? logicAppsError.BlockName : defaultSource;

                        try
                        {
                            //SET Message field

                            //Try direct error tag from internal logic app problems
                            if (result.error != null)
                            {
                                if (result.error is JObject)
                                {
                                    //Try error tag in result, switches and others
                                    if (!parsedOk && result.error.message != null && result.error.message != String.Empty)
                                    {
                                        logicAppsError.Message = result.error.message;
                                        parsedOk = true;
                                    }

                                    //Try error tag in result capital Message
                                    if (!parsedOk && result.error.Message != null && result.error.Message != String.Empty)
                                    {
                                        logicAppsError.Message = result.error.message;
                                        parsedOk = true;
                                    }
                                }
                                else
                                {
                                    //Try error tag as it is
                                    logicAppsError.Message = $"{result.error}";
                                    parsedOk = true;
                                }
                            }

                            //if body tag exists
                            if (result.outputs?.body != null)
                            {
                                if (result.outputs.body is JObject)
                                {
                                    //Try body error message from V1 .NET logic apps
                                    if (!parsedOk && result.outputs.body?.Message != null && result.outputs.body?.ClassName != null && result.outputs.body.Message != String.Empty)
                                    {
                                        logicAppsError.Message = result.outputs.body.Message;
                                        logicAppsError.Source = result.outputs.body.ClassName ?? logicAppsError.Source;
                                        logicAppsError.StackTrace = result.outputs.body.StackTraceString ?? "";
                                        parsedOk = true;
                                    }

                                    //Try body error message from V3 .NET logic apps
                                    if (!parsedOk && result.outputs.body?.message != null && result.outputs.body?.source != null && result.outputs.body.message != String.Empty)
                                    {
                                        logicAppsError.Message = result.outputs.body.message;
                                        logicAppsError.Source = result.outputs.body.source ?? logicAppsError.Source;
                                        logicAppsError.StackTrace = result.outputs.body.stackTrace ?? "";
                                        parsedOk = true;
                                    }

                                    //Try body error message from Durable functions V2
                                    if (!parsedOk && result.outputs.body?.instanceId != null && result.outputs.body?.runtimeStatus != null && result.outputs.body.output != null)
                                    {
                                        //Try if customStatus contains Exception block
                                        if (result.outputs.body?.customStatus != null && result.outputs.body?.customStatus is JObject
                                            && result.outputs.body?.customStatus.Message != null && result.outputs.body?.customStatus.ClassName != null)
                                        {
                                            logicAppsError.Message = result.outputs.body?.customStatus.Message;
                                            logicAppsError.Source = result.outputs.body?.customStatus.ClassName ?? logicAppsError.Source;
                                            logicAppsError.StackTrace = result.outputs.body?.customStatus.StackTraceString ?? "";
                                            parsedOk = true;
                                        }
                                        else
                                        {
                                            logicAppsError.Message = $"{result.outputs.body.output}";
                                            logicAppsError.Source = result.outputs.body.name;
                                            logicAppsError.StackTrace = "";
                                            parsedOk = true;
                                        }
                                    }

                                    //Try copy same response body array from other logic app call 
                                    if (!parsedOk && result.outputs.body.response != null)
                                    {
                                        var errors = result.outputs.body.response;

                                        //Workaround Newtonsoft version mismatches, never compare types or cast them inbetween different JSON.NET versions
                                        if (errors is JArray)
                                        {
                                            //Copy logic app errors as it is
                                            foreach (dynamic res in errors)
                                            {
                                                //Double check if key error body fields present
                                                if ((res.code != null) && (res.clientTrackingId != null) && (res.message != null) && (res.blockName != null) && (res.stackTrace != null))
                                                {
                                                    Models.LogicAppsError appsError = res.ToObject<Models.LogicAppsError>();
                                                    logicAppsErrors.Add(appsError);
                                                }
                                            }
                                            //Revert to null since we copied the error array as it is, single message not needed anymore
                                            logicAppsError = null;
                                        }
                                        else
                                        {
                                            logicAppsError.Message = errors.ToString();
                                        }

                                        parsedOk = true;
                                    }

                                    //Try body error message from schema problem indicators
                                    if (!parsedOk && result.outputs.body?.error != null)
                                    {
                                        if (result.outputs.body?.error is JObject)
                                        {
                                            //Try body error message from V1 .NET logic apps
                                            if (!parsedOk && result.outputs.body.error.Message != null && result.outputs.body.error.Message != String.Empty)
                                            {
                                                logicAppsError.Message = result.outputs.body.error.Message;
                                                logicAppsError.Source = result.outputs.body.ClassName ?? logicAppsError.Source;
                                                parsedOk = true;
                                            }

                                            //Try body error message from V3 .NET logic apps
                                            if (!parsedOk && result.outputs.body.error.message != null && result.outputs.body.error.code != null && result.outputs.body.error.message != String.Empty)
                                            {
                                                logicAppsError.Message = result.outputs.body.error.message;
                                                logicAppsError.Code = result.outputs.body.error.code ?? logicAppsError.Code;
                                                parsedOk = true;
                                            }

                                        }
                                        else
                                        //Take it as it is
                                        {
                                            logicAppsError.Message = $"{result.outputs.body.error}";
                                            parsedOk = true;
                                        }

                                    }
                                }

                                //Try 4 get outputs body as it is
                                if (!parsedOk)
                                {
                                    logicAppsError.Message = $"{result.outputs.body}";
                                    parsedOk = true;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            parsedOk = false;
                        }

                        //Default
                        if (!parsedOk)
                        {
                            logicAppsError.Message = defaultFaultMessage;
                        }

                        if (logicAppsError != null)
                        {
                            logicAppsErrors.Add(logicAppsError);
                        }
                    }
                }
            }

            return new OkObjectResult(logicAppsErrors);
        }
    }
}
