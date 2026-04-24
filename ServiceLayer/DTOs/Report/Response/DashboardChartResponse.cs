namespace ServiceLayer.DTOs.Report.Response;

public class DashboardChartResponse
{
    public List<DashboardChartItem> Items { get; set; } = [];
}

public class DashboardChartItem
{
    public string Period { get; set; } = string.Empty; // Ví dụ: "Tháng 1", "Tháng 2", "Tuần 1", "15/04"
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal Revenue { get; set; }
}
