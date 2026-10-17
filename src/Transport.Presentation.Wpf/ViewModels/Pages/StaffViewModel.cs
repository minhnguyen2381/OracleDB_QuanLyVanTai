using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Quản lý nhân viên = user Oracle (PKG_STAFF). Không xóa, chỉ khóa/mở khóa.
public sealed partial class StaffViewModel : CrudPageViewModel<StaffMember>
{
    private readonly StaffService _staff;
    private StaffRole _originalRole;

    public StaffViewModel(PageServices services, StaffService staff)
        : base(services)
    {
        _staff = staff;
        Username = string.Empty;
        Password = string.Empty;
        FullName = string.Empty;
        Phone = string.Empty;
        Role = StaffRole.TicketSeller;
    }

    public override bool CanDeleteItems => false;

    public IReadOnlyList<StaffRole> Roles { get; } = Enum.GetValues<StaffRole>();

    // Luật chi tiết (3–30 ký tự, bắt đầu bằng chữ) do StaffService/PKG_STAFF kiểm tra
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    public partial string Username { get; set; }

    // Chỉ dùng khi tạo mới; StaffService kiểm tra độ dài và ký tự
    [ObservableProperty]
    public partial string Password { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [MaxLength(100, ErrorMessage = "Val_TooLong")]
    public partial string FullName { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [RegularExpression(@"^\s*(0\d{9})?\s*$", ErrorMessage = "Val_Phone")]
    public partial string Phone { get; set; }

    [ObservableProperty]
    public partial StaffRole Role { get; set; }

    protected override string ExportTitleKey => "Staff_ExportTitle";

    protected override IReadOnlyList<ExportColumn<StaffMember>> ExportColumns =>
    [
        new("Staff_Username", s => s.Username),
        new("Staff_FullName", s => s.FullName),
        new("Staff_Phone", s => s.Phone),
        new("Staff_Role", s => Loc[$"StaffRole_{s.Role}"]),
        new("Staff_Status", s => Loc[$"StaffStatus_{s.Status}"]),
        new("Staff_CreatedAt", s => s.CreatedAt, "dd/MM/yyyy HH:mm"),
        new("Staff_CreatedBy", s => s.CreatedBy),
    ];

    protected override Task<IReadOnlyList<StaffMember>> LoadItemsAsync() => _staff.GetAllAsync();

    protected override bool Matches(StaffMember item, string text) =>
        Contains(item.Username, text) || Contains(item.FullName, text) || Contains(item.Phone, text);

    protected override void OnSelectionChanged(StaffMember? item)
    {
        ResetPasswordCommand.NotifyCanExecuteChanged();
        ToggleLockCommand.NotifyCanExecuteChanged();
    }

    protected override void FillForm(StaffMember? item)
    {
        Username = item?.Username ?? string.Empty;
        Password = string.Empty;
        FullName = item?.FullName ?? string.Empty;
        Phone = item?.Phone ?? string.Empty;
        Role = item?.Role ?? StaffRole.TicketSeller;
        _originalRole = Role;
    }

    protected override async Task SaveFormAsync(bool isNew)
    {
        if (isNew)
        {
            await _staff.CreateAsync(new NewStaffRequest(Username.Trim(), Password, FullName, Phone, Role));
            return;
        }

        await _staff.UpdateInfoAsync(Username, FullName, Phone);
        if (Role != _originalRole)
        {
            await _staff.ChangeRoleAsync(Username, Role);
        }
    }

    protected override Task DeleteItemAsync(StaffMember item) => throw new NotSupportedException();

    protected override string Describe(StaffMember item) => item.Username;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private Task ResetPasswordAsync() => RunBusyAsync(async () =>
    {
        var username = SelectedItem!.Username;
        if (Dialogs.PromptPassword(Loc.Format("Staff_ResetTitle", username)) is not { } password)
        {
            return;
        }

        await _staff.ResetPasswordAsync(username, password);
        Notify.ShowSuccess(Loc["Staff_PasswordReset"]);
    });

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private Task ToggleLockAsync() => RunBusyAsync(async () =>
    {
        var member = SelectedItem!;
        var lockIt = member.Status == StaffStatus.Active;
        if (!Dialogs.Confirm(Loc.Format(lockIt ? "Staff_ConfirmLock" : "Staff_ConfirmUnlock", member.Username)))
        {
            return;
        }

        await _staff.SetLockedAsync(member.Username, lockIt);
        Notify.ShowSuccess(Loc[lockIt ? "Staff_Locked" : "Staff_Unlocked"]);
        await ReloadAsync();
    });

    private bool HasSelection() => SelectedItem is not null;
}
