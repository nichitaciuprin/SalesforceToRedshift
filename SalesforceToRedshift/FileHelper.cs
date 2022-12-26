public static class FileHelper
{
    public static void ValidateDirectoryName(string name)
    {
        var invalidChars = Path.GetInvalidPathChars();
        foreach (var invalidChar in invalidChars)
        {
            if (name.Contains(invalidChar))
                throw new Exception($"{name} is not a valid for directory name");
        }
    }
    public static void SaveCreate(string saveFilePath, string saveName)
    {
        File.AppendAllLines(saveFilePath, new[]{saveName});
    }
    public static bool SaveExists(string saveFilePath, string saveName)
    {
        if (!File.Exists(saveFilePath)) return false;
        var lines = File.ReadAllLines(saveFilePath);
        return lines.Contains(saveName);
    }
    public static void SimpleFilePrint(string filePath)
    {
        using var streamReader = new StreamReader(@"C:\Users\Nikita\Desktop\IVALIS\05_12_2022\Merge\Blue_sheet__c.csv.new");
        Console.WriteLine("!");
        while (true)
        {
            Console.ReadKey();
            var line = streamReader.ReadLine();
            if (line == null) return;
            Console.WriteLine(line);
            Console.WriteLine("-----------");
        }
    }
    public static int CountLines(string filePath)
    {
        var lineCount = 0;
        using var reader = File.OpenText(filePath);
        while (reader.ReadLine() != null)
            lineCount++;
        return lineCount;
    }
    public static void PrintLines(string filePath, string searchText)
    {
        using var streamReader = new StreamReader(filePath,System.Text.Encoding.ASCII);
        while (true)
        {
            var line = streamReader.ReadLine();
            if (line == null) break;

            if (line.Contains(searchText))
            {
                Console.WriteLine(line);
                Console.WriteLine();
            }
        }
    }
    public static void PrintFileSize(string filePath)
    {
        var gigabytes = FileSizeInGB(filePath);
        var msg = String.Format("{0:0.00}", gigabytes);
        Console.WriteLine($"Size of file {filePath} {msg} GB");
    }
    public static string GetLargestFile(params string[] paths)
    {
        if (paths.Length == 0)
            throw new InvalidOperationException("input is empty");

        return paths
            .Select(x => (x,FileSizeInBytes(x)))
            .MaxBy(x => x.Item2)
            .x;
    }
    public static long FileSizeInBytes(string filePath) => (new System.IO.FileInfo(filePath)).Length;
    public static float FileSizeInGB(string filePath)
    {
        var bytes = FileSizeInBytes(filePath);
        var megabytes = bytes / 1024 / 1024;
        var gigabytes = (float)megabytes / 1024;
        return gigabytes;
    }
    public static void PrintFirstLine(string filePath)
    {
        using var streamReader = new StreamReader(filePath);
        var line = streamReader.ReadLine();
        if (line == null)
        {
            Console.WriteLine("NO FIRST LINE");
            return;
        }
        line = line!.Substring(0,Math.Clamp(line.Length,0,100));
        Console.WriteLine(line);
    }
    public static void AppendTo(string filePath_src, string filePath_target, int skipLinesCount)
    {
        var index = 0;
        var lineCount = FileHelper.CountLines(filePath_src);
        Console.WriteLine($"Appending file. {filePath_src} -> {filePath_target}");
        var progressBar = new ProgressBar();

        using var streamReader = new StreamReader(filePath_src);
        using var streamWriter = new StreamWriter(filePath_target,true);

        while (true)
        {
            progressBar.ReportInteration(index,lineCount);

            var line = streamReader.ReadLine();
            if (line == null) break;
            if (skipLinesCount > 0)
                skipLinesCount--;
            else
                streamWriter.WriteLine(line);

            index++;
        }

        progressBar.Dispose();
    }
    public static void GetFirstLine(string filePath)
    {
        using var streamReader = new StreamReader(filePath);
        var line = streamReader.ReadLine();
        if (line == null)
        return;
        line = line!.Substring(0,Math.Clamp(line.Length,0,100));
        Console.WriteLine(line);
    }
}