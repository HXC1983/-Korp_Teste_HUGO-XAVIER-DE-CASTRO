using FaturamentoService.Models;
using Microsoft.EntityFrameworkCore;

namespace FaturamentoService.Data
{
    public class FaturamentoDbContext : DbContext
    {
        public FaturamentoDbContext(DbContextOptions<FaturamentoDbContext> options) : base(options) { }

        public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
        public DbSet<ItemNotaFiscal> ItensNotaFiscal => Set<ItemNotaFiscal>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NotaFiscal>(entity =>
            {
                entity.HasIndex(n => n.Numero).IsUnique();
                entity.HasMany(n => n.Itens)
                      .WithOne(i => i.NotaFiscal)
                      .HasForeignKey(i => i.NotaFiscalId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
