using CarInsuranceBot.Domain.Models;
using System.Reflection.Metadata;

namespace CarInsuranceBot.Application.Interfaces;

//A Contract for the document recognition via Mindee API.
public interface IMindeeService
{
    //Accepts passport photo as Stream.
    Task<ExtractedDocumentData> ExtractPassportDataAsync(
        Stream photoStream,
        CancellationToken ct = default);

    //Accepts vehicles passport photo as Stream.
    Task<ExtractedDocumentData> ExtractVehicleDocDataAsync(
        Stream photoStream,
        CancellationToken ct = default);
}