namespace Transport.Application.Models;

// Điều kiện lọc chuyến; From/To là khoảng [From, To) theo giờ khởi hành
public sealed record TripSearchCriteria
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public int? RouteId { get; init; }
    public string? Origin { get; init; }
    public string? Destination { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    // Chỉ chuyến còn bán vé (SCHEDULED và chưa khởi hành)
    public bool OnlyOpenForSale { get; init; }
}
