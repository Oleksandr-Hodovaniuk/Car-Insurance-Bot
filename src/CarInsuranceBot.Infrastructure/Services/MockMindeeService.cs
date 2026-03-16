using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Domain.Exceptions;
using CarInsuranceBot.Domain.Models;

namespace CarInsuranceBot.Infrastructure.Services;

// Mock implementation — generates random data every time.
// Simulates real Mindee API without external calls.
public class MockMindeeService : IMindeeService
{
    private static readonly Random Rng = Random.Shared;
    private static readonly TimeSpan ApiDelay = TimeSpan.FromSeconds(2);

    private static readonly string[] FirstNames =
        ["John", "Michael", "Oleksiy", "Ivan", "Anna", "Maria", "David", "James"];

    private static readonly string[] LastNames =
        ["Doe", "Smith", "Kovalenko", "Shevchenko", "Brown", "Wilson", "Johnson"];

    private static readonly string[] Nationalities =
        ["Ukrainian", "Polish", "German", "French", "British", "American"];

    private static readonly string[] VehicleBrands =
        ["Toyota Camry", "Honda Civic", "BMW 3 Series", "Volkswagen Golf", "Ford Focus"];

    private static readonly string[] CityPrefixes =
        ["AA", "AB", "AK", "AM", "BA", "BB", "BC", "CA", "CB", "KA"];

    public async Task<ExtractedDocumentData> ExtractPassportDataAsync(
        Stream photoStream,
        CancellationToken ct = default)
    {
        try
        {
            await Task.Delay(ApiDelay, ct);

            var firstName = Pick(FirstNames);
            var lastName = Pick(LastNames);
            var nationality = Pick(Nationalities);

            var birthDate = DateTime.Now
                .AddYears(-Rng.Next(18, 70))
                .AddDays(-Rng.Next(0, 365));

            var expiryDate = DateTime.Now
                .AddYears(Rng.Next(1, 10));

            var docNumber = $"{RandomLetter()}{RandomLetter()}{Rng.Next(100000, 999999)}";

            return new ExtractedDocumentData
            {
                RawText = $"Mock passport: {firstName} {lastName}",
                Fields = new Dictionary<string, string>
                {
                    ["First Name"] = firstName,
                    ["Last Name"] = lastName,
                    ["Date of Birth"] = birthDate.ToString("dd.MM.yyyy"),
                    ["Nationality"] = nationality,
                    ["Document Number"] = docNumber,
                    ["Expiry Date"] = expiryDate.ToString("dd.MM.yyyy"),
                    ["Country"] = nationality == "Ukrainian" ? "Ukraine" : nationality
                }
            };
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DocumentParseException(
                "Failed to generate mock passport data", ex);
        }
    }

    public async Task<ExtractedDocumentData> ExtractVehicleDocDataAsync(
        Stream photoStream,
        CancellationToken ct = default)
    {
        try
        {
            await Task.Delay(ApiDelay, ct);

            var ownerFirstName = Pick(FirstNames);
            var ownerLastName = Pick(LastNames);
            var vehicle = Pick(VehicleBrands);

            var cityPrefix = Pick(CityPrefixes);
            var plate = $"{cityPrefix}{Rng.Next(1000, 9999)}{RandomLetter()}{RandomLetter()}";

            var year = DateTime.Now.Year - Rng.Next(5, 20);

            return new ExtractedDocumentData
            {
                RawText = $"Mock vehicle doc: {plate} {vehicle}",
                Fields = new Dictionary<string, string>
                {
                    ["Owner First Name"] = ownerFirstName,
                    ["Owner Last Name"] = ownerLastName,
                    ["License Plate"] = plate,
                    ["Vehicle Formula"] = $"{vehicle} {year}",
                    ["MRZ"] = $"{vehicle.Replace(" ", "").ToUpper()}{year}"
                }
            };
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DocumentParseException(
                "Failed to generate mock vehicle document data", ex);
        }
    }

    private static T Pick<T>(T[] array)
        => array[Rng.Next(array.Length)];

    private static char RandomLetter()
        => (char)('A' + Rng.Next(26));
}