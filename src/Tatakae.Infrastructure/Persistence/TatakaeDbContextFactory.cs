using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tatakae.Infrastructure.Persistence;

public sealed class TatakaeDbContextFactory : IDesignTimeDbContextFactory<TatakaeDbContext>
{
    public TatakaeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TatakaeDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=TatakaeEmbroideryCommerce_Phase14ReliableSeedV1;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True")
            .Options;

        return new TatakaeDbContext(options);
    }
}
