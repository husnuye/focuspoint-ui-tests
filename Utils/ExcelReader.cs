using ClosedXML.Excel;
using System;
using System.IO;

namespace WebTests.Utils
{
    /// <summary>
    /// Reads a simple 3-column test vector from Excel:
    ///   Row 1 = headers (ignored)
    ///   Row 2 = data    (FirstKeyword | SecondKeyword | ExpectedPrice)
    ///
    /// Notes:
    /// - Sheet is resolved by exact name; if not found, falls back to the first worksheet.
    /// - Values are read as strings (trimmed). Price remains string on purpose
    ///   because we normalize currency/symbols later during UI assertions.
    /// - Throws clear exceptions when file, sheet, or cells are not usable.
    /// </summary>
    public static class ExcelReader
    {
        public static (string first, string second, string expectedPrice)
            ReadFirstRowStrict(string filePath, string sheetName)
        {
            // Fail fast if Excel file is missing.
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel file not found: {Path.GetFullPath(filePath)}");

            using var wb = new XLWorkbook(filePath);

            // Resolve worksheet: exact name first, otherwise first worksheet as a reasonable default.
            IXLWorksheet ws;
            if (!wb.TryGetWorksheet(sheetName, out ws))
            {
                ws = wb.Worksheet(1); // fallback
            }

            // Read row 2 (row 1 assumed to be headers).
            var r = ws.Row(2);

            // Local helper: read+trim+validate a single cell.
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
            var price  = ReadCell(3, "ExpectedPrice"); // stays string; normalization happens in UI layer

            return (first, second, price);
        }
    }
}