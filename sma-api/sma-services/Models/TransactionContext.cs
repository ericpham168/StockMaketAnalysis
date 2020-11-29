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
            builder.Entity<Transaction>()
            .HasData(
              new Transaction { TID = 1, ItemSet = "", Price = 460 }, 
              new Transaction { TID = 2, ItemSet = "c", Price = 480 },
              new Transaction { TID = 3, ItemSet = "a,b", Price = 500 },
              new Transaction { TID = 4, ItemSet = "", Price = 510 },
              new Transaction { TID = 5, ItemSet = "b", Price = 490 },
              new Transaction { TID = 6, ItemSet = "c", Price = 500 },
              new Transaction { TID = 7, ItemSet = "a,b", Price = 510 },
              new Transaction { TID = 8, ItemSet = "", Price = 520 },
              new Transaction { TID = 9, ItemSet = "b", Price = 570 },
              new Transaction { TID = 10, ItemSet = "", Price = 610 }

            );
        }
    }
}