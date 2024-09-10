using System.Globalization;
using CsvHelper;

namespace Helpers.CSV
{
    public static class CSVHelperMethods
    {
        public static List<T>? ReadCsvFromFilePath<T>(string filePath)
            where T : class
        {
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                return csv.GetRecords<T>().ToList();
            }
        }
    }
}
