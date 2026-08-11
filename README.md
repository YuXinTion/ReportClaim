# ReportClaim
A prototype that helps insurance claims officers assess baggage damage claims faster using AI. 
Upload a passport, boarding pass, and baggage photos, and the system extracts customer/flight 
details, detects damage, and generates a recommendation (Approve / Reject / Needs Investigation) 
with a confidence score.

# Architecture
The browser uploads a passport photo, boarding pass photo, baggage damage photos, and a 
description to a single ASP.NET Core API endpoint. The API forwards everything to Google's Gemini 
vision model in one request, which extracts customer and flight details, detects baggage damage, 
and returns a recommendation with a confidence score as JSON. 

# Tech Stack
Backend: ASP.NET Core 8 
Frontend: Plain HTML/JS 
AI: Google Gemini API (gemini-flash-latest)

# Setup & Run
Requirements: .NET 8 SDK, a free Gemini API key

git clone <your-repo-url>
cd ReportClaim
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "YOUR_API_KEY_HERE"
dotnet run
Open http://localhost:5000

# Example Output
{
  "customerName": "John Tan",
  "passportNo": "A1234567",
  "flightNo": "MH603",
  "damageDetected": true,
  "severity": "High",
  "recommendation": "Approve",
  "confidence": 0.87
}
