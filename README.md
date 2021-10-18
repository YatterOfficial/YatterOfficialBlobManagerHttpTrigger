# YatterOfficialBlobManagerHttpTrigger


## Quickstart

Set up the following Environmental Variables:

- ```YATTER_STORAGE_CONNECTIONSTRING```
- ```YATTER_REQUESTHEADER_KEY```
- ```YATTER_REQUESTHEADER_VALUE```
- ```YATTER_STORAGE_GENERALBLOBREQUESTCONTAINER```

Environment:

- If running locally, set thease values in the file: local.settings.json
- If running in Azure, go to the Azure Resource for this HttpTrigger, select Settings/Configuration, then 'New application setting', and add the keys above, and their corresponding values.


 
EXAMPLE CALLS:

```
{URL}/api/data?operation=exists&path=myfile.txt&trequest=GeneralBlobRequest&tresponse=BlobResponse 
POST, GET
Response: 
200 OK with {"DataType":"Yatter.Storage.Azure.ExistsResponse","Exists":true} in Body
 
{URL}/api/data?operation=get&path=myfile.txt&trequest=GeneralBlobRequest&tresponse=BlobResponse 
POST, GET
Response:
200 OK with blob content in body
 
{URL}/api/data?operation=add&path=myfile.txt&trequest=GeneralBlobRequest&tresponse=BlobResponse 
POST upload content in body of request
Response:
200 OK
 
{URL}/api/data?operation=delete&path=myfile.txt&trequest=GeneralBlobRequest&tresponse=BlobResponse POST, GET
POST, GET
Response: 
200 OK
```

These calls will only allow file interaction with the container that is declared in ```YATTER_STORAGE_GENERALBLOBREQUESTCONTAINER```

- To add a new container, create a new BlobRequest, using the following pattern:

```
    public sealed class MyNewBlobRequest : RequestBase
    {
        public MyNewBlobRequest()
        {
            ContainerName = System.Environment.GetEnvironmentVariable("YATTER_STORAGE_MYNEWBLOBREQUESTCONTAINER");

            // Goto the Azure Resource for this HttpTrigger, select Settings/Configuration, then 'New application setting'
        }

        public void SetConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public void SetContainerName(string name)
        {
            ContainerName = name;
        }

        public void SetBlobPath(string path)
        {
            BlobPath = path;
        }
    }
```

- then adjust the URL to suit the new BlobRequest:
  - {URL}/api/data?operation=exists&path=myfile.txt&trequest=MyNewBlobRequest&tresponse=BlobResponse 
- as well as adding the new 'MyNewBlobRequest' to an if-else in the code.

