using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Yatter.Storage.Azure;
using YatterOfficialSimpleBlobManagerHttpTrigger.Models;
using YatterOfficialSimpleBlobManagerHttpTrigger.Models.BlobStorage;

namespace YatterOfficialSimpleBlobManagerHttpTrigger
{
    /* 
     * EXAMPLE CALLS:
     * 
        OPERATION: exists
        {URL}/api/data?operation=exists&path=myfile.txt&trequest=GeneralBlobRequest&tresponse=BlobResponse 
        POST, GET
        Response: 
        200 OK with {"DataType":"Yatter.Storage.Azure.ExistsResponse","Exists":true} in Body
        200 OK with {"DataType":"Yatter.Storage.Azure.ExistsResponse","Exists":false} in Body
        400 BadRequest with MessageDto serialized in body

        OPERATION: get
        {URL}/api/data?operation=get&path=myfile.txt&trequest=GeneralBlobRequest&tresponse=BlobResponse 
        POST, GET
        Response:
        200 OK with blob content in body
        400 BadRequest with MessageDto serialized in body

        OPERATION: add
        {URL}/api/data?operation=add&path=myfile.txt&trequest=GeneralBlobRequest&tresponse=BlobResponse 
        POST upload content in body of request
        Response:
        200 OK
        400 BadRequest with MessageDto serialized in body

        OPERATION: delete
        {URL}/api/data?operation=delete&path=myfile.txt&trequest=GeneralBlobRequest&tresponse=BlobResponse POST, GET
        POST, GET
        Response: 
        200 OK
        400 BadRequest with MessageDto serialized in body
     * */

    public static class YatterOfficialBlobManagerHttpTrigger
    {
        [FunctionName("YatterOfficialBlobManagerHttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "data")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string connectionString = System.Environment.GetEnvironmentVariable("YATTER_STORAGE_CONNECTIONSTRING"); // Goto the Azure Resource for this HttpTrigger, select Settings/Configuration, then 'New application setting'
            // The Value should be the connection string

            #region 'Dead Canary' Secret Key handling
            // i.e. If there isn't the correct security key and value in the request header, 'the canary is dead' and we prevent the request from completing

            string xYatterCanaryHeader = req.Headers[System.Environment.GetEnvironmentVariable("YATTER_REQUESTHEADER_KEY")]; // Goto the Azure Resource for this HttpTrigger, select Settings/Configuration, then 'New application setting'
            // The Value should be your desired header key, e.g. XMyHeaderKeyName

            var appSecrets = new string[]
            {
                System.Environment.GetEnvironmentVariable("YATTER_REQUESTHEADER_VALUE") // Goto the Azure Resource for this HttpTrigger, select Settings/Configuration, then 'New application setting'
                // The Value should be your secret, e.g. generate a GUID and put that in

                // put as many secrets in this string as your decentralised circumstances dictate
            };


            bool hasSecret = false;

            for (int x = 0; x < appSecrets.Length; x++)
            {
                if (xYatterCanaryHeader.Equals(appSecrets[x]))
                {
                    hasSecret = true;
                    log.LogInformation($"Secret: {appSecrets[x]}");
                }
            }

            if (!string.IsNullOrEmpty(xYatterCanaryHeader) && hasSecret)
            {
                // Pass-through
                log.LogInformation("Canary Alive!");
            }
            else
            {
                log.LogInformation("Dead Canary!");
                return new BadRequestObjectResult(new MessageDto { Message = "Dead Canary!" });
            }
            #endregion

            string operation = req.Query["operation"];
            string path = req.Query["path"];
            string trequest = req.Query["trequest"];
            string tresponse = req.Query["tresponse"];

            if (!string.IsNullOrEmpty(operation) && !string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(connectionString))
            {
                var blobManager = new Yatter.Storage.Azure.ResponsiveBlobManager();

                if (string.IsNullOrEmpty(trequest) || trequest.Equals("GeneralBlobRequest"))
                {
                    var blobRequest = new GeneralBlobRequest();
                    blobRequest.SetConnectionString(connectionString);
                    blobRequest.SetBlobPath(path);
                    // NOTE: CONTAINER NAME STORED IN YATTER_STORAGE_GENERALBLOBREQUESTCONTAINER, IN GeneralBlobRequest

                    if (string.IsNullOrEmpty(tresponse) || tresponse.Equals("BlobResponse"))
                    {
                        log.LogInformation($"Executing Operation '{operation}', '{blobRequest.ContainerName}', '{blobRequest.BlobPath}'");
                        return await Operate<BlobResponse, Models.BlobStorage.GeneralBlobRequest>(log, req, operation, blobManager, blobRequest);
                    }
                }
                /*
                 * If you want access to any other container other than the one defined in the GeneralBlobRequest class, 
                 * you will have to create a class that implements Yatter.Storage.Azure.RequestBase and
                 * define the container name in it. You can either use the existing BlobResponse, or define a new one by creating a class
                 * that implements Yatter.Storage.Azure.ResponseBase
                 * 
                 * The following 'else if' demonstrates the if-else handling pattern to accommodate any additional items
                 * 
                else if (string.IsNullOrEmpty(trequest) || trequest.Equals("MyBespokeBlobRequest"))
                {
                    // create MyBespokeBlobRequest in Models/BlobStorage and fill out this code block using same pattern as above.

                    // If you want to handle the BlobResponse differently, just create a new MyBlobResponse, inherit Yatter.Storage.Azure.ResponseBase and
                    // use it's properties / override, as appropriate
                }
                */
            }
            else
            {
                if (string.IsNullOrEmpty(operation) || string.IsNullOrEmpty(path))
                {
                    log.LogInformation($"Exiting, BadRequest, Querystring is not correctly formed");
                    return new BadRequestObjectResult(new MessageDto { Message = "Querystring is not correctly formed" });
                }

                if (string.IsNullOrEmpty(connectionString))
                {
                    log.LogInformation($"Exiting, BadRequest, Our Azure ConnectionString has not been set internally, please see an Administrator!");
                    return new BadRequestObjectResult(new MessageDto { Message = "Our Azure ConnectionString has not been set internally, please see an Administrator!" });
                }
            }

            log.LogInformation($"Exiting, BadRequest, You've hit the 'Failed API Catch-All', no further information is available.");
            return new BadRequestObjectResult(new MessageDto { Message = "You've hit the 'Failed API Catch-All', no further information is available." });
        }

        private static async Task<IActionResult> Operate<TResponse, TRequest>(ILogger log, HttpRequest req, string operation, Yatter.Storage.Azure.ResponsiveBlobManager responsiveBlobManager, TRequest blobRequest) where TResponse : ResponseBase, new() where TRequest : RequestBase, new()
        {
            if (operation.Equals("get"))
            {
                var response = await responsiveBlobManager.GetBlobAsync<TResponse, TRequest>(blobRequest);
                if (response.IsSuccess)
                {
                    log.LogInformation($"Exiting, Success, with Content");
                    return new OkObjectResult(response.Content);
                }
                else
                {
                    log.LogInformation($"Exiting, BadRequest, {response.Message}");
                    return new BadRequestObjectResult(JsonConvert.SerializeObject(new MessageDto { Message = response.Message }));
                }
            }
            else if (operation.Equals("add"))
            {
                blobRequest.BlobContent = await new StreamReader(req.Body).ReadToEndAsync();
                var response = await responsiveBlobManager.UploadBlobAsync<BlobResponse, TRequest>(blobRequest);
                if (response.IsSuccess)
                {
                    log.LogInformation($"Exiting, Success, with string.Empty");
                    return new OkObjectResult(string.Empty);
                }
                else
                {
                    log.LogInformation($"Exiting, BadRequest, {response.Message}");
                    return new BadRequestObjectResult(JsonConvert.SerializeObject(new MessageDto { Message = response.Message }));
                }
            }
            else if (operation.Equals("exists"))
            {
                var response = await responsiveBlobManager.ExistsBlobAsync<BlobResponse, TRequest>(blobRequest);
                if (response.IsSuccess)
                {
                    var existsDto = JsonConvert.DeserializeObject<Yatter.Storage.Azure.ExistsResponse>(response.Message);

                    return new OkObjectResult(response.Message);
                }
                else
                {
                    log.LogInformation($"Exiting, BadRequest, {JsonConvert.SerializeObject(new MessageDto { Message = response.Message })}");
                    return new BadRequestObjectResult(JsonConvert.SerializeObject(new MessageDto { Message = response.Message }));
                }
            }
            else if (operation.Equals("delete"))
            {
                blobRequest.BlobContent = await new StreamReader(req.Body).ReadToEndAsync();
                var response = await responsiveBlobManager.DeleteBlobAsync<BlobResponse, TRequest>(blobRequest);
                if (response.IsSuccess)
                {
                    log.LogInformation($"Exiting, Success, with string.Empty");
                    return new OkObjectResult(string.Empty);
                }
                else
                {
                    log.LogInformation($"Exiting, BadRequest, {JsonConvert.SerializeObject(new MessageDto { Message = response.Message })}");
                    return new BadRequestObjectResult(JsonConvert.SerializeObject(new MessageDto { Message = response.Message }));
                }
            }
            else
            {
                log.LogInformation($"Exiting, BadRequest, {JsonConvert.SerializeObject(new MessageDto { Message = $"Unexpected operation in querystring: '{operation}'" })}");
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new MessageDto { Message = $"Unexpected operation in querystring: '{operation}'" }));
            }

            // Unreachable
        }
    }
}

