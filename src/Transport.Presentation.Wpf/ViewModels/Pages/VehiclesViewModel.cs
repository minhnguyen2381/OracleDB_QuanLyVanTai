using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Quản lý phương tiện (chỉ điều hành)
public sealed partial class VehiclesViewModel : CrudPageViewModel<Vehicle>
{
    private readonly IVehicleRepository _vehicles;
    private int _editingId;

    public VehiclesViewModel(PageServices services, IVehicleRepository vehicles)
        : base(services)
    {
        _vehicles = vehicles;
        PlateNumber = string.Empty;
        VehicleType = string.Empty;
        SeatCount = 40;
        Status = VehicleStatus.Active;
    }

    public IReadOnlyList<VehicleStatus> Statuses { get; } = Enum.GetValues<VehicleStatus>();

    // Biển số dạng 29B-196.88 hoặc 29B-12345 (không phân biệt hoa thường, lưu chữ hoa)
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [RegularExpression(@"(?i)^\d{2}[A-Z]{1,2}-(\d{3}\.\d{2}|\d{4,5})$", ErrorMessage = "Val_Plate")]
    public partial string PlateNumber { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [MaxLength(50, ErrorMessage = "Val_TooLong")]
    public partial string VehicleType { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, 60, ErrorMessage = "Val_SeatRange")]
    public partial int SeatCount { get; set; }

    [ObservableProperty]
    public partial VehicleStatus Status { get; set; }

    protected override string ExportTitleKey => "Vehicles_ExportTitle";

    protected override IReadOnlyList<ExportColumn<Vehicle>> ExportColumns =>
    [
        new("Vehicles_Plate", v => v.PlateNumber),
        new("Vehicles_Type", v => v.VehicleType),
        new("Vehicles_Seats", v => v.SeatCount),
        new("Vehicles_Status", v => Loc[$"VehicleStatus_{v.Status}"]),
    ];

    protected override Task<IReadOnlyList<Vehicle>> LoadItemsAsync() => _vehicles.GetAllAsync();

    protected override bool Matches(Vehicle item, string text) =>
        Contains(item.PlateNumber, text) || Contains(item.VehicleType, text);

    protected override void FillForm(Vehicle? item)
    {
        _editingId = item?.Id ?? 0;
        PlateNumber = item?.PlateNumber ?? string.Empty;
        VehicleType = item?.VehicleType ?? string.Empty;
        SeatCount = item?.SeatCount ?? 40;
        Status = item?.Status ?? VehicleStatus.Active;
    }

    protected override Task SaveFormAsync(bool isNew)
    {
        var vehicle = new Vehicle
        {
            Id = _editingId,
            PlateNumber = PlateNumber.Trim().ToUpperInvariant(),
            VehicleType = VehicleType.Trim(),
            SeatCount = SeatCount,
            Status = Status,
        };
        return isNew ? _vehicles.AddAsync(vehicle) : _vehicles.UpdateAsync(vehicle);
    }

    protected override Task DeleteItemAsync(Vehicle item) => _vehicles.DeleteAsync(item.Id);

    protected override string Describe(Vehicle item) => item.PlateNumber;
}
