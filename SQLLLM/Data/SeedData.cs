using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SQLLLM.Models;

namespace SQLLLM.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            // Look for any existing sales data
            if (context.Sales.Any())
            {
                return; // DB has been seeded already
            }

            var regions = new[] { "North", "South", "East", "West", "Central" };
            var random = new Random();
            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 12, 31);

            // Create sample sales data spanning across 2025
            var salesData = Enumerable.Range(1, 500).Select(_ =>
            {
                var dayRange = (endDate - startDate).Days;
                var saleDate = startDate.AddDays(random.Next(dayRange));
                
                return new Sales
                {
                    Region = regions[random.Next(regions.Length)],
                    SaleDate = saleDate,
                    SalesAmount = decimal.Round((decimal)(random.NextDouble() * 10000), 2)
                };
            }).ToArray();

            context.Sales.AddRange(salesData);
            context.SaveChanges();
        }
    }
}