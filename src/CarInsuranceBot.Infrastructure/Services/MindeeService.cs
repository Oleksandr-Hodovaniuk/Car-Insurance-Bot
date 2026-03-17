using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Domain.Exceptions;
using CarInsuranceBot.Domain.Models;
using Microsoft.Extensions.Logging;
using Mindee;
using Mindee.Input;

public class MindeeService : IMindeeService
{
    private readonly MindeeClient _client;
    private readonly string _passportEndpoint;
    private readonly string _vehicleEndpoint;
    private readonly string _accountName;
    private readonly ILogger<MindeeService> _logger;

    public MindeeService(MindeeClient client, ILogger<MindeeService> logger)
    {
        _client = client;
        _logger = logger;
        _accountName = Environment.GetEnvironmentVariable("MindeeSettings__AccountName")!;
        _passportEndpoint = Environment.GetEnvironmentVariable("MindeeSettings__PassportEndpointName")!;
        _vehicleEndpoint = Environment.GetEnvironmentVariable("MindeeSettings__VehicleEndpointName")!;
    }

    public async Task<ExtractedDocumentData> ExtractPassportDataAsync(
        Stream photoStream,
        CancellationToken ct = default)
    {
        try
        {       
            var inputSource = new LocalInputSource(photoStream, "passport.jpg");

            var endpoint = new Mindee.Http.CustomEndpoint(
                endpointName: _passportEndpoint,
                accountName: _accountName,
                version: "2");

            var response = await _client
                .ParseAsync<Mindee.Product.Generated.GeneratedV1>(inputSource, endpoint);

            var fields = new Dictionary<string, string>();

            if (response?.Document?.Inference?.Prediction?.Fields is { } resultFields)
            {
                foreach (var field in resultFields)
                {
                    var value = field.Value?.ToString();
                    if (!string.IsNullOrEmpty(value))
                        fields[field.Key] = value;
                }
            }

            if (fields.Count == 0)
                throw new DocumentParseException(
                    "No fields could be extracted from passport.");

            return new ExtractedDocumentData
            {
                RawText = string.Join("\n", fields.Select(f => $"{f.Key}: {f.Value}")),
                Fields = fields
            };
        }
        catch (DocumentParseException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract passport data");
            throw new DocumentParseException("Failed to extract passport data", ex);
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
                endpointName: _vehicleEndpoint,
                accountName: _accountName,
                version: "2");

            var response = await _client
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