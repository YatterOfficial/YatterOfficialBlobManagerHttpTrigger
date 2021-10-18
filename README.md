# YatterOfficialBlobManagerHttpTrigger

## Overview

This HttpTrigger uses a TRequest / TResponse URL pattern with secret key/s in the Request Header to facilitate highly secure Azure Storage blob accessibility, using the following operations:

- exists
- get
- add
- delete

An example url is as follows:

- ```{URL}/api/data?operation=get&path=myfile.txt&trequest=GeneralBlobRequest&tresponse=BlobResponse```

Blobs can only be accessed where the container name is defined in a class that inherits from RequestBase, and without any modification, this Blob Manager can interact with a container that is defined in the Environmental Settings with the key ```YATTER_STORAGE_GENERALBLOBREQUESTCONTAINER```

Additional containers can be accessed by creating a new class, see below for an example.

Requests will only be accepted if there is a key in the header that is defined by the Environmental Variable key ```YATTER_REQUESTHEADER_KEY``` and which has a corresponding ```YATTER_REQUESTHEADER_VALUE``` value.

Special handling of the blob can be undertaken by creating a new class that inherits from ResponseBase, and replacing the TResponse class with the new one, eg:

- ```{URL}/api/data?operation=get&path=myfile.txt&trequest=GeneralBlobRequest&tresponse=MySpecialBlobResponse```

In such a case, additional handling will have to be undertaken in the TRequest / TResponse if-else routine, which is quite easy to implement.

## Quickstart

Set up the following Environmental Variables:

- ```YATTER_STORAGE_CONNECTIONSTRING```
- ```YATTER_REQUESTHEADER_KEY```
- ```YATTER_REQUESTHEADER_VALUE```
- ```YATTER_STORAGE_GENERALBLOBREQUESTCONTAINER```

Environment:

- If running locally, set these values in the file: local.settings.json
- If running in Azure, go to the Azure Resource for this HttpTrigger, select Settings/Configuration, then 'New application setting', and add the keys above, and their corresponding values.


 
EXAMPLE CALLS:

```
OPERATION: exists
{URL}/api/data?operation=exists&path=myfile.txt&trequest=GeneralBlobRequest&tresponse=BlobResponse 
POST, GET
Response: 
200 OK with {"DataType":"Yatter.Storage.Azure.ExistsResponse","Exists":true} in Body
200 OK with {"DataType":"Yatter.Storage.Azure.ExistsResponse","Exists":false} in Body
400 BadRequest with TResponse serialized in body with Message exposed from ResponseBase
 
OPERATION: get
{URL}/api/data?operation=get&path=myfile.txt&trequest=GeneralBlobRequest&tresponse=BlobResponse 
POST, GET
Response:
200 OK with blob content in body
400 BadRequest with TResponse serialized in body with Message exposed from ResponseBase

OPERATION: add
{URL}/api/data?operation=add&path=myfile.txt&trequest=GeneralBlobRequest&tresponse=BlobResponse 
POST upload content in body of request
Response:
200 OK
400 BadRequest with TResponse serialized in body with Message exposed from ResponseBase
 
OPERATION: delete
{URL}/api/data?operation=delete&path=myfile.txt&trequest=GeneralBlobRequest&tresponse=BlobResponse POST, GET
POST, GET
Response: 
200 OK
400 BadRequest with TResponse serialized in body with Message exposed from ResponseBase
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
  - ```{URL}/api/data?operation=exists&path=myfile.txt&trequest=MyNewBlobRequest&tresponse=BlobResponse```
- as well as adding the new 'MyNewBlobRequest' to an if-else in the code in [this](https://github.com/YatterOfficial/YatterOfficialBlobManagerHttpTrigger/blob/master/YatterOfficialSimpleBlobManagerHttpTrigger/YatterBlobManagerHttpTrigger.cs) file.

