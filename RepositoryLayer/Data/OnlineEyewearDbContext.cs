using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Entities;

namespace RepositoryLayer.Data;

public class OnlineEyewearDbContext(DbContextOptions<OnlineEyewearDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();

    public DbSet<OrderType> OrderTypes => Set<OrderType>();

    public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();

    public DbSet<ShippingStatus> ShippingStatuses => Set<ShippingStatus>();

    public DbSet<PrescriptionStatus> PrescriptionStatuses => Set<PrescriptionStatus>();

    public DbSet<PaymentStatus> PaymentStatuses => Set<PaymentStatus>();

    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    public DbSet<ReturnStatus> ReturnStatuses => Set<ReturnStatus>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<Inventory> Inventory => Set<Inventory>();

    public DbSet<SupplyReceipt> SupplyReceipts => Set<SupplyReceipt>();

    public DbSet<Promotion> Promotions => Set<Promotion>();

    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<PrescriptionSpec> PrescriptionSpecs => Set<PrescriptionSpec>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PaymentHistory> PaymentHistory => Set<PaymentHistory>();

    public DbSet<Return> Returns => Set<Return>();

    public DbSet<Policy> Policies => Set<Policy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureLookups(modelBuilder);
        ConfigureUsers(modelBuilder);
        ConfigureCatalog(modelBuilder);
        ConfigureInventory(modelBuilder);
        ConfigurePromotions(modelBuilder);
        ConfigureOrders(modelBuilder);
        ConfigurePayments(modelBuilder);
        ConfigurePolicies(modelBuilder);
        SeedLookups(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureLookups(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.RoleId);
            entity.Property(x => x.RoleName).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.RoleName).IsUnique();
        });

        modelBuilder.Entity<OrderType>(entity =>
        {
            entity.ToTable("OrderTypes");
            entity.HasKey(x => x.OrderTypeId);
            entity.Property(x => x.OrderTypeName).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.OrderTypeName).IsUnique();
        });

        modelBuilder.Entity<OrderStatus>(entity =>
        {
            entity.ToTable("OrderStatuses");
            entity.HasKey(x => x.OrderStatusId);
            entity.Property(x => x.StatusName).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.StatusName).IsUnique();
        });

        modelBuilder.Entity<ShippingStatus>(entity =>
        {
            entity.ToTable("ShippingStatuses");
            entity.HasKey(x => x.ShippingStatusId);
            entity.Property(x => x.StatusName).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.StatusName).IsUnique();
        });

        modelBuilder.Entity<PrescriptionStatus>(entity =>
        {
            entity.ToTable("PrescriptionStatuses");
            entity.HasKey(x => x.PrescriptionStatusId);
            entity.Property(x => x.StatusName).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.StatusName).IsUnique();
        });

        modelBuilder.Entity<PaymentStatus>(entity =>
        {
            entity.ToTable("PaymentStatuses");
            entity.HasKey(x => x.PaymentStatusId);
            entity.Property(x => x.StatusName).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.StatusName).IsUnique();
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("PaymentMethods");
            entity.HasKey(x => x.PaymentMethodId);
            entity.Property(x => x.MethodName).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.MethodName).IsUnique();
        });

        modelBuilder.Entity<ReturnStatus>(entity =>
        {
            entity.ToTable("ReturnStatuses");
            entity.HasKey(x => x.ReturnStatusId);
            entity.Property(x => x.StatusName).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.StatusName).IsUnique();
        });
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.UserId);

            entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(255);
            entity.Property(x => x.Phone).HasMaxLength(20);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasIndex(x => x.Email).IsUnique();

            entity.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");
            entity.HasKey(x => x.CartId);

            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.User)
                .WithMany(x => x.Carts)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems", table =>
            {
                table.HasCheckConstraint("CK_CartItems_Quantity", "[Quantity] > 0");
            });

            entity.HasKey(x => x.CartItemId);

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
            entity.ToTable("PrescriptionSpecs");
            entity.HasKey(x => x.PrescriptionId);

            entity.Property(x => x.SphLeft).HasColumnName("SPH_Left").HasPrecision(5, 2);
            entity.Property(x => x.SphRight).HasColumnName("SPH_Right").HasPrecision(5, 2);
            entity.Property(x => x.CylLeft).HasColumnName("CYL_Left").HasPrecision(5, 2);
            entity.Property(x => x.CylRight).HasColumnName("CYL_Right").HasPrecision(5, 2);
            entity.Property(x => x.Pd).HasColumnName("PD").HasPrecision(5, 2);
            entity.Property(x => x.PrescriptionImage).HasMaxLength(500);
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

            entity.HasOne(x => x.PrescriptionStatus)
                .WithMany(x => x.PrescriptionSpecs)
                .HasForeignKey(x => x.PrescriptionStatusId)
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
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.ProductId);

            entity.Property(x => x.ProductName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.BasePrice).HasPrecision(10, 2);
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

            entity.Property(x => x.Sku).HasColumnName("SKU").HasMaxLength(100);
            entity.Property(x => x.FrameType).HasMaxLength(50);
            entity.Property(x => x.Size).HasMaxLength(20);
            entity.Property(x => x.Color).HasMaxLength(50);
            entity.Property(x => x.Price).HasPrecision(10, 2).IsRequired();

            entity.HasIndex(x => x.Sku)
                .IsUnique()
                .HasFilter(null);

            entity.HasOne(x => x.Product)
                .WithMany(x => x.Variants)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("ProductImages");
            entity.HasKey(x => x.ImageId);

            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.Property(x => x.Is3D).HasColumnName("Is3D").HasDefaultValue(false);

            entity.HasOne(x => x.Product)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.ToTable("Inventory", table =>
            {
                table.HasCheckConstraint("CK_Inventory_Quantity", "[Quantity] >= 0");
                table.HasCheckConstraint("CK_Inventory_ReservedQuantity", "[ReservedQuantity] >= 0");
            });

            entity.HasKey(x => x.VariantId);

            entity.Property(x => x.Quantity).IsRequired();
            entity.Property(x => x.ReservedQuantity).HasDefaultValue(0);
            entity.Property(x => x.IsPreOrderAllowed).HasDefaultValue(false);

            entity.HasOne(x => x.Variant)
                .WithOne(x => x.Inventory)
                .HasForeignKey<Inventory>(x => x.VariantId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SupplyReceipt>(entity =>
        {
            entity.ToTable("SupplyReceipts", table =>
            {
                table.HasCheckConstraint("CK_SupplyReceipts_QuantityReceived", "[QuantityReceived] > 0");
            });

            entity.HasKey(x => x.ReceiptId);

            entity.Property(x => x.QuantityReceived).IsRequired();
            entity.Property(x => x.ReceivedDate).HasDefaultValueSql("GETDATE()");
            entity.Property(x => x.BatchNumber).HasMaxLength(100);
            entity.Property(x => x.Note).HasMaxLength(255);

            entity.HasOne(x => x.Variant)
                .WithMany(x => x.SupplyReceipts)
                .HasForeignKey(x => x.VariantId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Staff)
                .WithMany(x => x.SupplyReceipts)
                .HasForeignKey(x => x.StaffId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigurePromotions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("Promotions");
            entity.HasKey(x => x.PromotionId);

            entity.Property(x => x.PromoCode).HasMaxLength(50);
            entity.Property(x => x.PromotionName).HasMaxLength(255);
            entity.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasIndex(x => x.PromoCode)
                .IsUnique()
                .HasFilter(null);
        });
    }

    private static void ConfigureOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(x => x.OrderId);

            entity.Property(x => x.DiscountAmount).HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(x => x.TotalAmount).HasPrecision(10, 2);
            entity.Property(x => x.ReceiverName).HasMaxLength(255);
            entity.Property(x => x.ReceiverPhone).HasMaxLength(20);
            entity.Property(x => x.ShippingAddress).HasMaxLength(500);
            entity.Property(x => x.ShippingCode).HasMaxLength(100);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.User)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Promotion)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.PromotionId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.OrderType)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.OrderTypeId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.OrderStatus)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.OrderStatusId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.ShippingStatus)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.ShippingStatusId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.SalesStaff)
                .WithMany(x => x.SalesOrders)
                .HasForeignKey(x => x.SalesStaffId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.OperationsStaff)
                .WithMany(x => x.OperationsOrders)
                .HasForeignKey(x => x.OperationsStaffId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems", table =>
            {
                table.HasCheckConstraint("CK_OrderItems_Quantity", "[Quantity] > 0");
            });

            entity.HasKey(x => x.OrderItemId);

            entity.Property(x => x.Price).HasPrecision(10, 2);

            entity.HasOne(x => x.Order)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Variant)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.VariantId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Prescription)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.PrescriptionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("OrderStatusHistory");
            entity.HasKey(x => x.HistoryId);

            entity.Property(x => x.Note).HasMaxLength(255);
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.Order)
                .WithMany(x => x.OrderStatusHistories)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.OrderStatus)
                .WithMany(x => x.OrderStatusHistories)
                .HasForeignKey(x => x.OrderStatusId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.UpdatedByUser)
                .WithMany(x => x.UpdatedOrderStatusHistories)
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Return>(entity =>
        {
            entity.ToTable("Returns");
            entity.HasKey(x => x.ReturnId);

            entity.Property(x => x.Reason).HasMaxLength(255);
            entity.Property(x => x.RefundAmount).HasPrecision(10, 2);

            entity.HasOne(x => x.Order)
                .WithMany(x => x.Returns)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.ReturnStatus)
                .WithMany(x => x.Returns)
                .HasForeignKey(x => x.ReturnStatusId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigurePayments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(x => x.PaymentId);

            entity.Property(x => x.Amount).HasPrecision(10, 2);

            entity.HasOne(x => x.Order)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.PaymentMethod)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.PaymentMethodId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.PaymentStatus)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.PaymentStatusId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PaymentHistory>(entity =>
        {
            entity.ToTable("PaymentHistory");
            entity.HasKey(x => x.PaymentHistoryId);

            entity.Property(x => x.TransactionCode).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(255);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.Payment)
                .WithMany(x => x.PaymentHistories)
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.PaymentStatus)
                .WithMany(x => x.PaymentHistories)
                .HasForeignKey(x => x.PaymentStatusId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigurePolicies(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Policy>(entity =>
        {
            entity.ToTable("Policies");
            entity.HasKey(x => x.PolicyId);

            entity.Property(x => x.Title).HasMaxLength(255);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
        });
    }

    private static void SeedLookups(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "SalesStaff" },
            new Role { RoleId = 3, RoleName = "OperationsStaff" },
            new Role { RoleId = 4, RoleName = "Manager" },
            new Role { RoleId = 5, RoleName = "Customer" });

        modelBuilder.Entity<OrderType>().HasData(
            new OrderType { OrderTypeId = 1, OrderTypeName = "Ready" },
            new OrderType { OrderTypeId = 2, OrderTypeName = "PreOrder" },
            new OrderType { OrderTypeId = 3, OrderTypeName = "Prescription" });

        modelBuilder.Entity<OrderStatus>().HasData(
            new OrderStatus { OrderStatusId = 1, StatusName = "Pending" },
            new OrderStatus { OrderStatusId = 2, StatusName = "Confirmed" },
            new OrderStatus { OrderStatusId = 3, StatusName = "AwaitingStock" },
            new OrderStatus { OrderStatusId = 4, StatusName = "Processing" },
            new OrderStatus { OrderStatusId = 5, StatusName = "Shipped" },
            new OrderStatus { OrderStatusId = 6, StatusName = "Completed" },
            new OrderStatus { OrderStatusId = 7, StatusName = "Cancelled" });

        modelBuilder.Entity<ShippingStatus>().HasData(
            new ShippingStatus { ShippingStatusId = 1, StatusName = "Pending" },
            new ShippingStatus { ShippingStatusId = 2, StatusName = "Picking" },
            new ShippingStatus { ShippingStatusId = 3, StatusName = "Delivering" },
            new ShippingStatus { ShippingStatusId = 4, StatusName = "Delivered" },
            new ShippingStatus { ShippingStatusId = 5, StatusName = "Failed" });

        modelBuilder.Entity<PrescriptionStatus>().HasData(
            new PrescriptionStatus { PrescriptionStatusId = 1, StatusName = "Submitted" },
            new PrescriptionStatus { PrescriptionStatusId = 2, StatusName = "Reviewing" },
            new PrescriptionStatus { PrescriptionStatusId = 3, StatusName = "Approved" },
            new PrescriptionStatus { PrescriptionStatusId = 4, StatusName = "Rejected" });

        modelBuilder.Entity<PaymentStatus>().HasData(
            new PaymentStatus { PaymentStatusId = 1, StatusName = "Pending" },
            new PaymentStatus { PaymentStatusId = 2, StatusName = "Completed" },
            new PaymentStatus { PaymentStatusId = 3, StatusName = "Failed" },
            new PaymentStatus { PaymentStatusId = 4, StatusName = "Refunded" });

        modelBuilder.Entity<PaymentMethod>().HasData(
            new PaymentMethod { PaymentMethodId = 1, MethodName = "COD" },
            new PaymentMethod { PaymentMethodId = 2, MethodName = "VNPay" },
            new PaymentMethod { PaymentMethodId = 3, MethodName = "Momo" });

        modelBuilder.Entity<ReturnStatus>().HasData(
            new ReturnStatus { ReturnStatusId = 1, StatusName = "Pending" },
            new ReturnStatus { ReturnStatusId = 2, StatusName = "Approved" },
            new ReturnStatus { ReturnStatusId = 3, StatusName = "Rejected" },
            new ReturnStatus { ReturnStatusId = 4, StatusName = "Refunded" });
    }
}
