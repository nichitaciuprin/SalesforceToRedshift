using System.Data;
using System.Data.Odbc;

public class RedshiftClient : IDisposable
{
    public const int VarcharMaxBytesCount = 65_535;
    public OdbcConnection connection;
    private bool disposed;

    public RedshiftClient(RedshiftConfig redsfhiftInfo)
    {
        this.connection = CreateConnection(redsfhiftInfo);
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
        Run(sql);
    }
    public void Copy(string schemaName, string tableName, string s3CsvFilePath, S3Config s3Info)
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
    public void MoveTables(string srcSchema, string targetSchema)
    {
        DisposeGuard();
        var tableNames = GetTableNames(srcSchema);
        foreach (var tableName in tableNames)
            MoveTable(srcSchema,tableName,targetSchema,tableName);
    }
    public void MoveTable(string srcSchema, string srcTable, string targetSchema, string targetTable)
    {
        DisposeGuard();
        var srcPath = $"\"{srcSchema}\".\"{srcTable}\"";
        var targetPath = $"\"{targetSchema}\".\"{targetTable}\"";
        using var command = new OdbcCommand();
        command.Connection = connection;
        command.CommandText = $"CREATE TABLE {targetPath} (LIKE {srcPath})";     command.ExecuteNonQuery();
        command.CommandText = $"ALTER TABLE {targetPath} APPEND FROM {srcPath}"; command.ExecuteNonQuery();
        command.CommandText = $"DROP TABLE {srcPath}";                           command.ExecuteNonQuery();
    }
    public void CreateSchemaIfNotExists(string schemaName)
    {
        DisposeGuard();
        Console.WriteLine($"Creating schema \"{schemaName}\" if not exists");
        var sql = $"create schema if not exists \"{schemaName}\"";
        using var command = new OdbcCommand(sql, connection);
        command.ExecuteReader();
    }
    public void DropSchema(string schemaName)
    {
        DisposeGuard();
        var sql = $"drop schema if exists {schemaName}";
        var command = new OdbcCommand(sql, connection);
        command.ExecuteReader();
    }
    public DataTable GetUserTable()
    {
        DisposeGuard();
        var sql = $"select * from pg_user";
        using var command = new OdbcCommand(sql, connection);
        using var reader = command.ExecuteReader();
        var dataTable = new DataTable();
        return dataTable;
    }
    public string[] GetSchemaNames()
    {
        DisposeGuard();
        var sql = $"select nspname from pg_namespace";
        var command = new OdbcCommand(sql, connection);
        var dataTable = new DataTable();
        var reader = command.ExecuteReader();
        dataTable.Load(reader);
        return dataTable.AsEnumerable().Select(x => x[0] as string).Select(x=>x!).ToArray();
    }
    public string[] GetTableNames(string schema)
    {
        DisposeGuard();
        schema = $"\"{schema}\"";
        var sql =
        @$"
            select t.table_name
            from information_schema.tables t
            where t.table_schema = {schema}
        ";
        using var command = new OdbcCommand(sql, connection);
        using var dataTable = new DataTable();
        using var reader = command.ExecuteReader();
        dataTable.Load(reader);
        return dataTable.AsEnumerable().Select(x => x[0] as string).Select(x=>x!).ToArray();
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
        command.CommandTimeout = 300; // 5 MIN
        command.ExecuteScalar();
    }
    private void DisposeGuard()
    {
        if (disposed) throw new ObjectDisposedException(nameof(S3Client));
    }
    private void PrintOdbcDrivers()
    {
        // #if Windows
        // #endif

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();

        var localMachineHive = Microsoft.Win32.Registry.LocalMachine;
        var odbcDriversKey = localMachineHive.OpenSubKey(@"SOFTWARE\ODBC\ODBCINST.INI\ODBC Drivers");
        if (odbcDriversKey == null) return;
        var msg = string.Join('\n',odbcDriversKey.GetValueNames());
        Console.WriteLine(msg);
    }
    private OdbcConnection CreateConnection(RedshiftConfig redsfhiftInfo)
    {
        var fields = new[]
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
    private string ToSqlArray(string[] strs) => $"({string.Join(',',strs.Select(x=>ToSqlString(x)).ToArray())})";
    private string ToSqlString(string str) => '\''+str+'\'';
}