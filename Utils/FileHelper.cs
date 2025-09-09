using System.IO;

namespace WebTests.Utils;

public static class FileHelper
{
    public static void WriteProductInfo(string outputTxtPath, string name, string price)
    {
        var dir = Path.GetDirectoryName(outputTxtPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(outputTxtPath, $"Product: {name} | Price: {price}");
    }
}