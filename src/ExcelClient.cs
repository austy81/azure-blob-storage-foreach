using AzureBlobStorageForeach.DTOs;
using ClosedXML.Excel;

public class ExcelClient
{
    public List<ServiceObject> LoadServiceObjects(string filePath, string sheetName)
    {
        var serviceObjects = new List<ServiceObject>();

        // Load the Excel workbook
        using (var workbook = new XLWorkbook(filePath))
        {
            var worksheet = workbook.Worksheet(sheetName);

            // Start reading from the second row
            var rows = worksheet.RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                var serviceObject = new ServiceObject
                {
                    Id = Guid.Parse(row.Cell(2).GetString()), // Column B
                    Name = row.Cell(5).GetString(),           // Column E
                    IdentificationNumber = row.Cell(8).GetString(), // Column H
                    NextRevisionPeriodInMonths = row.Cell(13).GetString(), // Column M
                    Description = row.Cell(14).GetString()     // Column N
                };

                serviceObjects.Add(serviceObject);
            }
        }

        return serviceObjects;
    }
}
