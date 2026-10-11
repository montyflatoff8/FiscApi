using FiscApi.Repositories;
using FiscApi.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

var builder = WebApplication.CreateBuilder(args);

// ---------- SERVICES (dependency injection) ----------

builder.Services.AddControllers(options =>
{
    // Validation error keys use JSON names ("description") instead of C# names ("Description").
    options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
});

// OpenAPI document at /openapi/v1.json (Postman imports from this).
builder.Services.AddOpenApi();

// Standard RFC 7807 "problem details" JSON bodies for error responses.
builder.Services.AddProblemDetails();

// Repository
builder.Services.AddSingleton<ITransactionRepository, JsonTransactionRepository>();

// Service: SCOPED, so a new instance is created for each HTTP request.
builder.Services.AddScoped<ITransactionService, TransactionService>();

var app = builder.Build();

// ---------- REQUEST PIPELINE (middleware, runs in order for every request) ----------

// Catches any unhandled exception and returns a 500 problem-details response.
// This is the ONE place unexpected errors are handled, so controllers need no try/catch.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();