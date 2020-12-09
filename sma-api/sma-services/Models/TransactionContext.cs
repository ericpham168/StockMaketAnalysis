using Microsoft.EntityFrameworkCore;
using System;

namespace sma_services.Models
{
    public class TransactionContext : DbContext
    {

        private readonly string _connectionString;

        public TransactionContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=DESKTOP-8OMVIBT\SQLEXPRESS;Database=sma-database;Trusted_Connection=True;", builder =>
            {
                builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });
            base.OnConfiguring(optionsBuilder);
        }

        public TransactionContext(DbContextOptions<TransactionContext> options)
          : base(options)
        {

        }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Transaction>();
        }
    }
}