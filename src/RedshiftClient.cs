using System.Data.Odbc;

public class RedshiftClient : IDisposable
{
    public const int VarcharMaxSizeInBytes = 65_535;
    private readonly OdbcConnection connection;
    private bool disposed;

    public RedshiftClient(ConfigRedshift redsfhiftInfo)
    {
        connection = CreateConnection(redsfhiftInfo);
    }
    public void DropAndCreateTable(string schema, string table, string[] columnLines)
    {
        DisposeGuard();
        schema = $"\"{schema}\"";
        table = $"\"{table}\"";
        var tablePath = $"{schema}.{table}";
        var sql =
@$"drop table if exists {tablePath};
create table {tablePath}
(
    {string.Join(",\n    ",columnLines)}
);";
        Console.WriteLine(sql);
        Run(sql);
    }
    public void Copy(string schemaName, string tableName, string s3CsvFilePath, ConfigS3 s3Info)
    {
        DisposeGuard();
        Console.WriteLine("RedshiftClient. COPY");
        schemaName = '\"'+schemaName+'\"';
        tableName = '\"'+tableName+'\"';
        var sql =
$@"COPY {schemaName}.{tableName}
FROM 's3://{s3CsvFilePath}'
credentials 'aws_access_key_id={s3Info.AccessKey};aws_secret_access_key={s3Info.SecretKey}'
IGNOREHEADER 1 DATEFORMAT 'auto' TIMEFORMAT 'auto' ACCEPTINVCHARS CSV;";
        Run(sql);
    }
    public void CreateSchemaIfNotExists(string schemaName)
    {
        DisposeGuard();
        Console.WriteLine($"Creating schema \"{schemaName}\" if not exists");
        var sql = $"create schema if not exists \"{schemaName}\"";
        using var command = new OdbcCommand(sql, connection);
        command.ExecuteReader();
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        connection.Dispose();
    }
    private void Run(string sql)
    {
        DisposeGuard();
        using var command = new OdbcCommand(sql, connection);
        var min5 = 300;
        command.CommandTimeout = min5;
        command.ExecuteScalar();
    }
    private void DisposeGuard()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(S3Client));
    }
    private static OdbcConnection CreateConnection(ConfigRedshift redsfhiftInfo)
    {
        var fields = new []
        {
            "Driver=Amazon Redshift ODBC Driver (x64)",
            $"Server={redsfhiftInfo.Host}",
            $"Port={redsfhiftInfo.Port}",
            $"Database={redsfhiftInfo.DbName}",
            $"UID={redsfhiftInfo.Username}",
            $"PWD={redsfhiftInfo.Password}",
            "SSL=true",
        };
        var connectionString = string.Join(';',fields);
        var connection = new OdbcConnection(connectionString);
        connection.Open();
        return connection;
    }
}