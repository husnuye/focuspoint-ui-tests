using ClosedXML.Excel;
using System;
using System.IO;

namespace WebTests.Utils
{
    public static class ExcelReader
    {
        public static (string first, string second, string expectedPrice)
            ReadFirstRowStrict(string filePath, string sheetName)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel file not found: {filePath}");

            using var wb = new XLWorkbook(filePath);

            // Try exact sheet name first; otherwise fall back to the first worksheet
            IXLWorksheet ws;
            if (!wb.TryGetWorksheet(sheetName, out ws))
            {
                ws = wb.Worksheet(1); // fallback to first worksheet
            }

            var r = ws.Row(2); // Row 1 = headers, Row 2 = data

            string ReadCell(int col, string logicalName)
            {
                var val = r.Cell(col).GetString().Trim();
                if (string.IsNullOrWhiteSpace(val))
                    throw new InvalidDataException(
                        $"Cell for '{logicalName}' (row=2,col={col}) is empty in sheet '{ws.Name}'.");
                return val;
            }

            var first  = ReadCell(1, "FirstKeyword");
            var second = ReadCell(2, "SecondKeyword");
            var price  = ReadCell(3, "ExpectedPrice");

            return (first, second, price);
        }
    }
}