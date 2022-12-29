using System.Globalization;
using CsvHelper;

public static class CsvHelper2
{
    public static void Merge(string[] filePaths, string targetFilePath)
    {
        if (filePaths.Length == 0) return;

        if (filePaths.Length == 1)
        {
            File.Move(filePaths[0],targetFilePath,true);
            return;
        }

        var destFile = FileHelper.GetLargestFile(filePaths);
        var sourceFiles = filePaths.Where(x => x != destFile).ToArray();

        Console.WriteLine($"Moving file. {destFile} -> {targetFilePath}");
        File.Move(destFile,targetFilePath,true);
        destFile = targetFilePath;

        foreach (var sourceFile in sourceFiles)
        {
            FileHelper.AppendTo(sourceFile,destFile,1);
            File.Delete(sourceFile);
        }
    }
    public static string? SplitByColumnSizeIfLargeValueExists(string filePath, int maxColumnSize)
    {
        if (maxColumnSize < 1)
            throw new ArgumentException($"{nameof(maxColumnSize)} < 1");

        var linesCount = FileHelper.CountLines(filePath);

        var maxColumnValues = GetMaxFieldValues(filePath);
        if (maxColumnValues.Max() <= maxColumnSize) return null;

        var dirPath = Path.GetDirectoryName(filePath)!;
        var fileName = Path.GetFileName(filePath+".new");
        var filePath_new = Path.Combine(dirPath,fileName);

        using var fileStream1 = File.OpenRead(filePath);
        using var fileStream2 = File.Create(filePath_new);
        using var streamReader = new StreamReader(fileStream1);
        using var streamWriter = new StreamWriter(fileStream2);
        using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
        using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

        // HEADER
        csvReader.Read();
        csvReader.ReadHeader();
        var header_old = csvReader.HeaderRecord!;
        var header_new = header_old.Select((x,i) => SplitHeader(x,maxColumnValues[i],maxColumnSize)).SelectMany(x=>x).ToArray();
        foreach (var item in header_new)
            csvWriter.WriteField(item,true);
        csvWriter.NextRecord();

        // RECORDS
        var count = 0;
        using var progressBar = new ProgressBar();
        while (csvReader.Read())
        {
            count++;
            progressBar.Report((float)count/linesCount);

            csvReader.ReadHeader();
            var record = csvReader.HeaderRecord!;

            for (int i = 0; i < record.Length; i++)
            {
                var field = record[i];
                var splitedField = SplitField(field,maxColumnValues[i],maxColumnSize);
                foreach (var item in splitedField)
                    csvWriter.WriteField(item,true);
            }
            csvWriter.NextRecord();
        }
        progressBar.Dispose();
        Console.WriteLine("Done");

        return filePath_new;
    }
    public static int CountColumns(int maxFieldSizeInColumn, int maxColumnSize) => (maxFieldSizeInColumn / maxColumnSize) + ((maxFieldSizeInColumn % maxColumnSize) > 0 ? 1 : 0);
    public static void Validate(string filePath)
    {
        Console.WriteLine($"Validating CSV file {filePath}");
        using var streamReader = new StreamReader(filePath,System.Text.Encoding.ASCII);
        using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
        var countColumns = 0;
        csvReader.Read();
        csvReader.ReadHeader();
        countColumns = csvReader.HeaderRecord!.Length;
        while (csvReader.Read())
        {
            csvReader.ReadHeader();
            var length = csvReader.HeaderRecord!.Length;
            if (length != countColumns)
                throw new Exception($"Bad CSV file. FilePath: {filePath}");
        }
    }
    public static int[] GetMaxFieldValues(string filePath)
    {
        using var streamReader = new StreamReader(filePath,System.Text.Encoding.ASCII);
        using var csvReader = new CsvReader(streamReader, CultureInfo.CurrentCulture);
        csvReader.Read();
        csvReader.ReadHeader();
        var columnCount = csvReader.HeaderRecord!.Count();
        var result = new int[columnCount];
        while (csvReader.Read())
        {
            for (int i = 0; i < columnCount; i++)
            {
                var value = csvReader.GetField(i);
                if (value == null) continue;
                result[i] = Math.Max(result[i],value.Length);
            }
        }
        return result;
    }
    public static string[] GetHeaders(string filePath)
    {
        using var streamReader = new StreamReader(filePath);
        using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
        csvReader.Read();
        csvReader.ReadHeader();
        return csvReader.HeaderRecord!;
    }
    public static string[] GetHeaders2(string filePath)
    {
        var result = GetFirstLine(filePath)
            .Split(',')
            .Select(x => x.Substring(1,x.Length-2)) // HANDLE DOUBLE QUOTES
            .ToArray();
        return result;
    }
    public static string GetFirstLine(string filePath)
    {
        using var streamReader = new StreamReader(filePath);
        var line = streamReader.ReadLine();
        if (line == null) return string.Empty;
        return line;
    }
    public static (string,int)[] SplitColumnInfos((string,int)[] columnInfos, int maxFieldSizeInBytes)
    {
        var list = new List<(string,int)>();
        foreach (var x in columnInfos)
        {
            var columnName = x.Item1;
            var columnSize = x.Item2;

            if (x.Item2 <= maxFieldSizeInBytes)
            {
                list.Add((x));
                continue;
            }

            var result = Divide(columnSize,maxFieldSizeInBytes);

            for (int i = 0; i < result.Length; i++)
            {
                var item = ($"{columnName}_{i+1}", result[i]);
                list.Add(item);
            }
        }
        return list.ToArray();
    }
    public static (string,int)[] GetTableInfo(string filePath)
    {
        var linesCount = FileHelper.CountLines(filePath);
        using var streamReader = new StreamReader(filePath,System.Text.Encoding.ASCII);
        using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
        csvReader.Read();
        csvReader.ReadHeader();
        var columnCount = csvReader.HeaderRecord!.Length;
        var result = new int[columnCount];
        var count = 0;
        var progressBar = new ProgressBar();
        while (csvReader.Read())
        {
            progressBar.ReportInteration(count,linesCount);
            for (int i = 0; i < columnCount; i++)
            {
                var field = csvReader.GetField(i);
                if (field == null) continue;
                var length = System.Text.Encoding.ASCII.GetBytes(field).Length;
                result[i] = Math.Max(result[i],length);
            }
            count++;
        }
        progressBar.Dispose();
        return csvReader.HeaderRecord!
            .Select((x,i) => (x,result[i]))
            .ToArray();
    }
    private static int[] Divide(int value, int div)
    {
        var count = value / div;
        var rest = value % div;
        if (rest == 0)
        {
            var result = new int[count];
            for (int i = 0; i < count; i++)
                result[i] = div;
            return result;
        }
        else
        {
            count++;
            var result = new int[count];
            for (int i = 0; i < count; i++)
                result[i] = div;
            result[count-1] = rest;
            return result;
        }
    }
    private static string[] SplitHeader(string header, int maxFieldSizeInColumn, int maxColumnSize)
    {
        var count = CountColumns(maxFieldSizeInColumn, maxColumnSize);
        if (count == 1) return new [] { header };
        return (new string[count]).Select((x,i) => header+"_"+i).ToArray();
    }
    private static string[] SplitField(string field, int maxFieldSizeInColumn, int maxColumnSize)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(field);
        return Chunk(bytes,maxFieldSizeInColumn,maxColumnSize)
            .Select(x => System.Text.Encoding.ASCII.GetString(x))
            .ToArray();
    }
    private static byte[][] Chunk(byte[] field, int maxFieldSizeInColumn, int maxColumnSize)
    {
        var result = field.Chunk(maxColumnSize).ToList();
        var countColumns = CountColumns(maxFieldSizeInColumn, maxColumnSize) - result.Count();
        for (int i = 0; i < countColumns; i++)
            result.Add(Array.Empty<byte>());
        return result.ToArray();
    }
}