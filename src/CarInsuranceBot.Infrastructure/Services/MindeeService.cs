using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Domain.Exceptions;
using CarInsuranceBot.Domain.Models;
using Mindee;
using Mindee.Input;
using Mindee.Product.Passport;
using Microsoft.Extensions.Logging;

namespace CarInsuranceBot.Infrastructure.Services;

// Real Mindee API implementation.
// Sends photos to Mindee cloud OCR service and returns extracted data.
public class MindeeService : IMindeeService
{
    private readonly MindeeClient _mindeeClient;
    private readonly ILogger<MindeeService> _logger;

    public MindeeService(
        MindeeClient mindeeClient,
        ILogger<MindeeService> logger)
    {
        _mindeeClient = mindeeClient;
        _logger = logger;
    }

    public async Task<ExtractedDocumentData> ExtractPassportDataAsync(
        Stream photoStream,
        CancellationToken ct = default)
    {
        try
        {
            var inputSource = new LocalInputSource(photoStream, "passport.jpg");

            var response = await _mindeeClient
                .ParseAsync<PassportV1>(inputSource);

            var prediction = response.Document.Inference.Prediction;

            if (prediction is null)
                throw new DocumentParseException(
                    "Mindee returned empty passport prediction");

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
                    "No fields could be extracted from passport. " +
                    "Please retake the photo with better lighting.");

            return new ExtractedDocumentData
            {
                RawText = prediction.ToString() ?? string.Empty,
                Fields = fields
            };
        }
        catch (DocumentParseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract passport data from Mindee");
            throw new DocumentParseException(
                "Failed to extract passport data", ex);
        }
    }

    public async Task<ExtractedDocumentData> ExtractVehicleDocDataAsync(
    Stream photoStream,
    CancellationToken ct = default)
    {
        try
        {
            var inputSource = new LocalInputSource(photoStream, "vehicle.jpg");

            var endpoint = new Mindee.Http.CustomEndpoint(
                endpointName: Environment.GetEnvironmentVariable("MindeeSettings__VehicleEndpointName")!,
                accountName: Environment.GetEnvironmentVariable("MindeeSettings__AccountName")!,
                version: "1");

            var response = await _mindeeClient
                .ParseAsync<Mindee.Product.Generated.GeneratedV1>(inputSource, endpoint);

            var rawFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (response?.Document?.Inference?.Prediction?.Fields is { } resultFields)
            {
                // Custom models can expose arbitrary field names.
                // We keep them as-is to avoid coupling to a specific document template.
                foreach (var kv in resultFields)
                {
                    var value = kv.Value?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(value))
                        rawFields[kv.Key] = value;
                }
            }

            if (rawFields.Count == 0)
                throw new DocumentParseException(
                    "No fields could be extracted from vehicle document.");

            // Normalize common vehicle-registration fields so the rest of the bot
            // can display consistent labels across different document templates/models.
            var fields = NormalizeVehicleFields(rawFields);

            // Fallback: if nothing matched our common set, at least return raw fields
            // so the user can still confirm what the model produced.
            if (fields.Count == 0)
                fields = rawFields;

            return new ExtractedDocumentData
            {
                RawText = string.Join("\n", rawFields.Select(f => $"{f.Key}: {f.Value}")),
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

    private static Dictionary<string, string> NormalizeVehicleFields(
        IReadOnlyDictionary<string, string> raw)
    {
        // Pick a stable subset that exists across most vehicle registration documents:
        // plate, VIN, owner names, make/model, and key dates.
        //
        // The alias list below includes Mindee Carte Grise-like keys (a, c1, c2, d1, d3, e, b, i)
        // and also more explicit custom-field names (license_plate, vin, owner_first_name, ...).
        var normalized = new Dictionary<string, string>();

        static string? FirstPresent(IReadOnlyDictionary<string, string> src, params string[] keys)
        {
            foreach (var k in keys)
            {
                if (src.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v) && v != "null")
                    return v.Trim();
            }
            return null;
        }

        void Add(string label, params string[] keys)
        {
            var v = FirstPresent(raw, keys);
            if (!string.IsNullOrWhiteSpace(v))
                normalized[label] = v;
        }

        // License plate / document number (often same on French carte grise: "a" and document_number)
        Add("License Plate",
            "license_plate", "plate", "registration_number", "document_number", "a");

        Add("VIN",
            "vin", "vehicle_identification_number", "e");

        Add("Owner First Name",
            "owner_first_name", "first_name", "given_name", "c2");

        Add("Owner Last Name",
            "owner_last_name", "last_name", "surname", "family_name", "c1");

        Add("Owner Address",
            "owner_address", "address", "c3");

        Add("Make",
            "make", "brand", "manufacturer", "d1");

        Add("Model",
            "model", "vehicle_model", "d3");

        Add("Vehicle Category",
            "category", "vehicle_category", "j", "j1");

        Add("First Registration Date",
            "first_registration_date", "first_registration", "b");

        Add("Registration Date",
            "registration_date", "issue_date", "i");

        return normalized;
    }
}