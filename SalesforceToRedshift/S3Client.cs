using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

// TODO. Multipart upload
public class S3Client : IDisposable
{
    private readonly BasicAWSCredentials creds;
    private readonly AmazonS3Client client;
    private readonly TransferUtility transferUtility;
    private readonly string bucketName;
    private bool disposed;

    public S3Client(S3Config s3Info)
    {
        creds = new BasicAWSCredentials(s3Info.AccessKey,s3Info.SecretKey);
        client = new AmazonS3Client(creds);
        transferUtility = new TransferUtility(client);
        bucketName = s3Info.BucketName;
    }
    public bool FileExists(string key)
    {
        DisposeGuard();
        try
        {
            client.GetObjectAsync(bucketName,key).Wait();
            return true;
        }
        catch (System.Exception)
        {
            return false;
        }
    }
    public void DownloadFile(string key, string filePath)
    {
        DisposeGuard();
        var request = new TransferUtilityDownloadRequest
        {
            BucketName = bucketName,
            Key = key,
            FilePath = filePath,
        };
        Console.Write($"Downloading file from S3");
        using var progressBar = new ProgressBar();
        request.WriteObjectProgressEvent += (object? sender, WriteObjectProgressArgs e) =>
        {
            progressBar.Report((double)e.PercentDone/100);
        };
        transferUtility.Download(request);
        progressBar.Dispose();
        Console.WriteLine($"DONE");
    }
    public void UploadFile(string key, string filePath)
    {
        DisposeGuard();
        Console.WriteLine($"Uploading file to S3 {filePath} -> {bucketName}");
        var request = new TransferUtilityUploadRequest
        {
            BucketName = bucketName,
            Key = key,
            FilePath = filePath,
        };
        using var progressBar = new ProgressBar();
        request.UploadProgressEvent += (object? sender, UploadProgressArgs e) =>
        {
            progressBar.Report((double)e.PercentDone/100);
        };
        transferUtility.Upload(request);
        progressBar.Dispose();
    }
    public async Task UploadDirectory(string directoryPath)
    {
        DisposeGuard();
        var config = new AmazonS3Config();
        var bucketRegion = RegionEndpoint.USEast1;
        var request = new TransferUtilityUploadDirectoryRequest
        {
            BucketName = bucketName,
            Directory = directoryPath,
            SearchOption = SearchOption.AllDirectories,
            SearchPattern = "*",
            UploadFilesConcurrently = true,
        };
        await transferUtility.UploadDirectoryAsync(request);
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        client.Dispose();
        transferUtility.Dispose();
    }
    private void DisposeGuard()
    {
        if (disposed) throw new ObjectDisposedException(nameof(S3Client));
    }
}
// public void UploadFileMultipart(string bucketName, string filePath)
// {
//     using var client = new AmazonS3Client(creds);
//     using var transferUtil = new TransferUtility(client);
//     var key = Path.GetFileName(filePath);
//     var request = new TransferUtilityUploadRequest
//     {
//         BucketName = bucketName,
//         Key = key,
//         FilePath = filePath,
//         StorageClass = S3StorageClass.StandardInfrequentAccess,
//         PartSize = (long)Math.Pow(2, 20) * 5,
//     };
//     Console.WriteLine($"Starting file upload to S3");
//     Console.WriteLine($"{filePath} -> {bucketName}");
//     using var progressBar = new ProgressBar();
//     request.UploadProgressEvent += (object? sender, UploadProgressArgs e) => { progressBar.Report((double)e.PercentDone/100); };
//     transferUtil.Upload(request);
//     progressBar.Dispose();
//     Console.WriteLine($"Finished file upload to S3");
// }
// public async Task UploadFileMultipart(string filePath)
// {
//     DisposeGuard();
//     using var transferUtil = new TransferUtility(client);
//     var key = Path.GetFileName(filePath);
//     var uploadResponses = new List<UploadPartResponse>();
//     var initiateRequest = new InitiateMultipartUploadRequest
//     {
//         BucketName = bucketName,
//         Key = key
//     };
//     var initResponse = await client.InitiateMultipartUploadAsync(initiateRequest);
//     var contentLength = new FileInfo(filePath).Length;
//     var partSize = 5 * (long)Math.Pow(2, 20);

//     Console.WriteLine(initResponse.UploadId);

//     try
//     {
//         long filePosition = 0;
//         for (int i = 1; filePosition < contentLength; i++)
//         {
//             var uploadRequest = new UploadPartRequest
//             {
//                 BucketName = bucketName,
//                 Key = key,
//                 UploadId = initResponse.UploadId,
//                 PartNumber = i,
//                 PartSize = partSize,
//                 FilePosition = filePosition,
//                 FilePath = filePath
//             };
//             using var progressBar = new ProgressBar();
//             uploadRequest.StreamTransferProgress += (object? sender, StreamTransferProgressArgs e) => { progressBar.Report((double)e.PercentDone/100); };
//             var uploadResponse = await client.UploadPartAsync(uploadRequest);
//             progressBar.Dispose();
//             Console.WriteLine("DONE");
//             uploadResponses.Add(uploadResponse);
//             filePosition += partSize;
//         }
//         var completeRequest = new CompleteMultipartUploadRequest
//         {
//             BucketName = bucketName,
//             Key = key,
//             UploadId = initResponse.UploadId
//         };
//         completeRequest.AddPartETags(uploadResponses);
//         var completeUploadResponse = await client.CompleteMultipartUploadAsync(completeRequest);
//     }
//     catch (Exception exception)
//     {
//         Console.WriteLine($"An AmazonS3Exception was thrown: {exception.Message}");
//         AbortMultipartUploadRequest abortMPURequest = new AbortMultipartUploadRequest
//         {
//             BucketName = bucketName,
//             Key = key,
//             UploadId = initResponse.UploadId
//         };
//         await client.AbortMultipartUploadAsync(abortMPURequest);
//     }
// }