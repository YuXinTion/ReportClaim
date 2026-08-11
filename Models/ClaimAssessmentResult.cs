using System.Text.Json.Serialization;

namespace InsuranceClaimApi.Models;

/// <summary>
/// The structured result returned by the AI after analyzing the passport,
/// boarding pass, baggage images, and claim description.
/// This matches the example schema in the assessment brief.
/// </summary>
public class ClaimAssessmentResult
{
    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("passportNo")]
    public string PassportNo { get; set; } = string.Empty;

    [JsonPropertyName("flightNo")]
    public string FlightNo { get; set; } = string.Empty;

    [JsonPropertyName("damageDetected")]
    public bool DamageDetected { get; set; }

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "Low"; // Low | Medium | High

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = "Needs Investigation"; // Approve | Reject | Needs Investigation

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    // Populated by our own code if the AI call fails or returns malformed data,
    // so the frontend / evaluator can see something meaningful happened.
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
