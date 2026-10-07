using c1Soft_b4bProje.Models;
using c1Soft_b4bProje.Services;
using Microsoft.EntityFrameworkCore;

namespace c1Soft_b4bProje.Data;

public class ApplicationDbContext : DbContext
{
    private readonly TenantService tenant;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, TenantService tenant)
        : base(options)
    {
        this.tenant = tenant;
    }

    private int? AktifFirmaId => tenant.FirmaId;

    public DbSet<Firma> Firma { get; set; }
    public DbSet<Kullanici> Kullanici { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<SiparisR> SiparisR { get; set; }
    public DbSet<SiparisD> SiparisD { get; set; }
    public DbSet<KullaniciOturum> KullaniciOturum { get; set; }
    public DbSet<IslemLog> IslemLog { get; set; }
    public DbSet<HataLog> HataLog { get; set; }

    // Tablo ilişkilerini, indexleri ve firma filtresini ayarlar
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Firma>()
            .HasIndex(x => x.FirmaKodu)
            .IsUnique();

        modelBuilder.Entity<Kullanici>()
            .HasOne(x => x.Firma)
            .WithMany(x => x.Kullanicilar)
            .HasForeignKey(x => x.FirmaId);

        modelBuilder.Entity<Kullanici>()
            .HasIndex(x => new { x.FirmaId, x.KulAdi })
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasOne(x => x.Firma)
            .WithMany()
            .HasForeignKey(x => x.FirmaId);

        modelBuilder.Entity<SiparisR>()
            .HasOne(x => x.Kullanici)
            .WithMany()
            .HasForeignKey(x => x.KullaniciId);

        modelBuilder.Entity<SiparisD>()
            .HasOne(x => x.Siparis)
            .WithMany(x => x.Kalemler)
            .HasForeignKey(x => x.SiparisId);

        modelBuilder.Entity<SiparisD>()
            .HasOne(x => x.Urun)
            .WithMany()
            .HasForeignKey(x => x.UrunId);

        modelBuilder.Entity<KullaniciOturum>()
            .HasOne(x => x.Kullanici)
            .WithMany()
            .HasForeignKey(x => x.KullaniciId);

        modelBuilder.Entity<KullaniciOturum>()
            .HasOne(x => x.Firma)
            .WithMany()
            .HasForeignKey(x => x.FirmaId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<KullaniciOturum>()
            .HasIndex(x => x.OturumAnahtari)
            .IsUnique();

        modelBuilder.Entity<Product>().HasQueryFilter(x => x.FirmaId == AktifFirmaId);
        modelBuilder.Entity<SiparisR>().HasQueryFilter(x => x.FirmaId == AktifFirmaId);
        modelBuilder.Entity<SiparisD>().HasQueryFilter(x => x.Siparis!.FirmaId == AktifFirmaId);
    }
}
