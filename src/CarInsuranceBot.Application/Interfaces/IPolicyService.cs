using CarInsuranceBot.Domain.Models;

namespace CarInsuranceBot.Application.Interfaces;

// Contract for generating an insurance policy.
public interface IPolicyService
{
    // Accepts a session with confirmed data from both documents.
    // Returns the finished policy with PDF bytes inside.
    Task<InsurancePolicy> GenerateAsync(
        UserSession session,
        CancellationToken ct = default);
}