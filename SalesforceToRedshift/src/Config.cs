#nullable disable warnings
public class Config
{
    public readonly ConfigS3 ConfigS3;
    public readonly ConfigRedshift ConfigRedshift;
    public readonly string SalesforceUserName;
    public readonly string LocalZipFilesSearchPath;
    public readonly string LocalWorkingDirectory;
    public readonly string S3WorkingDirectory;
}
public class ConfigS3
{
    public readonly string AccessKey;
    public readonly string SecretKey;
}
public class ConfigRedshift
{
    public readonly string Host;
    public readonly int Port;
    public readonly string DbName;
    public readonly string Username;
    public readonly string Password;
    public readonly string SchemaName;
}