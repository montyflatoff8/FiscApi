using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FiscApi.Authentication;
using FiscApi.Configuration;
using FiscApi.Data;
using FiscApi.Repositories;
using FiscApi.Services;
using FiscApi.Setup;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AuthenticationOptions = FiscApi.Configuration.AuthenticationOptions;

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


// Repository: picked ONCE here from appsettings ("DataSource:Provider"). Nothing else in the
// app knows which storage is active; everything depends on ITransactionRepository.
var dataSourceOptions = builder.Configuration.GetSection(DataSourceOptions.SectionName).Get<DataSourceOptions>()
    ?? new DataSourceOptions();

if (dataSourceOptions.Provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    // The connection string holds the SA password, so it lives in User Secrets, not appsettings.
    var connectionString = builder.Configuration.GetConnectionString("FiscApi")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:FiscApi is required when DataSource:Provider is SqlServer. Set it with dotnet user-secrets (see README).");

    // Development only: start Docker Desktop and the SQL container if they aren't running,
    // so F5 / dotnet run is the only command you need. Turn off with DataSource:AutoStartDocker=false.
    if (builder.Environment.IsDevelopment() && dataSourceOptions.AutoStartDocker)
    {
        DockerBootstrap.EnsureSqlContainer(builder.Environment.ContentRootPath);
    }

    builder.Services.AddDbContext<FiscDbContext>(options =>
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

    // SCOPED: DbContext is one-per-request, so the repository that uses it must be too.
    builder.Services.AddScoped<ITransactionRepository, SqlTransactionRepository>();
}
else if (dataSourceOptions.Provider.Equals("Json", StringComparison.OrdinalIgnoreCase))
{
    // SINGLETON: every request must share the same file lock.
    builder.Services.AddSingleton<ITransactionRepository, JsonTransactionRepository>();
}
else
{
    throw new InvalidOperationException(
        $"DataSource:Provider must be 'Json' or 'SqlServer'. Found: {dataSourceOptions.Provider}");
}


// ---------- AUTHENTICATION (who are you?) + AUTHORIZATION (are you allowed?) ----------

var authOptions = builder.Configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>()
    ?? new AuthenticationOptions();
builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection(AuthenticationOptions.SectionName));

// Safety net: turning auth off is a classroom/testing switch, never allowed in Production.
if (!authOptions.Enabled && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("Authentication cannot be disabled outside Development.");
}

if (authOptions.Enabled)
{
    if (string.IsNullOrWhiteSpace(authOptions.SigningKey))
    {
        throw new InvalidOperationException(
            "Authentication:SigningKey is required when authentication is enabled. Set it with dotnet user-secrets (see README).");
    }

    // On every request with "Authorization: Bearer ...", check the token is genuine and current.
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = authOptions.Issuer,
                ValidAudience = authOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey)),
                NameClaimType = JwtRegisteredClaimNames.Name,   // so User.Identity.Name works
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });
}
else
{
    // Still registered so [Authorize] attributes resolve; the policy below just lets everyone through.
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
}


// "TransactionWrite" is the policy on POST/PUT/DELETE. The controller never checks Enabled;
// the decision lives here, in configuration.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TransactionWrite", policy =>
    {
        if (authOptions.Enabled)
        {
            policy.RequireAuthenticatedUser();
        }
        else
        {
            policy.RequireAssertion(_ => true);
        }
    });
});


builder.Services.AddSingleton<DemoUserStore>();
builder.Services.AddSingleton<JwtTokenService>();

// Service: SCOPED, so a new instance is created for each HTTP request.
builder.Services.AddScoped<ITransactionService, TransactionService>();

var app = builder.Build();


// ---------- STARTUP LOG (what is this instance actually running with?) ----------
// Message templates ({Name}) instead of $"..." so log tools can search by field.
// Never log the connection string, signing key, or password - only safe facts about them.
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

if (dataSourceOptions.Provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    // Pull out only the server and database name; the password in the same string stays private.
    var sqlInfo = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("FiscApi"));
    app.Logger.LogInformation("Data source: SqlServer (server {Server}, database {Database})",
        sqlInfo.DataSource, sqlInfo.InitialCatalog);
}
else
{
    app.Logger.LogInformation("Data source: Json (Data/transactions.json)");
}

if (authOptions.Enabled)
{
    app.Logger.LogInformation("Authentication: ENABLED - POST/PUT/DELETE require a bearer token");
}
else
{
    app.Logger.LogWarning("Authentication: DISABLED - all writes are open (Development only)");
}

// Development only: create the database/table by applying the checked-in migrations.
if (app.Environment.IsDevelopment() &&
    dataSourceOptions.Provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) &&
    dataSourceOptions.MigrateOnStartup)
{
    app.Logger.LogInformation("Applying database migrations.");
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<FiscDbContext>().Database.Migrate();
}


// ---------- REQUEST PIPELINE (middleware, runs in order for every request) ----------

// Catches any unhandled exception and returns a 500 problem-details response.
// This is the ONE place unexpected errors are handled, so controllers need no try/catch.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Order matters: authentication reads the token first, then authorization checks the policy.
if (authOptions.Enabled)
{
    app.UseAuthentication();
}

app.UseAuthorization();

app.MapControllers();

app.Run();