using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Domain.Exceptions;
using CarInsuranceBot.Domain.Models;
using Microsoft.Extensions.Logging;
using Mindee;
using Mindee.Input;
using Mindee.Product.Passport;

public class MindeeService : IMindeeService
{
    private readonly MindeeClient _clientV1;
    private readonly MindeeClient _clientV2;
    private readonly ILogger<MindeeService> _logger;

    public MindeeService(
        // Отримуємо фабрику і створюємо два клієнти
        Func<string, MindeeClient> mindeeClientFactory,
        ILogger<MindeeService> logger)
    {
        _clientV1 = mindeeClientFactory("v1");
        _clientV2 = mindeeClientFactory("v2");
        _logger = logger;
    }

    // Паспорт — через V1 клієнт з PassportV1 моделлю
    public async Task<ExtractedDocumentData> ExtractPassportDataAsync(
        Stream photoStream,
        CancellationToken ct = default)
    {
        try
        {
            var inputSource = new LocalInputSource(photoStream, "passport.jpg");

            // V1 клієнт з V1 токеном — працює з PassportV1
            var response = await _clientV1.ParseAsync<PassportV1>(inputSource);
            var prediction = response.Document.Inference.Prediction;

            if (prediction is null)
                throw new DocumentParseException("Mindee returned empty passport prediction");

            var fields = new Dictionary<string, string>();

            if (prediction.GivenNames?.FirstOrDefault()?.Value is { } firstName)
                fields["First Name"] = firstName;

            if (prediction.Surname?.Value is { } surname)
                fields["Last Name"] = surname;

            if (prediction.BirthDate?.Value is { } birthDate)
                fields["Date of Birth"] = birthDate;

            if (prediction.Country?.Value is { } nationality)
                fields["Nationality"] = nationality;

            if (prediction.IdNumber?.Value is { } docNumber)
                fields["Document Number"] = docNumber;

            if (prediction.ExpiryDate?.Value is { } expiryDate)
                fields["Expiry Date"] = expiryDate;

            if (prediction.Country?.Value is { } country)
                fields["Country"] = country;

            if (fields.Count == 0)
                throw new DocumentParseException(
                    "No fields could be extracted from passport.");

            return new ExtractedDocumentData
            {
                RawText = prediction.ToString() ?? string.Empty,
                Fields = fields
            };
        }
        catch (DocumentParseException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract passport data from Mindee");
            throw new DocumentParseException("Failed to extract passport data", ex);
        }
    }

    // Тех-паспорт — через V2 клієнт з кастомною моделлю
    public async Task<ExtractedDocumentData> ExtractVehicleDocDataAsync(
        Stream photoStream,
        CancellationToken ct = default)
    {
        try
        {
            var inputSource = new LocalInputSource(photoStream, "vehicle.jpg");

            // V2 клієнт з V2 токеном — працює з кастомною моделлю
            var endpoint = new Mindee.Http.CustomEndpoint(
                endpointName: Environment.GetEnvironmentVariable("MindeeSettings__VehicleEndpointName")!,
                accountName: Environment.GetEnvironmentVariable("MindeeSettings__AccountName")!,
                version: "1");

            var response = await _clientV2
                .ParseAsync<Mindee.Product.Generated.GeneratedV1>(inputSource, endpoint);

            var fields = new Dictionary<string, string>();

            if (response?.Document?.Inference?.Prediction?.Fields is { } resultFields)
            {
                if (resultFields.TryGetValue("a", out var plate))
                    fields["License Plate"] = plate.ToString() ?? string.Empty;

                if (resultFields.TryGetValue("c1", out var lastName))
                    fields["Owner Last Name"] = lastName.ToString() ?? string.Empty;

                if (resultFields.TryGetValue("c2", out var firstName))
                    fields["Owner First Name"] = firstName.ToString() ?? string.Empty;

                if (resultFields.TryGetValue("d1", out var brand))
                    fields["Brand"] = brand.ToString() ?? string.Empty;

                if (resultFields.TryGetValue("d3", out var model))
                    fields["Model"] = model.ToString() ?? string.Empty;

                if (resultFields.TryGetValue("e", out var vin))
                    fields["VIN"] = vin.ToString() ?? string.Empty;

                if (resultFields.TryGetValue("i", out var regDate))
                    fields["Registration Date"] = regDate.ToString() ?? string.Empty;

                if (resultFields.TryGetValue("b", out var firstReg))
                    fields["First Registration"] = firstReg.ToString() ?? string.Empty;
            }

            if (fields.Count == 0)
                throw new DocumentParseException(
                    "No fields could be extracted from vehicle document.");

            return new ExtractedDocumentData
            {
                RawText = string.Join("\n", fields.Select(f => $"{f.Key}: {f.Value}")),
                Fields = fields
            };
        }
        catch (DocumentParseException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract vehicle document data");
            throw new DocumentParseException("Failed to extract vehicle document data", ex);
        }
    }
}