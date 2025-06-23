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
                    ExternalId = row.Cell("A").GetString(),
                    Name = row.Cell("B").GetString(),
                    //IdentificationNumber = row.Cell("H").GetString(),
                    //NextRevisionPeriodInMonths = row.Cell("M").GetString(),
                    //Description = row.Cell("N").GetString()
                };

                serviceObjects.Add(serviceObject);
            }
        }

        return serviceObjects;
    }

    public List<Material> LoadMaterial(string filePath, string sheetName)
    {
        var materials = new List<Material>();

        // Load the Excel workbook
        using (var workbook = new XLWorkbook(filePath))
        {
            var worksheet = workbook.Worksheet(sheetName);

            // Start reading from the second row
            var rows = worksheet.RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                var material = new Material
                {
                    //Id = Guid.Parse(row.Cell("A").GetString()),
                    ExternalId = row.Cell("A").GetString(),
                    //Name = row.Cell("C").GetString(),
                    //Code = row.Cell("D").GetString(),
                    //Price = decimal.Parse(row.Cell("E").GetString()),
                };
                materials.Add(material);
            }
        }

        return materials;
    }

    internal List<ContactPerson> LoadContactPeople(string filePath, string worksheetName)
    {
        var contactPeople = new List<ContactPerson>();

        // Load the Excel workbook
        using (var workbook = new XLWorkbook(filePath))
        {
            var worksheet = workbook.Worksheet(worksheetName);

            // Start reading from the second row
            var rows = worksheet.RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                var contactPerson = new ContactPerson
                {
                    ExternalId = row.Cell("A").GetString(),
                    PhonePrefix = row.Cell("F").GetString(),
                    PhoneNumber = row.Cell("G").GetString(),
                };
                contactPeople.Add(contactPerson);
            }
        }

        return contactPeople;
    }
}
