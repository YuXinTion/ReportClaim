using InsuranceClaimApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IAiClaimService, AiClaimService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();   // serves wwwroot/index.html at "/"
app.UseStaticFiles();

app.MapPost("/api/claims/assess", async (
    HttpRequest request,
    IAiClaimService aiService,
    ILogger<Program> logger) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { error = "Expected multipart/form-data." });
    }

    var form = await request.ReadFormAsync();

    var passport = form.Files.GetFile("passport");
    var boardingPass = form.Files.GetFile("boardingPass");
    var baggageImages = form.Files.Where(f => f.Name == "baggageImages").ToList();
    var description = form["description"].ToString();

    if (passport is null || boardingPass is null || baggageImages.Count == 0)
    {
        return Results.BadRequest(new
        {
            error = "Passport, boardingPass, and at least one baggageImages file are required."
        });
    }

    if (baggageImages.Count > 5)
    {
        return Results.BadRequest(new { error = "Maximum 5 baggage images allowed." });
    }

    logger.LogInformation("Assessing claim with {Count} baggage images", baggageImages.Count);

    var result = await aiService.AssessClaimAsync(passport, boardingPass, baggageImages, description);

    return Results.Ok(result);
})
.DisableAntiforgery();

app.Run();
