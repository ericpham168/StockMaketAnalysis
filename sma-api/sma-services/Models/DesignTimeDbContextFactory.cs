using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;


namespace sma_services.Models
{
    public class TravelApiContextFactory : IDesignTimeDbContextFactory<TransactionContext>
    {
        TransactionContext IDesignTimeDbContextFactory<TransactionContext>.CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json")
           .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var builder = new DbContextOptionsBuilder<TransactionContext>();

            builder.UseSqlServer(connectionString);

            return new TransactionContext(builder.Options);
        }
    }
}