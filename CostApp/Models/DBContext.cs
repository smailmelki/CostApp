
using Microsoft.EntityFrameworkCore;

namespace CostApp.Models
{
    internal class DBContext : DbContext
    {
        public virtual DbSet<DetailItem> DetailItem { get; set; }
        public virtual DbSet<TreeItem> TreeItem { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            optionsBuilder.UseSqlite($"Data Source={path}\\CostAppDB.db;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("Arabic_100_CI_AS");

            modelBuilder.Entity<DetailItem>(entity =>
            {
                entity.ToTable("DetailItem");

                entity.Property(e => e.ID)
                    .IsRequired(true)
                    .HasColumnName("id");
                entity.Property(e => e.ParentID)
                    .IsRequired(true)
                    .HasMaxLength(30)
                    .HasColumnName("ParentID");
                entity.Property(e => e.Date)
                    .IsRequired(true)
                    .HasColumnName("Date");
                entity.Property(e => e.Amount)
                    .IsRequired(true)
                    .HasColumnName("Amount");
                entity.Property(e => e.Note)
                    .HasColumnName("Note");
            });

            modelBuilder.Entity<TreeItem>(entity =>
            {
                entity.ToTable("TreeItem");

                entity.Property(e => e.ID)
                    .IsRequired(true)
                    .HasColumnName("id");
                entity.Property(e => e.Title)
                    .IsRequired(true)
                    .HasMaxLength(30)
                    .HasColumnName("Title");
                entity.Ignore(p => p.Total);
            });
        }
    }
}
