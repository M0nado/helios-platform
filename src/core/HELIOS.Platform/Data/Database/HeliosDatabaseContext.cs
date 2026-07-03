using Microsoft.EntityFrameworkCore;

namespace HELIOS.Platform.Data.Database;

public sealed class HeliosDatabaseContext : DbContext
{
    public HeliosDatabaseContext(DbContextOptions<HeliosDatabaseContext> options)
        : base(options)
    {
    }
}
