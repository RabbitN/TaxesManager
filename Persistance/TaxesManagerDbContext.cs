using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistance
{
    public class TaxesManagerDbContext : DbContext
    {
        public TaxesManagerDbContext(DbContextOptions<TaxesManagerDbContext> options)
            : base(options)
        {

        }
        public DbSet<Tax> Taxes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tax>().HasKey(tax => new { tax.Municipality, tax.StartDate, tax.EndDate });
        }
    }
}
