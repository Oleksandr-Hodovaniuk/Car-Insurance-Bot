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

    private const string PassportModelId = "4f68e28f-18a3-4ac1-b09d-cf2003a7d971";

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

    public Task<ExtractedDocumentData> ExtractVehicleDocDataAsync(Stream photoStream, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}