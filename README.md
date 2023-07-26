# azure-blob-storage-foreach
Perform operation for each blob in given azure storage.

To connect to your blob storage, create secrets.json in fillowing format:

`
{
  "ConnectionStrings": {
    "BlobStorage": "My connection string to blob storage."
  }
}
`

More info on secrets to be found [here](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0&tabs=windows).
