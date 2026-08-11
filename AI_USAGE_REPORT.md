# AI Usage Report

# AI Tools Used
Claude — architecture design, code generation, debugging
Google Gemini API — damage detection, and claim recommendation

# How AI Was Used
Claude was used throughout development to scaffold the ASP.NET Core API, write the Gemini integration and prompt, 
and debug build/runtime errors. Gemini itself powers the core feature: given a passport, boarding pass, 
and baggage photos, it extracts customer/flight info, detects damage, and returns a structured recommendation.

# Example of an AI Error and Fix
Gemini repeatedly returned 404 Not Found because the hardcoded model name 
(gemini-1.5-flash, then gemini-2.5-flash) kept becoming unavailable as Google 
deprecated versions. 
Fix: switched to the auto-updating alias gemini-flash-latest.