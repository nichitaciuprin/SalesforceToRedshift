public static class Helper
{
    public static void DiffPrint(string[] left, string[] right)
    {
        string[] intersect;
        string[] leftUniqe;
        string[] rightUniqe;
        Diff(left, right, out intersect, out leftUniqe, out rightUniqe);
        foreach (var item in intersect)  { Console.WriteLine(item); }
        foreach (var item in leftUniqe)  { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine(item); }
        foreach (var item in rightUniqe) { Console.ForegroundColor = ConsoleColor.Red;   Console.WriteLine(item); }
        Console.ResetColor();
    }
    public static void Diff(string[] left, string[] right, out string[] intersect, out string[] leftUniqe, out string[] rightUniqe)
    {
        intersect = left.Intersect(right).ToArray();
        leftUniqe = left.Except(intersect).ToArray();
        rightUniqe = right.Except(intersect).ToArray();
    }
    public static string projectRoot => AppDomain.CurrentDomain.BaseDirectory+"..\\..\\..";
    public static void PrintProperties(object obj)
    {
        PrintProperties(obj, 0);
    }
    private static void PrintProperties(object obj, int indent)
    {
        if (obj == null) return;
        string indentString = new string(' ', indent);
        var objType = obj.GetType();
        var properties = objType.GetProperties();
        foreach (var property in properties)
        {
            object propValue = property.GetValue(obj, null)!;
            if (property.PropertyType.Assembly == objType.Assembly && !property.PropertyType.IsEnum)
            {
                Console.WriteLine("{0}{1}:", indentString, property.Name);
                PrintProperties(propValue, indent + 2);
            }
            else
            {
                Console.WriteLine("{0}{1}: {2}", indentString, property.Name, propValue);
            }
        }
    }
}