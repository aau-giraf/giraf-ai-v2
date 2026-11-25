using Giraf_AI.Data;
using Microsoft.EntityFrameworkCore;

namespace Giraf_AI.Test.TestHelpers;

public class DbContextFactory
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    // Create a mock database.
    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;
        
        return new AppDbContext(options);
    }

    public void Configure(DbContextOptionsBuilder options)
    {
        options.UseInMemoryDatabase(_databaseName);
    }
}