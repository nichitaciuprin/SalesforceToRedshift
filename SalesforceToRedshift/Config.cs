using System.Diagnostics;

public class Config
{
    public S3Config S3Config { get; }
    public RedshiftConfig RedshiftConfig { get; }
    public string SalesforceUserName { get; }
    public string RedsfhitTargetSchemaName { get; }
    public string ZipFilesSearchPath { get; }
    public string DirPath { get; }
    public Config(S3Config s3Config, RedshiftConfig redshiftConfig, string salesforceUserName, string zipFilesSearchPath, string redsfhitTargetSchemaName, string dirPath)
    {
        S3Config = s3Config;
        RedshiftConfig = redshiftConfig;
        SalesforceUserName = salesforceUserName;
        RedsfhitTargetSchemaName = redsfhitTargetSchemaName;
        ZipFilesSearchPath = zipFilesSearchPath;
        DirPath = dirPath;
        Debug.Assert(!string.IsNullOrEmpty(SalesforceUserName));
        Debug.Assert(!string.IsNullOrEmpty(RedsfhitTargetSchemaName));
        Debug.Assert(!string.IsNullOrEmpty(ZipFilesSearchPath));
        Debug.Assert(!string.IsNullOrEmpty(DirPath));
    }
}
public class RedshiftConfig
{
    public string Host { get; }
    public int Port { get; }
    public string DbName { get; }
    public string Username { get; }
    public string Password { get; }
    public RedshiftConfig(string host, int port, string dbname, string username, string password)
    {
        Host = host;
        Port = port;
        DbName = dbname;
        Username = username;
        Password = password;
        Debug.Assert(!string.IsNullOrEmpty(Host));
        Debug.Assert(Port != 0);
        Debug.Assert(!string.IsNullOrEmpty(DbName));
        Debug.Assert(!string.IsNullOrEmpty(Username));
        Debug.Assert(!string.IsNullOrEmpty(Password));
    }
}
public class S3Config
{
    public string AccessKey { get; }
    public string SecretKey { get; }
    public string BucketName { get; }
    public S3Config(string accessKey, string secretKey, string bucketName)
    {
        AccessKey = accessKey;
        SecretKey = secretKey;
        BucketName = bucketName;
        Debug.Assert(!string.IsNullOrEmpty(AccessKey));
        Debug.Assert(!string.IsNullOrEmpty(SecretKey));
        Debug.Assert(!string.IsNullOrEmpty(BucketName));
    }
}