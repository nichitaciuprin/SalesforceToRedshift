using System.Globalization;
using System.Text;
using CsvHelper;

public static class CsvHelper2
{
    public static void CreateNew(string oldCsv, string newCsv, int maxColumnSize)
    {
        if (maxColumnSize < 1) throw new Exception("maxColumnSize < 1");

        var maxColumnValues = GetMaxColumnValues(oldCsv);
        if (maxColumnValues.Max() <= maxColumnSize) return;

        var header = GetHeader(oldCsv);
        var records = TryEnumerateRecords(oldCsv);

        var header2 = ToResisedHeader(header,maxColumnValues,maxColumnSize);
        var records2 = ToResisedRecords(records,maxColumnValues,maxColumnSize);

        using var streamWriter = new StreamWriter(newCsv);
        WriteAsCsvLine(streamWriter, header2);
        foreach (var item in records2)
            WriteAsCsvLine(streamWriter, item);
    }
    public static string[] GetHeader(string csvFile)
    {
        using var reader = new StreamReader(csvFile);
        using var readerCsv = new CsvReader(reader, CultureInfo.InvariantCulture);
        readerCsv.Read();
        return readerCsv.HeaderRecord!;
    }
    public static IEnumerable<string[]> TryEnumerateRecords(string csvFile)
    {
        using var reader = new StreamReader(csvFile);
        using var readerCsv = new CsvReader(reader, CultureInfo.InvariantCulture);

        // SKIP HEADER
        readerCsv.Read();

        while (readerCsv.Read())
            yield return readerCsv.HeaderRecord!;
    }
    public static IEnumerable<string> TryEnumerateColumnValues(string csvFile, int ordinal)
    {
        using var reader = new StreamReader(csvFile);
        using var readerCsv = new CsvReader(reader, CultureInfo.InvariantCulture);

        // SKIP HEADER
        readerCsv.Read();

        while (readerCsv.Read())
            yield return readerCsv.GetField(ordinal)!;
    }
    public static int TryCountRecords(string csvFile)
    {
        var count = FileHelper.CountLines(csvFile);

        // SKIP HEADER
        count--;

        if (count < 0) count = 0;
        return count;
    }
    public static void Merge(string[] sourceFiles, string newFile)
    {
        using var streamWriter = new StreamWriter(newFile);

        var firstSource = sourceFiles.First();
        var header = File.ReadLines(firstSource).First();
        streamWriter.WriteLine(header);

        foreach (var sourceFile in sourceFiles)
        foreach (var line in File.ReadLines(sourceFile).Skip(1))
            streamWriter.WriteLine(line);
    }
    public static void WriteAsCsvLine(StreamWriter streamWriter, string[] lines)
    {
        streamWriter.WriteLine(string.Join(',',lines.Select(x => $"\"{x}\"")));
    }
    public static void Validate(string filePath)
    {
        Console.WriteLine($"Validating CSV file {filePath}");
        using var streamReader = new StreamReader(filePath, Encoding.ASCII);
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
                throw new Exception($"Bad CSV file. FilePath: {filePath}. Init count: {countColumns}. New count: {length}");
        }
    }
    public static int[] GetMaxColumnValues(string csvFile)
    {
        using var reader = new StreamReader(csvFile);
        using var readerCsv = new CsvReader(reader, CultureInfo.InvariantCulture);
        readerCsv.Read();
        var header = readerCsv.HeaderRecord!;
        var columnCount = header.Length;
        var result = new int[columnCount];
        while (readerCsv.Read())
        {
            var record = readerCsv.HeaderRecord!;
            for (int i = 0; i < columnCount; i++)
            {
                var value = record[i];
                result[i] = Math.Max(result[i],value.Length);
            }
        }
        return result;
    }
    private static string[] ToResisedHeader(string[] header, int[] maxColumnValues, int maxColumnSize)
    {
        var chunkCounts = maxColumnValues.Select(x => x % maxColumnSize).ToArray();
        return header.SelectMany((columnName,i) =>
        {
            var chunkCount = chunkCounts[i];
            if (chunkCount > 1)
                return Enumerable.Range(0,chunkCount).Select(x => columnName+'_'+x).ToArray();
            else
                return new [] { columnName };
        })
        .ToArray();
    }
    private static IEnumerable<string[]> ToResisedRecords(IEnumerable<string[]> oldRecords, int[] maxColumnValues, int maxColumnSize)
    {
        var chunkCounts = maxColumnValues.Select(x => x % maxColumnSize).ToArray();
        return oldRecords
            .Select(oldRecord => oldRecord
                .SelectMany((oldValue,i) =>
                    Split(oldValue,maxColumnSize,chunkCounts[i])).ToArray())
            .ToArray();
    }
    private static string[] Split(string str, int chunkSize, int chunkCount)
    {
        return
            Enumerable.Range(0,chunkCount)
            .Select(i => str.Substring(i * chunkSize, chunkSize))
            .ToArray();
    }
}