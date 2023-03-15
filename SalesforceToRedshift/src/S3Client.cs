using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

public class S3Client : IDisposable
{
    private readonly BasicAWSCredentials creds;
    private readonly AmazonS3Client client;
    private readonly TransferUtility transferUtility;
    private bool disposed;

    public S3Client(ConfigS3 s3Info)
    {
        creds = new BasicAWSCredentials(s3Info.AccessKey,s3Info.SecretKey);
        client = new AmazonS3Client(creds);
        transferUtility = new TransferUtility(client);
    }
    public S3Client(string accessKey, string secretKey)
    {
        creds = new BasicAWSCredentials(accessKey,secretKey);
        client = new AmazonS3Client(creds,Amazon.RegionEndpoint.USEast1);
        transferUtility = new TransferUtility(client);
    }
    // public async Task<string[]> ListFiles(string s3DirectoryPath)
    // {
    //     var listObjectRequest = new ListObjectsV2Request();
    //     listObjectRequest.BucketName = bucketName;
    //     listObjectRequest.Prefix = key;
    //     var response = await client.ListObjectsV2Async(listObjectRequest);
    //     return response.S3Objects.Select(x => x.Key).ToArray();
    // }
    public async Task<string> GetContentType(string s3FilePath)
    {
        var bucketNameAndKey = BucketNameAndKey(s3FilePath);
        var response = await client.GetObjectMetadataAsync(bucketNameAndKey.Item1,bucketNameAndKey.Item2);
        return response.Headers.ContentType;
    }
    public async Task SetContentType(string s3FilePath, string value)
    {
        var bucketNameAndKey = BucketNameAndKey(s3FilePath);
        var request = new CopyObjectRequest
        {
            MetadataDirective = S3MetadataDirective.REPLACE,
            ContentType = value,
            SourceBucket = bucketNameAndKey.Item1,
            SourceKey = bucketNameAndKey.Item2,
            DestinationBucket = bucketNameAndKey.Item1,
            DestinationKey = bucketNameAndKey.Item2
        };
        await client.CopyObjectAsync(request);
    }
    public bool FileExists(string path)
    {
        DisposeGuard();
        var bucketNameAndKey = BucketNameAndKey(path);
        try
        {
            client.GetObjectAsync(bucketNameAndKey.Item1,bucketNameAndKey.Item2).Wait();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public void UploadFile(string filePathLocal, string filePathS3)
    {
        DisposeGuard();
        var bucketNameAndKey = BucketNameAndKey(filePathS3);
        Console.WriteLine($"Uploading file to S3 {filePathLocal} -> {filePathS3}");
        var request = new TransferUtilityUploadRequest
        {
            BucketName = bucketNameAndKey.Item1,
            Key = bucketNameAndKey.Item2,
            FilePath = filePathLocal,
        };
        using var progressBar = new ProgressBar();
        request.UploadProgressEvent += (object? sender, UploadProgressArgs e) =>
        {
            progressBar.Report((double)e.PercentDone/100);
        };
        transferUtility.Upload(request);
        progressBar.Dispose();
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
        if (disposed)
            throw new ObjectDisposedException(nameof(S3Client));
    }
    private static (string,string) BucketNameAndKey(string path)
    {
        var split = path.Split('/');
        var bucketName = split.First();
        var key = string.Join('/',split.Skip(1));
        return (bucketName,key);
    }
}