using InsuranceClaimApi.Models;

namespace InsuranceClaimApi.Services;

public interface IAiClaimService
{
	Task<ClaimAssessmentResult> AssessClaimAsync(
		IFormFile passport,
		IFormFile boardingPass,
		List<IFormFile> baggageImages,
		string description,
		CancellationToken cancellationToken = default);
}
