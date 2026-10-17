using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Entities;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Quản lý tuyến xe
public sealed partial class RoutesViewModel : CrudPageViewModel<Route>
{
    private readonly IRouteRepository _routes;
    private int _editingId;

    public RoutesViewModel(PageServices services, IRouteRepository routes)
        : base(services)
    {
        _routes = routes;
        Name = string.Empty;
        Origin = string.Empty;
        Destination = string.Empty;
    }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [MaxLength(100, ErrorMessage = "Val_TooLong")]
    public partial string Name { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [MaxLength(50, ErrorMessage = "Val_TooLong")]
    public partial string Origin { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [MaxLength(50, ErrorMessage = "Val_TooLong")]
    public partial string Destination { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, 5000, ErrorMessage = "Val_Positive")]
    public partial int? DistanceKm { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, 9999, ErrorMessage = "Val_Positive")]
    public partial int? DurationMin { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(typeof(decimal), "1", "100000000", ErrorMessage = "Val_Positive")]
    public partial decimal BasePrice { get; set; }

    protected override string ExportTitleKey => "Routes_ExportTitle";

    protected override IReadOnlyList<ExportColumn<Route>> ExportColumns =>
    [
        new("Routes_Name", r => r.Name),
        new("Routes_Origin", r => r.Origin),
        new("Routes_Destination", r => r.Destination),
        new("Routes_Distance", r => r.DistanceKm),
        new("Routes_Duration", r => r.DurationMin),
        new("Routes_BasePrice", r => r.BasePrice, "#,##0"),
    ];

    protected override Task<IReadOnlyList<Route>> LoadItemsAsync() => _routes.GetAllAsync();

    protected override bool Matches(Route item, string text) =>
        Contains(item.Name, text) || Contains(item.Origin, text) || Contains(item.Destination, text);

    protected override void FillForm(Route? item)
    {
        _editingId = item?.Id ?? 0;
        Name = item?.Name ?? string.Empty;
        Origin = item?.Origin ?? string.Empty;
        Destination = item?.Destination ?? string.Empty;
        DistanceKm = item?.DistanceKm;
        DurationMin = item?.DurationMin;
        BasePrice = item?.BasePrice ?? 0;
    }

    protected override Task SaveFormAsync(bool isNew)
    {
        var route = new Route
        {
            Id = _editingId,
            Name = Name.Trim(),
            Origin = Origin.Trim(),
            Destination = Destination.Trim(),
            DistanceKm = DistanceKm,
            DurationMin = DurationMin,
            BasePrice = BasePrice,
        };
        return isNew ? _routes.AddAsync(route) : _routes.UpdateAsync(route);
    }

    protected override Task DeleteItemAsync(Route item) => _routes.DeleteAsync(item.Id);

    protected override string Describe(Route item) => item.Name;
}
