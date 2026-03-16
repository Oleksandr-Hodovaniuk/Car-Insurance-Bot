using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Domain.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CarInsuranceBot.Application.Services;

public class PolicyService(IAiService _openAiService) : IPolicyService
{
    public async Task<InsurancePolicy> GenerateAsync(
        UserSession session,
        CancellationToken ct = default)
    {
        var passportInfo = string.Join(", ",
            session.PassportData!.Fields.Select(f => $"{f.Key}: {f.Value}"));

        var vehicleInfo = string.Join(", ",
            session.VehicleData!.Fields.Select(f => $"{f.Key}: {f.Value}"));

        var policyText = await _openAiService.GeneratePolicyTextAsync
            (passportInfo, vehicleInfo, ct);

        policyText = policyText
            .Replace("**", "")
            .Replace("*", "")
            .Replace("##", "")
            .Replace("#", "");

        var pdfBytes = GeneratePdf(policyText);

        return new InsurancePolicy
        {
            ChatId = session.ChatId,
            PolicyText = policyText,
            PdfBytes = pdfBytes
        };
    }

    // Generates a PDF file via QuestPDF and returns it as a byte array.
    private static byte[] GeneratePdf(string policyText)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.5f, Unit.Centimetre);

                //Header.
                page.Header().Column(col =>
                {
                    col.Item().AlignCenter()
                        .Text("CAR INSURANCE POLICY")
                        .FontSize(20).Bold();

                    col.Item().PaddingTop(4).AlignCenter()
                        .Text("InsuranceBot — Powered by AI")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                });

                //Body.
                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Item().Text(policyText).FontSize(11);

                    col.Item().PaddingTop(20)
                        .Text($"Issued: {DateTime.UtcNow:dd MMMM yyyy}  |  " +
                              $"Valid until: {DateTime.UtcNow.AddYears(1):dd MMMM yyyy}  |  " +
                              $"Price: 100 USD")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                });

                //Footer.
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ").FontSize(9);
                    text.CurrentPageNumber().FontSize(9);
                    text.Span(" of ").FontSize(9);
                    text.TotalPages().FontSize(9);
                });
            });
        }).GeneratePdf();
    }
}