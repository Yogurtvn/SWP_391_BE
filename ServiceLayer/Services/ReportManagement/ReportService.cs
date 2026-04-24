using RepositoryLayer.Entities;
using RepositoryLayer.Enums;
using RepositoryLayer.Interfaces;
using ServiceLayer.Contracts.Report;
using ServiceLayer.DTOs.Report.Response;
using InventoryEntity = RepositoryLayer.Entities.Inventory; // Alias để tránh xung đột namespace

namespace ServiceLayer.Services.ReportManagement; // Dùng "ReportManagement" để tránh xung đột namespace

// Service xử lý logic nghiệp vụ báo cáo thống kê, sử dụng UnitOfWork + GenericRepository
public class ReportService(IUnitOfWork unitOfWork) : IReportService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork; // Inject UnitOfWork để truy cập repository

    private const int LowStockThreshold = 10; // Ngưỡng tồn kho thấp: variant có quantity <= 10 sẽ được tính là "low stock"

    // Tổng số đơn và phân loại theo OrderType trong khoảng thời gian
    public async Task<OrdersSummaryResponse> GetOrdersSummaryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var orderRepository = _unitOfWork.Repository<Order>(); // Lấy repository cho Order entity

        var orders = await orderRepository.FindAsync(
            filter: BuildDateFilter(startDate, endDate),      // Lọc theo khoảng thời gian
            tracked: false);                                  // Không cần track vì chỉ đọc dữ liệu

        var orderList = orders.ToList();                      // Chuyển sang List để đếm nhiều lần

        return new OrdersSummaryResponse
        {
            TotalOrders = orderList.Count,                                                        // Tổng số đơn
            ReadyOrders = orderList.Count(o => o.OrderType == OrderType.Ready),                   // Đếm đơn Ready
            PreOrderOrders = orderList.Count(o => o.OrderType == OrderType.PreOrder),              // Đếm đơn PreOrder
            PrescriptionOrders = orderList.Count(o => o.OrderType == OrderType.Prescription)       // Đếm đơn Prescription
        };
    }

    // Doanh thu theo thời gian, nhóm theo ngày/tuần/tháng
    public async Task<RevenuesSummaryResponse> GetRevenuesSummaryAsync(DateTime? startDate, DateTime? endDate, string groupBy = "month", CancellationToken cancellationToken = default)
    {
        var orderRepository = _unitOfWork.Repository<Order>(); // Lấy repository cho Order entity

        // Chỉ tính doanh thu từ đơn đã Completed (đã hoàn thành)
        var orders = await orderRepository.FindAsync(
            filter: order =>
                order.OrderStatus == OrderStatus.Completed &&                                     // Chỉ đơn Completed
                (!startDate.HasValue || order.CreatedAt >= startDate.Value) &&                     // Lọc từ ngày
                (!endDate.HasValue || order.CreatedAt <= endDate.Value),                           // Lọc đến ngày
            tracked: false);

        var orderList = orders.ToList();                      // Chuyển sang List để group

        // Nhóm đơn hàng theo khoảng thời gian (day/week/month)
        var groupedItems = GroupOrdersByPeriod(orderList, groupBy.ToLowerInvariant()) // Gọi helper group theo period
            .Select(group => new RevenueGroupItem
            {
                Period = group.Key,                           // Tên khoảng thời gian (vd: "2026-04-16")
                Revenue = group.Sum(o => o.TotalAmount),      // Tổng doanh thu trong khoảng
                OrderCount = group.Count()                    // Số đơn trong khoảng
            })
            .OrderBy(item => item.Period)                     // Sắp xếp theo thời gian tăng dần
            .ToList();

        return new RevenuesSummaryResponse
        {
            Items = groupedItems                              // Trả về danh sách doanh thu theo từng nhóm
        };
    }

    // Thống kê đơn prescription theo trạng thái PrescriptionStatus
    public async Task<PrescriptionsSummaryResponse> GetPrescriptionsSummaryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var prescriptionRepository = _unitOfWork.Repository<PrescriptionSpec>(); // Lấy repository cho PrescriptionSpec entity

        var prescriptions = await prescriptionRepository.FindAsync(
            filter: p =>
                (!startDate.HasValue || p.CreatedAt >= startDate.Value) &&                        // Lọc từ ngày
                (!endDate.HasValue || p.CreatedAt <= endDate.Value),                               // Lọc đến ngày
            tracked: false);

        var prescriptionList = prescriptions.ToList();        // Chuyển sang List để đếm nhiều lần

        return new PrescriptionsSummaryResponse
        {
            TotalPrescriptionOrders = prescriptionList.Count,                                                       // Tổng số đơn prescription
            Approved = prescriptionList.Count(p => p.PrescriptionStatus == PrescriptionStatus.Approved),             // Đếm đã duyệt
            NeedMoreInfo = 0,                                                                                          // Deprecated runtime state
            Rejected = prescriptionList.Count(p => p.PrescriptionStatus == PrescriptionStatus.Rejected)              // Đếm bị từ chối
        };
    }

    // Thống kê đơn pre-order theo OrderStatus
    public async Task<PreOrdersSummaryResponse> GetPreOrdersSummaryAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var orderRepository = _unitOfWork.Repository<Order>(); // Lấy repository cho Order entity

        // Chỉ lấy đơn PreOrder trong khoảng thời gian
        var orders = await orderRepository.FindAsync(
            filter: order =>
                order.OrderType == OrderType.PreOrder &&                                           // Chỉ đơn PreOrder
                (!startDate.HasValue || order.CreatedAt >= startDate.Value) &&                     // Lọc từ ngày
                (!endDate.HasValue || order.CreatedAt <= endDate.Value),                           // Lọc đến ngày
            tracked: false);

        var orderList = orders.ToList();                      // Chuyển sang List để đếm nhiều lần

        return new PreOrdersSummaryResponse
        {
            TotalPreOrders = orderList.Count,                                                     // Tổng số đơn pre-order
            AwaitingStock = orderList.Count(o => o.OrderStatus == OrderStatus.AwaitingStock),      // Đếm đang chờ hàng
            Processing = orderList.Count(o => o.OrderStatus == OrderStatus.Processing),            // Đếm đang xử lý
            Completed = orderList.Count(o => o.OrderStatus == OrderStatus.Completed)               // Đếm đã hoàn thành
        };
    }

    // Dashboard tổng hợp nhanh cho admin
    public async Task<DashboardResponse> GetDashboardAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var orderRepository = _unitOfWork.Repository<Order>();             // Lấy repository cho Order
        var inventoryRepository = _unitOfWork.Repository<InventoryEntity>(); // Lấy repository cho Inventory

        // Lấy tất cả đơn hàng trong khoảng thời gian
        var orders = await orderRepository.FindAsync(
            filter: BuildDateFilter(startDate, endDate),                   // Lọc theo khoảng thời gian
            tracked: false);

        var orderList = orders.ToList();                                   // Chuyển sang List

        // Tính tổng doanh thu từ các đơn đã Completed
        var revenue = orderList
            .Where(o => o.OrderStatus == OrderStatus.Completed)            // Chỉ đơn Completed mới tính doanh thu
            .Sum(o => o.TotalAmount);                                      // Cộng tổng TotalAmount

        // Đếm số đơn đang chờ xử lý (Pending)
        var pendingOrders = orderList
            .Count(o => o.OrderStatus == OrderStatus.Pending);             // Đếm đơn Pending

        // Đếm số variant có tồn kho thấp (quantity <= ngưỡng)
        var allInventories = await inventoryRepository.FindAsync(
            tracked: false);                                               // Lấy toàn bộ inventory
        var lowStockVariants = allInventories
            .Count(i => i.Quantity <= LowStockThreshold);                  // Đếm variant tồn kho <= 10

        return new DashboardResponse
        {
            TotalOrders = orderList.Count,                                 // Tổng số đơn trong khoảng thời gian
            Revenue = revenue,                                             // Tổng doanh thu (chỉ đơn Completed)
            PendingOrders = pendingOrders,                                 // Số đơn đang chờ (Pending)
            LowStockVariants = lowStockVariants                            // Số variant tồn kho thấp
        };
    }

    // Dashboard biểu đồ theo mốc thời gian (1 tuần, 1 tháng, 6 tháng, 1 năm)
    public async Task<DashboardChartResponse> GetDashboardChartAsync(string timeRange, CancellationToken cancellationToken = default)
    {
        var orderRepository = _unitOfWork.Repository<Order>();
        var now = DateTime.UtcNow;
        DateTime startDate;
        var items = new List<DashboardChartItem>();

        switch (timeRange.ToLowerInvariant())
        {
            case "week":
                startDate = now.AddDays(-6).Date; // 7 ngày bao gồm hôm nay
                break;
            case "month":
                startDate = now.AddDays(-29).Date; // 30 ngày bao gồm hôm nay
                break;
            case "6months":
                startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
                break;
            case "year":
                startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
                break;
            default:
                startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
                timeRange = "year";
                break;
        }

        var orders = await orderRepository.FindAsync(
            filter: o => o.CreatedAt >= startDate && o.CreatedAt <= now,
            tracked: false);

        var orderList = orders.ToList();

        if (timeRange == "week")
        {
            // Nhóm theo ngày (7 ngày)
            int days = (now.Date - startDate).Days;
            for (int i = 0; i <= days; i++)
            {
                var date = startDate.AddDays(i);
                var periodOrders = orderList.Where(o => o.CreatedAt.Date == date).ToList();
                items.Add(new DashboardChartItem
                {
                    Period = date.ToString("dd/MM"),
                    TotalOrders = periodOrders.Count,
                    CompletedOrders = periodOrders.Count(o => o.OrderStatus == OrderStatus.Completed),
                    Revenue = periodOrders.Where(o => o.OrderStatus == OrderStatus.Completed).Sum(o => o.TotalAmount)
                });
            }
        }
        else if (timeRange == "month")
        {
            // Nhóm theo tuần trong khoảng 30 ngày
            var weeks = new List<int>();
            for (int i = 0; i <= (now.Date - startDate).Days; i++)
            {
                int w = GetIso8601WeekOfYear(startDate.AddDays(i));
                if (!weeks.Contains(w)) weeks.Add(w);
            }

            foreach (var w in weeks)
            {
                var periodOrders = orderList.Where(o => GetIso8601WeekOfYear(o.CreatedAt) == w).ToList();
                items.Add(new DashboardChartItem
                {
                    Period = $"Tuần {w}",
                    TotalOrders = periodOrders.Count,
                    CompletedOrders = periodOrders.Count(o => o.OrderStatus == OrderStatus.Completed),
                    Revenue = periodOrders.Where(o => o.OrderStatus == OrderStatus.Completed).Sum(o => o.TotalAmount)
                });
            }
        }
        else
        {
            // Nhóm theo tháng (6 tháng hoặc 1 năm)
            int months = timeRange == "6months" ? 6 : 12;
            for (int i = 0; i < months; i++)
            {
                var monthDate = startDate.AddMonths(i);
                var periodOrders = orderList.Where(o => o.CreatedAt.Year == monthDate.Year && o.CreatedAt.Month == monthDate.Month).ToList();
                items.Add(new DashboardChartItem
                {
                    Period = $"Tháng {monthDate.Month}",
                    TotalOrders = periodOrders.Count,
                    CompletedOrders = periodOrders.Count(o => o.OrderStatus == OrderStatus.Completed),
                    Revenue = periodOrders.Where(o => o.OrderStatus == OrderStatus.Completed).Sum(o => o.TotalAmount)
                });
            }
        }

        return new DashboardChartResponse { Items = items };
    }

    // Helper: tạo filter Expression lọc Order theo khoảng thời gian CreatedAt
    private static System.Linq.Expressions.Expression<Func<Order, bool>>? BuildDateFilter(DateTime? startDate, DateTime? endDate)
    {
        if (!startDate.HasValue && !endDate.HasValue)
        {
            return null;                                                   // Không có filter nếu không truyền ngày
        }

        return order =>
            (!startDate.HasValue || order.CreatedAt >= startDate.Value) &&  // Lọc từ ngày nếu có
            (!endDate.HasValue || order.CreatedAt <= endDate.Value);        // Lọc đến ngày nếu có
    }

    // Helper: nhóm danh sách đơn hàng theo khoảng thời gian (day/week/month)
    private static IEnumerable<IGrouping<string, Order>> GroupOrdersByPeriod(List<Order> orders, string groupBy)
    {
        return groupBy switch
        {
            "day" => orders.GroupBy(o => o.CreatedAt.ToString("yyyy-MM-dd")),                                    // Nhóm theo ngày (vd: "2026-04-16")
            "week" => orders.GroupBy(o => $"{o.CreatedAt.Year}-W{GetIso8601WeekOfYear(o.CreatedAt):D2}"),         // Nhóm theo tuần ISO (vd: "2026-W16")
            _ => orders.GroupBy(o => o.CreatedAt.ToString("yyyy-MM"))                                            // Mặc định nhóm theo tháng (vd: "2026-04")
        };
    }

    // Helper: lấy số tuần ISO 8601 từ DateTime
    private static int GetIso8601WeekOfYear(DateTime date)
    {
        var day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date); // Lấy ngày trong tuần
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            date = date.AddDays(3);                                        // Điều chỉnh theo quy tắc ISO 8601
        }
        return System.Globalization.CultureInfo.InvariantCulture.Calendar
            .GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday); // Tính tuần theo ISO 8601
    }
}
