using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockManager.Core.Entities;

namespace StockManager.Data.Contexts;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Business> Businesses { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<ProductSupplier> ProductSuppliers { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }
    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<ReceiptLine> ReceiptLines { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Business configuration
        builder.Entity<Business>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => e.Name);
        });

        // ApplicationUser configuration
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(e => e.Business)
                  .WithMany(b => b.Users)
                  .HasForeignKey(e => e.BusinessId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.BusinessId);
            entity.HasIndex(e => e.Email);
        });

        // Category configuration
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Business)
                  .WithMany(b => b.Categories)
                  .HasForeignKey(e => e.BusinessId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => new { e.BusinessId, e.Name });
        });

        // Product configuration
        builder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SKU).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CurrentStock).HasPrecision(18, 2);
            entity.Property(e => e.CostPerUnit).HasPrecision(18, 2);

            entity.HasOne(e => e.Business)
                  .WithMany(b => b.Products)
                  .HasForeignKey(e => e.BusinessId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => new { e.BusinessId, e.SKU }).IsUnique();
            entity.HasIndex(e => e.Name);
        });

        // StockMovement configuration
        builder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 2);
            entity.Property(e => e.PreviousStock).HasPrecision(18, 2);
            entity.Property(e => e.NewStock).HasPrecision(18, 2);

            entity.HasOne(e => e.Product)
                  .WithMany(p => p.StockMovements)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.StockMovements)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Company configuration
        builder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.TaxNumber).HasMaxLength(50);

            entity.HasOne(e => e.Business)
                  .WithMany()
                  .HasForeignKey(e => e.BusinessId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => new { e.BusinessId, e.Name });
            entity.HasIndex(e => e.IsSupplier);
            entity.HasIndex(e => e.IsCustomer);
        });

        // ProductSupplier configuration (many-to-many junction table)
        builder.Entity<ProductSupplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SupplierPrice).HasPrecision(18, 2);

            entity.HasOne(e => e.Product)
                  .WithMany(p => p.ProductSuppliers)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Company)
                  .WithMany(c => c.ProductSuppliers)
                  .HasForeignKey(e => e.CompanyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ProductId, e.CompanyId }).IsUnique();
            entity.HasIndex(e => e.IsPrimarySupplier);
        });

        // PurchaseOrder configuration
        builder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SubTotal).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.ShippingCost).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.SupplierReference).HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(e => e.Business)
                  .WithMany()
                  .HasForeignKey(e => e.BusinessId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Company)
                  .WithMany()
                  .HasForeignKey(e => e.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.BusinessId, e.OrderNumber }).IsUnique();
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.OrderDate);
            entity.HasIndex(e => e.ExpectedDeliveryDate);
            entity.HasIndex(e => e.Status);
        });

        // PurchaseOrderLine configuration
        builder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ProductSku).IsRequired().HasMaxLength(100);
            entity.Property(e => e.QuantityOrdered).HasPrecision(18, 2);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.LineTotal).HasPrecision(18, 2);
            entity.Property(e => e.QuantityReceived).HasPrecision(18, 2);
            entity.Property(e => e.QuantityOutstanding).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(e => e.PurchaseOrder)
                  .WithMany(po => po.Lines)
                  .HasForeignKey(e => e.PurchaseOrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PurchaseOrderId);
            entity.HasIndex(e => e.ProductId);
        });

        // Receipt configuration
        builder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReceiptNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SupplierDeliveryNote).HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(e => e.Business)
                  .WithMany()
                  .HasForeignKey(e => e.BusinessId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PurchaseOrder)
                  .WithMany(po => po.Receipts)
                  .HasForeignKey(e => e.PurchaseOrderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReceivedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ReceivedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ValidatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ValidatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.BusinessId, e.ReceiptNumber }).IsUnique();
            entity.HasIndex(e => e.PurchaseOrderId);
            entity.HasIndex(e => e.ReceiptDate);
            entity.HasIndex(e => e.Status);
        });

        // ReceiptLine configuration
        builder.Entity<ReceiptLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuantityOrdered).HasPrecision(18, 2);
            entity.Property(e => e.QuantityReceived).HasPrecision(18, 2);
            entity.Property(e => e.QuantityVariance).HasPrecision(18, 2);
            entity.Property(e => e.UnitPriceOrdered).HasPrecision(18, 2);
            entity.Property(e => e.UnitPriceReceived).HasPrecision(18, 2);
            entity.Property(e => e.PriceVariance).HasPrecision(18, 2);
            entity.Property(e => e.Condition).HasConversion<string>();
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.BatchNumber).HasMaxLength(50);

            entity.HasOne(e => e.Receipt)
                  .WithMany(r => r.Lines)
                  .HasForeignKey(e => e.ReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PurchaseOrderLine)
                  .WithMany(pol => pol.ReceiptLines)
                  .HasForeignKey(e => e.PurchaseOrderLineId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ReceiptId);
            entity.HasIndex(e => e.PurchaseOrderLineId);
            entity.HasIndex(e => e.ProductId);
        });

        // PasswordResetToken configuration
        builder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token);
            entity.HasIndex(e => new { e.UserId, e.IsUsed });
            entity.HasIndex(e => e.ExpiresAt);
        });

        // RefreshToken configuration
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(256);
            entity.Property(e => e.CreatedByIp).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RevokedByIp).HasMaxLength(50);
            entity.Property(e => e.ReplacedByToken).HasMaxLength(256);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.UserId, e.ExpiresAt });
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
