using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Entities;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Quản lý hành khách; seller được thêm/sửa, chỉ điều hành được xóa
public sealed partial class PassengersViewModel : CrudPageViewModel<Passenger>
{
    private readonly IPassengerRepository _passengers;
    private readonly TicketService _tickets;
    private int _editingId;

    public PassengersViewModel(PageServices services, IPassengerRepository passengers, TicketService tickets)
        : base(services)
    {
        _passengers = passengers;
        _tickets = tickets;
        FullName = string.Empty;
        Phone = string.Empty;
        IdCard = string.Empty;
        Email = string.Empty;
    }

    public override bool CanCreate => true;

    public override bool CanEditItems => true;

    // Lịch sử vé của khách đang chọn
    public ObservableCollection<TicketListItem> History { get; } = [];

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [MaxLength(100, ErrorMessage = "Val_TooLong")]
    public partial string FullName { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Val_Phone")]
    public partial string Phone { get; set; }

    // Không bắt buộc; nếu nhập phải đủ 12 số
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [RegularExpression(@"^\s*(\d{12})?\s*$", ErrorMessage = "Val_IdCard")]
    public partial string IdCard { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(100, ErrorMessage = "Val_TooLong")]
    [RegularExpression(@"^\s*([^@\s]+@[^@\s]+\.[^@\s]+)?\s*$", ErrorMessage = "Val_Email")]
    public partial string Email { get; set; }

    protected override string ExportTitleKey => "Passengers_ExportTitle";

    protected override IReadOnlyList<ExportColumn<Passenger>> ExportColumns =>
    [
        new("Passengers_Name", p => p.FullName),
        new("Passengers_Phone", p => p.Phone),
        new("Passengers_IdCard", p => p.IdCard),
        new("Passengers_Email", p => p.Email),
    ];

    protected override Task<IReadOnlyList<Passenger>> LoadItemsAsync() => _passengers.GetAllAsync();

    protected override bool Matches(Passenger item, string text) =>
        Contains(item.FullName, text) || Contains(item.Phone, text) || Contains(item.IdCard, text);

    protected override void OnSelectionChanged(Passenger? item) => _ = RunBusyAsync(() => LoadHistoryAsync(item));

    protected override void FillForm(Passenger? item)
    {
        _editingId = item?.Id ?? 0;
        FullName = item?.FullName ?? string.Empty;
        Phone = item?.Phone ?? string.Empty;
        IdCard = item?.IdCard ?? string.Empty;
        Email = item?.Email ?? string.Empty;
    }

    protected override Task SaveFormAsync(bool isNew)
    {
        var passenger = new Passenger
        {
            Id = _editingId,
            FullName = FullName.Trim(),
            Phone = Phone.Trim(),
            IdCard = NullIfBlank(IdCard),
            Email = NullIfBlank(Email),
        };
        return isNew ? _passengers.AddAsync(passenger) : _passengers.UpdateAsync(passenger);
    }

    protected override Task DeleteItemAsync(Passenger item) => _passengers.DeleteAsync(item.Id);

    protected override string Describe(Passenger item) => $"{item.FullName} ({item.Phone})";

    private async Task LoadHistoryAsync(Passenger? passenger)
    {
        History.Clear();
        if (passenger is null)
        {
            return;
        }

        var tickets = await _tickets.SearchAsync(new TicketSearchCriteria { Keyword = passenger.Phone });
        foreach (var ticket in tickets.Where(t => t.Phone == passenger.Phone))
        {
            History.Add(ticket);
        }
    }
}
