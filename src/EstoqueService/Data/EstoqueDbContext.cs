using EstoqueService.Models;
using Microsoft.EntityFrameworkCore;

namespace EstoqueService.Data
{
    public class EstoqueDbContext : DbContext
    {
        public EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : base(options) { }

        public DbSet<Produto> Produtos => Set<Produto>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Produto>(entity =>
            {
                entity.HasIndex(p => p.Codigo).IsUnique();
            });
        }
    }
}
