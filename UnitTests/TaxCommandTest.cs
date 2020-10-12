using Xunit;
using Persistance;
using Application.Taxes;
using System.Threading.Tasks;
using Domain.Dto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Domain.Exceptions;
using Domain.Enums;
using System;

namespace UnitTests
{
    public class TaxCommandTest
    {
        private TaxesManagerDbContext _context;

        public TaxCommandTest()
        {
            var services = new ServiceCollection();

            services.AddDbContext<TaxesManagerDbContext>(options => options.UseInMemoryDatabase(databaseName: "TaxesManagerDbContext"));
            
            var serviceProvider = services.BuildServiceProvider();

            _context = serviceProvider.GetRequiredService<TaxesManagerDbContext>();

            CleanUpDatabase();
        }

        [Fact]
        public async Task ShouldPostTaxYearlyThrowsTaxesManagerException()
        {
            var service = new TaxCommand(_context);
            await Assert.ThrowsAsync<TaxesManagerException>(() => 
                service.PostTaxYearly(new TaxDto { Municipality = "Vilnius", TaxAmount = 8 }));
        }

        [Fact]
        public async Task ShouldPostTaxYearly()
        {
            var service = new TaxCommand(_context);
            var result = await service.PostTaxYearly(new TaxDto {Municipality = "Vilnius", TaxAmount = 0.15 } );
            Assert.Equal("Vilnius", result.Municipality);
            Assert.Equal(0.15, result.TaxAmount);
            Assert.Equal(Frequency.Yearly, result.Frequency);
        }

        [Fact]
        public async Task ShouldGetTaxAmountDaily()
        {
            var service = new TaxCommand(_context);
            var tax1 = await service.PostTaxYearly(new TaxDto { Municipality = "Vilnius", TaxAmount = 0.1 });
            var tax2 = await service.PostTaxMonthly(new TaxDto { Municipality = "Vilnius", TaxAmount = 0.3, Month = 9 });
            var tax3 = await service.PostTaxDaily(new TaxDto { Municipality = "Vilnius", TaxAmount = 0.5, Month = 9, Day = 14 });
            var tax = await service.GetTaxAmount("Vilnius", "2020-09-14");
            Assert.Equal(0.5, tax.TaxAmount);
        }

        [Fact]
        public async Task ShouldUpdateTax()
        {
            var service = new TaxCommand(_context);
            var tax1 = await service.PostTaxYearly(new TaxDto { Municipality = "Vilnius", TaxAmount = 0.1 });
            var tax2 = await service.PutTax("Vilnius", DateTime.Parse("2020-01-01"), DateTime.Parse("2020-12-31"), 0.2);
            var tax3 = await service.GetTax("Vilnius", DateTime.Parse("2020-01-01"), DateTime.Parse("2020-12-31"));
            Assert.Equal(0.2, tax3.TaxAmount);
        }

        internal async Task CleanUpDatabase()
        {
            var taxes = _context.Taxes;

            _context.Taxes.RemoveRange(taxes);

            await _context.SaveChangesAsync();
        }
    }
}
