using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Quản lý tài xế (chỉ điều hành); cảnh báo bằng lái sắp/đã hết hạn
public sealed partial class DriversViewModel : CrudPageViewModel<Driver>
{
    private readonly IDriverRepository _drivers;
    private readonly DateTime _today;
    private int _editingId;

    public DriversViewModel(PageServices services, IDriverRepository drivers, TimeProvider time)
        : base(services)
    {
        _drivers = drivers;
        _today = time.GetLocalNow().Date;
        FullName = string.Empty;
        IdCard = string.Empty;
        Phone = string.Empty;
        LicenseNo = string.Empty;
        LicenseClass = LicenseClass.D;
        LicenseExpiry = _today.AddYears(1);
        Status = DriverStatus.Active;
    }

    public IReadOnlyList<LicenseClass> LicenseClasses { get; } = Enum.GetValues<LicenseClass>();

    public IReadOnlyList<DriverStatus> Statuses { get; } = Enum.GetValues<DriverStatus>();

    // null khi mọi bằng lái còn hạn trên 30 ngày
    [ObservableProperty]
    public partial string? ExpiryWarning { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [MaxLength(100, ErrorMessage = "Val_TooLong")]
    public partial string FullName { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [RegularExpression(@"^\d{12}$", ErrorMessage = "Val_IdCard")]
    public partial string IdCard { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Val_Phone")]
    public partial string Phone { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [MaxLength(20, ErrorMessage = "Val_TooLong")]
    public partial string LicenseNo { get; set; }

    [ObservableProperty]
    public partial LicenseClass LicenseClass { get; set; }

    [ObservableProperty]
    public partial DateTime LicenseExpiry { get; set; }

    [ObservableProperty]
    public partial DriverStatus Status { get; set; }

    protected override string ExportTitleKey => "Drivers_ExportTitle";

    protected override IReadOnlyList<ExportColumn<Driver>> ExportColumns =>
    [
        new("Drivers_Name", d => d.FullName),
        new("Drivers_IdCard", d => d.IdCard),
        new("Drivers_Phone", d => d.Phone),
        new("Drivers_LicenseNo", d => d.LicenseNo),
        new("Drivers_LicenseClass", d => d.LicenseClass.ToString()),
        new("Drivers_LicenseExpiry", d => d.LicenseExpiry, "dd/MM/yyyy"),
        new("Drivers_Status", d => Loc[$"DriverStatus_{d.Status}"]),
    ];

    protected override Task<IReadOnlyList<Driver>> LoadItemsAsync() => _drivers.GetAllAsync();

    protected override void OnItemsLoaded(IReadOnlyList<Driver> items)
    {
        var count = items.Count(d => d.GetLicenseState(_today) != LicenseState.Valid);
        ExpiryWarning = count == 0 ? null : Loc.Format("Drivers_ExpiryWarning", count);
    }

    protected override bool Matches(Driver item, string text) =>
        Contains(item.FullName, text) || Contains(item.Phone, text) || Contains(item.LicenseNo, text) || Contains(item.IdCard, text);

    protected override void FillForm(Driver? item)
    {
        _editingId = item?.Id ?? 0;
        FullName = item?.FullName ?? string.Empty;
        IdCard = item?.IdCard ?? string.Empty;
        Phone = item?.Phone ?? string.Empty;
        LicenseNo = item?.LicenseNo ?? string.Empty;
        LicenseClass = item?.LicenseClass ?? LicenseClass.D;
        LicenseExpiry = item?.LicenseExpiry ?? _today.AddYears(1);
        Status = item?.Status ?? DriverStatus.Active;
    }

    protected override Task SaveFormAsync(bool isNew)
    {
        var driver = new Driver
        {
            Id = _editingId,
            FullName = FullName.Trim(),
            IdCard = IdCard.Trim(),
            Phone = Phone.Trim(),
            LicenseNo = LicenseNo.Trim(),
            LicenseClass = LicenseClass,
            LicenseExpiry = LicenseExpiry.Date,
            Status = Status,
        };
        return isNew ? _drivers.AddAsync(driver) : _drivers.UpdateAsync(driver);
    }

    protected override Task DeleteItemAsync(Driver item) => _drivers.DeleteAsync(item.Id);

    protected override string Describe(Driver item) => item.FullName;
}
