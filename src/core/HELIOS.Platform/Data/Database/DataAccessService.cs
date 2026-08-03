namespace HELIOS.Platform.Data.Database;

public interface IDataAccessService
{
}

public sealed class DataAccessService : IDataAccessService
{
    public DataAccessService(HeliosDatabaseContext dbContext)
    {
        DbContext = dbContext;
    }

    public HeliosDatabaseContext DbContext { get; }
}
