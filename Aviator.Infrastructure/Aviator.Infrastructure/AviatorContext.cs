using Aviator.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Aviator.Infrastructure
{
    public class AviatorContext : DbContext
    {
        public DbSet<AircraftModel> Aircrafts { get; set; }
        public DbSet<PilotModel> Pilots { get; set; }
        public DbSet<FlightModel> Flights { get; set; }

        public AviatorContext(DbContextOptions<AviatorContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Зв'язок Flight ↔ Aircraft
            modelBuilder.Entity<FlightModel>()
                .HasOne(f => f.Aircraft)
                .WithMany(a => a.Flights)
                .HasForeignKey(f => f.AircraftModelId);

            // Зв'язок Flight ↔ Pilot
            modelBuilder.Entity<FlightModel>()
                .HasOne(f => f.Pilot)
                .WithMany(p => p.Flights)
                .HasForeignKey(f => f.PilotModelId);
        }
    }
}
