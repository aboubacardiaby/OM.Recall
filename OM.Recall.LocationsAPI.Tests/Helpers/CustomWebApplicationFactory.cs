﻿using Moq;
using Xunit;
using FluentAssertions;
using OM.Recall.LocationsAPI.Data;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using OM.Recall.LocationsAPI.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace OM.Recall.LocationsAPI.Tests.Helpers
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add DbContext using in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"InMemoryDbForTesting_{Guid.NewGuid()}");
                });

                // Build the service provider
                var sp = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database context
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                // Ensure the database is created
                db.Database.EnsureCreated();

                try
                {
                    // Seed the database with test data if needed
                    SeedTestData(db);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the database with test messages. Error: {Message}", ex.Message);
                }
            });
        }

        private void SeedTestData(ApplicationDbContext context)
        {
            if (!context.Locations.Any())
            {
                var testLocations = TestDataGenerator.GenerateLocations(3);
                context.Locations.AddRange(testLocations);
                context.SaveChanges();
            }
        }
    }
}
