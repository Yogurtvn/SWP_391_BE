using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Entities;
using RepositoryLayer.Enums;

namespace RepositoryLayer.Data;

public class OnlineEyewearDbContext(DbContextOptions<OnlineEyewearDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<LensType> LensTypes => Set<LensType>();

    public DbSet<Inventory> Inventory => Set<Inventory>();

    public DbSet<StockReceipt> StockReceipts => Set<StockReceipt>();

    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<PrescriptionSpec> PrescriptionSpecs => Set<PrescriptionSpec>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PaymentHistory> PaymentHistory => Set<PaymentHistory>();

    public DbSet<Policy> Policies => Set<Policy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureCatalog(modelBuilder);
        ConfigureInventory(modelBuilder);
        ConfigureOrders(modelBuilder);
        ConfigurePayments(modelBuilder);
        ConfigurePolicies(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", table =>
            {
                table.HasCheckConstraint("CK_Users_Role", "[Role] IN (1, 2, 3)");
            });

            entity.HasKey(x => x.UserId);

            entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(255);
            entity.Property(x => x.Phone).HasMaxLength(20);
            entity.Property(x => x.Role).HasColumnType("tinyint");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");
            entity.HasKey(x => x.CartId);

            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.User)
                .WithOne(x => x.Cart)
                .HasForeignKey<Cart>(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems", table =>
            {
                table.HasCheckConstraint("CK_CartItems_Quantity", "[Quantity] > 0");
            });

            entity.HasKey(x => x.CartItemId);
            entity.HasIndex(x => new { x.CartId, x.VariantId }).IsUnique();

            entity.HasOne(x => x.Cart)
                .WithMany(x => x.CartItems)
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Variant)
                .WithMany(x => x.CartItems)
                .HasForeignKey(x => x.VariantId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PrescriptionSpec>(entity =>
        {
            entity.ToTable("PrescriptionSpecs", table =>
            {
                table.HasCheckConstraint("CK_PrescriptionSpecs_PrescriptionStatus", "[PrescriptionStatus] IN (1, 2, 3, 4, 5, 6)");
            });

            entity.HasKey(x => x.PrescriptionId);

            entity.Property(x => x.SphLeft).HasColumnName("SPH_Left").HasPrecision(5, 2);
            entity.Property(x => x.SphRight).HasColumnName("SPH_Right").HasPrecision(5, 2);
            entity.Property(x => x.CylLeft).HasColumnName("CYL_Left").HasPrecision(5, 2);
            entity.Property(x => x.CylRight).HasColumnName("CYL_Right").HasPrecision(5, 2);
            entity.Property(x => x.AxisLeft).HasColumnName("Axis_Left");
            entity.Property(x => x.AxisRight).HasColumnName("Axis_Right");
            entity.Property(x => x.Pd).HasColumnName("PD").HasPrecision(5, 2);
            entity.Property(x => x.PrescriptionImage).HasMaxLength(500);
            entity.Property(x => x.PrescriptionStatus).HasColumnType("tinyint");
            entity.Property(x => x.Notes).HasMaxLength(255);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.User)
                .WithMany(x => x.PrescriptionSpecs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Staff)
                .WithMany(x => x.VerifiedPrescriptionSpecs)
                .HasForeignKey(x => x.StaffId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureCatalog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(x => x.CategoryId);
            entity.Property(x => x.CategoryName).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.CategoryName).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.ProductId);

            entity.Property(x => x.ProductName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.BasePrice).HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("ProductVariants");
            entity.HasKey(x => x.VariantId);

            entity.Property(x => x.Sku).HasColumnName("SKU").HasMaxLength(100).IsRequired();
            entity.Property(x => x.FrameType).HasMaxLength(50);
            entity.Property(x => x.Size).HasMaxLength(20);
            entity.Property(x => x.Color).HasMaxLength(50);
            entity.Property(x => x.Price).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasIndex(x => x.Sku).IsUnique();

            entity.HasOne(x => x.Product)
                .WithMany(x => x.Variants)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("ProductImages");
            entity.HasKey(x => x.ImageId);

            entity.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.DisplayOrder).HasDefaultValue(1);
            entity.Property(x => x.IsPrimary).HasDefaultValue(false);

            entity.HasOne(x => x.Product)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<LensType>(entity =>
        {
            entity.ToTable("LensTypes");
            entity.HasKey(x => x.LensTypeId);

            entity.Property(x => x.LensName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.Price).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
        });
    }

    private static void ConfigureInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.ToTable("Inventory", table =>
            {
                table.HasCheckConstraint("CK_Inventory_Quantity", "[Quantity] >= 0");
            });

            entity.HasKey(x => x.VariantId);

            entity.Property(x => x.IsPreOrderAllowed).HasDefaultValue(false);
            entity.Property(x => x.PreOrderNote).HasMaxLength(255);

            entity.HasOne(x => x.Variant)
                .WithOne(x => x.Inventory)
                .HasForeignKey<Inventory>(x => x.VariantId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<StockReceipt>(entity =>
        {
            entity.ToTable("StockReceipts", table =>
            {
                table.HasCheckConstraint("CK_StockReceipts_QuantityReceived", "[QuantityReceived] > 0");
            });

            entity.HasKey(x => x.ReceiptId);

            entity.Property(x => x.ReceivedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(x => x.Note).HasMaxLength(255);

            entity.HasOne(x => x.Variant)
                .WithMany(x => x.StockReceipts)
                .HasForeignKey(x => x.VariantId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Staff)
                .WithMany(x => x.StockReceipts)
                .HasForeignKey(x => x.StaffId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders", table =>
            {
                table.HasCheckConstraint("CK_Orders_OrderType", "[OrderType] IN (1, 2, 3)");
                table.HasCheckConstraint("CK_Orders_OrderStatus", "[OrderStatus] IN (1, 2, 3, 4, 5, 6, 7)");
                table.HasCheckConstraint("CK_Orders_ShippingStatus", "[ShippingStatus] IS NULL OR [ShippingStatus] IN (1, 2, 3, 4, 5)");
            });

            entity.HasKey(x => x.OrderId);

            entity.Property(x => x.OrderType).HasColumnType("tinyint");
            entity.Property(x => x.OrderStatus).HasColumnType("tinyint");
            entity.Property(x => x.ShippingStatus).HasColumnType("tinyint");
            entity.Property(x => x.TotalAmount).HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(x => x.ReceiverName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ReceiverPhone).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ShippingAddress).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ShippingCode).HasMaxLength(100);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.User)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Staff)
                .WithMany(x => x.HandledOrders)
                .HasForeignKey(x => x.StaffId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems", table =>
            {
                table.HasCheckConstraint("CK_OrderItems_Quantity", "[Quantity] > 0");
            });

            entity.HasKey(x => x.OrderItemId);

            entity.Property(x => x.FramePrice).HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(x => x.LensPrice).HasPrecision(10, 2).HasDefaultValue(0m);

            entity.HasOne(x => x.Order)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Variant)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.VariantId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.LensType)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.LensTypeId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Prescription)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.PrescriptionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("OrderStatusHistory", table =>
            {
                table.HasCheckConstraint("CK_OrderStatusHistory_OrderStatus", "[OrderStatus] IN (1, 2, 3, 4, 5, 6, 7)");
            });

            entity.HasKey(x => x.HistoryId);

            entity.Property(x => x.Note).HasMaxLength(255);
            entity.Property(x => x.OrderStatus).HasColumnType("tinyint");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.Order)
                .WithMany(x => x.OrderStatusHistories)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.UpdatedByUser)
                .WithMany(x => x.UpdatedOrderStatusHistories)
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigurePayments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments", table =>
            {
                table.HasCheckConstraint("CK_Payments_PaymentMethod", "[PaymentMethod] IN (1, 2, 3)");
                table.HasCheckConstraint("CK_Payments_PaymentStatus", "[PaymentStatus] IN (1, 2, 3)");
            });

            entity.HasKey(x => x.PaymentId);

            entity.Property(x => x.Amount).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.PaymentMethod).HasColumnType("tinyint");
            entity.Property(x => x.PaymentStatus).HasColumnType("tinyint");

            entity.HasOne(x => x.Order)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PaymentHistory>(entity =>
        {
            entity.ToTable("PaymentHistory", table =>
            {
                table.HasCheckConstraint("CK_PaymentHistory_PaymentStatus", "[PaymentStatus] IN (1, 2, 3)");
            });

            entity.HasKey(x => x.PaymentHistoryId);

            entity.Property(x => x.PaymentStatus).HasColumnType("tinyint");
            entity.Property(x => x.TransactionCode).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(255);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.Payment)
                .WithMany(x => x.PaymentHistories)
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigurePolicies(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Policy>(entity =>
        {
            entity.ToTable("Policies");
            entity.HasKey(x => x.PolicyId);

            entity.Property(x => x.Title).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");
        });
    }

}
