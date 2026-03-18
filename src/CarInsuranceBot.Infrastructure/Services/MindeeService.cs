using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Domain.Exceptions;
using CarInsuranceBot.Domain.Models;
using Microsoft.Extensions.Logging;
using Mindee;
using Mindee.Input;
using Mindee.Parsing.V2;

public class MindeeService : IMindeeService
{
    private readonly MindeeClientV2 _client;
    private readonly ILogger<MindeeService> _logger;

    public MindeeService(MindeeClientV2 client, ILogger<MindeeService> logger)
    {
        _client = client;
        _logger = logger;
    }

    private string PassportModelId = Environment.GetEnvironmentVariable("MindeeSettings__PassportModelId")!;
    private string VehicleModelId = Environment.GetEnvironmentVariable("MindeeSettings__VehicleModelId")!;

    public async Task<ExtractedDocumentData> ExtractPassportDataAsync(
        Stream photoStream,
        CancellationToken ct = default)
    {
        try
        {
            var inputSource = new LocalInputSource(photoStream, "passport.jpg");

            var inferenceParams = new InferenceParameters(modelId: PassportModelId);

            var response = await _client.EnqueueAndGetInferenceAsync(inputSource, inferenceParams);

            var fields = new Dictionary<string, string>();

            foreach (var field in response.Inference.Result.Fields)
            {
                var value = field.Value?.ToString();
                if (!string.IsNullOrEmpty(value))
                    fields[field.Key] = value;
            }

            if (fields.Count == 0)
                throw new DocumentParseException("No fields could be extracted from passport.");

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
            var inferenceParams = new InferenceParameters(modelId: VehicleModelId);
            var response = await _client.EnqueueAndGetInferenceAsync(inputSource, inferenceParams);

            var fieldMapping = new Dictionary<string, string>
        {
            { "a",    "License Plate" },
            { "b",    "First Registration" },
            { "c1",   "Owner Last Name" },
            { "c2",   "Owner First Name" },
            { "c3",   "Owner Address" },
            { "d1",   "Brand" },
            { "d2",   "Version" },
            { "d2_1", "Model" },
            { "e",    "VIN" },
            { "f1",   "Max Weight" },
            { "f2",   "Gross Weight" },
            { "g",    "Curb Weight" },
            { "i",    "Registration Date" },
            { "j",    "Vehicle Category" },
            { "k",    "Type Approval" },
            { "p1",   "Engine Displacement" },
            { "p2",   "Engine Power" },
            { "p3",   "Fuel Type" },
            { "s1",   "Seats" },
        };

            var fields = new Dictionary<string, string>();

            foreach (var field in response.Inference.Result.Fields)
            {
                var value = field.Value?.ToString();
                if (string.IsNullOrEmpty(value)) continue;

                var name = fieldMapping.TryGetValue(field.Key, out var mapped)
                    ? mapped
                    : field.Key;

                fields[name] = value;
            }

            if (fields.Count == 0)
                throw new DocumentParseException("No fields could be extracted from vehicle document.");

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