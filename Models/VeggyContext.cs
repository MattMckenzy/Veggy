﻿namespace Veggy.Models;

public class VeggyContext(IConfiguration configuration) : DbContext
{
    public DbSet<Community> Communities { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Setting> Settings { get; set; }

    public required IConfiguration Configuration { get; set; } = configuration;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        NpgsqlConnectionStringBuilder builder = new()
        {
            Host = Configuration["Database:Host"],
            Database = Configuration["Database:Name"],
            Username = Configuration["Database:Username"],
            Password = Configuration["Database:Password"],
            Port = int.TryParse(Configuration["Database:Port"], out int parsedPort) ? parsedPort : 5432
        };

        optionsBuilder                
            .UseNpgsql(builder.ToString());
    }
}