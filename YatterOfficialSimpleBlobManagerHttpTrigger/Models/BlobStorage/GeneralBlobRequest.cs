using System;
using Yatter.Storage.Azure;

namespace YatterOfficialSimpleBlobManagerHttpTrigger.Models.BlobStorage
{
    public sealed class GeneralBlobRequest : RequestBase
    {
        public GeneralBlobRequest()
        {
            ContainerName = System.Environment.GetEnvironmentVariable("YATTER_STORAGE_GENERALBLOBREQUESTCONTAINER");

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
}

