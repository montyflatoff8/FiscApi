using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FiscApi.Data;

/// <summary>
/// Used only by the "dotnet ef" tools (e.g. "dotnet ef migrations add"), so they can build a
/// FiscDbContext without starting the web app or knowing the real password. Creating a
/// migration never connects to the database, so a placeholder connection string is enough.
/// </summary>
public class FiscDbContextFactory : IDesignTimeDbContextFactory<FiscDbContext>
{
    public FiscDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FiscDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=FiscApi;Integrated Security=true;TrustServerCertificate=True;");
        return new FiscDbContext(optionsBuilder.Options);
    }
}
