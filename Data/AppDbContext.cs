using Microsoft.EntityFrameworkCore;
using Digital_Krushi_Officer.Models;

namespace Digital_Krushi_Officer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TblUser> Users { get; set; }
        public DbSet<TblFarmerDetails> FarmerDetails { get; set; }
        public DbSet<TblOfficerDetails> OfficerDetails { get; set; }
        public DbSet<TblOfficerAssignment> OfficerAssignments { get; set; }
        public DbSet<TblFarmerOfficerAssignment> FarmerOfficerAssignments { get; set; }
        public DbSet<TblCrop> Crops { get; set; }
        public DbSet<TblFarmerCrop> FarmerCrops { get; set; }
        public DbSet<TblQuery> Queries { get; set; }
        public DbSet<TblAIResponse> AIResponses { get; set; }
        public DbSet<TblEscalation> Escalations { get; set; }
        public DbSet<TblFeedback> Feedbacks { get; set; }
        public DbSet<TblWeatherLog> WeatherLogs { get; set; }
        public DbSet<TblGovernmentScheme> GovernmentSchemes { get; set; }
        public DbSet<TblDiseaseDataset> DiseaseDatasets { get; set; }
        public DbSet<TblNotificationLog> NotificationLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TblUser
            modelBuilder.Entity<TblUser>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // TblFarmerDetails - One-to-One with TblUser
            modelBuilder.Entity<TblFarmerDetails>(entity =>
            {
                entity.HasOne(f => f.User)
                      .WithOne(u => u.FarmerDetails)
                      .HasForeignKey<TblFarmerDetails>(f => f.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // TblOfficerDetails - One-to-One with TblUser
            modelBuilder.Entity<TblOfficerDetails>(entity =>
            {
                entity.HasOne(o => o.User)
                      .WithOne(u => u.OfficerDetails)
                      .HasForeignKey<TblOfficerDetails>(o => o.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // TblFarmerCrop
            modelBuilder.Entity<TblFarmerCrop>(entity =>
            {
                entity.HasOne(fc => fc.Farmer)
                      .WithMany(u => u.FarmerCrops)
                      .HasForeignKey(fc => fc.FarmerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(fc => fc.Crop)
                      .WithMany(c => c.FarmerCrops)
                      .HasForeignKey(fc => fc.CropId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // TblQuery
            modelBuilder.Entity<TblQuery>(entity =>
            {
                entity.HasOne(q => q.Farmer)
                      .WithMany(u => u.Queries)
                      .HasForeignKey(q => q.FarmerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // TblAIResponse - One-to-One with TblQuery
            modelBuilder.Entity<TblAIResponse>(entity =>
            {
                entity.HasOne(a => a.Query)
                      .WithOne(q => q.AIResponse)
                      .HasForeignKey<TblAIResponse>(a => a.QueryId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // TblEscalation - One-to-One with TblQuery
            modelBuilder.Entity<TblEscalation>(entity =>
            {
                entity.HasOne(e => e.Query)
                      .WithOne(q => q.Escalation)
                      .HasForeignKey<TblEscalation>(e => e.QueryId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Officer)
                      .WithMany()
                      .HasForeignKey(e => e.OfficerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // TblFeedback
            modelBuilder.Entity<TblFeedback>(entity =>
            {
                entity.HasOne(f => f.Query)
                      .WithMany(q => q.Feedbacks)
                      .HasForeignKey(f => f.QueryId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.AIResponse)
                      .WithMany()
                      .HasForeignKey(f => f.AIResponseId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // TblNotificationLog
            modelBuilder.Entity<TblNotificationLog>(entity =>
            {
                entity.HasOne(n => n.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // TblOfficerAssignment
            modelBuilder.Entity<TblOfficerAssignment>(entity =>
            {
                entity.HasOne(a => a.Officer)
                      .WithMany()
                      .HasForeignKey(a => a.OfficerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // TblFarmerOfficerAssignment
            modelBuilder.Entity<TblFarmerOfficerAssignment>(entity =>
            {
                entity.HasOne(a => a.Farmer)
                      .WithMany()
                      .HasForeignKey(a => a.FarmerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Officer)
                      .WithMany()
                      .HasForeignKey(a => a.OfficerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(a => new { a.FarmerId, a.OfficerId }).IsUnique();
            });

            // Seed Admin user
            modelBuilder.Entity<TblUser>().HasData(new TblUser
            {
                Id = 1,
                Name = "System Admin",
                Email = "admin@digitalkrushi.com",
                Mobile = "9999999999",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "Admin",
                State = "Gujarat",
                District = "Ahmedabad",
                PreferredLanguage = "Gujarati",
                IsEmailVerified = true,
                IsActive = true,
                CreatedDate = new DateTime(2024, 1, 1)
            });

            // Seed Crops
            modelBuilder.Entity<TblCrop>().HasData(
                new TblCrop { Id = 1, CropName = "Cotton", Season = "Kharif", Description = "Major cash crop of Gujarat", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblCrop { Id = 2, CropName = "Groundnut", Season = "Kharif", Description = "Oilseed crop suitable for Gujarat soil", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblCrop { Id = 3, CropName = "Wheat", Season = "Rabi", Description = "Winter cereal crop", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblCrop { Id = 4, CropName = "Bajra", Season = "Kharif", Description = "Millet crop drought resistant", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblCrop { Id = 5, CropName = "Turmeric", Season = "Kharif", Description = "Spice crop with medicinal value", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblCrop { Id = 6, CropName = "Rice", Season = "Kharif", Description = "Staple food crop", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblCrop { Id = 7, CropName = "Sugarcane", Season = "Year-round", Description = "Cash crop for sugar production", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblCrop { Id = 8, CropName = "Castor", Season = "Kharif", Description = "Oilseed industrial crop", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) }
            );

            // Seed Government Schemes
            modelBuilder.Entity<TblGovernmentScheme>().HasData(
                new TblGovernmentScheme { Id = 1, SchemeName = "PM-KISAN", Description = "Direct income support of Rs. 6000/year to farmer families", Eligibility = "Small and marginal farmers with cultivable land", Category = "Subsidy", Status = "Active", StartDate = new DateTime(2019, 2, 1), LastDate = new DateTime(2026, 3, 31), IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblGovernmentScheme { Id = 2, SchemeName = "Soil Health Card Scheme", Description = "Free soil testing and recommendation of nutrients", Eligibility = "All farmers", Category = "Training", Status = "Active", StartDate = new DateTime(2015, 2, 1), LastDate = new DateTime(2026, 3, 31), IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblGovernmentScheme { Id = 3, SchemeName = "Kisan Credit Card", Description = "Short-term credit for cultivation at low interest", Eligibility = "All farmers including sharecroppers", Category = "Loan", Status = "Active", StartDate = new DateTime(1998, 8, 1), LastDate = new DateTime(2026, 3, 31), IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblGovernmentScheme { Id = 4, SchemeName = "Crop Insurance (PMFBY)", Description = "Comprehensive crop insurance against natural calamities", Eligibility = "All farmers growing notified crops", Category = "Insurance", Status = "Active", StartDate = new DateTime(2016, 4, 1), LastDate = new DateTime(2026, 3, 31), IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblGovernmentScheme { Id = 5, SchemeName = "Agricultural Mechanization", Description = "Subsidy on farm machinery and equipment", Eligibility = "Individual farmers, cooperatives", Category = "Subsidy", Status = "Active", StartDate = new DateTime(2014, 4, 1), LastDate = new DateTime(2026, 3, 31), IsActive = true, CreatedDate = new DateTime(2024, 1, 1) }
            );

            // Seed Disease Dataset
            modelBuilder.Entity<TblDiseaseDataset>().HasData(
                new TblDiseaseDataset { Id = 1, CropName = "Cotton", DiseaseName = "Cotton Leaf Curl Virus", Symptoms = "Leaves curl upward, vein thickening, stunted growth", Solution = "Remove infected plants, use resistant varieties, control whiteflies", RecommendedPesticide = "Imidacloprid 17.8% SL", SeverityLevel = "High", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblDiseaseDataset { Id = 2, CropName = "Groundnut", DiseaseName = "Tikka Disease", Symptoms = "Circular spots on leaves, defoliation", Solution = "Spray fungicide at 15-day intervals, crop rotation", RecommendedPesticide = "Mancozeb 75% WP", SeverityLevel = "Medium", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblDiseaseDataset { Id = 3, CropName = "Wheat", DiseaseName = "Rust Disease", Symptoms = "Orange-brown pustules on leaves and stems", Solution = "Use resistant varieties, timely sowing, fungicide spray", RecommendedPesticide = "Propiconazole 25% EC", SeverityLevel = "High", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblDiseaseDataset { Id = 4, CropName = "Cotton", DiseaseName = "Bollworm", Symptoms = "Bolls damaged, larvae inside bolls", Solution = "Pheromone traps, biological control, insecticide spray", RecommendedPesticide = "Spinosad 45% SC", SeverityLevel = "Critical", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) },
                new TblDiseaseDataset { Id = 5, CropName = "Groundnut", DiseaseName = "Root Rot", Symptoms = "Wilting, yellowing, root decay", Solution = "Soil treatment, proper drainage, resistant varieties", RecommendedPesticide = "Carbendazim 50% WP", SeverityLevel = "High", IsActive = true, CreatedDate = new DateTime(2024, 1, 1) }
            );
        }
    }
}
