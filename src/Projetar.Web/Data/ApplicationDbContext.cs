using Projetar.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Projetar.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Principio> Principios => Set<Principio>();
    public DbSet<Item> Itens => Set<Item>();
    public DbSet<ItemRevisao> ItemRevisoes => Set<ItemRevisao>();
    public DbSet<ItemRevisaoApoio> ItemRevisaoApoios => Set<ItemRevisaoApoio>();
    public DbSet<Comentario> Comentarios => Set<Comentario>();
    public DbSet<Voto> Votos => Set<Voto>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<Referencia> Referencias => Set<Referencia>();
    public DbSet<TagSugestao> TagSugestoes => Set<TagSugestao>();
    public DbSet<TagCatalogo> TagsCatalogo => Set<TagCatalogo>();
    public DbSet<BannerSugestao> BannerSugestoes => Set<BannerSugestao>();
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<MensagemModeracao> MensagensModeracao => Set<MensagemModeracao>();
    public DbSet<EscopoModeracao> EscoposModeracao => Set<EscopoModeracao>();
    public DbSet<EmailConfirmationCode> EmailConfirmationCodes => Set<EmailConfirmationCode>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            // Reforça no banco o RequireUniqueEmail já configurado no Identity (Program.cs).
            entity.HasIndex(u => u.NormalizedEmail).IsUnique().HasDatabaseName("EmailIndex");
        });

        builder.Entity<EmailConfirmationCode>(entity =>
        {
            entity.HasIndex(c => new { c.UsuarioId, c.Usado });
            entity.HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PasswordResetCode>(entity =>
        {
            entity.HasIndex(c => new { c.UsuarioId, c.Usado });
            entity.HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Principio>(entity =>
        {
            entity.Property(m => m.Id).ValueGeneratedNever();
            entity.HasIndex(m => m.Ordem);
        });

        builder.Entity<Item>(entity =>
        {
            entity.HasIndex(i => i.Slug).IsUnique();
            entity.HasIndex(i => i.Status);
            entity.HasOne(i => i.Principio)
                .WithMany(m => m.Itens)
                .HasForeignKey(i => i.PrincipioId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.CriadoPorUsuario)
                .WithMany()
                .HasForeignKey(i => i.CriadoPorUsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(i => i.RevisadoPorUsuario)
                .WithMany()
                .HasForeignKey(i => i.RevisadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ItemRevisao>(entity =>
        {
            entity.HasIndex(r => r.Status);
            entity.HasOne(r => r.Item)
                .WithMany(i => i.Revisoes)
                .HasForeignKey(r => r.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.AutorUsuario)
                .WithMany()
                .HasForeignKey(r => r.AutorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.RevisadoPorUsuario)
                .WithMany()
                .HasForeignKey(r => r.RevisadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ItemRevisaoApoio>(entity =>
        {
            entity.HasIndex(a => new { a.ItemRevisaoId, a.UsuarioId }).IsUnique();
            entity.HasOne(a => a.ItemRevisao)
                .WithMany(r => r.Apoios)
                .HasForeignKey(a => a.ItemRevisaoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.Usuario)
                .WithMany()
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Comentario>(entity =>
        {
            entity.HasOne(c => c.Item)
                .WithMany(i => i.Comentarios)
                .HasForeignKey(c => c.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.ComentarioPai)
                .WithMany(c => c.Respostas)
                .HasForeignKey(c => c.ComentarioPaiId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Voto>(entity =>
        {
            entity.HasIndex(v => new { v.ItemId, v.UsuarioId }).IsUnique();
            entity.HasOne(v => v.Item)
                .WithMany(i => i.Votos)
                .HasForeignKey(v => v.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(v => v.Usuario)
                .WithMany()
                .HasForeignKey(v => v.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Documento>(entity =>
        {
            entity.HasIndex(d => d.Status);
            entity.HasOne(d => d.Item)
                .WithMany(i => i.Documentos)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Usuario)
                .WithMany()
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.RevisadoPorUsuario)
                .WithMany()
                .HasForeignKey(d => d.RevisadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Referencia>(entity =>
        {
            entity.HasIndex(r => r.Status);
            entity.HasOne(r => r.Item)
                .WithMany(i => i.Referencias)
                .HasForeignKey(r => r.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Usuario)
                .WithMany()
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.RevisadoPorUsuario)
                .WithMany()
                .HasForeignKey(r => r.RevisadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TagSugestao>(entity =>
        {
            entity.HasIndex(t => t.Status);
            entity.HasOne(t => t.Item)
                .WithMany()
                .HasForeignKey(t => t.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(t => t.Usuario)
                .WithMany()
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.RevisadoPorUsuario)
                .WithMany()
                .HasForeignKey(t => t.RevisadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TagCatalogo>(entity =>
        {
            entity.HasIndex(t => t.Nome).IsUnique();
        });

        builder.Entity<BannerSugestao>(entity =>
        {
            entity.HasIndex(b => b.Status);
            entity.HasOne(b => b.Item)
                .WithMany()
                .HasForeignKey(b => b.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(b => b.Usuario)
                .WithMany()
                .HasForeignKey(b => b.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.RevisadoPorUsuario)
                .WithMany()
                .HasForeignKey(b => b.RevisadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notificacao>(entity =>
        {
            entity.HasIndex(n => new { n.UsuarioDestinoId, n.Lida });
            entity.HasOne(n => n.UsuarioDestino)
                .WithMany()
                .HasForeignKey(n => n.UsuarioDestinoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(n => n.UsuarioOrigem)
                .WithMany()
                .HasForeignKey(n => n.UsuarioOrigemId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(n => n.Item)
                .WithMany()
                .HasForeignKey(n => n.ItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<MensagemModeracao>(entity =>
        {
            entity.HasIndex(m => new { m.TipoAlvo, m.AlvoId });
            entity.HasOne(m => m.Item)
                .WithMany()
                .HasForeignKey(m => m.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(m => m.AutorUsuario)
                .WithMany()
                .HasForeignKey(m => m.AutorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EscopoModeracao>(entity =>
        {
            entity.HasIndex(e => new { e.UsuarioId, e.TipoEscopo, e.PrincipioId, e.ItemId }).IsUnique();
            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AdicionadoPorUsuario)
                .WithMany()
                .HasForeignKey(e => e.AdicionadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Principio)
                .WithMany()
                .HasForeignKey(e => e.PrincipioId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Item)
                .WithMany()
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
