using FiscApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FiscApi.Data;

/// <summary>
/// EF Core's view of the database: one DbSet per table. OnModelCreating sets column sizes
/// and precision so the SQL table matches the validation rules on TransactionRequest.
/// </summary>
public class FiscDbContext : DbContext
{
    public FiscDbContext(DbContextOptions<FiscDbContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.CreatedAt).IsRequired();
            entity.Property(t => t.Date).IsRequired();
            // decimal(18,2): without this, EF warns and SQL Server may round money values.
            entity.Property(t => t.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(t => t.Type).HasMaxLength(10).IsRequired();
            entity.Property(t => t.Category).HasMaxLength(50);
            entity.Property(t => t.Description).HasMaxLength(100).IsRequired();
            entity.Property(t => t.Notes).HasMaxLength(250);
        });
    }
}
