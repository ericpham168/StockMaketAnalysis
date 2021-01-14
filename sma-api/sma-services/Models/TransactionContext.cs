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
            optionsBuilder.UseSqlServer(@"Server=DESKTOP-1P4H4S8;Database=sma-database;Trusted_Connection=True;", builder =>
            {
                builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });
            base.OnConfiguring(optionsBuilder);
        }

        public TransactionContext(DbContextOptions<TransactionContext> options)
          : base(options)
        {

        }
        public virtual DbSet<Transaction> Transactions { get; set; }
        public virtual DbSet<TickerTranSaction> TickerTranSactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<TickerTranSaction>()
                .HasMany(trans => trans.Transactions)
                .WithOne(Ticker => Ticker.TickerTranSaction)
                .HasForeignKey(trans => trans.TickerID);

            builder.Entity<TickerTranSaction>().HasIndex(o => o.Ticker).IsUnique();
        }
    }
}