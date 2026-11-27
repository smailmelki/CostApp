using Microsoft.EntityFrameworkCore;

namespace CostApp.Models
{
    public class DBContext : DbContext
    {
        public virtual DbSet<DetailItem> DetailItem { get; set; }
        public virtual DbSet<TreeItem> TreeItem { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            var databasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CostAppDB.db");

            optionsBuilder.UseSqlite($"Data Source={databasePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DetailItem>(entity =>
            {
                entity.ToTable("DetailItem");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.ParentID).IsRequired().HasColumnName("ParentID");
                entity.Property(e => e.Date).IsRequired().HasColumnName("Date");
                entity.Property(e => e.Amount).IsRequired().HasColumnName("Amount");
                entity.Property(e => e.Note).HasMaxLength(30).HasColumnName("Note");
            });

            modelBuilder.Entity<TreeItem>(entity =>
            {
                entity.ToTable("TreeItem");
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(20).HasColumnName("Title");
                entity.Ignore(p => p.Details);
                entity.Ignore(p => p.Total);
            });
        }
    }
}