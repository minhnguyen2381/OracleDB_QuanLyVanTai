# Plan 3 – Application Screens Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build all 11 screens of the WPF app on top of the Plan 2 shell: Dashboard, Sell tickets (seat map + price filter + payment + printing), Tickets, Passengers, Routes, Vehicles, Drivers, Schedules, Statistics, Staff and Backup – each with Excel export.

**Architecture:** Each screen = one ViewModel in `ViewModels/Pages` (MVVM Toolkit, tested with fakes) + one `UserControl` in `Views/Pages` + one `DataTemplate` line in `Views/PageTemplates.xaml` + one `services.AddPage<...>()` line in `PresentationServices.AddPages`. Catalog screens share `CrudPageViewModel<TEntity>` and the `CrudToolbar` / `FormHeader` / `FormButtons` user controls.

**Tech Stack:** Same as Plan 2 (.NET 10 WPF, CommunityToolkit.Mvvm 8.4.2, xunit.v3).

**Spec:** `docs/superpowers/specs/2026-10-17-transport-management-design.md` §12.3–12.6. **Prerequisite:** Plans 1 and 2 completed.

## Global Constraints

- Everything in Plan 2's Global Constraints applies (English identifiers, Vietnamese comments, text only from `.resx`, no initializers on `[ObservableProperty]` partial properties, no `System.Windows` in ViewModels, `System.Windows.Application` written fully qualified, warnings are errors).
- Every new resource key is added to **both** `Resources/Strings.resx` (English) and `Resources/Strings.vi.resx` (Vietnamese); the `<data>` lines go just before `</root>`. `ResourceParityTests` must stay green.
- Validation attributes use a resource key as `ErrorMessage` (e.g. `ErrorMessage = "Val_Required"`); the TextBox style from Plan 2 translates it in the tooltip.
- Page registration: inside `AddPages` in `src/Transport.Presentation.Wpf/PresentationServices.cs`, add the line given by the task **before** `return services;`. Page template: inside `src/Transport.Presentation.Wpf/Views/PageTemplates.xaml`, add the `DataTemplate` given by the task; the dictionary root must declare
  `xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"` and `xmlns:pages="clr-namespace:Transport.Presentation.Wpf.Views.Pages"` (Task 1 adds them).
- Page code-behind contains only `InitializeComponent()`:

```csharp
using System.Windows.Controls;

namespace Transport.Presentation.Wpf.Views.Pages;

public partial class RoutesPage : UserControl
{
    public RoutesPage() => InitializeComponent();
}
```
(Each page task creates the same file with its own class name.)

- Keyboard shortcuts use the `Shortcut.Gesture` attached property (Task 1): `Ctrl+F` search box, `Ctrl+N` new, `Ctrl+S` save, `Ctrl+E` export, `Ctrl+P` print, `F5` refresh.
- Menu order / icon (Segoe Fluent Icons code point) / operator-only:

| Page | Title key | Glyph | Order | Operator only |
|---|---|---|---|---|
| Dashboard | `Menu_Dashboard` | `` | 10 | no |
| Sell tickets | `Menu_SellTicket` | `` | 20 | no |
| Tickets | `Menu_Tickets` | `` | 30 | no |
| Passengers | `Menu_Passengers` | `` | 40 | no |
| Routes | `Menu_Routes` | `` | 50 | no |
| Vehicles | `Menu_Vehicles` | `` | 60 | yes |
| Drivers | `Menu_Drivers` | `` | 70 | yes |
| Schedules | `Menu_Schedules` | `` | 80 | no |
| Statistics | `Menu_Statistics` | `` | 90 | no |
| Staff | `Menu_Staff` | `` | 100 | yes |
| Backup | `Menu_Backup` | `` | 110 | yes |

- Commands: run `dotnet test --project tests/Transport.UnitTests` after each task; build must have 0 warnings.

---

### Task 1: CRUD base, shared page controls, shortcuts and the Routes screen

**Files:**
- Create: `src/Transport.Presentation.Wpf/ViewModels/CrudPageViewModel.cs`, `ViewModels/FilterOption.cs`, `ViewModels/ChartBar.cs`
- Create: `src/Transport.Presentation.Wpf/Controls/Shortcut.cs`
- Create: `src/Transport.Presentation.Wpf/Views/Controls/CrudToolbar.xaml(.cs)`, `FormHeader.xaml(.cs)`, `FormButtons.xaml(.cs)`
- Create: `src/Transport.Presentation.Wpf/ViewModels/Pages/RoutesViewModel.cs`, `Views/Pages/RoutesPage.xaml(.cs)`
- Modify: `Themes/Controls.xaml` (add `FormCardStyle`, `ListGridStyle`, `BarChartStyle`), `Views/PageTemplates.xaml`, `PresentationServices.cs`, both `.resx`
- Test: `tests/Transport.UnitTests/Fakes/FakeCrudRepository.cs`, `TestData.cs`; `tests/Transport.UnitTests/Presentation/RoutesViewModelTests.cs`, `ChartBarTests.cs`

**Interfaces:**
- Produces `CrudPageViewModel<TEntity>` (namespace `Transport.Presentation.Wpf.ViewModels`):
  - public: `Items`, `SelectedItem`, `SearchText`, `IsFormOpen`, `IsNewItem`, virtual `CanCreate`/`CanEditItems`/`CanDeleteItems` (default `IsOperator`), commands `RefreshCommand`, `NewCommand`, `EditCommand`, `CancelFormCommand`, `SaveCommand`, `DeleteCommand`, `ExportToExcelCommand`
  - protected abstract: `ExportTitleKey`, `ExportColumns`, `LoadItemsAsync()`, `Matches(item, text)`, `FillForm(item?)`, `SaveFormAsync(bool isNew)`, `DeleteItemAsync(item)`, `Describe(item)`
  - protected virtual hooks: `CanEditItem(item)`, `CanDeleteItem(item)`, `OnSelectionChanged(item?)`, `OnItemsLoaded(items)`; helpers `ReloadAsync()`, `Contains(source, text)`, `NullIfBlank(value)`
- Produces `FilterOption<T>(T Value, string Label)`; `ChartBar(string Label, decimal Value, double Height)` with `static IReadOnlyList<ChartBar> Build(IEnumerable<(string Label, decimal Value)> points, double maxHeight = 150)`.
- Produces `Shortcut.Gesture` attached property (namespace `Transport.Presentation.Wpf.Controls`).
- Produces test helpers `FakeCrudRepository<T>` (`Items`, `Added`, `Updated`, `Deleted`, `GetAllCount`), `FakeRouteRepository`, `FakeVehicleRepository`, `FakeDriverRepository`, `FakePassengerRepository`, `TestData` builders.

- [ ] **Step 1: Write the test helpers**

`tests/Transport.UnitTests/Fakes/FakeCrudRepository.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Domain.Entities;

namespace Transport.UnitTests.Fakes;

internal class FakeCrudRepository<T>(Func<T, int> getId) : ICrudRepository<T>
{
    public List<T> Items { get; } = [];
    public List<T> Added { get; } = [];
    public List<T> Updated { get; } = [];
    public List<int> Deleted { get; } = [];
    public int GetAllCount { get; private set; }

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        GetAllCount++;
        return Task.FromResult<IReadOnlyList<T>>(Items.ToList());
    }

    public Task AddAsync(T entity, CancellationToken ct = default)
    {
        Added.Add(entity);
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        Updated.Add(entity);
        var index = Items.FindIndex(x => getId(x) == getId(entity));
        if (index >= 0)
        {
            Items[index] = entity;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Deleted.Add(id);
        Items.RemoveAll(x => getId(x) == id);
        return Task.CompletedTask;
    }
}

internal sealed class FakeRouteRepository() : FakeCrudRepository<Route>(r => r.Id), IRouteRepository
{
}

internal sealed class FakeVehicleRepository() : FakeCrudRepository<Vehicle>(v => v.Id), IVehicleRepository
{
}

internal sealed class FakeDriverRepository() : FakeCrudRepository<Driver>(d => d.Id), IDriverRepository
{
}

internal sealed class FakePassengerRepository() : FakeCrudRepository<Passenger>(p => p.Id), IPassengerRepository
{
    public Task<Passenger?> FindByPhoneAsync(string phone, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(p => p.Phone == phone));
}
```

`tests/Transport.UnitTests/Fakes/TestData.cs`:
```csharp
using Transport.Application.Models;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.UnitTests.Fakes;

// Dữ liệu mẫu cho test
internal static class TestData
{
    public static Route Route(int id, string name = "Hà Nội - Lào Cai", decimal price = 280000, int? durationMin = 330,
        string origin = "Hà Nội", string destination = "Lào Cai") => new()
    {
        Id = id,
        Name = name,
        Origin = origin,
        Destination = destination,
        DistanceKm = 300,
        DurationMin = durationMin,
        BasePrice = price,
    };

    public static Vehicle Vehicle(int id, string plate = "29B-196.88", int seats = 40,
        VehicleStatus status = VehicleStatus.Active) => new()
    {
        Id = id,
        PlateNumber = plate,
        VehicleType = "Giường nằm 40 chỗ",
        SeatCount = seats,
        Status = status,
    };

    public static Driver Driver(int id, string name = "Nguyễn Văn An", DateTime? expiry = null,
        DriverStatus status = DriverStatus.Active) => new()
    {
        Id = id,
        FullName = name,
        IdCard = $"0010850{id:00000}",
        Phone = $"09123456{id:00}",
        LicenseNo = $"7901234567{id:00}",
        LicenseClass = LicenseClass.D,
        LicenseExpiry = expiry ?? new DateTime(2030, 1, 1),
        Status = status,
    };

    public static Passenger Passenger(int id, string name = "Nguyễn Thị Hoa", string phone = "0987000001") => new()
    {
        Id = id,
        FullName = name,
        Phone = phone,
        IdCard = null,
        Email = null,
    };

    public static StaffMember Staff(string username, StaffRole role = StaffRole.TicketSeller,
        StaffStatus status = StaffStatus.Active) => new()
    {
        Username = username,
        FullName = "Trần Thị Bán",
        Phone = "0912000002",
        Role = role,
        Status = status,
        CreatedAt = new DateTime(2026, 10, 1),
        CreatedBy = "TRANSPORT",
    };

    public static TicketListItem Ticket(long id, TicketStatus status = TicketStatus.Booked, decimal price = 280000,
        string phone = "0987000001") => new()
    {
        TicketId = id,
        ScheduleId = 1,
        RouteName = "Hà Nội - Lào Cai",
        DepartureTime = new DateTime(2026, 10, 18, 7, 0, 0),
        PlateNumber = "29B-196.88",
        SeatNo = 5,
        PassengerName = "Nguyễn Thị Hoa",
        Phone = phone,
        Price = price,
        Status = status,
        BookedAt = new DateTime(2026, 10, 16, 9, 0, 0),
        SoldBy = "SELLER01",
    };

    public static TicketPrintModel PrintModel(long id) => new()
    {
        TicketId = id,
        RouteName = "Hà Nội - Lào Cai",
        Origin = "Hà Nội",
        Destination = "Lào Cai",
        DepartureTime = new DateTime(2026, 10, 18, 7, 0, 0),
        ArrivalTime = new DateTime(2026, 10, 18, 12, 30, 0),
        PlateNumber = "29B-196.88",
        VehicleType = "Giường nằm 40 chỗ",
        SeatNo = 5,
        PassengerName = "Nguyễn Thị Hoa",
        Phone = "0987000001",
        Price = 280000,
        Status = TicketStatus.Paid,
        SoldBy = "SELLER01",
        BookedAt = new DateTime(2026, 10, 16, 9, 0, 0),
    };
}
```

- [ ] **Step 2: Write the failing tests**

`tests/Transport.UnitTests/Presentation/ChartBarTests.cs`:
```csharp
using Transport.Presentation.Wpf.ViewModels;

namespace Transport.UnitTests.Presentation;

public class ChartBarTests
{
    [Fact]
    public void Build_ScalesHeightsToTheLargestValue()
    {
        var bars = ChartBar.Build([("A", 50m), ("B", 100m), ("C", 0m)], maxHeight: 200);

        Assert.Equal(new[] { 100d, 200d, 0d }, bars.Select(b => b.Height));
        Assert.Equal("B", bars[1].Label);
    }

    [Fact]
    public void Build_AllZero_GivesZeroHeights()
    {
        Assert.All(ChartBar.Build([("A", 0m)]), b => Assert.Equal(0, b.Height));
    }
}
```

`tests/Transport.UnitTests/Presentation/RoutesViewModelTests.cs`:
```csharp
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class RoutesViewModelTests
{
    private readonly PageTestContext _context = new();
    private readonly FakeRouteRepository _routes = new();

    public RoutesViewModelTests()
    {
        _routes.Items.Add(TestData.Route(1, "Hà Nội - Lào Cai"));
        _routes.Items.Add(TestData.Route(2, "Hà Nội - Hải Phòng", 120000, 150, destination: "Hải Phòng"));
    }

    private RoutesViewModel Create(PageTestContext? context = null) => new((context ?? _context).Services, _routes);

    [Fact]
    public async Task LoadAsync_ListsItems_AndSearchFilters()
    {
        var vm = Create();
        await vm.LoadAsync();
        Assert.Equal(2, vm.Items.Count);

        vm.SearchText = "lào";

        Assert.Equal("Hà Nội - Lào Cai", Assert.Single(vm.Items).Name);
    }

    [Fact]
    public async Task New_Save_AddsTrimmedRoute_AndClosesForm()
    {
        var vm = Create();
        await vm.LoadAsync();

        vm.NewCommand.Execute(null);
        Assert.True(vm.IsFormOpen);
        Assert.True(vm.IsNewItem);
        vm.Name = " Hà Nội - Ninh Bình ";
        vm.Origin = "Hà Nội";
        vm.Destination = "Ninh Bình";
        vm.DistanceKm = 95;
        vm.DurationMin = 120;
        vm.BasePrice = 100000;
        await vm.SaveCommand.ExecuteAsync(null);

        var added = Assert.Single(_routes.Added);
        Assert.Equal("Hà Nội - Ninh Bình", added.Name);
        Assert.Equal(0, added.Id);
        Assert.False(vm.IsFormOpen);
        Assert.Equal(3, vm.Items.Count);
        Assert.Equal(new[] { "Msg_Saved" }, _context.Notifications.Successes);
    }

    [Fact]
    public async Task Save_WithMissingFields_WarnsAndDoesNotSave()
    {
        var vm = Create();
        vm.NewCommand.Execute(null);

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(_routes.Added);
        Assert.True(vm.IsFormOpen);
        Assert.Equal(new[] { "Val_Required" }, _context.Notifications.Warnings);
    }

    [Fact]
    public async Task Edit_FillsForm_AndSaveUpdates()
    {
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[0];

        vm.EditCommand.Execute(null);
        Assert.Equal("Hà Nội - Lào Cai", vm.Name);
        Assert.False(vm.IsNewItem);
        vm.BasePrice = 300000;
        await vm.SaveCommand.ExecuteAsync(null);

        var updated = Assert.Single(_routes.Updated);
        Assert.Equal(1, updated.Id);
        Assert.Equal(300000, updated.BasePrice);
    }

    [Fact]
    public async Task Delete_AsksForConfirmation()
    {
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[1];

        _context.Dialogs.ConfirmResult = false;
        await vm.DeleteCommand.ExecuteAsync(null);
        Assert.Empty(_routes.Deleted);

        _context.Dialogs.ConfirmResult = true;
        await vm.DeleteCommand.ExecuteAsync(null);
        Assert.Equal(new[] { 2 }, _routes.Deleted);
        Assert.Equal("Msg_ConfirmDelete|Hà Nội - Hải Phòng", _context.Dialogs.ConfirmMessages[0]);
    }

    [Fact]
    public async Task Seller_IsReadOnly()
    {
        var vm = Create(PageTestContext.ForSeller());
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[0];

        vm.NewCommand.Execute(null);

        Assert.False(vm.CanCreate);
        Assert.False(vm.IsFormOpen);
        Assert.False(vm.EditCommand.CanExecute(null));
        Assert.False(vm.DeleteCommand.CanExecute(null));
    }

    [Fact]
    public async Task Export_UsesRouteColumns()
    {
        var vm = Create();
        await vm.LoadAsync();

        await vm.ExportToExcelCommand.ExecuteAsync(null);

        Assert.Equal("Routes_ExportTitle", _context.Exporter.Title);
        Assert.Equal(6, _context.Exporter.Headers.Count);
        Assert.Equal(2, _context.Exporter.RowCount);
    }
}
```

- [ ] **Step 3: Run to verify they fail**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build errors for `ChartBar` and `RoutesViewModel`.

- [ ] **Step 4: Write the shared view-model types**

`ViewModels/FilterOption.cs`:
```csharp
namespace Transport.Presentation.Wpf.ViewModels;

// Một lựa chọn trong combobox lọc; Value = null nghĩa là "Tất cả"
public sealed record FilterOption<T>(T Value, string Label);
```

`ViewModels/ChartBar.cs`:
```csharp
namespace Transport.Presentation.Wpf.ViewModels;

// Một cột của biểu đồ cột vẽ bằng ItemsControl (BarChartStyle)
public sealed record ChartBar(string Label, decimal Value, double Height)
{
    public static IReadOnlyList<ChartBar> Build(IEnumerable<(string Label, decimal Value)> points, double maxHeight = 150)
    {
        var list = points.ToList();
        var max = list.Count == 0 ? 0 : list.Max(p => p.Value);
        return [.. list.Select(p => new ChartBar(p.Label, p.Value, max <= 0 ? 0 : (double)(p.Value / max) * maxHeight))];
    }
}
```

`ViewModels/CrudPageViewModel.cs`:
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Models;

namespace Transport.Presentation.Wpf.ViewModels;

// Lớp nền cho trang danh mục: danh sách + tìm kiếm + form thêm/sửa + xóa + xuất Excel
public abstract partial class CrudPageViewModel<TEntity> : PageViewModel
    where TEntity : class
{
    private List<TEntity> _all = [];

    protected CrudPageViewModel(PageServices services)
        : base(services) => SearchText = string.Empty;

    public ObservableCollection<TEntity> Items { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    public partial TEntity? SelectedItem { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; }

    [ObservableProperty]
    public partial bool IsFormOpen { get; set; }

    // true = form đang thêm mới, false = đang sửa
    [ObservableProperty]
    public partial bool IsNewItem { get; set; }

    // Mặc định chỉ điều hành được thêm/sửa/xóa (seller chỉ xem)
    public virtual bool CanCreate => IsOperator;

    public virtual bool CanEditItems => IsOperator;

    public virtual bool CanDeleteItems => IsOperator;

    protected abstract string ExportTitleKey { get; }

    protected abstract IReadOnlyList<ExportColumn<TEntity>> ExportColumns { get; }

    public override Task LoadAsync() => RunBusyAsync(ReloadAsync);

    protected abstract Task<IReadOnlyList<TEntity>> LoadItemsAsync();

    protected abstract bool Matches(TEntity item, string text);

    // item = null: form thêm mới
    protected abstract void FillForm(TEntity? item);

    protected abstract Task SaveFormAsync(bool isNew);

    protected abstract Task DeleteItemAsync(TEntity item);

    // Tên hiển thị trong câu hỏi xác nhận xóa
    protected abstract string Describe(TEntity item);

    protected virtual bool CanEditItem(TEntity item) => true;

    protected virtual bool CanDeleteItem(TEntity item) => true;

    protected virtual void OnSelectionChanged(TEntity? item)
    {
    }

    protected virtual void OnItemsLoaded(IReadOnlyList<TEntity> items)
    {
    }

    protected static bool Contains(string? source, string text) =>
        source?.Contains(text, StringComparison.CurrentCultureIgnoreCase) == true;

    protected static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    protected async Task ReloadAsync()
    {
        _all = [.. await LoadItemsAsync()];
        ApplyFilter();
        OnItemsLoaded(_all);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedItemChanged(TEntity? value) => OnSelectionChanged(value);

    private void ApplyFilter()
    {
        var text = SearchText.Trim();
        Items.Clear();
        foreach (var item in _all.Where(i => text.Length == 0 || Matches(i, text)))
        {
            Items.Add(item);
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => RunBusyAsync(ReloadAsync);

    [RelayCommand]
    private void New()
    {
        if (CanCreate)
        {
            OpenForm(null);
        }
    }

    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private void Edit()
    {
        if (SelectedItem is { } item && CanEditSelected())
        {
            OpenForm(item);
        }
    }

    [RelayCommand]
    private void CancelForm()
    {
        IsFormOpen = false;
        ClearErrors();
    }

    [RelayCommand]
    private Task SaveAsync() => RunBusyAsync(async () =>
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            Notify.ShowWarning(FirstValidationError());
            return;
        }

        await SaveFormAsync(IsNewItem);
        IsFormOpen = false;
        Notify.ShowSuccess(Loc["Msg_Saved"]);
        await ReloadAsync();
    });

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private Task DeleteAsync() => RunBusyAsync(async () =>
    {
        if (SelectedItem is not { } item || !Dialogs.Confirm(Loc.Format("Msg_ConfirmDelete", Describe(item))))
        {
            return;
        }

        await DeleteItemAsync(item);
        Notify.ShowSuccess(Loc["Msg_Deleted"]);
        await ReloadAsync();
    });

    [RelayCommand]
    private Task ExportToExcelAsync() => ExportAsync(Items.ToList(), ExportColumns, ExportTitleKey);

    private void OpenForm(TEntity? item)
    {
        IsNewItem = item is null;
        FillForm(item);
        // Gán giá trị form xong mới xóa lỗi, để form mới không đỏ sẵn
        ClearErrors();
        IsFormOpen = true;
    }

    private bool CanEditSelected() => CanEditItems && SelectedItem is { } item && CanEditItem(item);

    private bool CanDeleteSelected() => CanDeleteItems && SelectedItem is { } item && CanDeleteItem(item);
}
```

- [ ] **Step 5: Write `ViewModels/Pages/RoutesViewModel.cs`**

```csharp
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
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 7: Write `Controls/Shortcut.cs`**

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Transport.Presentation.Wpf.Controls;

// Phím tắt cấp cửa sổ cho một nút (chạy Command của nút) hoặc một ô nhập (đưa con trỏ vào ô).
// Dùng: <Button c:Shortcut.Gesture="Ctrl+E" Command="{Binding ExportToExcelCommand}" />
public static class Shortcut
{
    public static readonly DependencyProperty GestureProperty = DependencyProperty.RegisterAttached(
        "Gesture", typeof(string), typeof(Shortcut), new PropertyMetadata(null, OnGestureChanged));

    // Lưu KeyBinding và cửa sổ đã gắn để gỡ đúng chỗ khi trang bị đóng
    private static readonly DependencyProperty KeyBindingProperty = DependencyProperty.RegisterAttached(
        "KeyBinding", typeof(KeyBinding), typeof(Shortcut));

    private static readonly DependencyProperty OwnerWindowProperty = DependencyProperty.RegisterAttached(
        "OwnerWindow", typeof(Window), typeof(Shortcut));

    public static string? GetGesture(DependencyObject element) => (string?)element.GetValue(GestureProperty);

    public static void SetGesture(DependencyObject element, string? value) => element.SetValue(GestureProperty, value);

    private static void OnGestureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        element.Loaded -= OnLoaded;
        element.Unloaded -= OnUnloaded;
        if (e.NewValue is string)
        {
            element.Loaded += OnLoaded;
            element.Unloaded += OnUnloaded;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        if (element.GetValue(KeyBindingProperty) is not null || Window.GetWindow(element) is not { } window
            || GetGesture(element) is not { } text)
        {
            return;
        }

        var gesture = (KeyGesture)new KeyGestureConverter().ConvertFromInvariantString(text)!;
        var binding = new KeyBinding(new RelayCommand(() => Activate(element)), gesture);
        window.InputBindings.Add(binding);
        element.SetValue(KeyBindingProperty, binding);
        element.SetValue(OwnerWindowProperty, window);
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        if (element.GetValue(KeyBindingProperty) is KeyBinding binding && element.GetValue(OwnerWindowProperty) is Window window)
        {
            window.InputBindings.Remove(binding);
        }

        element.ClearValue(KeyBindingProperty);
        element.ClearValue(OwnerWindowProperty);
    }

    private static void Activate(FrameworkElement element)
    {
        if (!element.IsVisible || !element.IsEnabled)
        {
            return;
        }

        if (element is ButtonBase { Command: { } command } button)
        {
            if (command.CanExecute(button.CommandParameter))
            {
                command.Execute(button.CommandParameter);
            }
        }
        else
        {
            element.Focus();
            (element as TextBox)?.SelectAll();
        }
    }
}
```

- [ ] **Step 8: Add shared styles to `Themes/Controls.xaml`** (insert before `</ResourceDictionary>`)

```xml
    <!-- Bảng danh sách chỉ đọc -->
    <Style x:Key="ListGridStyle" TargetType="DataGrid" BasedOn="{StaticResource {x:Type DataGrid}}">
        <Setter Property="AutoGenerateColumns" Value="False" />
        <Setter Property="IsReadOnly" Value="True" />
        <Setter Property="SelectionMode" Value="Single" />
        <Setter Property="SelectionUnit" Value="FullRow" />
        <Setter Property="HeadersVisibility" Value="Column" />
        <Setter Property="CanUserAddRows" Value="False" />
        <Setter Property="CanUserDeleteRows" Value="False" />
        <Setter Property="GridLinesVisibility" Value="Horizontal" />
    </Style>

    <!-- Thẻ form thêm/sửa bên phải trang danh mục; DataContext = CrudPageViewModel -->
    <Style x:Key="FormCardStyle" TargetType="Border" BasedOn="{StaticResource CardStyle}">
        <Setter Property="Width" Value="360" />
        <Setter Property="Margin" Value="16,0,0,0" />
        <Setter Property="Visibility" Value="{Binding IsFormOpen, Converter={StaticResource BoolToVisibility}}" />
    </Style>

    <!-- Biểu đồ cột; ItemsSource = danh sách ChartBar -->
    <Style x:Key="BarChartStyle" TargetType="ItemsControl">
        <Setter Property="Height" Value="210" />
        <Setter Property="ItemsPanel">
            <Setter.Value>
                <ItemsPanelTemplate>
                    <StackPanel Orientation="Horizontal" />
                </ItemsPanelTemplate>
            </Setter.Value>
        </Setter>
        <Setter Property="ItemTemplate">
            <Setter.Value>
                <DataTemplate>
                    <StackPanel Width="96" Margin="4,0" VerticalAlignment="Bottom">
                        <TextBlock Text="{Binding Value, StringFormat={}{0:N0}}" FontSize="11" HorizontalAlignment="Center"
                                   Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                        <Border Height="{Binding Height}" Margin="18,4,18,0" CornerRadius="4,4,0,0"
                                Background="{StaticResource HaUiNavyBrush}" ToolTip="{Binding Value, StringFormat={}{0:N0} ₫}" />
                        <TextBlock Text="{Binding Label}" FontSize="11" TextAlignment="Center" TextWrapping="Wrap" Margin="0,4,0,0" />
                    </StackPanel>
                </DataTemplate>
            </Setter.Value>
        </Setter>
    </Style>
```

- [ ] **Step 9: Write the shared page controls** (namespace `Transport.Presentation.Wpf.Views.Controls`; each `.xaml.cs` only calls `InitializeComponent()`)

`Views/Controls/CrudToolbar.xaml` (DataContext = the page's `CrudPageViewModel`):
```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Controls.CrudToolbar"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls">
    <DockPanel Margin="0,0,0,12">
        <StackPanel DockPanel.Dock="Right" Orientation="Horizontal">
            <Button Command="{Binding RefreshCommand}" c:Shortcut.Gesture="F5" Margin="8,0,0,0"
                    ToolTip="{l:Loc Btn_Refresh}" AutomationProperties.Name="{l:Loc Btn_Refresh}">
                <TextBlock Text="&#xE72C;" FontFamily="{StaticResource SymbolFont}" />
            </Button>
            <Button Command="{Binding ExportToExcelCommand}" Content="{l:Loc Btn_ExportExcel}" c:Shortcut.Gesture="Ctrl+E" Margin="8,0,0,0" />
            <Button Command="{Binding DeleteCommand}" Content="{l:Loc Btn_Delete}" Margin="8,0,0,0"
                    Visibility="{Binding CanDeleteItems, Converter={StaticResource BoolToVisibility}}" />
            <Button Command="{Binding EditCommand}" Content="{l:Loc Btn_Edit}" Margin="8,0,0,0"
                    Visibility="{Binding CanEditItems, Converter={StaticResource BoolToVisibility}}" />
            <Button Command="{Binding NewCommand}" Content="{l:Loc Btn_New}" c:Shortcut.Gesture="Ctrl+N" Margin="8,0,0,0"
                    Style="{StaticResource PrimaryButtonStyle}"
                    Visibility="{Binding CanCreate, Converter={StaticResource BoolToVisibility}}" />
        </StackPanel>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="&#xE721;" FontFamily="{StaticResource SymbolFont}" VerticalAlignment="Center" Margin="0,0,8,0" />
            <TextBox Width="320" Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}" c:Shortcut.Gesture="Ctrl+F"
                     ToolTip="{l:Loc Lbl_Search}" AutomationProperties.Name="{l:Loc Lbl_Search}" />
        </StackPanel>
    </DockPanel>
</UserControl>
```

`Views/Controls/FormHeader.xaml`:
```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Controls.FormHeader"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization">
    <Grid Margin="0,0,0,4">
        <TextBlock Text="{l:Loc Form_NewTitle}" Style="{StaticResource SectionTitleStyle}"
                   Visibility="{Binding IsNewItem, Converter={StaticResource BoolToVisibility}}" />
        <TextBlock Text="{l:Loc Form_EditTitle}" Style="{StaticResource SectionTitleStyle}"
                   Visibility="{Binding IsNewItem, Converter={StaticResource InverseBoolToVisibility}}" />
    </Grid>
</UserControl>
```

`Views/Controls/FormButtons.xaml`:
```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Controls.FormButtons"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls">
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0">
        <Button Content="{l:Loc Btn_Cancel}" Command="{Binding CancelFormCommand}" Margin="0,0,8,0" MinWidth="96" />
        <Button Content="{l:Loc Btn_Save}" Command="{Binding SaveCommand}" c:Shortcut.Gesture="Ctrl+S"
                Style="{StaticResource PrimaryButtonStyle}" MinWidth="96" />
    </StackPanel>
</UserControl>
```

Code-behind example (`CrudToolbar.xaml.cs`; same for the other two):
```csharp
using System.Windows.Controls;

namespace Transport.Presentation.Wpf.Views.Controls;

public partial class CrudToolbar : UserControl
{
    public CrudToolbar() => InitializeComponent();
}
```

- [ ] **Step 10: Write `Views/Pages/RoutesPage.xaml`** (+ code-behind per Global Constraints)

```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Pages.RoutesPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:ctl="clr-namespace:Transport.Presentation.Wpf.Views.Controls"
             xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"
             mc:Ignorable="d" d:DesignWidth="1100" d:DesignHeight="680"
             d:DataContext="{d:DesignInstance Type=vm:RoutesViewModel}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>

        <Border Style="{StaticResource CardStyle}">
            <DockPanel>
                <ctl:CrudToolbar DockPanel.Dock="Top" />
                <DataGrid Style="{StaticResource ListGridStyle}" ItemsSource="{Binding Items}" SelectedItem="{Binding SelectedItem}"
                          AutomationProperties.Name="{l:Loc Menu_Routes}">
                    <DataGrid.InputBindings>
                        <MouseBinding Gesture="LeftDoubleClick" Command="{Binding EditCommand}" />
                    </DataGrid.InputBindings>
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="{l:Loc Routes_Name}" Binding="{Binding Name}" Width="2*" />
                        <DataGridTextColumn Header="{l:Loc Routes_Origin}" Binding="{Binding Origin}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Routes_Destination}" Binding="{Binding Destination}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Routes_Distance}" Binding="{Binding DistanceKm}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Routes_Duration}" Binding="{Binding DurationMin}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Routes_BasePrice}" Binding="{Binding BasePrice, StringFormat={}{0:N0} ₫}" Width="Auto" />
                    </DataGrid.Columns>
                </DataGrid>
            </DockPanel>
        </Border>

        <Border Grid.Column="1" Style="{StaticResource FormCardStyle}">
            <StackPanel>
                <ctl:FormHeader />
                <TextBlock Text="{l:Loc Routes_Name}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}" AutomationProperties.Name="{l:Loc Routes_Name}" />
                <TextBlock Text="{l:Loc Routes_Origin}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding Origin, UpdateSourceTrigger=PropertyChanged}" AutomationProperties.Name="{l:Loc Routes_Origin}" />
                <TextBlock Text="{l:Loc Routes_Destination}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding Destination, UpdateSourceTrigger=PropertyChanged}" AutomationProperties.Name="{l:Loc Routes_Destination}" />
                <TextBlock Text="{l:Loc Routes_Distance}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding DistanceKm, UpdateSourceTrigger=PropertyChanged, TargetNullValue=''}" AutomationProperties.Name="{l:Loc Routes_Distance}" />
                <TextBlock Text="{l:Loc Routes_Duration}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding DurationMin, UpdateSourceTrigger=PropertyChanged, TargetNullValue=''}" AutomationProperties.Name="{l:Loc Routes_Duration}" />
                <TextBlock Text="{l:Loc Routes_BasePrice}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding BasePrice, UpdateSourceTrigger=PropertyChanged}" AutomationProperties.Name="{l:Loc Routes_BasePrice}" />
                <ctl:FormButtons />
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 11: Register the page**

`Views/PageTemplates.xaml` becomes:
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"
                    xmlns:pages="clr-namespace:Transport.Presentation.Wpf.Views.Pages">
    <!-- Mỗi trang một DataTemplate: ViewModel -> View -->
    <DataTemplate DataType="{x:Type vm:RoutesViewModel}"><pages:RoutesPage /></DataTemplate>
</ResourceDictionary>
```

In `PresentationServices.AddPages`, before `return services;`:
```csharp
        services.AddPage<RoutesViewModel>("Menu_Routes", "", 50);
```
and add `using Transport.Presentation.Wpf.ViewModels.Pages;` at the top of the file.

- [ ] **Step 12: Add resource keys**

`Strings.resx`:
```xml
  <data name="Btn_New" xml:space="preserve"><value>New</value></data>
  <data name="Btn_Edit" xml:space="preserve"><value>Edit</value></data>
  <data name="Btn_Delete" xml:space="preserve"><value>Delete</value></data>
  <data name="Btn_Save" xml:space="preserve"><value>Save</value></data>
  <data name="Btn_Refresh" xml:space="preserve"><value>Refresh (F5)</value></data>
  <data name="Btn_ExportExcel" xml:space="preserve"><value>Export Excel</value></data>
  <data name="Btn_Search" xml:space="preserve"><value>Search</value></data>
  <data name="Lbl_Search" xml:space="preserve"><value>Search (Ctrl+F)</value></data>
  <data name="Lbl_All" xml:space="preserve"><value>All</value></data>
  <data name="Lbl_From" xml:space="preserve"><value>From</value></data>
  <data name="Lbl_To" xml:space="preserve"><value>To</value></data>
  <data name="Form_NewTitle" xml:space="preserve"><value>Add new</value></data>
  <data name="Form_EditTitle" xml:space="preserve"><value>Edit</value></data>
  <data name="Val_TooLong" xml:space="preserve"><value>Too long.</value></data>
  <data name="Menu_Routes" xml:space="preserve"><value>Routes</value></data>
  <data name="Routes_Name" xml:space="preserve"><value>Route name</value></data>
  <data name="Routes_Origin" xml:space="preserve"><value>Origin</value></data>
  <data name="Routes_Destination" xml:space="preserve"><value>Destination</value></data>
  <data name="Routes_Distance" xml:space="preserve"><value>Distance (km)</value></data>
  <data name="Routes_Duration" xml:space="preserve"><value>Duration (min)</value></data>
  <data name="Routes_BasePrice" xml:space="preserve"><value>Base price</value></data>
  <data name="Routes_ExportTitle" xml:space="preserve"><value>Route list</value></data>
```

`Strings.vi.resx`:
```xml
  <data name="Btn_New" xml:space="preserve"><value>Thêm mới</value></data>
  <data name="Btn_Edit" xml:space="preserve"><value>Sửa</value></data>
  <data name="Btn_Delete" xml:space="preserve"><value>Xóa</value></data>
  <data name="Btn_Save" xml:space="preserve"><value>Lưu</value></data>
  <data name="Btn_Refresh" xml:space="preserve"><value>Làm mới (F5)</value></data>
  <data name="Btn_ExportExcel" xml:space="preserve"><value>Xuất Excel</value></data>
  <data name="Btn_Search" xml:space="preserve"><value>Tìm</value></data>
  <data name="Lbl_Search" xml:space="preserve"><value>Tìm kiếm (Ctrl+F)</value></data>
  <data name="Lbl_All" xml:space="preserve"><value>Tất cả</value></data>
  <data name="Lbl_From" xml:space="preserve"><value>Từ ngày</value></data>
  <data name="Lbl_To" xml:space="preserve"><value>Đến ngày</value></data>
  <data name="Form_NewTitle" xml:space="preserve"><value>Thêm mới</value></data>
  <data name="Form_EditTitle" xml:space="preserve"><value>Cập nhật</value></data>
  <data name="Val_TooLong" xml:space="preserve"><value>Quá dài.</value></data>
  <data name="Menu_Routes" xml:space="preserve"><value>Tuyến xe</value></data>
  <data name="Routes_Name" xml:space="preserve"><value>Tên tuyến</value></data>
  <data name="Routes_Origin" xml:space="preserve"><value>Điểm đi</value></data>
  <data name="Routes_Destination" xml:space="preserve"><value>Điểm đến</value></data>
  <data name="Routes_Distance" xml:space="preserve"><value>Quãng đường (km)</value></data>
  <data name="Routes_Duration" xml:space="preserve"><value>Thời gian (phút)</value></data>
  <data name="Routes_BasePrice" xml:space="preserve"><value>Giá cơ bản</value></data>
  <data name="Routes_ExportTitle" xml:space="preserve"><value>Danh sách tuyến xe</value></data>
```

- [ ] **Step 13: Build, test, run**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests`
Expected: 0 warnings, `failed: 0`.
Manual: run the app, sign in as `operator01` → menu "Tuyến xe" lists 5 routes; Ctrl+N opens the form, saving an empty form shows the warning bar and red fields; sign in as `seller01` → no New/Edit/Delete buttons.

- [ ] **Step 14: Commit**

```bash
git add src/Transport.Presentation.Wpf tests/Transport.UnitTests
git commit -m "feat(ui): add CRUD page base, shortcuts and routes screen"
```

---

### Task 2: Vehicles screen

**Files:**
- Create: `ViewModels/Pages/VehiclesViewModel.cs`, `Views/Pages/VehiclesPage.xaml(.cs)`
- Modify: `Views/PageTemplates.xaml`, `PresentationServices.cs`, both `.resx`
- Test: `tests/Transport.UnitTests/Presentation/VehiclesViewModelTests.cs`

**Interfaces:**
- Consumes: `CrudPageViewModel<Vehicle>`, `IVehicleRepository`.
- Produces: `VehiclesViewModel(PageServices, IVehicleRepository)` with form properties `PlateNumber`, `VehicleType`, `SeatCount`, `Status`, list `Statuses`.

- [ ] **Step 1: Write the failing test**

```csharp
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class VehiclesViewModelTests
{
    private readonly PageTestContext _context = new();
    private readonly FakeVehicleRepository _vehicles = new();

    [Fact]
    public async Task New_DefaultsToActive_AndSaveUppercasesPlate()
    {
        var vm = new VehiclesViewModel(_context.Services, _vehicles);
        vm.NewCommand.Execute(null);
        Assert.Equal(VehicleStatus.Active, vm.Status);

        vm.PlateNumber = "30a-123.45";
        vm.VehicleType = "Limousine 16 chỗ";
        vm.SeatCount = 16;
        await vm.SaveCommand.ExecuteAsync(null);

        var added = Assert.Single(_vehicles.Added);
        Assert.Equal("30A-123.45", added.PlateNumber);
        Assert.Equal(16, added.SeatCount);
    }

    [Theory]
    [InlineData("29B19688", 40)]
    [InlineData("29B-196.88", 0)]
    [InlineData("29B-196.88", 61)]
    public async Task Save_RejectsInvalidPlateOrSeatCount(string plate, int seats)
    {
        var vm = new VehiclesViewModel(_context.Services, _vehicles);
        vm.NewCommand.Execute(null);
        vm.PlateNumber = plate;
        vm.VehicleType = "Ghế ngồi";
        vm.SeatCount = seats;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(_vehicles.Added);
        Assert.Single(_context.Notifications.Warnings);
    }

    [Fact]
    public async Task Edit_KeepsIdAndStatus()
    {
        _vehicles.Items.Add(TestData.Vehicle(3, status: VehicleStatus.Maintenance));
        var vm = new VehiclesViewModel(_context.Services, _vehicles);
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[0];

        vm.EditCommand.Execute(null);
        vm.Status = VehicleStatus.Active;
        await vm.SaveCommand.ExecuteAsync(null);

        var updated = Assert.Single(_vehicles.Updated);
        Assert.Equal(3, updated.Id);
        Assert.Equal(VehicleStatus.Active, updated.Status);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build error `VehiclesViewModel` not found.

- [ ] **Step 3: Write `ViewModels/Pages/VehiclesViewModel.cs`**

```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 5: Write `Views/Pages/VehiclesPage.xaml`** (+ code-behind)

```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Pages.VehiclesPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:ctl="clr-namespace:Transport.Presentation.Wpf.Views.Controls"
             xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"
             mc:Ignorable="d" d:DesignWidth="1100" d:DesignHeight="680"
             d:DataContext="{d:DesignInstance Type=vm:VehiclesViewModel}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>

        <Border Style="{StaticResource CardStyle}">
            <DockPanel>
                <ctl:CrudToolbar DockPanel.Dock="Top" />
                <DataGrid Style="{StaticResource ListGridStyle}" ItemsSource="{Binding Items}" SelectedItem="{Binding SelectedItem}"
                          AutomationProperties.Name="{l:Loc Menu_Vehicles}">
                    <DataGrid.InputBindings>
                        <MouseBinding Gesture="LeftDoubleClick" Command="{Binding EditCommand}" />
                    </DataGrid.InputBindings>
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="{l:Loc Vehicles_Plate}" Binding="{Binding PlateNumber}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Vehicles_Type}" Binding="{Binding VehicleType}" Width="2*" />
                        <DataGridTextColumn Header="{l:Loc Vehicles_Seats}" Binding="{Binding SeatCount}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Vehicles_Status}" Width="*"
                                            Binding="{Binding Status, Converter={StaticResource Localize}, ConverterParameter=VehicleStatus_}" />
                    </DataGrid.Columns>
                </DataGrid>
            </DockPanel>
        </Border>

        <Border Grid.Column="1" Style="{StaticResource FormCardStyle}">
            <StackPanel>
                <ctl:FormHeader />
                <TextBlock Text="{l:Loc Vehicles_Plate}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding PlateNumber, UpdateSourceTrigger=PropertyChanged}" CharacterCasing="Upper"
                         AutomationProperties.Name="{l:Loc Vehicles_Plate}" />
                <TextBlock Text="{l:Loc Vehicles_Type}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding VehicleType, UpdateSourceTrigger=PropertyChanged}" AutomationProperties.Name="{l:Loc Vehicles_Type}" />
                <TextBlock Text="{l:Loc Vehicles_Seats}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding SeatCount, UpdateSourceTrigger=PropertyChanged}" AutomationProperties.Name="{l:Loc Vehicles_Seats}" />
                <TextBlock Text="{l:Loc Vehicles_Status}" Style="{StaticResource FieldLabelStyle}" />
                <ComboBox ItemsSource="{Binding Statuses}" SelectedItem="{Binding Status}" AutomationProperties.Name="{l:Loc Vehicles_Status}">
                    <ComboBox.ItemTemplate>
                        <DataTemplate>
                            <TextBlock Text="{Binding Converter={StaticResource Localize}, ConverterParameter=VehicleStatus_}" />
                        </DataTemplate>
                    </ComboBox.ItemTemplate>
                </ComboBox>
                <ctl:FormButtons />
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 6: Register** – `PageTemplates.xaml`: `<DataTemplate DataType="{x:Type vm:VehiclesViewModel}"><pages:VehiclesPage /></DataTemplate>`; `AddPages`: `services.AddPage<VehiclesViewModel>("Menu_Vehicles", "", 60, operatorOnly: true);`

- [ ] **Step 7: Add resource keys**

`Strings.resx`:
```xml
  <data name="Menu_Vehicles" xml:space="preserve"><value>Vehicles</value></data>
  <data name="Vehicles_Plate" xml:space="preserve"><value>Plate number</value></data>
  <data name="Vehicles_Type" xml:space="preserve"><value>Vehicle type</value></data>
  <data name="Vehicles_Seats" xml:space="preserve"><value>Seats</value></data>
  <data name="Vehicles_Status" xml:space="preserve"><value>Status</value></data>
  <data name="Vehicles_ExportTitle" xml:space="preserve"><value>Vehicle list</value></data>
  <data name="VehicleStatus_Active" xml:space="preserve"><value>In service</value></data>
  <data name="VehicleStatus_Maintenance" xml:space="preserve"><value>Maintenance</value></data>
  <data name="VehicleStatus_Retired" xml:space="preserve"><value>Retired</value></data>
  <data name="Val_Plate" xml:space="preserve"><value>Format like 29B-196.88 or 29B-12345.</value></data>
  <data name="Val_SeatRange" xml:space="preserve"><value>From 1 to 60 seats.</value></data>
```

`Strings.vi.resx`:
```xml
  <data name="Menu_Vehicles" xml:space="preserve"><value>Phương tiện</value></data>
  <data name="Vehicles_Plate" xml:space="preserve"><value>Biển số</value></data>
  <data name="Vehicles_Type" xml:space="preserve"><value>Loại xe</value></data>
  <data name="Vehicles_Seats" xml:space="preserve"><value>Số ghế</value></data>
  <data name="Vehicles_Status" xml:space="preserve"><value>Trạng thái</value></data>
  <data name="Vehicles_ExportTitle" xml:space="preserve"><value>Danh sách phương tiện</value></data>
  <data name="VehicleStatus_Active" xml:space="preserve"><value>Đang hoạt động</value></data>
  <data name="VehicleStatus_Maintenance" xml:space="preserve"><value>Bảo trì</value></data>
  <data name="VehicleStatus_Retired" xml:space="preserve"><value>Ngừng sử dụng</value></data>
  <data name="Val_Plate" xml:space="preserve"><value>Định dạng như 29B-196.88 hoặc 29B-12345.</value></data>
  <data name="Val_SeatRange" xml:space="preserve"><value>Từ 1 đến 60 ghế.</value></data>
```

- [ ] **Step 8: Build, test, commit**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests` → 0 warnings, `failed: 0`.
```bash
git add src/Transport.Presentation.Wpf tests/Transport.UnitTests
git commit -m "feat(ui): add vehicles screen"
```

---

### Task 3: Drivers screen (licence-expiry highlight)

**Files:**
- Create: `ViewModels/Pages/DriversViewModel.cs`, `Views/Pages/DriversPage.xaml(.cs)`, `Converters/LicenseStateConverter.cs`
- Modify: `Themes/Controls.xaml` (register converter), `Views/PageTemplates.xaml`, `PresentationServices.cs`, both `.resx`
- Test: `tests/Transport.UnitTests/Presentation/DriversViewModelTests.cs`

**Interfaces:**
- Consumes: `CrudPageViewModel<Driver>`, `IDriverRepository`, `TimeProvider`, `Driver.GetLicenseState`.
- Produces: `DriversViewModel(PageServices, IDriverRepository, TimeProvider)` with `FullName`, `IdCard`, `Phone`, `LicenseNo`, `LicenseClass`, `LicenseExpiry`, `Status`, `LicenseClasses`, `Statuses`, `ExpiryWarning` (string?).

- [ ] **Step 1: Write the failing test**

```csharp
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class DriversViewModelTests
{
    private static readonly FixedTimeProvider Time = new(new DateTimeOffset(2026, 10, 17, 8, 0, 0, TimeSpan.Zero));
    private readonly PageTestContext _context = new();
    private readonly FakeDriverRepository _drivers = new();

    [Fact]
    public async Task Load_CountsExpiringAndExpiredLicences()
    {
        _drivers.Items.Add(TestData.Driver(1, expiry: new DateTime(2028, 1, 1)));
        _drivers.Items.Add(TestData.Driver(2, expiry: new DateTime(2026, 11, 1)));
        _drivers.Items.Add(TestData.Driver(3, expiry: new DateTime(2026, 10, 1)));
        var vm = new DriversViewModel(_context.Services, _drivers, Time);

        await vm.LoadAsync();

        Assert.Equal("Drivers_ExpiryWarning|2", vm.ExpiryWarning);
    }

    [Fact]
    public async Task Load_NoWarningWhenAllValid()
    {
        _drivers.Items.Add(TestData.Driver(1, expiry: new DateTime(2028, 1, 1)));
        var vm = new DriversViewModel(_context.Services, _drivers, Time);

        await vm.LoadAsync();

        Assert.Null(vm.ExpiryWarning);
    }

    [Fact]
    public async Task Save_ValidatesIdCardAndPhone_ThenAdds()
    {
        var vm = new DriversViewModel(_context.Services, _drivers, Time);
        vm.NewCommand.Execute(null);
        Assert.Equal(new DateTime(2027, 10, 17), vm.LicenseExpiry);
        vm.FullName = "Đỗ Văn Giang";
        vm.IdCard = "12345";
        vm.Phone = "0912345699";
        vm.LicenseNo = "790999999999";

        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Empty(_drivers.Added);
        Assert.Equal(new[] { "Val_IdCard" }, _context.Notifications.Warnings);

        vm.IdCard = "001099012345";
        vm.LicenseClass = LicenseClass.D2;
        await vm.SaveCommand.ExecuteAsync(null);
        var added = Assert.Single(_drivers.Added);
        Assert.Equal(LicenseClass.D2, added.LicenseClass);
        Assert.Equal("Đỗ Văn Giang", added.FullName);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build error `DriversViewModel` not found.

- [ ] **Step 3: Write `ViewModels/Pages/DriversViewModel.cs`**

```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 5: Write `Converters/LicenseStateConverter.cs` and register it**

```csharp
using System.Globalization;
using System.Windows.Data;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.Converters;

// Driver -> LicenseState theo ngày hôm nay, dùng để tô màu dòng
public sealed class LicenseStateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Driver driver ? driver.GetLicenseState(DateTime.Today) : LicenseState.Valid;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
```

In `Themes/Controls.xaml`, next to the other converters:
```xml
    <conv:LicenseStateConverter x:Key="LicenseState" />
```

- [ ] **Step 6: Write `Views/Pages/DriversPage.xaml`** (+ code-behind)

```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Pages.DriversPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:ctl="clr-namespace:Transport.Presentation.Wpf.Views.Controls"
             xmlns:enums="clr-namespace:Transport.Domain.Enums;assembly=Transport.Domain"
             xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"
             mc:Ignorable="d" d:DesignWidth="1100" d:DesignHeight="680"
             d:DataContext="{d:DesignInstance Type=vm:DriversViewModel}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>

        <Border Style="{StaticResource CardStyle}">
            <DockPanel>
                <ctl:CrudToolbar DockPanel.Dock="Top" />
                <Border DockPanel.Dock="Top" Background="{StaticResource WarningBackgroundBrush}" BorderBrush="{StaticResource WarningBrush}"
                        BorderThickness="1" CornerRadius="6" Padding="12,8" Margin="0,0,0,12"
                        Visibility="{Binding ExpiryWarning, Converter={StaticResource NullToCollapsed}}">
                    <TextBlock Text="{Binding ExpiryWarning}" Foreground="{StaticResource WarningBrush}" TextWrapping="Wrap" />
                </Border>
                <DataGrid Style="{StaticResource ListGridStyle}" ItemsSource="{Binding Items}" SelectedItem="{Binding SelectedItem}"
                          AutomationProperties.Name="{l:Loc Menu_Drivers}">
                    <DataGrid.RowStyle>
                        <Style TargetType="DataGridRow" BasedOn="{StaticResource {x:Type DataGridRow}}">
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding Converter={StaticResource LicenseState}}" Value="{x:Static enums:LicenseState.ExpiringSoon}">
                                    <Setter Property="Background" Value="{StaticResource WarningBackgroundBrush}" />
                                </DataTrigger>
                                <DataTrigger Binding="{Binding Converter={StaticResource LicenseState}}" Value="{x:Static enums:LicenseState.Expired}">
                                    <Setter Property="Background" Value="{StaticResource ErrorBackgroundBrush}" />
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </DataGrid.RowStyle>
                    <DataGrid.InputBindings>
                        <MouseBinding Gesture="LeftDoubleClick" Command="{Binding EditCommand}" />
                    </DataGrid.InputBindings>
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="{l:Loc Drivers_Name}" Binding="{Binding FullName}" Width="2*" />
                        <DataGridTextColumn Header="{l:Loc Drivers_Phone}" Binding="{Binding Phone}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Drivers_LicenseNo}" Binding="{Binding LicenseNo}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Drivers_LicenseClass}" Binding="{Binding LicenseClass}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Drivers_LicenseExpiry}" Binding="{Binding LicenseExpiry, StringFormat={}{0:dd/MM/yyyy}}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Drivers_Status}" Width="*"
                                            Binding="{Binding Status, Converter={StaticResource Localize}, ConverterParameter=DriverStatus_}" />
                    </DataGrid.Columns>
                </DataGrid>
            </DockPanel>
        </Border>

        <Border Grid.Column="1" Style="{StaticResource FormCardStyle}">
            <ScrollViewer VerticalScrollBarVisibility="Auto">
                <StackPanel>
                    <ctl:FormHeader />
                    <TextBlock Text="{l:Loc Drivers_Name}" Style="{StaticResource FieldLabelStyle}" />
                    <TextBox Text="{Binding FullName, UpdateSourceTrigger=PropertyChanged}" AutomationProperties.Name="{l:Loc Drivers_Name}" />
                    <TextBlock Text="{l:Loc Drivers_IdCard}" Style="{StaticResource FieldLabelStyle}" />
                    <TextBox Text="{Binding IdCard, UpdateSourceTrigger=PropertyChanged}" MaxLength="12" AutomationProperties.Name="{l:Loc Drivers_IdCard}" />
                    <TextBlock Text="{l:Loc Drivers_Phone}" Style="{StaticResource FieldLabelStyle}" />
                    <TextBox Text="{Binding Phone, UpdateSourceTrigger=PropertyChanged}" MaxLength="10" AutomationProperties.Name="{l:Loc Drivers_Phone}" />
                    <TextBlock Text="{l:Loc Drivers_LicenseNo}" Style="{StaticResource FieldLabelStyle}" />
                    <TextBox Text="{Binding LicenseNo, UpdateSourceTrigger=PropertyChanged}" AutomationProperties.Name="{l:Loc Drivers_LicenseNo}" />
                    <TextBlock Text="{l:Loc Drivers_LicenseClass}" Style="{StaticResource FieldLabelStyle}" />
                    <ComboBox ItemsSource="{Binding LicenseClasses}" SelectedItem="{Binding LicenseClass}" AutomationProperties.Name="{l:Loc Drivers_LicenseClass}" />
                    <TextBlock Text="{l:Loc Drivers_LicenseExpiry}" Style="{StaticResource FieldLabelStyle}" />
                    <DatePicker SelectedDate="{Binding LicenseExpiry}" AutomationProperties.Name="{l:Loc Drivers_LicenseExpiry}" />
                    <TextBlock Text="{l:Loc Drivers_Status}" Style="{StaticResource FieldLabelStyle}" />
                    <ComboBox ItemsSource="{Binding Statuses}" SelectedItem="{Binding Status}" AutomationProperties.Name="{l:Loc Drivers_Status}">
                        <ComboBox.ItemTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding Converter={StaticResource Localize}, ConverterParameter=DriverStatus_}" />
                            </DataTemplate>
                        </ComboBox.ItemTemplate>
                    </ComboBox>
                    <ctl:FormButtons />
                </StackPanel>
            </ScrollViewer>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 7: Register** – `PageTemplates.xaml`: `<DataTemplate DataType="{x:Type vm:DriversViewModel}"><pages:DriversPage /></DataTemplate>`; `AddPages`: `services.AddPage<DriversViewModel>("Menu_Drivers", "", 70, operatorOnly: true);`

- [ ] **Step 8: Add resource keys**

`Strings.resx`:
```xml
  <data name="Menu_Drivers" xml:space="preserve"><value>Drivers</value></data>
  <data name="Drivers_Name" xml:space="preserve"><value>Full name</value></data>
  <data name="Drivers_IdCard" xml:space="preserve"><value>ID card</value></data>
  <data name="Drivers_Phone" xml:space="preserve"><value>Phone</value></data>
  <data name="Drivers_LicenseNo" xml:space="preserve"><value>Licence number</value></data>
  <data name="Drivers_LicenseClass" xml:space="preserve"><value>Licence class</value></data>
  <data name="Drivers_LicenseExpiry" xml:space="preserve"><value>Licence expiry</value></data>
  <data name="Drivers_Status" xml:space="preserve"><value>Status</value></data>
  <data name="Drivers_ExportTitle" xml:space="preserve"><value>Driver list</value></data>
  <data name="Drivers_ExpiryWarning" xml:space="preserve"><value>{0} driver(s) have a licence expiring within 30 days or already expired (highlighted).</value></data>
  <data name="DriverStatus_Active" xml:space="preserve"><value>Working</value></data>
  <data name="DriverStatus_Inactive" xml:space="preserve"><value>Inactive</value></data>
  <data name="Val_IdCard" xml:space="preserve"><value>12 digits.</value></data>
```

`Strings.vi.resx`:
```xml
  <data name="Menu_Drivers" xml:space="preserve"><value>Tài xế</value></data>
  <data name="Drivers_Name" xml:space="preserve"><value>Họ tên</value></data>
  <data name="Drivers_IdCard" xml:space="preserve"><value>CCCD</value></data>
  <data name="Drivers_Phone" xml:space="preserve"><value>Số điện thoại</value></data>
  <data name="Drivers_LicenseNo" xml:space="preserve"><value>Số bằng lái</value></data>
  <data name="Drivers_LicenseClass" xml:space="preserve"><value>Hạng bằng</value></data>
  <data name="Drivers_LicenseExpiry" xml:space="preserve"><value>Hạn bằng lái</value></data>
  <data name="Drivers_Status" xml:space="preserve"><value>Trạng thái</value></data>
  <data name="Drivers_ExportTitle" xml:space="preserve"><value>Danh sách tài xế</value></data>
  <data name="Drivers_ExpiryWarning" xml:space="preserve"><value>{0} tài xế có bằng lái sắp hết hạn (dưới 30 ngày) hoặc đã hết hạn (dòng tô màu).</value></data>
  <data name="DriverStatus_Active" xml:space="preserve"><value>Đang làm việc</value></data>
  <data name="DriverStatus_Inactive" xml:space="preserve"><value>Nghỉ việc</value></data>
  <data name="Val_IdCard" xml:space="preserve"><value>Gồm 12 chữ số.</value></data>
```

- [ ] **Step 9: Build, test, commit**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests` → 0 warnings, `failed: 0`.
Manual: as `operator01`, "Tài xế" shows Phạm Văn Dũng's row highlighted yellow and the warning banner.
```bash
git add src/Transport.Presentation.Wpf tests/Transport.UnitTests
git commit -m "feat(ui): add drivers screen with licence expiry warning"
```

---

### Task 4: Passengers screen (with ticket history)

**Files:**
- Create: `ViewModels/Pages/PassengersViewModel.cs`, `Views/Pages/PassengersPage.xaml(.cs)`
- Modify: `Views/PageTemplates.xaml`, `PresentationServices.cs`, both `.resx`
- Test: `tests/Transport.UnitTests/Presentation/PassengersViewModelTests.cs`

**Interfaces:**
- Consumes: `CrudPageViewModel<Passenger>`, `IPassengerRepository`, `TicketService` (Plan 2), `FakeTicketRepository` (Plan 2).
- Produces: `PassengersViewModel(PageServices, IPassengerRepository, TicketService)` with `FullName`, `Phone`, `IdCard`, `Email`, `History` (`ObservableCollection<TicketListItem>`); ticket column keys `Tickets_*` and `TicketStatus_*` reused by later screens.

- [ ] **Step 1: Write the failing test**

```csharp
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class PassengersViewModelTests
{
    private readonly FakePassengerRepository _passengers = new();
    private readonly FakeTicketRepository _tickets = new();

    private PassengersViewModel Create(PageTestContext context) =>
        new(context.Services, _passengers, new TicketService(_tickets));

    [Fact]
    public void Seller_CanCreateAndEditButNotDelete()
    {
        var vm = Create(PageTestContext.ForSeller());

        Assert.True(vm.CanCreate);
        Assert.True(vm.CanEditItems);
        Assert.False(vm.CanDeleteItems);
    }

    [Fact]
    public async Task SelectingPassenger_LoadsTicketHistoryByPhone()
    {
        _passengers.Items.Add(TestData.Passenger(1, phone: "0987000001"));
        _tickets.SearchResult.Add(TestData.Ticket(10, TicketStatus.Paid, phone: "0987000001"));
        _tickets.SearchResult.Add(TestData.Ticket(11, TicketStatus.Paid, phone: "0987000999"));
        var vm = Create(new PageTestContext());
        await vm.LoadAsync();

        vm.SelectedItem = vm.Items[0];

        Assert.Equal("0987000001", _tickets.LastSearch!.Keyword);
        Assert.Equal(10, Assert.Single(vm.History).TicketId);
    }

    [Fact]
    public async Task Save_StoresBlankOptionalFieldsAsNull_AndRejectsBadEmail()
    {
        var context = new PageTestContext();
        var vm = Create(context);
        vm.NewCommand.Execute(null);
        vm.FullName = "Lê Thị Mai";
        vm.Phone = "0987123456";
        vm.Email = "not-an-email";

        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(new[] { "Val_Email" }, context.Notifications.Warnings);

        vm.Email = "  ";
        await vm.SaveCommand.ExecuteAsync(null);
        var added = Assert.Single(_passengers.Added);
        Assert.Null(added.Email);
        Assert.Null(added.IdCard);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build error `PassengersViewModel` not found.

- [ ] **Step 3: Write `ViewModels/Pages/PassengersViewModel.cs`**

```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 5: Write `Views/Pages/PassengersPage.xaml`** (+ code-behind)

```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Pages.PassengersPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:ctl="clr-namespace:Transport.Presentation.Wpf.Views.Controls"
             xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"
             mc:Ignorable="d" d:DesignWidth="1100" d:DesignHeight="680"
             d:DataContext="{d:DesignInstance Type=vm:PassengersViewModel}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height="3*" />
            <RowDefinition Height="2*" />
        </Grid.RowDefinitions>

        <Border Style="{StaticResource CardStyle}">
            <DockPanel>
                <ctl:CrudToolbar DockPanel.Dock="Top" />
                <DataGrid Style="{StaticResource ListGridStyle}" ItemsSource="{Binding Items}" SelectedItem="{Binding SelectedItem}"
                          AutomationProperties.Name="{l:Loc Menu_Passengers}">
                    <DataGrid.InputBindings>
                        <MouseBinding Gesture="LeftDoubleClick" Command="{Binding EditCommand}" />
                    </DataGrid.InputBindings>
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="{l:Loc Passengers_Name}" Binding="{Binding FullName}" Width="2*" />
                        <DataGridTextColumn Header="{l:Loc Passengers_Phone}" Binding="{Binding Phone}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Passengers_IdCard}" Binding="{Binding IdCard}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Passengers_Email}" Binding="{Binding Email}" Width="2*" />
                    </DataGrid.Columns>
                </DataGrid>
            </DockPanel>
        </Border>

        <Border Grid.Row="1" Style="{StaticResource CardStyle}" Margin="0,16,0,0">
            <DockPanel>
                <TextBlock DockPanel.Dock="Top" Text="{l:Loc Passengers_History}" Style="{StaticResource SectionTitleStyle}" />
                <DataGrid Style="{StaticResource ListGridStyle}" ItemsSource="{Binding History}"
                          AutomationProperties.Name="{l:Loc Passengers_History}">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="{l:Loc Tickets_Id}" Binding="{Binding TicketId}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Tickets_Route}" Binding="{Binding RouteName}" Width="2*" />
                        <DataGridTextColumn Header="{l:Loc Tickets_Departure}" Binding="{Binding DepartureTime, StringFormat={}{0:dd/MM/yyyy HH:mm}}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Tickets_Seat}" Binding="{Binding SeatNo}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Tickets_Price}" Binding="{Binding Price, StringFormat={}{0:N0} ₫}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Tickets_Status}" Width="*"
                                            Binding="{Binding Status, Converter={StaticResource Localize}, ConverterParameter=TicketStatus_}" />
                    </DataGrid.Columns>
                </DataGrid>
            </DockPanel>
        </Border>

        <Border Grid.Column="1" Grid.RowSpan="2" Style="{StaticResource FormCardStyle}">
            <StackPanel>
                <ctl:FormHeader />
                <TextBlock Text="{l:Loc Passengers_Name}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding FullName, UpdateSourceTrigger=PropertyChanged}" AutomationProperties.Name="{l:Loc Passengers_Name}" />
                <TextBlock Text="{l:Loc Passengers_Phone}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding Phone, UpdateSourceTrigger=PropertyChanged}" MaxLength="10" AutomationProperties.Name="{l:Loc Passengers_Phone}" />
                <TextBlock Text="{l:Loc Passengers_IdCard}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding IdCard, UpdateSourceTrigger=PropertyChanged}" MaxLength="12" AutomationProperties.Name="{l:Loc Passengers_IdCard}" />
                <TextBlock Text="{l:Loc Passengers_Email}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding Email, UpdateSourceTrigger=PropertyChanged}" AutomationProperties.Name="{l:Loc Passengers_Email}" />
                <ctl:FormButtons />
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 6: Register** – `PageTemplates.xaml`: `<DataTemplate DataType="{x:Type vm:PassengersViewModel}"><pages:PassengersPage /></DataTemplate>`; `AddPages`: `services.AddPage<PassengersViewModel>("Menu_Passengers", "", 40);`

- [ ] **Step 7: Add resource keys** (includes the ticket column/status keys shared with later screens)

`Strings.resx`:
```xml
  <data name="Menu_Passengers" xml:space="preserve"><value>Passengers</value></data>
  <data name="Passengers_Name" xml:space="preserve"><value>Full name</value></data>
  <data name="Passengers_Phone" xml:space="preserve"><value>Phone</value></data>
  <data name="Passengers_IdCard" xml:space="preserve"><value>ID card</value></data>
  <data name="Passengers_Email" xml:space="preserve"><value>Email</value></data>
  <data name="Passengers_History" xml:space="preserve"><value>Ticket history of the selected passenger</value></data>
  <data name="Passengers_ExportTitle" xml:space="preserve"><value>Passenger list</value></data>
  <data name="Val_Email" xml:space="preserve"><value>Invalid email address.</value></data>
  <data name="Tickets_Id" xml:space="preserve"><value>Ticket #</value></data>
  <data name="Tickets_Route" xml:space="preserve"><value>Route</value></data>
  <data name="Tickets_Departure" xml:space="preserve"><value>Departure</value></data>
  <data name="Tickets_Vehicle" xml:space="preserve"><value>Vehicle</value></data>
  <data name="Tickets_Seat" xml:space="preserve"><value>Seat</value></data>
  <data name="Tickets_Passenger" xml:space="preserve"><value>Passenger</value></data>
  <data name="Tickets_Phone" xml:space="preserve"><value>Phone</value></data>
  <data name="Tickets_Price" xml:space="preserve"><value>Price</value></data>
  <data name="Tickets_Status" xml:space="preserve"><value>Status</value></data>
  <data name="Tickets_BookedAt" xml:space="preserve"><value>Booked at</value></data>
  <data name="Tickets_SoldBy" xml:space="preserve"><value>Sold by</value></data>
  <data name="Tickets_CancelReason" xml:space="preserve"><value>Cancel reason</value></data>
  <data name="TicketStatus_Booked" xml:space="preserve"><value>Unpaid</value></data>
  <data name="TicketStatus_Paid" xml:space="preserve"><value>Paid</value></data>
  <data name="TicketStatus_Cancelled" xml:space="preserve"><value>Cancelled</value></data>
```

`Strings.vi.resx`:
```xml
  <data name="Menu_Passengers" xml:space="preserve"><value>Hành khách</value></data>
  <data name="Passengers_Name" xml:space="preserve"><value>Họ tên</value></data>
  <data name="Passengers_Phone" xml:space="preserve"><value>Số điện thoại</value></data>
  <data name="Passengers_IdCard" xml:space="preserve"><value>CCCD</value></data>
  <data name="Passengers_Email" xml:space="preserve"><value>Email</value></data>
  <data name="Passengers_History" xml:space="preserve"><value>Lịch sử vé của khách đang chọn</value></data>
  <data name="Passengers_ExportTitle" xml:space="preserve"><value>Danh sách hành khách</value></data>
  <data name="Val_Email" xml:space="preserve"><value>Email không hợp lệ.</value></data>
  <data name="Tickets_Id" xml:space="preserve"><value>Mã vé</value></data>
  <data name="Tickets_Route" xml:space="preserve"><value>Tuyến</value></data>
  <data name="Tickets_Departure" xml:space="preserve"><value>Khởi hành</value></data>
  <data name="Tickets_Vehicle" xml:space="preserve"><value>Xe</value></data>
  <data name="Tickets_Seat" xml:space="preserve"><value>Ghế</value></data>
  <data name="Tickets_Passenger" xml:space="preserve"><value>Hành khách</value></data>
  <data name="Tickets_Phone" xml:space="preserve"><value>SĐT</value></data>
  <data name="Tickets_Price" xml:space="preserve"><value>Giá vé</value></data>
  <data name="Tickets_Status" xml:space="preserve"><value>Trạng thái</value></data>
  <data name="Tickets_BookedAt" xml:space="preserve"><value>Thời điểm đặt</value></data>
  <data name="Tickets_SoldBy" xml:space="preserve"><value>Người bán</value></data>
  <data name="Tickets_CancelReason" xml:space="preserve"><value>Lý do hủy</value></data>
  <data name="TicketStatus_Booked" xml:space="preserve"><value>Chờ thanh toán</value></data>
  <data name="TicketStatus_Paid" xml:space="preserve"><value>Đã thanh toán</value></data>
  <data name="TicketStatus_Cancelled" xml:space="preserve"><value>Đã hủy</value></data>
```

- [ ] **Step 8: Build, test, commit**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests` → 0 warnings, `failed: 0`.
```bash
git add src/Transport.Presentation.Wpf tests/Transport.UnitTests
git commit -m "feat(ui): add passengers screen with ticket history"
```

---

### Task 5: Schedules screen

**Files:**
- Create: `ViewModels/Pages/SchedulesViewModel.cs`, `Views/Pages/SchedulesPage.xaml(.cs)`
- Modify: `Views/PageTemplates.xaml`, `PresentationServices.cs`, both `.resx`
- Test: `tests/Transport.UnitTests/Presentation/SchedulesViewModelTests.cs`

**Interfaces:**
- Consumes: `CrudPageViewModel<TripStatistic>`, `IStatisticsQueries`, `ScheduleService`, `IRouteRepository`, `IVehicleRepository`, `IDriverRepository`, `TimeProvider`, `Driver.CanDrive`.
- Produces: `SchedulesViewModel(PageServices, IStatisticsQueries, ScheduleService, IRouteRepository, IVehicleRepository, IDriverRepository, TimeProvider)` with filters `FromDate`, `ToDate`; lookups `Routes`, `Vehicles` (active only), `AvailableDrivers`; form `SelectedRoute`, `SelectedVehicle`, `SelectedDriver`, `DepartureDate`, `DepartureTimeText`, `ArrivalDate`, `ArrivalTimeText`, `TicketPrice`; commands `CancelTripCommand`, `MarkRunningCommand`, `MarkCompletedCommand`; `ScheduleStatus_*` resource keys.

- [ ] **Step 1: Write the failing test**

```csharp
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class SchedulesViewModelTests
{
    private static readonly FixedTimeProvider Time = new(new DateTimeOffset(2026, 10, 17, 8, 0, 0, TimeSpan.Zero));
    private readonly FakeStatisticsQueries _statistics = new();
    private readonly FakeScheduleRepository _scheduleRepository = new();
    private readonly FakeRouteRepository _routes = new();
    private readonly FakeVehicleRepository _vehicles = new();
    private readonly FakeDriverRepository _drivers = new();

    public SchedulesViewModelTests()
    {
        _routes.Items.Add(TestData.Route(1, "Hà Nội - Lào Cai", 280000, 330));
        _vehicles.Items.Add(TestData.Vehicle(1, "29B-196.88"));
        _vehicles.Items.Add(TestData.Vehicle(2, "35B-888.99", status: VehicleStatus.Maintenance));
        _drivers.Items.Add(TestData.Driver(1, "Nguyễn Văn An", new DateTime(2030, 1, 1)));
        _drivers.Items.Add(TestData.Driver(2, "Phạm Văn Dũng", new DateTime(2026, 10, 18)));
        _statistics.Trips.Add(FakeStatisticsQueries.Trip(5, departure: new DateTime(2026, 10, 18, 7, 0, 0)));
    }

    private SchedulesViewModel Create(PageTestContext context) => new(context.Services, _statistics,
        new ScheduleService(_scheduleRepository, context.Session), _routes, _vehicles, _drivers, Time);

    [Fact]
    public async Task Operator_LoadsTripsForNextWeekAndActiveVehiclesOnly()
    {
        var vm = Create(new PageTestContext());

        await vm.LoadAsync();

        Assert.Single(vm.Items);
        Assert.Equal("29B-196.88", Assert.Single(vm.Vehicles).PlateNumber);
        Assert.Equal(new DateTime(2026, 10, 17), _statistics.LastCriteria!.From);
        Assert.Equal(new DateTime(2026, 10, 25), _statistics.LastCriteria.To);
    }

    [Fact]
    public async Task Seller_SeesTripsButNoLookupsOrActions()
    {
        var vm = Create(PageTestContext.ForSeller());

        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[0];

        Assert.Single(vm.Items);
        Assert.Equal(0, _routes.GetAllCount);
        Assert.False(vm.CanCreate);
        Assert.False(vm.CancelTripCommand.CanExecute(null));
    }

    [Fact]
    public async Task NewForm_ChoosingRouteSetsPriceAndArrival()
    {
        var vm = Create(new PageTestContext());
        await vm.LoadAsync();

        vm.NewCommand.Execute(null);
        vm.DepartureTimeText = "07:00";
        vm.SelectedRoute = vm.Routes[0];

        Assert.Equal(280000, vm.TicketPrice);
        Assert.Equal(new DateTime(2026, 10, 18), vm.ArrivalDate);
        Assert.Equal("12:30", vm.ArrivalTimeText);
    }

    [Fact]
    public async Task Drivers_AreFilteredByLicenceValidityOnDepartureDay()
    {
        var vm = Create(new PageTestContext());
        await vm.LoadAsync();
        vm.NewCommand.Execute(null);

        vm.DepartureDate = new DateTime(2026, 10, 18);
        Assert.Equal(2, vm.AvailableDrivers.Count);

        vm.DepartureDate = new DateTime(2026, 10, 20);
        Assert.Equal("Nguyễn Văn An", Assert.Single(vm.AvailableDrivers).FullName);
    }

    [Fact]
    public async Task Save_CombinesDateAndTime()
    {
        var vm = Create(new PageTestContext());
        await vm.LoadAsync();
        vm.NewCommand.Execute(null);
        vm.SelectedRoute = vm.Routes[0];
        vm.SelectedVehicle = vm.Vehicles[0];
        vm.DepartureDate = new DateTime(2026, 10, 20);
        vm.DepartureTimeText = "13:15";
        vm.SelectedDriver = vm.AvailableDrivers[0];

        await vm.SaveCommand.ExecuteAsync(null);

        var saved = _scheduleRepository.LastSchedule!;
        Assert.Equal(new DateTime(2026, 10, 20, 13, 15, 0), saved.DepartureTime);
        Assert.Equal(new DateTime(2026, 10, 20, 18, 45, 0), saved.ArrivalTime);
        Assert.Equal(280000, saved.TicketPrice);
        Assert.Equal(new[] { "add" }, _scheduleRepository.Calls);
    }

    [Fact]
    public async Task CancelTrip_PromptsForReason()
    {
        var context = new PageTestContext();
        var vm = Create(context);
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items[0];

        context.Dialogs.PromptTextResult = null;
        await vm.CancelTripCommand.ExecuteAsync(null);
        Assert.Empty(_scheduleRepository.Calls);

        context.Dialogs.PromptTextResult = "Xe hỏng";
        await vm.CancelTripCommand.ExecuteAsync(null);
        Assert.Equal(new[] { "cancel:5:Xe hỏng" }, _scheduleRepository.Calls);
        Assert.Equal(new[] { "Schedules_Cancelled" }, context.Notifications.Successes);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build error `SchedulesViewModel` not found.

- [ ] **Step 3: Write `ViewModels/Pages/SchedulesViewModel.cs`**

```csharp
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Lịch chạy: danh sách đọc từ V_TRIP_STATISTICS (seller xem được), điều hành thêm/sửa/hủy/đổi trạng thái
public sealed partial class SchedulesViewModel : CrudPageViewModel<TripStatistic>
{
    private const string TimePattern = @"^([01]\d|2[0-3]):[0-5]\d$";

    private readonly IStatisticsQueries _statistics;
    private readonly ScheduleService _schedules;
    private readonly IRouteRepository _routes;
    private readonly IVehicleRepository _vehicles;
    private readonly IDriverRepository _drivers;
    private readonly DateTime _today;
    private IReadOnlyList<Driver> _allDrivers = [];
    private int _editingId;

    public SchedulesViewModel(PageServices services, IStatisticsQueries statistics, ScheduleService schedules,
        IRouteRepository routes, IVehicleRepository vehicles, IDriverRepository drivers, TimeProvider time)
        : base(services)
    {
        _statistics = statistics;
        _schedules = schedules;
        _routes = routes;
        _vehicles = vehicles;
        _drivers = drivers;
        _today = time.GetLocalNow().Date;
        FromDate = _today;
        ToDate = _today.AddDays(7);
        Routes = [];
        Vehicles = [];
        AvailableDrivers = [];
        DepartureDate = _today.AddDays(1);
        DepartureTimeText = "07:00";
        ArrivalDate = _today.AddDays(1);
        ArrivalTimeText = "12:00";
    }

    [ObservableProperty]
    public partial DateTime FromDate { get; set; }

    [ObservableProperty]
    public partial DateTime ToDate { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Route> Routes { get; set; }

    // Chỉ xe đang hoạt động
    [ObservableProperty]
    public partial IReadOnlyList<Vehicle> Vehicles { get; set; }

    // Tài xế đang làm việc và còn hạn bằng tại ngày khởi hành
    [ObservableProperty]
    public partial IReadOnlyList<Driver> AvailableDrivers { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    public partial Route? SelectedRoute { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    public partial Vehicle? SelectedVehicle { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    public partial Driver? SelectedDriver { get; set; }

    [ObservableProperty]
    public partial DateTime DepartureDate { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [RegularExpression(TimePattern, ErrorMessage = "Val_Time")]
    public partial string DepartureTimeText { get; set; }

    [ObservableProperty]
    public partial DateTime ArrivalDate { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Val_Required")]
    [RegularExpression(TimePattern, ErrorMessage = "Val_Time")]
    public partial string ArrivalTimeText { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(typeof(decimal), "1", "100000000", ErrorMessage = "Val_Positive")]
    public partial decimal TicketPrice { get; set; }

    protected override string ExportTitleKey => "Schedules_ExportTitle";

    protected override IReadOnlyList<ExportColumn<TripStatistic>> ExportColumns =>
    [
        new("Schedules_Departure", t => t.DepartureTime, "dd/MM/yyyy HH:mm"),
        new("Schedules_Arrival", t => t.ArrivalTime, "dd/MM/yyyy HH:mm"),
        new("Schedules_Route", t => t.RouteName),
        new("Schedules_Vehicle", t => t.PlateNumber),
        new("Schedules_Driver", t => t.DriverName),
        new("Schedules_Price", t => t.TicketPrice, "#,##0"),
        new("Schedules_Sold", t => t.SeatsSold),
        new("Schedules_SeatCount", t => t.SeatCount),
        new("Schedules_Status", t => Loc[$"ScheduleStatus_{t.Status}"]),
    ];

    public override async Task LoadAsync()
    {
        // Seller không có quyền đọc bảng DRIVERS nên chỉ điều hành mới tải danh mục cho form
        if (IsOperator)
        {
            await RunBusyAsync(LoadLookupsAsync);
        }

        await base.LoadAsync();
    }

    protected override Task<IReadOnlyList<TripStatistic>> LoadItemsAsync() =>
        _statistics.GetTripsAsync(new TripSearchCriteria { From = FromDate.Date, To = ToDate.Date.AddDays(1) });

    protected override bool Matches(TripStatistic item, string text) =>
        Contains(item.RouteName, text) || Contains(item.PlateNumber, text) || Contains(item.DriverName, text);

    // Chỉ sửa/xóa chuyến chưa chạy; xóa thêm điều kiện chưa có vé nào
    protected override bool CanEditItem(TripStatistic item) => item.Status == ScheduleStatus.Scheduled;

    protected override bool CanDeleteItem(TripStatistic item) =>
        item.Status == ScheduleStatus.Scheduled && item.SeatsSold == 0 && item.TicketsCancelled == 0;

    protected override void OnSelectionChanged(TripStatistic? item)
    {
        CancelTripCommand.NotifyCanExecuteChanged();
        MarkRunningCommand.NotifyCanExecuteChanged();
        MarkCompletedCommand.NotifyCanExecuteChanged();
    }

    protected override void FillForm(TripStatistic? item)
    {
        _editingId = item?.ScheduleId ?? 0;
        if (item is null)
        {
            SelectedRoute = null;
            SelectedVehicle = null;
            SelectedDriver = null;
            DepartureDate = _today.AddDays(1);
            DepartureTimeText = "07:00";
            ArrivalDate = _today.AddDays(1);
            ArrivalTimeText = "12:00";
            TicketPrice = 0;
            return;
        }

        DepartureDate = item.DepartureTime.Date;
        DepartureTimeText = item.DepartureTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        ArrivalDate = item.ArrivalTime.Date;
        ArrivalTimeText = item.ArrivalTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        TicketPrice = item.TicketPrice;
        SelectedRoute = Routes.FirstOrDefault(r => r.Id == item.RouteId);
        SelectedVehicle = Vehicles.FirstOrDefault(v => v.Id == item.VehicleId);
        SelectedDriver = AvailableDrivers.FirstOrDefault(d => d.Id == item.DriverId);
    }

    protected override Task SaveFormAsync(bool isNew)
    {
        // Giờ đã được kiểm tra bằng TimePattern nên ghép luôn được
        TryCombine(DepartureDate, DepartureTimeText, out var departure);
        TryCombine(ArrivalDate, ArrivalTimeText, out var arrival);
        var schedule = new Schedule
        {
            Id = _editingId,
            RouteId = SelectedRoute!.Id,
            VehicleId = SelectedVehicle!.Id,
            DriverId = SelectedDriver!.Id,
            DepartureTime = departure,
            ArrivalTime = arrival,
            TicketPrice = TicketPrice,
        };
        return isNew ? _schedules.AddAsync(schedule) : _schedules.UpdateAsync(schedule);
    }

    protected override Task DeleteItemAsync(TripStatistic item) => _schedules.DeleteAsync(item.ScheduleId);

    protected override string Describe(TripStatistic item) =>
        $"{item.RouteName} {item.DepartureTime.ToString("dd/MM HH:mm", CultureInfo.InvariantCulture)}";

    // Chọn tuyến khi thêm mới: lấy giá cơ bản và tự tính giờ đến
    partial void OnSelectedRouteChanged(Route? value)
    {
        if (IsNewItem && value is not null)
        {
            TicketPrice = value.BasePrice;
            UpdateArrival();
        }
    }

    partial void OnDepartureDateChanged(DateTime value)
    {
        RefreshAvailableDrivers();
        if (IsNewItem)
        {
            UpdateArrival();
        }
    }

    partial void OnDepartureTimeTextChanged(string value)
    {
        if (IsNewItem)
        {
            UpdateArrival();
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelTrip))]
    private Task CancelTripAsync() => RunBusyAsync(async () =>
    {
        var trip = SelectedItem!;
        var reason = Dialogs.PromptText(Loc["Schedules_CancelTitle"], Loc["Schedules_CancelReason"]);
        if (reason is null)
        {
            return;
        }

        await _schedules.CancelAsync(trip.ScheduleId, reason);
        Notify.ShowSuccess(Loc["Schedules_Cancelled"]);
        await ReloadAsync();
    });

    [RelayCommand(CanExecute = nameof(CanMarkRunning))]
    private Task MarkRunningAsync() => ChangeStatusAsync(id => _schedules.MarkRunningAsync(id));

    [RelayCommand(CanExecute = nameof(CanMarkCompleted))]
    private Task MarkCompletedAsync() => ChangeStatusAsync(id => _schedules.MarkCompletedAsync(id));

    private Task ChangeStatusAsync(Func<int, Task> change) => RunBusyAsync(async () =>
    {
        await change(SelectedItem!.ScheduleId);
        Notify.ShowSuccess(Loc["Schedules_StatusUpdated"]);
        await ReloadAsync();
    });

    private bool CanCancelTrip() => IsOperator && SelectedItem?.Status == ScheduleStatus.Scheduled;

    private bool CanMarkRunning() => IsOperator && SelectedItem?.Status == ScheduleStatus.Scheduled;

    private bool CanMarkCompleted() => IsOperator && SelectedItem?.Status == ScheduleStatus.Running;

    private async Task LoadLookupsAsync()
    {
        Routes = await _routes.GetAllAsync();
        Vehicles = [.. (await _vehicles.GetAllAsync()).Where(v => v.Status == VehicleStatus.Active)];
        _allDrivers = await _drivers.GetAllAsync();
        RefreshAvailableDrivers();
    }

    private void RefreshAvailableDrivers()
    {
        var current = SelectedDriver;
        AvailableDrivers = [.. _allDrivers.Where(d => d.CanDrive(DepartureDate))];
        SelectedDriver = current is null ? null : AvailableDrivers.FirstOrDefault(d => d.Id == current.Id);
    }

    private void UpdateArrival()
    {
        if (SelectedRoute?.DurationMin is not { } minutes || !TryCombine(DepartureDate, DepartureTimeText, out var departure))
        {
            return;
        }

        var arrival = departure.AddMinutes(minutes);
        ArrivalDate = arrival.Date;
        ArrivalTimeText = arrival.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private static bool TryCombine(DateTime date, string time, out DateTime result)
    {
        result = date.Date;
        if (!TimeSpan.TryParseExact(time, @"hh\:mm", CultureInfo.InvariantCulture, out var span))
        {
            return false;
        }

        result = date.Date + span;
        return true;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 5: Write `Views/Pages/SchedulesPage.xaml`** (+ code-behind)

```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Pages.SchedulesPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:ctl="clr-namespace:Transport.Presentation.Wpf.Views.Controls"
             xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"
             mc:Ignorable="d" d:DesignWidth="1100" d:DesignHeight="680"
             d:DataContext="{d:DesignInstance Type=vm:SchedulesViewModel}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>

        <Border Style="{StaticResource CardStyle}">
            <DockPanel>
                <!-- Lọc theo khoảng ngày + thao tác trạng thái chuyến -->
                <DockPanel DockPanel.Dock="Top" Margin="0,0,0,12">
                    <StackPanel DockPanel.Dock="Right" Orientation="Horizontal"
                                Visibility="{Binding IsOperator, Converter={StaticResource BoolToVisibility}}">
                        <Button Content="{l:Loc Schedules_MarkRunning}" Command="{Binding MarkRunningCommand}" Margin="8,0,0,0" />
                        <Button Content="{l:Loc Schedules_MarkCompleted}" Command="{Binding MarkCompletedCommand}" Margin="8,0,0,0" />
                        <Button Content="{l:Loc Schedules_Cancel}" Command="{Binding CancelTripCommand}" Margin="8,0,0,0" />
                    </StackPanel>
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="{l:Loc Lbl_From}" VerticalAlignment="Center" Margin="0,0,8,0" />
                        <DatePicker SelectedDate="{Binding FromDate}" Width="150" AutomationProperties.Name="{l:Loc Lbl_From}" />
                        <TextBlock Text="{l:Loc Lbl_To}" VerticalAlignment="Center" Margin="16,0,8,0" />
                        <DatePicker SelectedDate="{Binding ToDate}" Width="150" AutomationProperties.Name="{l:Loc Lbl_To}" />
                        <Button Content="{l:Loc Btn_Search}" Command="{Binding RefreshCommand}" Margin="8,0,0,0" />
                    </StackPanel>
                </DockPanel>
                <ctl:CrudToolbar DockPanel.Dock="Top" />
                <DataGrid Style="{StaticResource ListGridStyle}" ItemsSource="{Binding Items}" SelectedItem="{Binding SelectedItem}"
                          AutomationProperties.Name="{l:Loc Menu_Schedules}">
                    <DataGrid.InputBindings>
                        <MouseBinding Gesture="LeftDoubleClick" Command="{Binding EditCommand}" />
                    </DataGrid.InputBindings>
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="{l:Loc Schedules_Departure}" Binding="{Binding DepartureTime, StringFormat={}{0:dd/MM HH:mm}}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Schedules_Route}" Binding="{Binding RouteName}" Width="2*" />
                        <DataGridTextColumn Header="{l:Loc Schedules_Vehicle}" Binding="{Binding PlateNumber}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Schedules_Driver}" Binding="{Binding DriverName}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Schedules_Price}" Binding="{Binding TicketPrice, StringFormat={}{0:N0} ₫}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Schedules_Seats}" Width="Auto">
                            <DataGridTextColumn.Binding>
                                <MultiBinding StringFormat="{}{0}/{1}">
                                    <Binding Path="SeatsSold" />
                                    <Binding Path="SeatCount" />
                                </MultiBinding>
                            </DataGridTextColumn.Binding>
                        </DataGridTextColumn>
                        <DataGridTextColumn Header="{l:Loc Schedules_Status}" Width="*"
                                            Binding="{Binding Status, Converter={StaticResource Localize}, ConverterParameter=ScheduleStatus_}" />
                    </DataGrid.Columns>
                </DataGrid>
            </DockPanel>
        </Border>

        <Border Grid.Column="1" Style="{StaticResource FormCardStyle}">
            <ScrollViewer VerticalScrollBarVisibility="Auto">
                <StackPanel>
                    <ctl:FormHeader />
                    <TextBlock Text="{l:Loc Schedules_Route}" Style="{StaticResource FieldLabelStyle}" />
                    <ComboBox ItemsSource="{Binding Routes}" SelectedItem="{Binding SelectedRoute}" DisplayMemberPath="Name"
                              AutomationProperties.Name="{l:Loc Schedules_Route}" />
                    <TextBlock Text="{l:Loc Schedules_Vehicle}" Style="{StaticResource FieldLabelStyle}" />
                    <ComboBox ItemsSource="{Binding Vehicles}" SelectedItem="{Binding SelectedVehicle}"
                              AutomationProperties.Name="{l:Loc Schedules_Vehicle}">
                        <ComboBox.ItemTemplate>
                            <DataTemplate>
                                <TextBlock>
                                    <Run Text="{Binding PlateNumber, Mode=OneWay}" FontWeight="SemiBold" />
                                    <Run Text="–" />
                                    <Run Text="{Binding VehicleType, Mode=OneWay}" />
                                </TextBlock>
                            </DataTemplate>
                        </ComboBox.ItemTemplate>
                    </ComboBox>

                    <TextBlock Text="{l:Loc Schedules_Departure}" Style="{StaticResource FieldLabelStyle}" />
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition />
                            <ColumnDefinition Width="96" />
                        </Grid.ColumnDefinitions>
                        <DatePicker SelectedDate="{Binding DepartureDate}" AutomationProperties.Name="{l:Loc Schedules_Date}" />
                        <TextBox Grid.Column="1" Margin="8,0,0,0" Text="{Binding DepartureTimeText, UpdateSourceTrigger=PropertyChanged}"
                                 ToolTip="{l:Loc Schedules_Time}" AutomationProperties.Name="{l:Loc Schedules_Time}" />
                    </Grid>

                    <TextBlock Text="{l:Loc Schedules_Arrival}" Style="{StaticResource FieldLabelStyle}" />
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition />
                            <ColumnDefinition Width="96" />
                        </Grid.ColumnDefinitions>
                        <DatePicker SelectedDate="{Binding ArrivalDate}" AutomationProperties.Name="{l:Loc Schedules_Date}" />
                        <TextBox Grid.Column="1" Margin="8,0,0,0" Text="{Binding ArrivalTimeText, UpdateSourceTrigger=PropertyChanged}"
                                 ToolTip="{l:Loc Schedules_Time}" AutomationProperties.Name="{l:Loc Schedules_Time}" />
                    </Grid>

                    <TextBlock Text="{l:Loc Schedules_Driver}" Style="{StaticResource FieldLabelStyle}" />
                    <ComboBox ItemsSource="{Binding AvailableDrivers}" SelectedItem="{Binding SelectedDriver}" DisplayMemberPath="FullName"
                              AutomationProperties.Name="{l:Loc Schedules_Driver}" />
                    <TextBlock Text="{l:Loc Schedules_Price}" Style="{StaticResource FieldLabelStyle}" />
                    <TextBox Text="{Binding TicketPrice, UpdateSourceTrigger=PropertyChanged}" AutomationProperties.Name="{l:Loc Schedules_Price}" />
                    <ctl:FormButtons />
                </StackPanel>
            </ScrollViewer>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 6: Register** – `PageTemplates.xaml`: `<DataTemplate DataType="{x:Type vm:SchedulesViewModel}"><pages:SchedulesPage /></DataTemplate>`; `AddPages`: `services.AddPage<SchedulesViewModel>("Menu_Schedules", "", 80);`

- [ ] **Step 7: Add resource keys**

`Strings.resx`:
```xml
  <data name="Menu_Schedules" xml:space="preserve"><value>Schedules</value></data>
  <data name="Schedules_Departure" xml:space="preserve"><value>Departure</value></data>
  <data name="Schedules_Arrival" xml:space="preserve"><value>Arrival</value></data>
  <data name="Schedules_Route" xml:space="preserve"><value>Route</value></data>
  <data name="Schedules_Vehicle" xml:space="preserve"><value>Vehicle</value></data>
  <data name="Schedules_Driver" xml:space="preserve"><value>Driver</value></data>
  <data name="Schedules_Price" xml:space="preserve"><value>Ticket price</value></data>
  <data name="Schedules_Seats" xml:space="preserve"><value>Sold / seats</value></data>
  <data name="Schedules_Sold" xml:space="preserve"><value>Seats sold</value></data>
  <data name="Schedules_SeatCount" xml:space="preserve"><value>Seats</value></data>
  <data name="Schedules_Status" xml:space="preserve"><value>Status</value></data>
  <data name="Schedules_Date" xml:space="preserve"><value>Date</value></data>
  <data name="Schedules_Time" xml:space="preserve"><value>Time (HH:mm)</value></data>
  <data name="Schedules_ExportTitle" xml:space="preserve"><value>Schedule list</value></data>
  <data name="Schedules_Cancel" xml:space="preserve"><value>Cancel trip</value></data>
  <data name="Schedules_CancelTitle" xml:space="preserve"><value>Cancel trip</value></data>
  <data name="Schedules_CancelReason" xml:space="preserve"><value>Reason (all tickets are cancelled and paid tickets refunded)</value></data>
  <data name="Schedules_Cancelled" xml:space="preserve"><value>Trip cancelled; paid tickets were refunded.</value></data>
  <data name="Schedules_MarkRunning" xml:space="preserve"><value>Start trip</value></data>
  <data name="Schedules_MarkCompleted" xml:space="preserve"><value>Complete trip</value></data>
  <data name="Schedules_StatusUpdated" xml:space="preserve"><value>Trip status updated.</value></data>
  <data name="ScheduleStatus_Scheduled" xml:space="preserve"><value>Scheduled</value></data>
  <data name="ScheduleStatus_Running" xml:space="preserve"><value>Running</value></data>
  <data name="ScheduleStatus_Completed" xml:space="preserve"><value>Completed</value></data>
  <data name="ScheduleStatus_Cancelled" xml:space="preserve"><value>Cancelled</value></data>
  <data name="Val_Time" xml:space="preserve"><value>Use HH:mm, for example 07:30.</value></data>
```

`Strings.vi.resx`:
```xml
  <data name="Menu_Schedules" xml:space="preserve"><value>Lịch chạy</value></data>
  <data name="Schedules_Departure" xml:space="preserve"><value>Khởi hành</value></data>
  <data name="Schedules_Arrival" xml:space="preserve"><value>Đến nơi</value></data>
  <data name="Schedules_Route" xml:space="preserve"><value>Tuyến</value></data>
  <data name="Schedules_Vehicle" xml:space="preserve"><value>Xe</value></data>
  <data name="Schedules_Driver" xml:space="preserve"><value>Tài xế</value></data>
  <data name="Schedules_Price" xml:space="preserve"><value>Giá vé</value></data>
  <data name="Schedules_Seats" xml:space="preserve"><value>Đã bán / số ghế</value></data>
  <data name="Schedules_Sold" xml:space="preserve"><value>Ghế đã bán</value></data>
  <data name="Schedules_SeatCount" xml:space="preserve"><value>Số ghế</value></data>
  <data name="Schedules_Status" xml:space="preserve"><value>Trạng thái</value></data>
  <data name="Schedules_Date" xml:space="preserve"><value>Ngày</value></data>
  <data name="Schedules_Time" xml:space="preserve"><value>Giờ (HH:mm)</value></data>
  <data name="Schedules_ExportTitle" xml:space="preserve"><value>Danh sách lịch chạy</value></data>
  <data name="Schedules_Cancel" xml:space="preserve"><value>Hủy chuyến</value></data>
  <data name="Schedules_CancelTitle" xml:space="preserve"><value>Hủy chuyến xe</value></data>
  <data name="Schedules_CancelReason" xml:space="preserve"><value>Lý do (mọi vé sẽ bị hủy, vé đã thanh toán được hoàn tiền)</value></data>
  <data name="Schedules_Cancelled" xml:space="preserve"><value>Đã hủy chuyến và hoàn tiền các vé đã thanh toán.</value></data>
  <data name="Schedules_MarkRunning" xml:space="preserve"><value>Bắt đầu chạy</value></data>
  <data name="Schedules_MarkCompleted" xml:space="preserve"><value>Hoàn thành</value></data>
  <data name="Schedules_StatusUpdated" xml:space="preserve"><value>Đã cập nhật trạng thái chuyến.</value></data>
  <data name="ScheduleStatus_Scheduled" xml:space="preserve"><value>Chờ chạy</value></data>
  <data name="ScheduleStatus_Running" xml:space="preserve"><value>Đang chạy</value></data>
  <data name="ScheduleStatus_Completed" xml:space="preserve"><value>Hoàn thành</value></data>
  <data name="ScheduleStatus_Cancelled" xml:space="preserve"><value>Đã hủy</value></data>
  <data name="Val_Time" xml:space="preserve"><value>Nhập dạng HH:mm, ví dụ 07:30.</value></data>
```

- [ ] **Step 8: Build, test, commit**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests` → 0 warnings, `failed: 0`.
Manual: as `operator01`, add a trip (choose route → price/arrival filled), cancel a trip that has tickets → tickets become cancelled with refunds (check in Tickets screen after Task 7).
```bash
git add src/Transport.Presentation.Wpf tests/Transport.UnitTests
git commit -m "feat(ui): add schedules screen"
```

---

### Task 6: Sell-ticket screen (price filter, seat map, payment, printing)

**Files:**
- Create: `Services/ITicketPrintService.cs`, `Services/TicketPrintService.cs`
- Create: `ViewModels/Pages/SeatViewModel.cs`, `ViewModels/Pages/SeatRowViewModel.cs`, `ViewModels/Pages/SellTicketViewModel.cs`
- Create: `Views/Pages/SellTicketPage.xaml(.cs)`
- Modify: `Themes/Controls.xaml` (add `SeatButtonStyle`), `PresentationServices.cs` (register print service + page), `Views/PageTemplates.xaml`, both `.resx`
- Test: `tests/Transport.UnitTests/Fakes/FakeTicketPrintService.cs`, `tests/Transport.UnitTests/Presentation/SellTicketViewModelTests.cs`

**Interfaces:**
- Consumes: `IStatisticsQueries`, `TicketService`, `IPassengerRepository`, `IDialogService.ChoosePaymentMethod/Confirm`, `TimeProvider`.
- Produces:
  - `ITicketPrintService { bool Print(TicketPrintModel ticket); }` (namespace `Transport.Presentation.Wpf.Services`) – also used by Task 7
  - `SeatViewModel(int number, bool isBooked)` with `Number`, `IsBooked`, `IsSelected`; `SeatRowViewModel(IReadOnlyList<SeatViewModel> Left, IReadOnlyList<SeatViewModel> Right)`
  - `SellTicketViewModel(PageServices, IStatisticsQueries, TicketService, IPassengerRepository, ITicketPrintService, TimeProvider)` with `Origin`, `Destination`, `TravelDate`, `MinPrice`, `MaxPrice`, `Trips`, `SelectedTrip`, `SeatRows`, `SelectedSeat`, `Phone`, `FullName`, `IdCard`, `PassengerHint`, `SearchCommand`, `SelectSeatCommand`, `LookupPassengerCommand`, `BookCommand`
  - Print resource keys `Print_*`.

- [ ] **Step 1: Write the fake and the failing tests**

`tests/Transport.UnitTests/Fakes/FakeTicketPrintService.cs`:
```csharp
using Transport.Application.Models;
using Transport.Presentation.Wpf.Services;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeTicketPrintService : ITicketPrintService
{
    public List<TicketPrintModel> Printed { get; } = [];

    public bool Print(TicketPrintModel ticket)
    {
        Printed.Add(ticket);
        return true;
    }
}
```

`tests/Transport.UnitTests/Presentation/SellTicketViewModelTests.cs`:
```csharp
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class SellTicketViewModelTests
{
    private static readonly FixedTimeProvider Time = new(new DateTimeOffset(2026, 10, 17, 8, 0, 0, TimeSpan.Zero));
    private readonly PageTestContext _context = new();
    private readonly FakeStatisticsQueries _statistics = new();
    private readonly FakeTicketRepository _tickets = new() { NextTicketId = 100 };
    private readonly FakePassengerRepository _passengers = new();
    private readonly FakeTicketPrintService _printer = new();

    public SellTicketViewModelTests()
    {
        _statistics.Trips.Add(FakeStatisticsQueries.Trip(7, seatsSold: 2, seatCount: 16, price: 280000,
            departure: new DateTime(2026, 10, 17, 20, 0, 0)));
        _tickets.BookedSeats.UnionWith([1, 2]);
    }

    private SellTicketViewModel Create() =>
        new(_context.Services, _statistics, new TicketService(_tickets), _passengers, _printer, Time);

    private async Task<SellTicketViewModel> CreateWithTripSelectedAsync()
    {
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedTrip = vm.Trips[0];
        return vm;
    }

    [Fact]
    public async Task Search_UsesDatePriceRangeAndOnlyOpenTrips()
    {
        var vm = Create();
        vm.Origin = "Hà Nội";
        vm.MinPrice = 100000;
        vm.MaxPrice = 300000;

        await vm.SearchCommand.ExecuteAsync(null);

        var criteria = _statistics.LastCriteria!;
        Assert.Equal(new DateTime(2026, 10, 17), criteria.From);
        Assert.Equal(new DateTime(2026, 10, 18), criteria.To);
        Assert.True(criteria.OnlyOpenForSale);
        Assert.Equal(100000, criteria.MinPrice);
        Assert.Equal(300000, criteria.MaxPrice);
        Assert.Equal("Hà Nội", criteria.Origin);
        Assert.Single(vm.Trips);
    }

    [Fact]
    public async Task SelectingTrip_BuildsSeatMapWithBookedSeats()
    {
        var vm = await CreateWithTripSelectedAsync();

        Assert.Equal(4, vm.SeatRows.Count);
        Assert.Equal(new[] { 1, 2 }, vm.SeatRows[0].Left.Select(s => s.Number));
        Assert.Equal(new[] { 3, 4 }, vm.SeatRows[0].Right.Select(s => s.Number));
        Assert.True(vm.SeatRows[0].Left[0].IsBooked);
        Assert.False(vm.SeatRows[0].Right[0].IsBooked);
    }

    [Fact]
    public async Task SelectSeat_IgnoresBookedSeats_AndKeepsSingleSelection()
    {
        var vm = await CreateWithTripSelectedAsync();
        var booked = vm.SeatRows[0].Left[0];
        var seat3 = vm.SeatRows[0].Right[0];
        var seat4 = vm.SeatRows[0].Right[1];

        vm.SelectSeatCommand.Execute(booked);
        Assert.Null(vm.SelectedSeat);

        vm.SelectSeatCommand.Execute(seat3);
        vm.SelectSeatCommand.Execute(seat4);
        Assert.Same(seat4, vm.SelectedSeat);
        Assert.False(seat3.IsSelected);
        Assert.True(seat4.IsSelected);
    }

    [Fact]
    public async Task LookupPassenger_FillsReturningPassenger()
    {
        _passengers.Items.Add(TestData.Passenger(1, "Nguyễn Thị Hoa", "0987000001"));
        var vm = Create();
        vm.Phone = "0987000001";

        await vm.LookupPassengerCommand.ExecuteAsync(null);

        Assert.Equal("Nguyễn Thị Hoa", vm.FullName);
        Assert.Equal("Sell_FoundPassenger|Nguyễn Thị Hoa", vm.PassengerHint);
    }

    [Fact]
    public async Task Book_PaysAndPrints_WhenUserAgrees()
    {
        _context.Dialogs.PaymentResult = PaymentMethod.Transfer;
        _context.Dialogs.ConfirmResult = true;
        _tickets.PrintModel = TestData.PrintModel(100);
        var vm = await CreateWithTripSelectedAsync();
        vm.SelectSeatCommand.Execute(vm.SeatRows[1].Left[0]);
        vm.Phone = "0987123456";
        vm.FullName = "Lê Văn Khách";
        Assert.True(vm.BookCommand.CanExecute(null));

        await vm.BookCommand.ExecuteAsync(null);

        Assert.Equal(new BookingRequest(7, "Lê Văn Khách", "0987123456", null, 5), _tickets.LastBooking);
        Assert.Equal((100L, PaymentMethod.Transfer), Assert.Single(_tickets.Payments));
        Assert.Equal(100, Assert.Single(_printer.Printed).TicketId);
        Assert.Equal(new[] { "Sell_Booked|100,5", "Sell_Paid|100" }, _context.Notifications.Successes);
        Assert.Equal(string.Empty, vm.Phone);
        Assert.True(vm.SeatRows[1].Left[0].IsBooked);
    }

    [Fact]
    public async Task Book_WithoutPayment_LeavesTicketUnpaidAndUnprinted()
    {
        _context.Dialogs.PaymentResult = null;
        _context.Dialogs.ConfirmResult = false;
        var vm = await CreateWithTripSelectedAsync();
        vm.SelectSeatCommand.Execute(vm.SeatRows[1].Left[0]);
        vm.Phone = "0987123456";
        vm.FullName = "Lê Văn Khách";

        await vm.BookCommand.ExecuteAsync(null);

        Assert.NotNull(_tickets.LastBooking);
        Assert.Empty(_tickets.Payments);
        Assert.Empty(_printer.Printed);
    }

    [Fact]
    public async Task Book_SeatTakenMeanwhile_ShowsSeatNumberInError()
    {
        _tickets.BookError = new BusinessRuleException(BusinessErrorCode.SeatAlreadyBooked, "Seat 5 is already booked.");
        var vm = await CreateWithTripSelectedAsync();
        vm.SelectSeatCommand.Execute(vm.SeatRows[1].Left[0]);
        vm.Phone = "0987123456";
        vm.FullName = "Lê Văn Khách";

        await vm.BookCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "Err_20003|5" }, _context.Notifications.Errors);
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build errors for `ITicketPrintService`, `SellTicketViewModel`.

- [ ] **Step 3: Write the print service interface and seat view models**

`Services/ITicketPrintService.cs`:
```csharp
using Transport.Application.Models;

namespace Transport.Presentation.Wpf.Services;

public interface ITicketPrintService
{
    // Mở hộp thoại in; false nếu người dùng hủy
    bool Print(TicketPrintModel ticket);
}
```

`ViewModels/Pages/SeatViewModel.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Một ghế trên sơ đồ
public sealed partial class SeatViewModel : ObservableObject
{
    public SeatViewModel(int number, bool isBooked)
    {
        Number = number;
        IsBooked = isBooked;
    }

    public int Number { get; }

    public bool IsBooked { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
```

`ViewModels/Pages/SeatRowViewModel.cs`:
```csharp
namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Một hàng ghế: 2 ghế bên trái lối đi, 2 ghế bên phải
public sealed record SeatRowViewModel(IReadOnlyList<SeatViewModel> Left, IReadOnlyList<SeatViewModel> Right);
```

- [ ] **Step 4: Write `ViewModels/Pages/SellTicketViewModel.cs`**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Bán vé tại quầy: tìm chuyến (kể cả theo khoảng giá) -> chọn ghế -> đặt -> thanh toán -> in vé
public sealed partial class SellTicketViewModel : PageViewModel
{
    private const int SeatsPerRow = 4;

    private readonly IStatisticsQueries _statistics;
    private readonly TicketService _tickets;
    private readonly IPassengerRepository _passengers;
    private readonly ITicketPrintService _printer;

    public SellTicketViewModel(PageServices services, IStatisticsQueries statistics, TicketService tickets,
        IPassengerRepository passengers, ITicketPrintService printer, TimeProvider time)
        : base(services)
    {
        _statistics = statistics;
        _tickets = tickets;
        _passengers = passengers;
        _printer = printer;
        Origin = string.Empty;
        Destination = string.Empty;
        TravelDate = time.GetLocalNow().Date;
        Phone = string.Empty;
        FullName = string.Empty;
        IdCard = string.Empty;
    }

    public ObservableCollection<TripStatistic> Trips { get; } = [];

    public ObservableCollection<SeatRowViewModel> SeatRows { get; } = [];

    [ObservableProperty]
    public partial string Origin { get; set; }

    [ObservableProperty]
    public partial string Destination { get; set; }

    [ObservableProperty]
    public partial DateTime TravelDate { get; set; }

    [ObservableProperty]
    public partial decimal? MinPrice { get; set; }

    [ObservableProperty]
    public partial decimal? MaxPrice { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BookCommand))]
    public partial TripStatistic? SelectedTrip { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BookCommand))]
    public partial SeatViewModel? SelectedSeat { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BookCommand))]
    public partial string Phone { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BookCommand))]
    public partial string FullName { get; set; }

    [ObservableProperty]
    public partial string IdCard { get; set; }

    // "Khách quen ..." / "Khách mới ..." sau khi tra SĐT
    [ObservableProperty]
    public partial string? PassengerHint { get; set; }

    public override Task LoadAsync() => RunBusyAsync(SearchCoreAsync);

    partial void OnSelectedTripChanged(TripStatistic? value) => _ = RunBusyAsync(LoadSeatsAsync);

    [RelayCommand]
    private Task SearchAsync() => RunBusyAsync(SearchCoreAsync);

    [RelayCommand]
    private void SelectSeat(SeatViewModel? seat)
    {
        if (seat is null || seat.IsBooked)
        {
            return;
        }

        if (SelectedSeat is not null)
        {
            SelectedSeat.IsSelected = false;
        }

        seat.IsSelected = true;
        SelectedSeat = seat;
    }

    [RelayCommand]
    private Task LookupPassengerAsync() => RunBusyAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Phone))
        {
            return;
        }

        var passenger = await _passengers.FindByPhoneAsync(Phone.Trim());
        if (passenger is null)
        {
            PassengerHint = Loc["Sell_NewPassenger"];
            return;
        }

        FullName = passenger.FullName;
        IdCard = passenger.IdCard ?? string.Empty;
        PassengerHint = Loc.Format("Sell_FoundPassenger", passenger.FullName);
    });

    [RelayCommand(CanExecute = nameof(CanBook))]
    private Task BookAsync()
    {
        var trip = SelectedTrip!;
        var seat = SelectedSeat!;
        // Số ghế được truyền vào bản dịch lỗi "Ghế {0} đã có người đặt."
        return RunBusyAsync(async () =>
        {
            var ticketId = await _tickets.BookAsync(new BookingRequest(trip.ScheduleId, FullName, Phone, IdCard, seat.Number));
            Notify.ShowSuccess(Loc.Format("Sell_Booked", ticketId, seat.Number));

            if (Dialogs.ChoosePaymentMethod(trip.TicketPrice) is { } method)
            {
                await _tickets.PayAsync(ticketId, method);
                Notify.ShowSuccess(Loc.Format("Sell_Paid", ticketId));
            }

            if (Dialogs.Confirm(Loc["Sell_ConfirmPrint"]) && await _tickets.GetPrintModelAsync(ticketId) is { } ticket)
            {
                _printer.Print(ticket);
            }

            Phone = string.Empty;
            FullName = string.Empty;
            IdCard = string.Empty;
            PassengerHint = null;
            await SearchCoreAsync();
        }, seat.Number);
    }

    private bool CanBook() =>
        SelectedTrip is not null && SelectedSeat is not null
        && !string.IsNullOrWhiteSpace(Phone) && !string.IsNullOrWhiteSpace(FullName);

    // Tải lại danh sách chuyến; giữ chuyến đang chọn và nạp lại sơ đồ ghế của nó
    private async Task SearchCoreAsync()
    {
        var selectedId = SelectedTrip?.ScheduleId;
        SelectedTrip = null;
        var trips = await _statistics.GetTripsAsync(new TripSearchCriteria
        {
            From = TravelDate.Date,
            To = TravelDate.Date.AddDays(1),
            Origin = Origin,
            Destination = Destination,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            OnlyOpenForSale = true,
        });
        Trips.Clear();
        foreach (var trip in trips)
        {
            Trips.Add(trip);
        }

        SelectedTrip = Trips.FirstOrDefault(t => t.ScheduleId == selectedId);
    }

    private async Task LoadSeatsAsync()
    {
        SelectedSeat = null;
        SeatRows.Clear();
        if (SelectedTrip is not { } trip)
        {
            return;
        }

        var booked = await _tickets.GetBookedSeatsAsync(trip.ScheduleId);
        var seats = Enumerable.Range(1, trip.SeatCount).Select(n => new SeatViewModel(n, booked.Contains(n)));
        foreach (var row in seats.Chunk(SeatsPerRow))
        {
            SeatRows.Add(new SeatRowViewModel([.. row.Take(2)], [.. row.Skip(2)]));
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 6: Write `Services/TicketPrintService.cs`**

```csharp
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Transport.Application.Models;
using Transport.Presentation.Wpf.Localization;

namespace Transport.Presentation.Wpf.Services;

// In vé khổ A6 dọc bằng FlowDocument; chọn "Microsoft Print to PDF" để ra file PDF
public sealed class TicketPrintService(ILocalizationService localization, TimeProvider time) : ITicketPrintService
{
    private const double PageWidth = 397;   // 105 mm ở 96 dpi
    private const double PageHeight = 559;  // 148 mm
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly Brush Navy = new SolidColorBrush(Color.FromRgb(0x0B, 0x3D, 0x91));

    public bool Print(TicketPrintModel ticket)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        var document = BuildDocument(ticket);
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Ticket {ticket.TicketId}");
        return true;
    }

    private FlowDocument BuildDocument(TicketPrintModel ticket)
    {
        var document = new FlowDocument
        {
            PageWidth = PageWidth,
            PageHeight = PageHeight,
            ColumnWidth = PageWidth,
            PagePadding = new Thickness(28),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
        };

        document.Blocks.Add(new Paragraph(new Run("HaUI")) { FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Navy, Margin = new Thickness(0) });
        document.Blocks.Add(new Paragraph(new Run(localization["Print_Title"]))
        {
            FontSize = 20, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Foreground = Navy, Margin = new Thickness(0, 8, 0, 0),
        });
        document.Blocks.Add(new Paragraph(new Run($"{localization["Print_TicketNo"]}: {ticket.TicketId}"))
        {
            FontSize = 16, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 4, 0, 12),
        });

        var table = new Table { CellSpacing = 0 };
        table.Columns.Add(new TableColumn { Width = new GridLength(110) });
        table.Columns.Add(new TableColumn());
        var rows = new TableRowGroup();
        table.RowGroups.Add(rows);

        void AddRow(string labelKey, string value, bool emphasize = false)
        {
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(new Run(localization[labelKey])) { Foreground = Brushes.Gray, Margin = new Thickness(0, 3, 0, 3) }));
            row.Cells.Add(new TableCell(new Paragraph(new Run(value))
            {
                FontWeight = emphasize ? FontWeights.Bold : FontWeights.Normal,
                FontSize = emphasize ? 14 : 12,
                Margin = new Thickness(0, 3, 0, 3),
            }));
            rows.Rows.Add(row);
        }

        AddRow("Print_Route", ticket.RouteName, emphasize: true);
        AddRow("Print_Departure", ticket.DepartureTime.ToString("dd/MM/yyyy HH:mm", Vietnamese), emphasize: true);
        AddRow("Print_Arrival", ticket.ArrivalTime.ToString("dd/MM/yyyy HH:mm", Vietnamese));
        AddRow("Print_Vehicle", $"{ticket.PlateNumber} – {ticket.VehicleType}");
        AddRow("Print_Seat", ticket.SeatNo.ToString(Vietnamese), emphasize: true);
        AddRow("Print_Passenger", $"{ticket.PassengerName} – {ticket.Phone}");
        AddRow("Print_Price", string.Format(Vietnamese, "{0:N0} ₫", ticket.Price), emphasize: true);
        AddRow("Print_Status", localization[$"TicketStatus_{ticket.Status}"]);
        AddRow("Print_SoldBy", ticket.SoldBy);
        AddRow("Print_PrintedAt", time.GetLocalNow().ToString("dd/MM/yyyy HH:mm", Vietnamese));
        document.Blocks.Add(table);

        document.Blocks.Add(new Paragraph(new Run(localization["Print_Footer"]))
        {
            FontStyle = FontStyles.Italic, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 16, 0, 0),
        });
        return document;
    }
}
```

In `PresentationServices.AddPresentation`, after the `IWindowService` registration, add:
```csharp
        services.AddSingleton<ITicketPrintService, TicketPrintService>();
```

- [ ] **Step 7: Add `SeatButtonStyle` to `Themes/Controls.xaml`**

```xml
    <!-- Ghế trên sơ đồ; DataContext = SeatViewModel -->
    <Style x:Key="SeatButtonStyle" TargetType="Button">
        <Setter Property="Width" Value="44" />
        <Setter Property="Height" Value="40" />
        <Setter Property="Margin" Value="4" />
        <Setter Property="Foreground" Value="White" />
        <Setter Property="FontWeight" Value="SemiBold" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Background" Value="{StaticResource SeatFreeBrush}" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="Root" Background="{TemplateBinding Background}" CornerRadius="8,8,4,4"
                            BorderThickness="2" BorderBrush="Transparent">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Root" Property="Opacity" Value="0.85" />
                        </Trigger>
                        <Trigger Property="IsKeyboardFocused" Value="True">
                            <Setter TargetName="Root" Property="BorderBrush" Value="{StaticResource HaUiGoldBrush}" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
        <Style.Triggers>
            <DataTrigger Binding="{Binding IsSelected}" Value="True">
                <Setter Property="Background" Value="{StaticResource SeatSelectedBrush}" />
            </DataTrigger>
            <DataTrigger Binding="{Binding IsBooked}" Value="True">
                <Setter Property="Background" Value="{StaticResource SeatBookedBrush}" />
                <Setter Property="Cursor" Value="No" />
            </DataTrigger>
        </Style.Triggers>
    </Style>
```

- [ ] **Step 8: Write `Views/Pages/SellTicketPage.xaml`** (+ code-behind)

```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Pages.SellTicketPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls"
             xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"
             mc:Ignorable="d" d:DesignWidth="1100" d:DesignHeight="680"
             d:DataContext="{d:DesignInstance Type=vm:SellTicketViewModel}">
    <UserControl.Resources>
        <ItemsPanelTemplate x:Key="HorizontalPanel">
            <StackPanel Orientation="Horizontal" />
        </ItemsPanelTemplate>
        <DataTemplate x:Key="SeatTemplate" DataType="{x:Type vm:SeatViewModel}">
            <Button Style="{StaticResource SeatButtonStyle}" Content="{Binding Number}"
                    Command="{Binding DataContext.SelectSeatCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                    CommandParameter="{Binding}" AutomationProperties.Name="{Binding Number}" />
        </DataTemplate>
    </UserControl.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition />
            <ColumnDefinition Width="Auto" />
            <ColumnDefinition Width="340" />
        </Grid.ColumnDefinitions>

        <!-- Bộ lọc chuyến -->
        <Border Grid.ColumnSpan="3" Style="{StaticResource CardStyle}" Margin="0,0,0,16">
            <WrapPanel>
                <StackPanel Width="170" Margin="0,0,12,0">
                    <TextBlock Text="{l:Loc Sell_Origin}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
                    <TextBox Text="{Binding Origin, UpdateSourceTrigger=PropertyChanged}" c:Shortcut.Gesture="Ctrl+F"
                             AutomationProperties.Name="{l:Loc Sell_Origin}" />
                </StackPanel>
                <StackPanel Width="170" Margin="0,0,12,0">
                    <TextBlock Text="{l:Loc Sell_Destination}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
                    <TextBox Text="{Binding Destination, UpdateSourceTrigger=PropertyChanged}" AutomationProperties.Name="{l:Loc Sell_Destination}" />
                </StackPanel>
                <StackPanel Width="160" Margin="0,0,12,0">
                    <TextBlock Text="{l:Loc Sell_Date}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
                    <DatePicker SelectedDate="{Binding TravelDate}" AutomationProperties.Name="{l:Loc Sell_Date}" />
                </StackPanel>
                <StackPanel Width="130" Margin="0,0,12,0">
                    <TextBlock Text="{l:Loc Sell_MinPrice}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
                    <TextBox Text="{Binding MinPrice, UpdateSourceTrigger=PropertyChanged, TargetNullValue=''}" AutomationProperties.Name="{l:Loc Sell_MinPrice}" />
                </StackPanel>
                <StackPanel Width="130" Margin="0,0,12,0">
                    <TextBlock Text="{l:Loc Sell_MaxPrice}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
                    <TextBox Text="{Binding MaxPrice, UpdateSourceTrigger=PropertyChanged, TargetNullValue=''}" AutomationProperties.Name="{l:Loc Sell_MaxPrice}" />
                </StackPanel>
                <Button Content="{l:Loc Btn_Search}" Command="{Binding SearchCommand}" Style="{StaticResource PrimaryButtonStyle}"
                        VerticalAlignment="Bottom" MinWidth="110" c:Shortcut.Gesture="F5" />
            </WrapPanel>
        </Border>

        <!-- Chuyến còn vé -->
        <Border Grid.Row="1" Style="{StaticResource CardStyle}">
            <DockPanel>
                <TextBlock DockPanel.Dock="Top" Text="{l:Loc Sell_Trips}" Style="{StaticResource SectionTitleStyle}" />
                <DataGrid Style="{StaticResource ListGridStyle}" ItemsSource="{Binding Trips}" SelectedItem="{Binding SelectedTrip}"
                          AutomationProperties.Name="{l:Loc Sell_Trips}">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="{l:Loc Schedules_Departure}" Binding="{Binding DepartureTime, StringFormat={}{0:HH:mm}}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Schedules_Route}" Binding="{Binding RouteName}" Width="2*" />
                        <DataGridTextColumn Header="{l:Loc Vehicles_Type}" Binding="{Binding VehicleType}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Schedules_Vehicle}" Binding="{Binding PlateNumber}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Schedules_Price}" Binding="{Binding TicketPrice, StringFormat={}{0:N0} ₫}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Sell_SeatsLeft}" Binding="{Binding SeatsAvailable}" Width="Auto" />
                    </DataGrid.Columns>
                </DataGrid>
            </DockPanel>
        </Border>

        <!-- Sơ đồ ghế -->
        <Border Grid.Row="1" Grid.Column="1" Style="{StaticResource CardStyle}" Margin="16,0" MinWidth="280">
            <DockPanel>
                <TextBlock DockPanel.Dock="Top" Text="{l:Loc Sell_SeatMap}" Style="{StaticResource SectionTitleStyle}" />
                <WrapPanel DockPanel.Dock="Bottom" Margin="0,12,0,0">
                    <Border Width="16" Height="16" CornerRadius="3" Background="{StaticResource SeatFreeBrush}" Margin="0,0,6,0" />
                    <TextBlock Text="{l:Loc Sell_LegendFree}" Margin="0,0,16,0" />
                    <Border Width="16" Height="16" CornerRadius="3" Background="{StaticResource SeatBookedBrush}" Margin="0,0,6,0" />
                    <TextBlock Text="{l:Loc Sell_LegendBooked}" Margin="0,0,16,0" />
                    <Border Width="16" Height="16" CornerRadius="3" Background="{StaticResource SeatSelectedBrush}" Margin="0,0,6,0" />
                    <TextBlock Text="{l:Loc Sell_LegendSelected}" />
                </WrapPanel>
                <ScrollViewer VerticalScrollBarVisibility="Auto">
                    <ItemsControl ItemsSource="{Binding SeatRows}" AutomationProperties.Name="{l:Loc Sell_SeatMap}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate DataType="{x:Type vm:SeatRowViewModel}">
                                <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                                    <ItemsControl ItemsSource="{Binding Left}" ItemsPanel="{StaticResource HorizontalPanel}"
                                                  ItemTemplate="{StaticResource SeatTemplate}" />
                                    <Border Width="28" />
                                    <ItemsControl ItemsSource="{Binding Right}" ItemsPanel="{StaticResource HorizontalPanel}"
                                                  ItemTemplate="{StaticResource SeatTemplate}" />
                                </StackPanel>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </ScrollViewer>
            </DockPanel>
        </Border>

        <!-- Khách hàng + đặt vé -->
        <Border Grid.Row="1" Grid.Column="2" Style="{StaticResource CardStyle}">
            <StackPanel>
                <TextBlock Text="{l:Loc Sell_Passenger}" Style="{StaticResource SectionTitleStyle}" />
                <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
                    <TextBlock Text="{l:Loc Sell_SelectedSeat}" Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
                    <TextBlock Text="{Binding SelectedSeat.Number, FallbackValue=–}" FontWeight="Bold" FontSize="16"
                               Foreground="{StaticResource HaUiNavyBrush}" Margin="8,0,0,0" />
                    <TextBlock Text="{Binding SelectedTrip.TicketPrice, StringFormat={}· {0:N0} ₫, FallbackValue=''}"
                               FontWeight="SemiBold" Margin="8,0,0,0" />
                </StackPanel>

                <TextBlock Text="{l:Loc Sell_Phone}" Style="{StaticResource FieldLabelStyle}" />
                <DockPanel>
                    <Button DockPanel.Dock="Right" Content="{l:Loc Sell_Lookup}" Command="{Binding LookupPassengerCommand}" Margin="8,0,0,0" />
                    <TextBox Text="{Binding Phone, UpdateSourceTrigger=PropertyChanged}" MaxLength="10" AutomationProperties.Name="{l:Loc Sell_Phone}" />
                </DockPanel>
                <TextBlock Text="{Binding PassengerHint}" Foreground="{StaticResource HaUiNavyBrush}" TextWrapping="Wrap" Margin="0,4,0,0"
                           Visibility="{Binding PassengerHint, Converter={StaticResource NullToCollapsed}}" />

                <TextBlock Text="{l:Loc Sell_FullName}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding FullName, UpdateSourceTrigger=PropertyChanged}" AutomationProperties.Name="{l:Loc Sell_FullName}" />
                <TextBlock Text="{l:Loc Sell_IdCard}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding IdCard, UpdateSourceTrigger=PropertyChanged}" MaxLength="12" AutomationProperties.Name="{l:Loc Sell_IdCard}" />

                <Button Content="{l:Loc Sell_Book}" Command="{Binding BookCommand}" Style="{StaticResource PrimaryButtonStyle}"
                        HorizontalAlignment="Stretch" Margin="0,24,0,0" c:Shortcut.Gesture="Ctrl+S" />
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 9: Register** – `PageTemplates.xaml`: `<DataTemplate DataType="{x:Type vm:SellTicketViewModel}"><pages:SellTicketPage /></DataTemplate>`; `AddPages`: `services.AddPage<SellTicketViewModel>("Menu_SellTicket", "", 20);`

- [ ] **Step 10: Add resource keys**

`Strings.resx`:
```xml
  <data name="Menu_SellTicket" xml:space="preserve"><value>Sell tickets</value></data>
  <data name="Sell_Origin" xml:space="preserve"><value>From</value></data>
  <data name="Sell_Destination" xml:space="preserve"><value>To</value></data>
  <data name="Sell_Date" xml:space="preserve"><value>Travel date</value></data>
  <data name="Sell_MinPrice" xml:space="preserve"><value>Price from</value></data>
  <data name="Sell_MaxPrice" xml:space="preserve"><value>Price to</value></data>
  <data name="Sell_Trips" xml:space="preserve"><value>Trips open for sale</value></data>
  <data name="Sell_SeatsLeft" xml:space="preserve"><value>Seats left</value></data>
  <data name="Sell_SeatMap" xml:space="preserve"><value>Seat map</value></data>
  <data name="Sell_LegendFree" xml:space="preserve"><value>Free</value></data>
  <data name="Sell_LegendBooked" xml:space="preserve"><value>Booked</value></data>
  <data name="Sell_LegendSelected" xml:space="preserve"><value>Selected</value></data>
  <data name="Sell_Passenger" xml:space="preserve"><value>Passenger</value></data>
  <data name="Sell_SelectedSeat" xml:space="preserve"><value>Selected seat:</value></data>
  <data name="Sell_Phone" xml:space="preserve"><value>Phone</value></data>
  <data name="Sell_FullName" xml:space="preserve"><value>Full name</value></data>
  <data name="Sell_IdCard" xml:space="preserve"><value>ID card (optional)</value></data>
  <data name="Sell_Lookup" xml:space="preserve"><value>Find</value></data>
  <data name="Sell_NewPassenger" xml:space="preserve"><value>New passenger – please enter the full name.</value></data>
  <data name="Sell_FoundPassenger" xml:space="preserve"><value>Returning passenger: {0}</value></data>
  <data name="Sell_Book" xml:space="preserve"><value>Book ticket (Ctrl+S)</value></data>
  <data name="Sell_Booked" xml:space="preserve"><value>Ticket #{0} booked (seat {1}).</value></data>
  <data name="Sell_Paid" xml:space="preserve"><value>Ticket #{0} paid.</value></data>
  <data name="Sell_ConfirmPrint" xml:space="preserve"><value>Print the ticket now?</value></data>
  <data name="Print_Title" xml:space="preserve"><value>BUS TICKET</value></data>
  <data name="Print_TicketNo" xml:space="preserve"><value>Ticket No.</value></data>
  <data name="Print_Route" xml:space="preserve"><value>Route</value></data>
  <data name="Print_Departure" xml:space="preserve"><value>Departure</value></data>
  <data name="Print_Arrival" xml:space="preserve"><value>Arrival (est.)</value></data>
  <data name="Print_Vehicle" xml:space="preserve"><value>Vehicle</value></data>
  <data name="Print_Seat" xml:space="preserve"><value>Seat</value></data>
  <data name="Print_Passenger" xml:space="preserve"><value>Passenger</value></data>
  <data name="Print_Price" xml:space="preserve"><value>Price</value></data>
  <data name="Print_Status" xml:space="preserve"><value>Payment</value></data>
  <data name="Print_SoldBy" xml:space="preserve"><value>Sold by</value></data>
  <data name="Print_PrintedAt" xml:space="preserve"><value>Printed at</value></data>
  <data name="Print_Footer" xml:space="preserve"><value>Please be at the station 15 minutes before departure.</value></data>
```

`Strings.vi.resx`:
```xml
  <data name="Menu_SellTicket" xml:space="preserve"><value>Bán vé</value></data>
  <data name="Sell_Origin" xml:space="preserve"><value>Điểm đi</value></data>
  <data name="Sell_Destination" xml:space="preserve"><value>Điểm đến</value></data>
  <data name="Sell_Date" xml:space="preserve"><value>Ngày đi</value></data>
  <data name="Sell_MinPrice" xml:space="preserve"><value>Giá từ</value></data>
  <data name="Sell_MaxPrice" xml:space="preserve"><value>Giá đến</value></data>
  <data name="Sell_Trips" xml:space="preserve"><value>Chuyến còn mở bán</value></data>
  <data name="Sell_SeatsLeft" xml:space="preserve"><value>Ghế trống</value></data>
  <data name="Sell_SeatMap" xml:space="preserve"><value>Sơ đồ ghế</value></data>
  <data name="Sell_LegendFree" xml:space="preserve"><value>Trống</value></data>
  <data name="Sell_LegendBooked" xml:space="preserve"><value>Đã đặt</value></data>
  <data name="Sell_LegendSelected" xml:space="preserve"><value>Đang chọn</value></data>
  <data name="Sell_Passenger" xml:space="preserve"><value>Hành khách</value></data>
  <data name="Sell_SelectedSeat" xml:space="preserve"><value>Ghế đang chọn:</value></data>
  <data name="Sell_Phone" xml:space="preserve"><value>Số điện thoại</value></data>
  <data name="Sell_FullName" xml:space="preserve"><value>Họ tên</value></data>
  <data name="Sell_IdCard" xml:space="preserve"><value>CCCD (không bắt buộc)</value></data>
  <data name="Sell_Lookup" xml:space="preserve"><value>Tìm khách</value></data>
  <data name="Sell_NewPassenger" xml:space="preserve"><value>Khách mới – vui lòng nhập họ tên.</value></data>
  <data name="Sell_FoundPassenger" xml:space="preserve"><value>Khách quen: {0}</value></data>
  <data name="Sell_Book" xml:space="preserve"><value>Đặt vé (Ctrl+S)</value></data>
  <data name="Sell_Booked" xml:space="preserve"><value>Đã đặt vé #{0} (ghế {1}).</value></data>
  <data name="Sell_Paid" xml:space="preserve"><value>Vé #{0} đã thanh toán.</value></data>
  <data name="Sell_ConfirmPrint" xml:space="preserve"><value>In vé ngay bây giờ?</value></data>
  <data name="Print_Title" xml:space="preserve"><value>VÉ XE KHÁCH</value></data>
  <data name="Print_TicketNo" xml:space="preserve"><value>Số vé</value></data>
  <data name="Print_Route" xml:space="preserve"><value>Tuyến</value></data>
  <data name="Print_Departure" xml:space="preserve"><value>Khởi hành</value></data>
  <data name="Print_Arrival" xml:space="preserve"><value>Dự kiến đến</value></data>
  <data name="Print_Vehicle" xml:space="preserve"><value>Xe</value></data>
  <data name="Print_Seat" xml:space="preserve"><value>Ghế</value></data>
  <data name="Print_Passenger" xml:space="preserve"><value>Hành khách</value></data>
  <data name="Print_Price" xml:space="preserve"><value>Giá vé</value></data>
  <data name="Print_Status" xml:space="preserve"><value>Thanh toán</value></data>
  <data name="Print_SoldBy" xml:space="preserve"><value>Người bán</value></data>
  <data name="Print_PrintedAt" xml:space="preserve"><value>In lúc</value></data>
  <data name="Print_Footer" xml:space="preserve"><value>Quý khách vui lòng có mặt tại bến trước giờ khởi hành 15 phút.</value></data>
```

- [ ] **Step 11: Build, test, manual check, commit**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests` → 0 warnings, `failed: 0`.
Manual (as `seller01`): Bán vé → pick tomorrow → choose a trip → red seats appear for seeded tickets → click a green seat (turns blue) → type phone `0987000001` → "Tìm khách" fills "Nguyễn Thị Hoa" → "Đặt vé" → payment dialog → choose "Chuyển khoản" → "In vé ngay?" → Yes → choose "Microsoft Print to PDF" and save; open the PDF: A6 ticket with all fields. Try typing price range 200000–300000 → only matching trips remain.
```bash
git add src/Transport.Presentation.Wpf tests/Transport.UnitTests
git commit -m "feat(ui): add sell-ticket screen with seat map, payment and printing"
```

---

### Task 7: Tickets screen (search, pay, cancel, print, export)

**Files:**
- Create: `ViewModels/Pages/TicketsViewModel.cs`, `Views/Pages/TicketsPage.xaml(.cs)`
- Modify: `Views/PageTemplates.xaml`, `PresentationServices.cs`, both `.resx`
- Test: `tests/Transport.UnitTests/Presentation/TicketsViewModelTests.cs`

**Interfaces:**
- Consumes: `TicketService`, `ITicketPrintService`, `FilterOption<T>`, `TimeProvider`.
- Produces: `TicketsViewModel(PageServices, TicketService, ITicketPrintService, TimeProvider)` with `Keyword`, `FromDate`, `ToDate`, `StatusOptions`, `SelectedStatus`, `Tickets`, `SelectedTicket`, `SearchCommand`, `PayCommand`, `CancelTicketCommand`, `PrintCommand`, `ExportToExcelCommand`.

- [ ] **Step 1: Write the failing test**

```csharp
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class TicketsViewModelTests
{
    private static readonly FixedTimeProvider Time = new(new DateTimeOffset(2026, 10, 17, 8, 0, 0, TimeSpan.Zero));
    private readonly PageTestContext _context = new();
    private readonly FakeTicketRepository _tickets = new();
    private readonly FakeTicketPrintService _printer = new();

    public TicketsViewModelTests()
    {
        _tickets.SearchResult.Add(TestData.Ticket(1, TicketStatus.Booked));
        _tickets.SearchResult.Add(TestData.Ticket(2, TicketStatus.Paid, 280000));
        _tickets.SearchResult.Add(TestData.Ticket(3, TicketStatus.Cancelled));
    }

    private TicketsViewModel Create() => new(_context.Services, new TicketService(_tickets), _printer, Time);

    [Fact]
    public async Task Load_SearchesLastAndNextSevenDays()
    {
        var vm = Create();

        await vm.LoadAsync();

        var criteria = _tickets.LastSearch!;
        Assert.Equal(new DateTime(2026, 10, 10), criteria.From);
        Assert.Equal(new DateTime(2026, 10, 25), criteria.To);
        Assert.Null(criteria.Status);
        Assert.Equal(3, vm.Tickets.Count);
    }

    [Fact]
    public async Task Search_PassesKeywordAndStatus()
    {
        var vm = Create();
        vm.Keyword = " 0987 ";
        vm.SelectedStatus = vm.StatusOptions[2];

        await vm.SearchCommand.ExecuteAsync(null);

        Assert.Equal("0987", _tickets.LastSearch!.Keyword);
        Assert.Equal(TicketStatus.Paid, _tickets.LastSearch.Status);
        Assert.Equal("Lbl_All", vm.StatusOptions[0].Label);
    }

    [Fact]
    public async Task Pay_OnlyForUnpaidTickets_UsingChosenMethod()
    {
        var vm = Create();
        await vm.LoadAsync();

        vm.SelectedTicket = vm.Tickets[1];
        Assert.False(vm.PayCommand.CanExecute(null));
        vm.SelectedTicket = vm.Tickets[0];
        Assert.True(vm.PayCommand.CanExecute(null));

        _context.Dialogs.PaymentResult = PaymentMethod.Card;
        await vm.PayCommand.ExecuteAsync(null);

        Assert.Equal((1L, PaymentMethod.Card), Assert.Single(_tickets.Payments));
    }

    [Fact]
    public async Task CancelPaidTicket_AsksReasonAndReportsRefund()
    {
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedTicket = vm.Tickets[1];
        _context.Dialogs.PromptTextResult = "Khách đổi ý";

        await vm.CancelTicketCommand.ExecuteAsync(null);

        Assert.Equal((2L, "Khách đổi ý"), Assert.Single(_tickets.Cancellations));
        Assert.Equal("Tickets_CancelTitle|2", _context.Dialogs.Prompts[0]);
        Assert.Equal(new[] { "Tickets_CancelledRefund|280000" }, _context.Notifications.Successes);
    }

    [Fact]
    public async Task CancelledTicket_CannotBeCancelledOrPrinted()
    {
        var vm = Create();
        await vm.LoadAsync();

        vm.SelectedTicket = vm.Tickets[2];

        Assert.False(vm.CancelTicketCommand.CanExecute(null));
        Assert.False(vm.PrintCommand.CanExecute(null));
    }

    [Fact]
    public async Task Print_SendsTicketToPrinter()
    {
        _tickets.PrintModel = TestData.PrintModel(1);
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedTicket = vm.Tickets[0];

        await vm.PrintCommand.ExecuteAsync(null);

        Assert.Equal(1, Assert.Single(_printer.Printed).TicketId);
    }

    [Fact]
    public async Task Export_WritesAllTicketColumns()
    {
        var vm = Create();
        await vm.LoadAsync();

        await vm.ExportToExcelCommand.ExecuteAsync(null);

        Assert.Equal("Tickets_ExportTitle", _context.Exporter.Title);
        Assert.Equal(10, _context.Exporter.Headers.Count);
        Assert.Equal(3, _context.Exporter.RowCount);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build error `TicketsViewModel` not found.

- [ ] **Step 3: Write `ViewModels/Pages/TicketsViewModel.cs`**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Tra cứu vé, thanh toán vé chưa trả, hủy vé (hoàn tiền nếu đã trả), in vé
public sealed partial class TicketsViewModel : PageViewModel
{
    private readonly TicketService _tickets;
    private readonly ITicketPrintService _printer;

    public TicketsViewModel(PageServices services, TicketService tickets, ITicketPrintService printer, TimeProvider time)
        : base(services)
    {
        _tickets = tickets;
        _printer = printer;
        var today = time.GetLocalNow().Date;
        Keyword = string.Empty;
        FromDate = today.AddDays(-7);
        ToDate = today.AddDays(7);
        StatusOptions =
        [
            new FilterOption<TicketStatus?>(null, Loc["Lbl_All"]),
            .. Enum.GetValues<TicketStatus>().Select(s => new FilterOption<TicketStatus?>(s, Loc[$"TicketStatus_{s}"])),
        ];
        SelectedStatus = StatusOptions[0];
    }

    public ObservableCollection<TicketListItem> Tickets { get; } = [];

    public IReadOnlyList<FilterOption<TicketStatus?>> StatusOptions { get; }

    [ObservableProperty]
    public partial string Keyword { get; set; }

    [ObservableProperty]
    public partial DateTime FromDate { get; set; }

    [ObservableProperty]
    public partial DateTime ToDate { get; set; }

    [ObservableProperty]
    public partial FilterOption<TicketStatus?> SelectedStatus { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PayCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelTicketCommand))]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    public partial TicketListItem? SelectedTicket { get; set; }

    public override Task LoadAsync() => RunBusyAsync(SearchCoreAsync);

    [RelayCommand]
    private Task SearchAsync() => RunBusyAsync(SearchCoreAsync);

    [RelayCommand(CanExecute = nameof(CanPay))]
    private Task PayAsync() => RunBusyAsync(async () =>
    {
        var ticket = SelectedTicket!;
        if (Dialogs.ChoosePaymentMethod(ticket.Price) is not { } method)
        {
            return;
        }

        await _tickets.PayAsync(ticket.TicketId, method);
        Notify.ShowSuccess(Loc.Format("Sell_Paid", ticket.TicketId));
        await SearchCoreAsync();
    });

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private Task CancelTicketAsync() => RunBusyAsync(async () =>
    {
        var ticket = SelectedTicket!;
        var reason = Dialogs.PromptText(Loc.Format("Tickets_CancelTitle", ticket.TicketId), Loc["Tickets_CancelReason"]);
        if (reason is null)
        {
            return;
        }

        await _tickets.CancelAsync(ticket.TicketId, reason);
        Notify.ShowSuccess(ticket.Status == TicketStatus.Paid
            ? Loc.Format("Tickets_CancelledRefund", ticket.Price)
            : Loc["Tickets_Cancelled"]);
        await SearchCoreAsync();
    });

    [RelayCommand(CanExecute = nameof(CanPrint))]
    private Task PrintAsync() => RunBusyAsync(async () =>
    {
        if (await _tickets.GetPrintModelAsync(SelectedTicket!.TicketId) is { } model)
        {
            _printer.Print(model);
        }
    });

    [RelayCommand]
    private Task ExportToExcelAsync() => ExportAsync(Tickets.ToList(),
    [
        new("Tickets_Id", t => t.TicketId),
        new("Tickets_Route", t => t.RouteName),
        new("Tickets_Departure", t => t.DepartureTime, "dd/MM/yyyy HH:mm"),
        new("Tickets_Vehicle", t => t.PlateNumber),
        new("Tickets_Seat", t => t.SeatNo),
        new("Tickets_Passenger", t => t.PassengerName),
        new("Tickets_Phone", t => t.Phone),
        new("Tickets_Price", t => t.Price, "#,##0"),
        new("Tickets_Status", t => Loc[$"TicketStatus_{t.Status}"]),
        new("Tickets_SoldBy", t => t.SoldBy),
    ], "Tickets_ExportTitle");

    private bool CanPay() => SelectedTicket?.Status == TicketStatus.Booked;

    private bool CanCancel() => SelectedTicket is { Status: not TicketStatus.Cancelled };

    private bool CanPrint() => SelectedTicket is { Status: not TicketStatus.Cancelled };

    private async Task SearchCoreAsync()
    {
        var items = await _tickets.SearchAsync(new TicketSearchCriteria
        {
            Keyword = Keyword,
            Status = SelectedStatus.Value,
            From = FromDate.Date,
            To = ToDate.Date.AddDays(1),
        });
        Tickets.Clear();
        foreach (var item in items)
        {
            Tickets.Add(item);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 5: Write `Views/Pages/TicketsPage.xaml`** (+ code-behind)

```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Pages.TicketsPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls"
             xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"
             mc:Ignorable="d" d:DesignWidth="1100" d:DesignHeight="680"
             d:DataContext="{d:DesignInstance Type=vm:TicketsViewModel}">
    <Border Style="{StaticResource CardStyle}">
        <DockPanel>
            <!-- Bộ lọc -->
            <WrapPanel DockPanel.Dock="Top" Margin="0,0,0,12">
                <StackPanel Width="260" Margin="0,0,12,0">
                    <TextBlock Text="{l:Loc Tickets_Keyword}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
                    <TextBox Text="{Binding Keyword, UpdateSourceTrigger=PropertyChanged}" c:Shortcut.Gesture="Ctrl+F"
                             AutomationProperties.Name="{l:Loc Tickets_Keyword}" />
                </StackPanel>
                <StackPanel Width="170" Margin="0,0,12,0">
                    <TextBlock Text="{l:Loc Tickets_Status}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
                    <ComboBox ItemsSource="{Binding StatusOptions}" SelectedItem="{Binding SelectedStatus}" DisplayMemberPath="Label"
                              AutomationProperties.Name="{l:Loc Tickets_Status}" />
                </StackPanel>
                <StackPanel Width="150" Margin="0,0,12,0">
                    <TextBlock Text="{l:Loc Lbl_From}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
                    <DatePicker SelectedDate="{Binding FromDate}" AutomationProperties.Name="{l:Loc Lbl_From}" />
                </StackPanel>
                <StackPanel Width="150" Margin="0,0,12,0">
                    <TextBlock Text="{l:Loc Lbl_To}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
                    <DatePicker SelectedDate="{Binding ToDate}" AutomationProperties.Name="{l:Loc Lbl_To}" />
                </StackPanel>
                <Button Content="{l:Loc Btn_Search}" Command="{Binding SearchCommand}" Style="{StaticResource PrimaryButtonStyle}"
                        VerticalAlignment="Bottom" MinWidth="100" IsDefault="True" c:Shortcut.Gesture="F5" />
            </WrapPanel>

            <!-- Thao tác trên vé đang chọn -->
            <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,12">
                <Button Content="{l:Loc Tickets_Pay}" Command="{Binding PayCommand}" Margin="0,0,8,0" />
                <Button Content="{l:Loc Tickets_Cancel}" Command="{Binding CancelTicketCommand}" Margin="0,0,8,0" />
                <Button Content="{l:Loc Tickets_Print}" Command="{Binding PrintCommand}" c:Shortcut.Gesture="Ctrl+P" Margin="0,0,8,0" />
                <Button Content="{l:Loc Btn_ExportExcel}" Command="{Binding ExportToExcelCommand}" c:Shortcut.Gesture="Ctrl+E" />
            </StackPanel>

            <DataGrid Style="{StaticResource ListGridStyle}" ItemsSource="{Binding Tickets}" SelectedItem="{Binding SelectedTicket}"
                      AutomationProperties.Name="{l:Loc Menu_Tickets}">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="{l:Loc Tickets_Id}" Binding="{Binding TicketId}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Tickets_Route}" Binding="{Binding RouteName}" Width="2*" />
                    <DataGridTextColumn Header="{l:Loc Tickets_Departure}" Binding="{Binding DepartureTime, StringFormat={}{0:dd/MM/yyyy HH:mm}}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Tickets_Vehicle}" Binding="{Binding PlateNumber}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Tickets_Seat}" Binding="{Binding SeatNo}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Tickets_Passenger}" Binding="{Binding PassengerName}" Width="*" />
                    <DataGridTextColumn Header="{l:Loc Tickets_Phone}" Binding="{Binding Phone}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Tickets_Price}" Binding="{Binding Price, StringFormat={}{0:N0} ₫}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Tickets_Status}" Width="Auto"
                                        Binding="{Binding Status, Converter={StaticResource Localize}, ConverterParameter=TicketStatus_}" />
                    <DataGridTextColumn Header="{l:Loc Tickets_SoldBy}" Binding="{Binding SoldBy}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Tickets_CancelReason}" Binding="{Binding CancelReason}" Width="*" />
                </DataGrid.Columns>
            </DataGrid>
        </DockPanel>
    </Border>
</UserControl>
```

- [ ] **Step 6: Register** – `PageTemplates.xaml`: `<DataTemplate DataType="{x:Type vm:TicketsViewModel}"><pages:TicketsPage /></DataTemplate>`; `AddPages`: `services.AddPage<TicketsViewModel>("Menu_Tickets", "", 30);`

- [ ] **Step 7: Add resource keys**

`Strings.resx`:
```xml
  <data name="Menu_Tickets" xml:space="preserve"><value>Tickets</value></data>
  <data name="Tickets_Keyword" xml:space="preserve"><value>Ticket #, phone or name</value></data>
  <data name="Tickets_Pay" xml:space="preserve"><value>Pay</value></data>
  <data name="Tickets_Cancel" xml:space="preserve"><value>Cancel ticket</value></data>
  <data name="Tickets_Print" xml:space="preserve"><value>Print (Ctrl+P)</value></data>
  <data name="Tickets_CancelTitle" xml:space="preserve"><value>Cancel ticket #{0}</value></data>
  <data name="Tickets_CancelReason" xml:space="preserve"><value>Reason</value></data>
  <data name="Tickets_Cancelled" xml:space="preserve"><value>Ticket cancelled.</value></data>
  <data name="Tickets_CancelledRefund" xml:space="preserve"><value>Ticket cancelled; {0:N0} ₫ refunded.</value></data>
  <data name="Tickets_ExportTitle" xml:space="preserve"><value>Ticket list</value></data>
```

`Strings.vi.resx`:
```xml
  <data name="Menu_Tickets" xml:space="preserve"><value>Quản lý vé</value></data>
  <data name="Tickets_Keyword" xml:space="preserve"><value>Mã vé, SĐT hoặc tên khách</value></data>
  <data name="Tickets_Pay" xml:space="preserve"><value>Thanh toán</value></data>
  <data name="Tickets_Cancel" xml:space="preserve"><value>Hủy vé</value></data>
  <data name="Tickets_Print" xml:space="preserve"><value>In vé (Ctrl+P)</value></data>
  <data name="Tickets_CancelTitle" xml:space="preserve"><value>Hủy vé #{0}</value></data>
  <data name="Tickets_CancelReason" xml:space="preserve"><value>Lý do hủy</value></data>
  <data name="Tickets_Cancelled" xml:space="preserve"><value>Đã hủy vé.</value></data>
  <data name="Tickets_CancelledRefund" xml:space="preserve"><value>Đã hủy vé và hoàn {0:N0} ₫.</value></data>
  <data name="Tickets_ExportTitle" xml:space="preserve"><value>Danh sách vé</value></data>
```

- [ ] **Step 8: Build, test, commit**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests` → 0 warnings, `failed: 0`.
```bash
git add src/Transport.Presentation.Wpf tests/Transport.UnitTests
git commit -m "feat(ui): add tickets screen"
```

---

### Task 8: Statistics screen (V_TRIP_STATISTICS + revenue by route chart)

**Files:**
- Create: `ViewModels/Pages/StatisticsViewModel.cs`, `Views/Pages/StatisticsPage.xaml(.cs)`
- Modify: `Views/PageTemplates.xaml`, `PresentationServices.cs`, both `.resx`
- Test: `tests/Transport.UnitTests/Presentation/StatisticsViewModelTests.cs`

**Interfaces:**
- Consumes: `IStatisticsQueries`, `IRouteRepository`, `ChartBar`, `FilterOption<int?>`, `TimeProvider`.
- Produces: `StatisticsViewModel(PageServices, IStatisticsQueries, IRouteRepository, TimeProvider)` with `FromDate`, `ToDate`, `RouteOptions`, `SelectedRoute`, `Trips`, `TotalTrips`, `TotalTicketsSold`, `TotalRevenue`, `AverageOccupancy`, `TotalCancelled`, `RevenueByRoute`, `SearchCommand`, `ExportToExcelCommand`; keys `Stats_*` (also used by the dashboard).

- [ ] **Step 1: Write the failing test**

```csharp
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class StatisticsViewModelTests
{
    private static readonly FixedTimeProvider Time = new(new DateTimeOffset(2026, 10, 17, 8, 0, 0, TimeSpan.Zero));
    private readonly PageTestContext _context = new();
    private readonly FakeStatisticsQueries _statistics = new();
    private readonly FakeRouteRepository _routes = new();

    public StatisticsViewModelTests()
    {
        _routes.Items.Add(TestData.Route(1, "Hà Nội - Lào Cai"));
        _routes.Items.Add(TestData.Route(2, "Hà Nội - Hải Phòng"));
        _statistics.Trips.Add(FakeStatisticsQueries.Trip(1, seatsSold: 20, seatCount: 40, revenue: 5_600_000, routeId: 1, routeName: "Hà Nội - Lào Cai"));
        _statistics.Trips.Add(FakeStatisticsQueries.Trip(2, seatsSold: 10, seatCount: 40, revenue: 1_200_000, routeId: 2, routeName: "Hà Nội - Hải Phòng"));
        _statistics.Trips.Add(FakeStatisticsQueries.Trip(3, status: ScheduleStatus.Cancelled, routeId: 2, routeName: "Hà Nội - Hải Phòng")
            with { TicketsCancelled = 4 });
    }

    private StatisticsViewModel Create() => new(_context.Services, _statistics, _routes, Time);

    [Fact]
    public async Task Load_DefaultsToCurrentMonth_AndComputesTotals()
    {
        var vm = Create();

        await vm.LoadAsync();

        Assert.Equal(new DateTime(2026, 10, 1), _statistics.LastCriteria!.From);
        Assert.Equal(new DateTime(2026, 10, 18), _statistics.LastCriteria.To);
        Assert.Null(_statistics.LastCriteria.RouteId);
        Assert.Equal(2, vm.TotalTrips);
        Assert.Equal(30, vm.TotalTicketsSold);
        Assert.Equal(6_800_000, vm.TotalRevenue);
        Assert.Equal(37.5m, vm.AverageOccupancy);
        Assert.Equal(4, vm.TotalCancelled);
        Assert.Equal("Hà Nội - Lào Cai", vm.RevenueByRoute[0].Label);
        Assert.Equal(150d, vm.RevenueByRoute[0].Height);
        Assert.Equal(3, vm.RouteOptions.Count);
        Assert.Equal("Stats_AllRoutes", vm.RouteOptions[0].Label);
    }

    [Fact]
    public async Task SelectingRoute_FiltersQuery()
    {
        var vm = Create();
        await vm.LoadAsync();

        vm.SelectedRoute = vm.RouteOptions[1];
        await vm.SearchCommand.ExecuteAsync(null);

        Assert.Equal(1, _statistics.LastCriteria!.RouteId);
    }

    [Fact]
    public async Task Export_WritesTripStatistics()
    {
        var vm = Create();
        await vm.LoadAsync();

        await vm.ExportToExcelCommand.ExecuteAsync(null);

        Assert.Equal("Stats_ExportTitle", _context.Exporter.Title);
        Assert.Equal(9, _context.Exporter.Headers.Count);
        Assert.Equal(3, _context.Exporter.RowCount);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build error `StatisticsViewModel` not found.

- [ ] **Step 3: Write `ViewModels/Pages/StatisticsViewModel.cs`**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Thống kê chuyến xe từ view V_TRIP_STATISTICS
public sealed partial class StatisticsViewModel : PageViewModel
{
    private readonly IStatisticsQueries _statistics;
    private readonly IRouteRepository _routes;

    public StatisticsViewModel(PageServices services, IStatisticsQueries statistics, IRouteRepository routes, TimeProvider time)
        : base(services)
    {
        _statistics = statistics;
        _routes = routes;
        var today = time.GetLocalNow().Date;
        FromDate = new DateTime(today.Year, today.Month, 1);
        ToDate = today;
        RouteOptions = [new FilterOption<int?>(null, Loc["Stats_AllRoutes"])];
        SelectedRoute = RouteOptions[0];
        RevenueByRoute = [];
    }

    public ObservableCollection<TripStatistic> Trips { get; } = [];

    [ObservableProperty]
    public partial DateTime FromDate { get; set; }

    [ObservableProperty]
    public partial DateTime ToDate { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<FilterOption<int?>> RouteOptions { get; set; }

    [ObservableProperty]
    public partial FilterOption<int?> SelectedRoute { get; set; }

    [ObservableProperty]
    public partial int TotalTrips { get; set; }

    [ObservableProperty]
    public partial int TotalTicketsSold { get; set; }

    [ObservableProperty]
    public partial decimal TotalRevenue { get; set; }

    [ObservableProperty]
    public partial decimal AverageOccupancy { get; set; }

    [ObservableProperty]
    public partial int TotalCancelled { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<ChartBar> RevenueByRoute { get; set; }

    public override Task LoadAsync() => RunBusyAsync(async () =>
    {
        var routes = await _routes.GetAllAsync();
        RouteOptions =
        [
            new FilterOption<int?>(null, Loc["Stats_AllRoutes"]),
            .. routes.Select(r => new FilterOption<int?>(r.Id, r.Name)),
        ];
        SelectedRoute = RouteOptions[0];
        await SearchCoreAsync();
    });

    [RelayCommand]
    private Task SearchAsync() => RunBusyAsync(SearchCoreAsync);

    [RelayCommand]
    private Task ExportToExcelAsync() => ExportAsync(Trips.ToList(),
    [
        new("Schedules_Departure", t => t.DepartureTime, "dd/MM/yyyy HH:mm"),
        new("Schedules_Route", t => t.RouteName),
        new("Schedules_Vehicle", t => t.PlateNumber),
        new("Schedules_Driver", t => t.DriverName),
        new("Schedules_SeatCount", t => t.SeatCount),
        new("Schedules_Sold", t => t.SeatsSold),
        new("Stats_Occupancy", t => t.OccupancyPct),
        new("Stats_Cancelled", t => t.TicketsCancelled),
        new("Stats_Revenue", t => t.Revenue, "#,##0"),
    ], "Stats_ExportTitle");

    private async Task SearchCoreAsync()
    {
        var trips = await _statistics.GetTripsAsync(new TripSearchCriteria
        {
            From = FromDate.Date,
            To = ToDate.Date.AddDays(1),
            RouteId = SelectedRoute.Value,
        });
        Trips.Clear();
        foreach (var trip in trips)
        {
            Trips.Add(trip);
        }

        // Chuyến đã hủy không tính vào số chuyến / vé bán / tỷ lệ lấp đầy
        var active = trips.Where(t => t.Status != ScheduleStatus.Cancelled).ToList();
        TotalTrips = active.Count;
        TotalTicketsSold = active.Sum(t => t.SeatsSold);
        TotalRevenue = trips.Sum(t => t.Revenue);
        AverageOccupancy = active.Count == 0 ? 0 : Math.Round(active.Average(t => t.OccupancyPct), 1);
        TotalCancelled = trips.Sum(t => t.TicketsCancelled);
        RevenueByRoute = ChartBar.Build(trips
            .GroupBy(t => t.RouteName)
            .Select(g => (g.Key, g.Sum(t => t.Revenue)))
            .OrderByDescending(p => p.Item2));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 5: Add a stat-card style to `Themes/Controls.xaml`**

```xml
    <!-- Ô số liệu (Tổng quan, Thống kê) -->
    <Style x:Key="StatValueStyle" TargetType="TextBlock">
        <Setter Property="FontSize" Value="26" />
        <Setter Property="FontWeight" Value="SemiBold" />
        <Setter Property="Foreground" Value="{StaticResource HaUiNavyBrush}" />
        <Setter Property="Margin" Value="0,4,0,0" />
    </Style>
    <Style x:Key="StatLabelStyle" TargetType="TextBlock">
        <Setter Property="Foreground" Value="{DynamicResource TextFillColorSecondaryBrush}" />
        <Setter Property="TextWrapping" Value="Wrap" />
    </Style>
```

- [ ] **Step 6: Write `Views/Pages/StatisticsPage.xaml`** (+ code-behind)

```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Pages.StatisticsPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls"
             xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"
             mc:Ignorable="d" d:DesignWidth="1100" d:DesignHeight="680"
             d:DataContext="{d:DesignInstance Type=vm:StatisticsViewModel}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>

        <!-- Bộ lọc -->
        <Border Style="{StaticResource CardStyle}" Margin="0,0,0,16">
            <DockPanel>
                <Button DockPanel.Dock="Right" Content="{l:Loc Btn_ExportExcel}" Command="{Binding ExportToExcelCommand}"
                        c:Shortcut.Gesture="Ctrl+E" VerticalAlignment="Bottom" />
                <WrapPanel>
                    <StackPanel Width="150" Margin="0,0,12,0">
                        <TextBlock Text="{l:Loc Lbl_From}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
                        <DatePicker SelectedDate="{Binding FromDate}" AutomationProperties.Name="{l:Loc Lbl_From}" />
                    </StackPanel>
                    <StackPanel Width="150" Margin="0,0,12,0">
                        <TextBlock Text="{l:Loc Lbl_To}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
                        <DatePicker SelectedDate="{Binding ToDate}" AutomationProperties.Name="{l:Loc Lbl_To}" />
                    </StackPanel>
                    <StackPanel Width="240" Margin="0,0,12,0">
                        <TextBlock Text="{l:Loc Stats_Route}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
                        <ComboBox ItemsSource="{Binding RouteOptions}" SelectedItem="{Binding SelectedRoute}" DisplayMemberPath="Label"
                                  AutomationProperties.Name="{l:Loc Stats_Route}" />
                    </StackPanel>
                    <Button Content="{l:Loc Btn_Search}" Command="{Binding SearchCommand}" Style="{StaticResource PrimaryButtonStyle}"
                            VerticalAlignment="Bottom" MinWidth="100" c:Shortcut.Gesture="F5" />
                </WrapPanel>
            </DockPanel>
        </Border>

        <!-- Tổng hợp -->
        <UniformGrid Grid.Row="1" Columns="5" Margin="0,0,0,16">
            <Border Style="{StaticResource CardStyle}" Margin="0,0,12,0">
                <StackPanel>
                    <TextBlock Text="{l:Loc Stats_TotalTrips}" Style="{StaticResource StatLabelStyle}" />
                    <TextBlock Text="{Binding TotalTrips}" Style="{StaticResource StatValueStyle}" />
                </StackPanel>
            </Border>
            <Border Style="{StaticResource CardStyle}" Margin="0,0,12,0">
                <StackPanel>
                    <TextBlock Text="{l:Loc Stats_TicketsSold}" Style="{StaticResource StatLabelStyle}" />
                    <TextBlock Text="{Binding TotalTicketsSold}" Style="{StaticResource StatValueStyle}" />
                </StackPanel>
            </Border>
            <Border Style="{StaticResource CardStyle}" Margin="0,0,12,0">
                <StackPanel>
                    <TextBlock Text="{l:Loc Stats_Revenue}" Style="{StaticResource StatLabelStyle}" />
                    <TextBlock Text="{Binding TotalRevenue, StringFormat={}{0:N0} ₫}" Style="{StaticResource StatValueStyle}" />
                </StackPanel>
            </Border>
            <Border Style="{StaticResource CardStyle}" Margin="0,0,12,0">
                <StackPanel>
                    <TextBlock Text="{l:Loc Stats_AvgOccupancy}" Style="{StaticResource StatLabelStyle}" />
                    <TextBlock Text="{Binding AverageOccupancy, StringFormat={}{0:N1} %}" Style="{StaticResource StatValueStyle}" />
                </StackPanel>
            </Border>
            <Border Style="{StaticResource CardStyle}">
                <StackPanel>
                    <TextBlock Text="{l:Loc Stats_Cancelled}" Style="{StaticResource StatLabelStyle}" />
                    <TextBlock Text="{Binding TotalCancelled}" Style="{StaticResource StatValueStyle}" />
                </StackPanel>
            </Border>
        </UniformGrid>

        <!-- Doanh thu theo tuyến -->
        <Border Grid.Row="2" Style="{StaticResource CardStyle}" Margin="0,0,0,16">
            <StackPanel>
                <TextBlock Text="{l:Loc Stats_RevenueByRoute}" Style="{StaticResource SectionTitleStyle}" />
                <ScrollViewer HorizontalScrollBarVisibility="Auto" VerticalScrollBarVisibility="Disabled">
                    <ItemsControl ItemsSource="{Binding RevenueByRoute}" Style="{StaticResource BarChartStyle}"
                                  AutomationProperties.Name="{l:Loc Stats_RevenueByRoute}" />
                </ScrollViewer>
            </StackPanel>
        </Border>

        <!-- Chi tiết từng chuyến -->
        <Border Grid.Row="3" Style="{StaticResource CardStyle}">
            <DataGrid Style="{StaticResource ListGridStyle}" ItemsSource="{Binding Trips}" AutomationProperties.Name="{l:Loc Menu_Statistics}">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="{l:Loc Schedules_Departure}" Binding="{Binding DepartureTime, StringFormat={}{0:dd/MM/yyyy HH:mm}}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Schedules_Route}" Binding="{Binding RouteName}" Width="2*" />
                    <DataGridTextColumn Header="{l:Loc Schedules_Vehicle}" Binding="{Binding PlateNumber}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Schedules_Driver}" Binding="{Binding DriverName}" Width="*" />
                    <DataGridTextColumn Header="{l:Loc Schedules_Seats}" Width="Auto">
                        <DataGridTextColumn.Binding>
                            <MultiBinding StringFormat="{}{0}/{1}">
                                <Binding Path="SeatsSold" />
                                <Binding Path="SeatCount" />
                            </MultiBinding>
                        </DataGridTextColumn.Binding>
                    </DataGridTextColumn>
                    <DataGridTextColumn Header="{l:Loc Stats_Occupancy}" Binding="{Binding OccupancyPct, StringFormat={}{0:N1}}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Stats_Cancelled}" Binding="{Binding TicketsCancelled}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Stats_Revenue}" Binding="{Binding Revenue, StringFormat={}{0:N0} ₫}" Width="Auto" />
                    <DataGridTextColumn Header="{l:Loc Schedules_Status}" Width="Auto"
                                        Binding="{Binding Status, Converter={StaticResource Localize}, ConverterParameter=ScheduleStatus_}" />
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 7: Register** – `PageTemplates.xaml`: `<DataTemplate DataType="{x:Type vm:StatisticsViewModel}"><pages:StatisticsPage /></DataTemplate>`; `AddPages`: `services.AddPage<StatisticsViewModel>("Menu_Statistics", "\uE9D2", 90);`

- [ ] **Step 8: Add resource keys**

`Strings.resx`:
```xml
  <data name="Menu_Statistics" xml:space="preserve"><value>Statistics</value></data>
  <data name="Stats_Route" xml:space="preserve"><value>Route</value></data>
  <data name="Stats_AllRoutes" xml:space="preserve"><value>All routes</value></data>
  <data name="Stats_TotalTrips" xml:space="preserve"><value>Trips</value></data>
  <data name="Stats_TicketsSold" xml:space="preserve"><value>Tickets sold</value></data>
  <data name="Stats_Revenue" xml:space="preserve"><value>Net revenue</value></data>
  <data name="Stats_AvgOccupancy" xml:space="preserve"><value>Average occupancy</value></data>
  <data name="Stats_Occupancy" xml:space="preserve"><value>Occupancy (%)</value></data>
  <data name="Stats_Cancelled" xml:space="preserve"><value>Cancelled tickets</value></data>
  <data name="Stats_RevenueByRoute" xml:space="preserve"><value>Net revenue by route</value></data>
  <data name="Stats_ExportTitle" xml:space="preserve"><value>Trip statistics</value></data>
```

`Strings.vi.resx`:
```xml
  <data name="Menu_Statistics" xml:space="preserve"><value>Thống kê</value></data>
  <data name="Stats_Route" xml:space="preserve"><value>Tuyến</value></data>
  <data name="Stats_AllRoutes" xml:space="preserve"><value>Tất cả tuyến</value></data>
  <data name="Stats_TotalTrips" xml:space="preserve"><value>Số chuyến</value></data>
  <data name="Stats_TicketsSold" xml:space="preserve"><value>Vé đã bán</value></data>
  <data name="Stats_Revenue" xml:space="preserve"><value>Doanh thu thực</value></data>
  <data name="Stats_AvgOccupancy" xml:space="preserve"><value>Tỷ lệ lấp đầy TB</value></data>
  <data name="Stats_Occupancy" xml:space="preserve"><value>Lấp đầy (%)</value></data>
  <data name="Stats_Cancelled" xml:space="preserve"><value>Vé hủy</value></data>
  <data name="Stats_RevenueByRoute" xml:space="preserve"><value>Doanh thu thực theo tuyến</value></data>
  <data name="Stats_ExportTitle" xml:space="preserve"><value>Thống kê chuyến xe</value></data>
```

- [ ] **Step 9: Build, test, commit**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests` → 0 warnings, `failed: 0`.
```bash
git add src/Transport.Presentation.Wpf tests/Transport.UnitTests
git commit -m "feat(ui): add statistics screen with revenue by route"
```

---

### Task 9: Dashboard screen

**Files:**
- Create: `ViewModels/Pages/DashboardViewModel.cs`, `Views/Pages/DashboardPage.xaml(.cs)`
- Modify: `Views/PageTemplates.xaml`, `PresentationServices.cs`, both `.resx`
- Test: `tests/Transport.UnitTests/Presentation/DashboardViewModelTests.cs`

**Interfaces:**
- Consumes: `DashboardService` (Plan 2), `ChartBar`, `BarChartStyle`, `StatValueStyle`.
- Produces: `DashboardViewModel(PageServices, DashboardService)` with `TripsToday`, `TicketsSoldToday`, `RevenueToday`, `AverageOccupancy`, `Last7Days`, `RefreshCommand`.

- [ ] **Step 1: Write the failing test**

```csharp
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class DashboardViewModelTests
{
    [Fact]
    public async Task Load_ShowsTodayFiguresAndSevenDailyBars()
    {
        var statistics = new FakeStatisticsQueries();
        statistics.Trips.Add(FakeStatisticsQueries.Trip(1, seatsSold: 20, seatCount: 40, revenue: 5_600_000));
        statistics.Daily.Add(new DailyRevenue { RevenueDate = new DateTime(2026, 10, 16), Revenue = 2_000_000, Tickets = 8 });
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 10, 17, 8, 0, 0, TimeSpan.Zero));
        var vm = new DashboardViewModel(new PageTestContext().Services, new DashboardService(statistics, time));

        await vm.LoadAsync();

        Assert.Equal(1, vm.TripsToday);
        Assert.Equal(20, vm.TicketsSoldToday);
        Assert.Equal(5_600_000, vm.RevenueToday);
        Assert.Equal(50m, vm.AverageOccupancy);
        Assert.Equal(7, vm.Last7Days.Count);
        Assert.Equal("11/10", vm.Last7Days[0].Label);
        Assert.Equal("17/10", vm.Last7Days[6].Label);
        Assert.Equal(150d, vm.Last7Days[5].Height);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build error `DashboardViewModel` not found.

- [ ] **Step 3: Write `ViewModels/Pages/DashboardViewModel.cs`**

```csharp
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Services;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Tổng quan hôm nay + doanh thu 7 ngày gần nhất
public sealed partial class DashboardViewModel : PageViewModel
{
    private readonly DashboardService _dashboard;

    public DashboardViewModel(PageServices services, DashboardService dashboard)
        : base(services)
    {
        _dashboard = dashboard;
        Last7Days = [];
    }

    [ObservableProperty]
    public partial int TripsToday { get; set; }

    [ObservableProperty]
    public partial int TicketsSoldToday { get; set; }

    [ObservableProperty]
    public partial decimal RevenueToday { get; set; }

    [ObservableProperty]
    public partial decimal AverageOccupancy { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<ChartBar> Last7Days { get; set; }

    public override Task LoadAsync() => RunBusyAsync(LoadCoreAsync);

    [RelayCommand]
    private Task RefreshAsync() => RunBusyAsync(LoadCoreAsync);

    private async Task LoadCoreAsync()
    {
        var summary = await _dashboard.GetSummaryAsync();
        TripsToday = summary.TripsToday;
        TicketsSoldToday = summary.TicketsSoldToday;
        RevenueToday = summary.RevenueToday;
        AverageOccupancy = summary.AverageOccupancyPct;
        Last7Days = ChartBar.Build(summary.Last7Days.Select(d =>
            (d.RevenueDate.ToString("dd/MM", CultureInfo.InvariantCulture), d.Revenue)));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 5: Write `Views/Pages/DashboardPage.xaml`** (+ code-behind)

```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Pages.DashboardPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls"
             xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"
             mc:Ignorable="d" d:DesignWidth="1100" d:DesignHeight="680"
             d:DataContext="{d:DesignInstance Type=vm:DashboardViewModel}">
    <StackPanel>
        <DockPanel Margin="0,0,0,16">
            <Button DockPanel.Dock="Right" Command="{Binding RefreshCommand}" c:Shortcut.Gesture="F5"
                    ToolTip="{l:Loc Btn_Refresh}" AutomationProperties.Name="{l:Loc Btn_Refresh}">
                <TextBlock Text="&#xE72C;" FontFamily="{StaticResource SymbolFont}" />
            </Button>
            <TextBlock Text="{l:Loc Dash_Today}" Style="{StaticResource SectionTitleStyle}" VerticalAlignment="Center" />
        </DockPanel>

        <UniformGrid Columns="4" Margin="0,0,0,16">
            <Border Style="{StaticResource CardStyle}" Margin="0,0,12,0">
                <DockPanel>
                    <TextBlock DockPanel.Dock="Right" Text="&#xE806;" FontFamily="{StaticResource SymbolFont}" FontSize="28"
                               Foreground="{StaticResource HaUiGoldBrush}" />
                    <StackPanel>
                        <TextBlock Text="{l:Loc Dash_TripsToday}" Style="{StaticResource StatLabelStyle}" />
                        <TextBlock Text="{Binding TripsToday}" Style="{StaticResource StatValueStyle}" />
                    </StackPanel>
                </DockPanel>
            </Border>
            <Border Style="{StaticResource CardStyle}" Margin="0,0,12,0">
                <DockPanel>
                    <TextBlock DockPanel.Dock="Right" Text="&#xE7BF;" FontFamily="{StaticResource SymbolFont}" FontSize="28"
                               Foreground="{StaticResource HaUiGoldBrush}" />
                    <StackPanel>
                        <TextBlock Text="{l:Loc Dash_TicketsToday}" Style="{StaticResource StatLabelStyle}" />
                        <TextBlock Text="{Binding TicketsSoldToday}" Style="{StaticResource StatValueStyle}" />
                    </StackPanel>
                </DockPanel>
            </Border>
            <Border Style="{StaticResource CardStyle}" Margin="0,0,12,0">
                <DockPanel>
                    <TextBlock DockPanel.Dock="Right" Text="&#xE9D2;" FontFamily="{StaticResource SymbolFont}" FontSize="28"
                               Foreground="{StaticResource HaUiGoldBrush}" />
                    <StackPanel>
                        <TextBlock Text="{l:Loc Dash_RevenueToday}" Style="{StaticResource StatLabelStyle}" />
                        <TextBlock Text="{Binding RevenueToday, StringFormat={}{0:N0} ₫}" Style="{StaticResource StatValueStyle}" />
                    </StackPanel>
                </DockPanel>
            </Border>
            <Border Style="{StaticResource CardStyle}">
                <DockPanel>
                    <TextBlock DockPanel.Dock="Right" Text="&#xE716;" FontFamily="{StaticResource SymbolFont}" FontSize="28"
                               Foreground="{StaticResource HaUiGoldBrush}" />
                    <StackPanel>
                        <TextBlock Text="{l:Loc Dash_Occupancy}" Style="{StaticResource StatLabelStyle}" />
                        <TextBlock Text="{Binding AverageOccupancy, StringFormat={}{0:N1} %}" Style="{StaticResource StatValueStyle}" />
                    </StackPanel>
                </DockPanel>
            </Border>
        </UniformGrid>

        <Border Style="{StaticResource CardStyle}">
            <StackPanel>
                <TextBlock Text="{l:Loc Dash_Last7Days}" Style="{StaticResource SectionTitleStyle}" />
                <ItemsControl ItemsSource="{Binding Last7Days}" Style="{StaticResource BarChartStyle}"
                              AutomationProperties.Name="{l:Loc Dash_Last7Days}" />
            </StackPanel>
        </Border>
    </StackPanel>
</UserControl>
```

- [ ] **Step 6: Register** – `PageTemplates.xaml`: `<DataTemplate DataType="{x:Type vm:DashboardViewModel}"><pages:DashboardPage /></DataTemplate>`; `AddPages`: `services.AddPage<DashboardViewModel>("Menu_Dashboard", "\uE80F", 10);` (order 10 → the first page after login)

- [ ] **Step 7: Add resource keys**

`Strings.resx`:
```xml
  <data name="Menu_Dashboard" xml:space="preserve"><value>Dashboard</value></data>
  <data name="Dash_Today" xml:space="preserve"><value>Today</value></data>
  <data name="Dash_TripsToday" xml:space="preserve"><value>Trips today</value></data>
  <data name="Dash_TicketsToday" xml:space="preserve"><value>Tickets sold today</value></data>
  <data name="Dash_RevenueToday" xml:space="preserve"><value>Net revenue today</value></data>
  <data name="Dash_Occupancy" xml:space="preserve"><value>Average occupancy today</value></data>
  <data name="Dash_Last7Days" xml:space="preserve"><value>Net revenue – last 7 days (by departure date)</value></data>
```

`Strings.vi.resx`:
```xml
  <data name="Menu_Dashboard" xml:space="preserve"><value>Tổng quan</value></data>
  <data name="Dash_Today" xml:space="preserve"><value>Hôm nay</value></data>
  <data name="Dash_TripsToday" xml:space="preserve"><value>Chuyến hôm nay</value></data>
  <data name="Dash_TicketsToday" xml:space="preserve"><value>Vé bán hôm nay</value></data>
  <data name="Dash_RevenueToday" xml:space="preserve"><value>Doanh thu thực hôm nay</value></data>
  <data name="Dash_Occupancy" xml:space="preserve"><value>Lấp đầy trung bình hôm nay</value></data>
  <data name="Dash_Last7Days" xml:space="preserve"><value>Doanh thu thực 7 ngày gần nhất (theo ngày khởi hành)</value></data>
```

- [ ] **Step 8: Build, test, commit**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests` → 0 warnings, `failed: 0`.
```bash
git add src/Transport.Presentation.Wpf tests/Transport.UnitTests
git commit -m "feat(ui): add dashboard screen"
```

---

### Task 10: Staff screen (create Oracle users from the app)

**Files:**
- Create: `ViewModels/Pages/StaffViewModel.cs`, `Views/Pages/StaffPage.xaml(.cs)`
- Modify: `Views/PageTemplates.xaml`, `PresentationServices.cs`, both `.resx`
- Test: `tests/Transport.UnitTests/Presentation/StaffViewModelTests.cs`

**Interfaces:**
- Consumes: `CrudPageViewModel<StaffMember>`, `StaffService` (Plan 2), `FakeStaffRepository` (Plan 2), `IDialogService.PromptPassword/Confirm`.
- Produces: `StaffViewModel(PageServices, StaffService)` with `Username`, `Password`, `FullName`, `Phone`, `Role`, `Roles`, `ResetPasswordCommand`, `ToggleLockCommand`.

- [ ] **Step 1: Write the failing test**

```csharp
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class StaffViewModelTests
{
    private readonly PageTestContext _context = new();
    private readonly FakeStaffRepository _repository = new();

    public StaffViewModelTests()
    {
        _repository.Staff.Add(TestData.Staff("OPERATOR01", StaffRole.Operator));
        _repository.Staff.Add(TestData.Staff("SELLER01"));
    }

    private StaffViewModel Create() => new(_context.Services, new StaffService(_repository, _context.Session));

    private async Task<StaffViewModel> CreateWithSellerSelectedAsync()
    {
        var vm = Create();
        await vm.LoadAsync();
        vm.SelectedItem = vm.Items.Single(s => s.Username == "SELLER01");
        return vm;
    }

    [Fact]
    public async Task New_CreatesOracleUserWithChosenRole()
    {
        var vm = Create();
        vm.NewCommand.Execute(null);
        vm.Username = "seller03";
        vm.Password = "Seller#003";
        vm.FullName = "Phạm Thị C";
        vm.Role = StaffRole.TicketSeller;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(new NewStaffRequest("seller03", "Seller#003", "Phạm Thị C", null, StaffRole.TicketSeller), _repository.LastCreate);
    }

    [Fact]
    public async Task New_WithShortPassword_ShowsTranslatedError()
    {
        var vm = Create();
        vm.NewCommand.Execute(null);
        vm.Username = "seller03";
        vm.Password = "short";
        vm.FullName = "Phạm Thị C";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(_repository.Calls);
        Assert.Equal(new[] { "Err_20044" }, _context.Notifications.Errors);
    }

    [Fact]
    public async Task Edit_UpdatesInfo_AndChangesRoleOnlyWhenChanged()
    {
        var vm = await CreateWithSellerSelectedAsync();

        vm.EditCommand.Execute(null);
        vm.FullName = "Trần Thị Bán B";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(new[] { "update:SELLER01:Trần Thị Bán B:0912000002" }, _repository.Calls);

        vm.EditCommand.Execute(null);
        vm.Role = StaffRole.Operator;
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Contains("role:SELLER01:Operator", _repository.Calls);
    }

    [Fact]
    public async Task ResetPassword_UsesPasswordDialog()
    {
        var vm = await CreateWithSellerSelectedAsync();
        _context.Dialogs.PromptPasswordResult = "NewPass#01";

        await vm.ResetPasswordCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "password:SELLER01" }, _repository.Calls);
        Assert.Equal("Staff_ResetTitle|SELLER01", _context.Dialogs.Prompts[0]);
        Assert.Equal(new[] { "Staff_PasswordReset" }, _context.Notifications.Successes);
    }

    [Fact]
    public async Task ToggleLock_LocksActiveAccountAfterConfirmation()
    {
        var vm = await CreateWithSellerSelectedAsync();

        await vm.ToggleLockCommand.ExecuteAsync(null);

        Assert.Equal("Staff_ConfirmLock|SELLER01", _context.Dialogs.ConfirmMessages[0]);
        Assert.Equal(new[] { "lock:SELLER01:True" }, _repository.Calls);
    }

    [Fact]
    public void StaffCannotBeDeleted()
    {
        Assert.False(Create().CanDeleteItems);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build error `StaffViewModel` not found.

- [ ] **Step 3: Write `ViewModels/Pages/StaffViewModel.cs`**

```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 5: Write `Views/Pages/StaffPage.xaml`** (+ code-behind)

```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Pages.StaffPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls"
             xmlns:ctl="clr-namespace:Transport.Presentation.Wpf.Views.Controls"
             xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"
             mc:Ignorable="d" d:DesignWidth="1100" d:DesignHeight="680"
             d:DataContext="{d:DesignInstance Type=vm:StaffViewModel}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>

        <Border Style="{StaticResource CardStyle}">
            <DockPanel>
                <ctl:CrudToolbar DockPanel.Dock="Top" />
                <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,12">
                    <Button Content="{l:Loc Staff_ResetPassword}" Command="{Binding ResetPasswordCommand}" Margin="0,0,8,0" />
                    <Button Content="{l:Loc Staff_Lock}" Command="{Binding ToggleLockCommand}" />
                </StackPanel>
                <DataGrid Style="{StaticResource ListGridStyle}" ItemsSource="{Binding Items}" SelectedItem="{Binding SelectedItem}"
                          AutomationProperties.Name="{l:Loc Menu_Staff}">
                    <DataGrid.InputBindings>
                        <MouseBinding Gesture="LeftDoubleClick" Command="{Binding EditCommand}" />
                    </DataGrid.InputBindings>
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="{l:Loc Staff_Username}" Binding="{Binding Username}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Staff_FullName}" Binding="{Binding FullName}" Width="2*" />
                        <DataGridTextColumn Header="{l:Loc Staff_Phone}" Binding="{Binding Phone}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Staff_Role}" Width="*"
                                            Binding="{Binding Role, Converter={StaticResource Localize}, ConverterParameter=StaffRole_}" />
                        <DataGridTextColumn Header="{l:Loc Staff_Status}" Width="Auto"
                                            Binding="{Binding Status, Converter={StaticResource Localize}, ConverterParameter=StaffStatus_}" />
                        <DataGridTextColumn Header="{l:Loc Staff_CreatedAt}" Binding="{Binding CreatedAt, StringFormat={}{0:dd/MM/yyyy}}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Staff_CreatedBy}" Binding="{Binding CreatedBy}" Width="Auto" />
                    </DataGrid.Columns>
                </DataGrid>
            </DockPanel>
        </Border>

        <Border Grid.Column="1" Style="{StaticResource FormCardStyle}">
            <StackPanel>
                <ctl:FormHeader />
                <TextBlock Text="{l:Loc Staff_Username}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}" IsEnabled="{Binding IsNewItem}"
                         AutomationProperties.Name="{l:Loc Staff_Username}" />
                <StackPanel Visibility="{Binding IsNewItem, Converter={StaticResource BoolToVisibility}}">
                    <TextBlock Text="{l:Loc Staff_Password}" Style="{StaticResource FieldLabelStyle}" />
                    <PasswordBox c:PasswordBoxAssistant.BoundPassword="{Binding Password}" AutomationProperties.Name="{l:Loc Staff_Password}" />
                </StackPanel>
                <TextBlock Text="{l:Loc Staff_FullName}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding FullName, UpdateSourceTrigger=PropertyChanged}" AutomationProperties.Name="{l:Loc Staff_FullName}" />
                <TextBlock Text="{l:Loc Staff_Phone}" Style="{StaticResource FieldLabelStyle}" />
                <TextBox Text="{Binding Phone, UpdateSourceTrigger=PropertyChanged}" MaxLength="10" AutomationProperties.Name="{l:Loc Staff_Phone}" />
                <TextBlock Text="{l:Loc Staff_Role}" Style="{StaticResource FieldLabelStyle}" />
                <ComboBox ItemsSource="{Binding Roles}" SelectedItem="{Binding Role}" AutomationProperties.Name="{l:Loc Staff_Role}">
                    <ComboBox.ItemTemplate>
                        <DataTemplate>
                            <TextBlock Text="{Binding Converter={StaticResource Localize}, ConverterParameter=StaffRole_}" />
                        </DataTemplate>
                    </ComboBox.ItemTemplate>
                </ComboBox>
                <ctl:FormButtons />
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 6: Register** – `PageTemplates.xaml`: `<DataTemplate DataType="{x:Type vm:StaffViewModel}"><pages:StaffPage /></DataTemplate>`; `AddPages`: `services.AddPage<StaffViewModel>("Menu_Staff", "\uE7EF", 100, operatorOnly: true);`

- [ ] **Step 7: Add resource keys**

`Strings.resx`:
```xml
  <data name="Menu_Staff" xml:space="preserve"><value>Staff</value></data>
  <data name="Staff_Username" xml:space="preserve"><value>Username</value></data>
  <data name="Staff_Password" xml:space="preserve"><value>Initial password (at least 8 characters)</value></data>
  <data name="Staff_FullName" xml:space="preserve"><value>Full name</value></data>
  <data name="Staff_Phone" xml:space="preserve"><value>Phone</value></data>
  <data name="Staff_Role" xml:space="preserve"><value>Role</value></data>
  <data name="Staff_Status" xml:space="preserve"><value>Status</value></data>
  <data name="Staff_CreatedAt" xml:space="preserve"><value>Created</value></data>
  <data name="Staff_CreatedBy" xml:space="preserve"><value>Created by</value></data>
  <data name="Staff_ExportTitle" xml:space="preserve"><value>Staff list</value></data>
  <data name="Staff_ResetPassword" xml:space="preserve"><value>Reset password</value></data>
  <data name="Staff_ResetTitle" xml:space="preserve"><value>Reset password for {0}</value></data>
  <data name="Staff_PasswordReset" xml:space="preserve"><value>Password updated.</value></data>
  <data name="Staff_Lock" xml:space="preserve"><value>Lock / unlock</value></data>
  <data name="Staff_ConfirmLock" xml:space="preserve"><value>Lock account {0}? The user will no longer be able to sign in.</value></data>
  <data name="Staff_ConfirmUnlock" xml:space="preserve"><value>Unlock account {0}?</value></data>
  <data name="Staff_Locked" xml:space="preserve"><value>Account locked.</value></data>
  <data name="Staff_Unlocked" xml:space="preserve"><value>Account unlocked.</value></data>
  <data name="StaffRole_Operator" xml:space="preserve"><value>Operator</value></data>
  <data name="StaffRole_TicketSeller" xml:space="preserve"><value>Ticket seller</value></data>
  <data name="StaffStatus_Active" xml:space="preserve"><value>Active</value></data>
  <data name="StaffStatus_Locked" xml:space="preserve"><value>Locked</value></data>
```

`Strings.vi.resx`:
```xml
  <data name="Menu_Staff" xml:space="preserve"><value>Nhân viên</value></data>
  <data name="Staff_Username" xml:space="preserve"><value>Tên đăng nhập</value></data>
  <data name="Staff_Password" xml:space="preserve"><value>Mật khẩu ban đầu (tối thiểu 8 ký tự)</value></data>
  <data name="Staff_FullName" xml:space="preserve"><value>Họ tên</value></data>
  <data name="Staff_Phone" xml:space="preserve"><value>Số điện thoại</value></data>
  <data name="Staff_Role" xml:space="preserve"><value>Vai trò</value></data>
  <data name="Staff_Status" xml:space="preserve"><value>Trạng thái</value></data>
  <data name="Staff_CreatedAt" xml:space="preserve"><value>Ngày tạo</value></data>
  <data name="Staff_CreatedBy" xml:space="preserve"><value>Người tạo</value></data>
  <data name="Staff_ExportTitle" xml:space="preserve"><value>Danh sách nhân viên</value></data>
  <data name="Staff_ResetPassword" xml:space="preserve"><value>Đặt lại mật khẩu</value></data>
  <data name="Staff_ResetTitle" xml:space="preserve"><value>Đặt lại mật khẩu cho {0}</value></data>
  <data name="Staff_PasswordReset" xml:space="preserve"><value>Đã đổi mật khẩu.</value></data>
  <data name="Staff_Lock" xml:space="preserve"><value>Khóa / mở khóa</value></data>
  <data name="Staff_ConfirmLock" xml:space="preserve"><value>Khóa tài khoản {0}? Người này sẽ không đăng nhập được nữa.</value></data>
  <data name="Staff_ConfirmUnlock" xml:space="preserve"><value>Mở khóa tài khoản {0}?</value></data>
  <data name="Staff_Locked" xml:space="preserve"><value>Đã khóa tài khoản.</value></data>
  <data name="Staff_Unlocked" xml:space="preserve"><value>Đã mở khóa tài khoản.</value></data>
  <data name="StaffRole_Operator" xml:space="preserve"><value>Điều hành</value></data>
  <data name="StaffRole_TicketSeller" xml:space="preserve"><value>Nhân viên bán vé</value></data>
  <data name="StaffStatus_Active" xml:space="preserve"><value>Hoạt động</value></data>
  <data name="StaffStatus_Locked" xml:space="preserve"><value>Đã khóa</value></data>
```

- [ ] **Step 8: Build, test, manual check, commit**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests` → 0 warnings, `failed: 0`.
Manual (as `operator01`): create `seller03` / `Seller#003` → sign out → sign in as `seller03` works; lock `seller03` as operator → `seller03` sign-in shows "Tài khoản đã bị khóa."; try to lock `OPERATOR01` (yourself) → error "Bạn không thể khóa hoặc đổi quyền tài khoản của chính mình.".
```bash
git add src/Transport.Presentation.Wpf tests/Transport.UnitTests
git commit -m "feat(ui): add staff management screen"
```

---

### Task 11: Backup screen

**Files:**
- Create: `ViewModels/Pages/BackupViewModel.cs`, `Views/Pages/BackupPage.xaml(.cs)`
- Modify: `Views/PageTemplates.xaml`, `PresentationServices.cs`, both `.resx`
- Test: `tests/Transport.UnitTests/Presentation/BackupViewModelTests.cs`

**Interfaces:**
- Consumes: `BackupService`, `FakeBackupRepository` (Plan 2).
- Produces: `BackupViewModel(PageServices, BackupService)` with `Logs`, `JobStatus`, `JobStateText`, `BackupNowCommand`, `ExportToExcelCommand`.

- [ ] **Step 1: Write the failing test**

```csharp
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Presentation.Wpf.ViewModels.Pages;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class BackupViewModelTests
{
    private readonly PageTestContext _context = new();
    private readonly FakeBackupRepository _repository = new();

    public BackupViewModelTests()
    {
        _repository.Logs.Add(new BackupLog
        {
            LogId = 3,
            BackupAt = new DateTime(2026, 10, 17, 2, 0, 0),
            PerformedBy = "TRANSPORT",
            ScheduleRows = 35,
            TicketRows = 210,
            PaymentRows = 190,
        });
        _repository.JobStatus = new BackupJobStatus
        {
            JobName = "JOB_NIGHTLY_BACKUP",
            Enabled = "TRUE",
            State = "SCHEDULED",
            NextRunDate = new DateTime(2026, 10, 18, 2, 0, 0),
            FailureCount = 0,
        };
    }

    private BackupViewModel Create() => new(_context.Services, new BackupService(_repository, _context.Session));

    [Fact]
    public async Task Load_ShowsLogsAndJobStatus()
    {
        var vm = Create();

        await vm.LoadAsync();

        Assert.Single(vm.Logs);
        Assert.Equal("Backup_JobEnabled", vm.JobStateText);
        Assert.Equal(new DateTime(2026, 10, 18, 2, 0, 0), vm.JobStatus!.NextRunDate);
    }

    [Fact]
    public async Task BackupNow_RunsAndReportsLatestCounts()
    {
        var vm = Create();

        await vm.BackupNowCommand.ExecuteAsync(null);

        Assert.Equal(1, _repository.RunCount);
        Assert.Equal(new[] { "Backup_Done|3,35,210,190" }, _context.Notifications.Successes);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build error `BackupViewModel` not found.

- [ ] **Step 3: Write `ViewModels/Pages/BackupViewModel.cs`**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Models;
using Transport.Application.Services;

namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Sao lưu lịch chạy + vé + thanh toán (BACKUP_OPERATIONAL_DATA) và theo dõi job sao lưu đêm
public sealed partial class BackupViewModel : PageViewModel
{
    private readonly BackupService _backups;

    public BackupViewModel(PageServices services, BackupService backups)
        : base(services)
    {
        _backups = backups;
        JobStateText = string.Empty;
    }

    public ObservableCollection<BackupLog> Logs { get; } = [];

    [ObservableProperty]
    public partial BackupJobStatus? JobStatus { get; set; }

    [ObservableProperty]
    public partial string JobStateText { get; set; }

    public override Task LoadAsync() => RunBusyAsync(LoadCoreAsync);

    [RelayCommand]
    private Task BackupNowAsync() => RunBusyAsync(async () =>
    {
        await _backups.RunAsync();
        await LoadCoreAsync();
        if (Logs.FirstOrDefault() is { } latest)
        {
            Notify.ShowSuccess(Loc.Format("Backup_Done", latest.LogId, latest.ScheduleRows ?? 0,
                latest.TicketRows ?? 0, latest.PaymentRows ?? 0));
        }
    });

    [RelayCommand]
    private Task ExportToExcelAsync() => ExportAsync(Logs.ToList(),
    [
        new("Backup_LogId", b => b.LogId),
        new("Backup_At", b => b.BackupAt, "dd/MM/yyyy HH:mm"),
        new("Backup_By", b => b.PerformedBy),
        new("Backup_Schedules", b => b.ScheduleRows),
        new("Backup_Tickets", b => b.TicketRows),
        new("Backup_Payments", b => b.PaymentRows),
    ], "Backup_ExportTitle");

    private async Task LoadCoreAsync()
    {
        var logs = await _backups.GetLogsAsync();
        Logs.Clear();
        foreach (var log in logs)
        {
            Logs.Add(log);
        }

        JobStatus = await _backups.GetJobStatusAsync();
        JobStateText = Loc[JobStatus?.IsEnabled == true ? "Backup_JobEnabled" : "Backup_JobDisabled"];
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 5: Write `Views/Pages/BackupPage.xaml`** (+ code-behind)

```xml
<UserControl x:Class="Transport.Presentation.Wpf.Views.Pages.BackupPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
             xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls"
             xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Pages"
             mc:Ignorable="d" d:DesignWidth="1100" d:DesignHeight="680"
             d:DataContext="{d:DesignInstance Type=vm:BackupViewModel}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>

        <UniformGrid Columns="2" Margin="0,0,0,16">
            <!-- Sao lưu ngay -->
            <Border Style="{StaticResource CardStyle}" Margin="0,0,12,0">
                <StackPanel>
                    <TextBlock Text="{l:Loc Backup_Now}" Style="{StaticResource SectionTitleStyle}" />
                    <TextBlock Text="{l:Loc Backup_NowHint}" Style="{StaticResource StatLabelStyle}" />
                    <Button Content="{l:Loc Backup_Now}" Command="{Binding BackupNowCommand}" Style="{StaticResource PrimaryButtonStyle}"
                            HorizontalAlignment="Left" Margin="0,16,0,0" />
                    <TextBlock Text="{l:Loc Backup_DataPumpHint}" Style="{StaticResource StatLabelStyle}" Margin="0,16,0,0" />
                </StackPanel>
            </Border>

            <!-- Job sao lưu đêm -->
            <Border Style="{StaticResource CardStyle}">
                <StackPanel>
                    <TextBlock Text="{l:Loc Backup_Job}" Style="{StaticResource SectionTitleStyle}" />
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="180" />
                            <ColumnDefinition />
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto" />
                            <RowDefinition Height="Auto" />
                            <RowDefinition Height="Auto" />
                            <RowDefinition Height="Auto" />
                        </Grid.RowDefinitions>
                        <TextBlock Text="{l:Loc Backup_JobState}" Style="{StaticResource StatLabelStyle}" />
                        <TextBlock Grid.Column="1" Text="{Binding JobStateText}" FontWeight="SemiBold" />
                        <TextBlock Grid.Row="1" Text="{l:Loc Backup_LastRun}" Style="{StaticResource StatLabelStyle}" Margin="0,6,0,0" />
                        <TextBlock Grid.Row="1" Grid.Column="1" Margin="0,6,0,0"
                                   Text="{Binding JobStatus.LastStartDate, StringFormat={}{0:dd/MM/yyyy HH:mm}, TargetNullValue=–, FallbackValue=–}" />
                        <TextBlock Grid.Row="2" Text="{l:Loc Backup_NextRun}" Style="{StaticResource StatLabelStyle}" Margin="0,6,0,0" />
                        <TextBlock Grid.Row="2" Grid.Column="1" Margin="0,6,0,0"
                                   Text="{Binding JobStatus.NextRunDate, StringFormat={}{0:dd/MM/yyyy HH:mm}, TargetNullValue=–, FallbackValue=–}" />
                        <TextBlock Grid.Row="3" Text="{l:Loc Backup_Failures}" Style="{StaticResource StatLabelStyle}" Margin="0,6,0,0" />
                        <TextBlock Grid.Row="3" Grid.Column="1" Margin="0,6,0,0"
                                   Text="{Binding JobStatus.FailureCount, TargetNullValue=0, FallbackValue=–}" />
                    </Grid>
                </StackPanel>
            </Border>
        </UniformGrid>

        <!-- Lịch sử sao lưu -->
        <Border Grid.Row="1" Style="{StaticResource CardStyle}">
            <DockPanel>
                <DockPanel DockPanel.Dock="Top" Margin="0,0,0,8">
                    <Button DockPanel.Dock="Right" Content="{l:Loc Btn_ExportExcel}" Command="{Binding ExportToExcelCommand}" c:Shortcut.Gesture="Ctrl+E" />
                    <TextBlock Text="{l:Loc Backup_History}" Style="{StaticResource SectionTitleStyle}" VerticalAlignment="Center" />
                </DockPanel>
                <DataGrid Style="{StaticResource ListGridStyle}" ItemsSource="{Binding Logs}" AutomationProperties.Name="{l:Loc Backup_History}">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="{l:Loc Backup_LogId}" Binding="{Binding LogId}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Backup_At}" Binding="{Binding BackupAt, StringFormat={}{0:dd/MM/yyyy HH:mm:ss}}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Backup_By}" Binding="{Binding PerformedBy}" Width="*" />
                        <DataGridTextColumn Header="{l:Loc Backup_Schedules}" Binding="{Binding ScheduleRows}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Backup_Tickets}" Binding="{Binding TicketRows}" Width="Auto" />
                        <DataGridTextColumn Header="{l:Loc Backup_Payments}" Binding="{Binding PaymentRows}" Width="Auto" />
                    </DataGrid.Columns>
                </DataGrid>
            </DockPanel>
        </Border>
    </Grid>
</UserControl>
```

- [ ] **Step 6: Register** – `PageTemplates.xaml`: `<DataTemplate DataType="{x:Type vm:BackupViewModel}"><pages:BackupPage /></DataTemplate>`; `AddPages`: `services.AddPage<BackupViewModel>("Menu_Backup", "\uE74E", 110, operatorOnly: true);`

- [ ] **Step 7: Add resource keys**

`Strings.resx`:
```xml
  <data name="Menu_Backup" xml:space="preserve"><value>Backup</value></data>
  <data name="Backup_Now" xml:space="preserve"><value>Back up now</value></data>
  <data name="Backup_NowHint" xml:space="preserve"><value>Copies all schedules, tickets and payments into the backup tables (the 7 latest copies are kept).</value></data>
  <data name="Backup_Done" xml:space="preserve"><value>Backup #{0} completed: {1} schedules, {2} tickets, {3} payments.</value></data>
  <data name="Backup_DataPumpHint" xml:space="preserve"><value>Full export / restore of the whole schema: run database\backup.bat or database\restore.bat &lt;file.dmp&gt; (Data Pump, folder D:\OracleBackup).</value></data>
  <data name="Backup_Job" xml:space="preserve"><value>Nightly backup job (02:00)</value></data>
  <data name="Backup_JobState" xml:space="preserve"><value>Status</value></data>
  <data name="Backup_JobEnabled" xml:space="preserve"><value>Enabled</value></data>
  <data name="Backup_JobDisabled" xml:space="preserve"><value>Disabled</value></data>
  <data name="Backup_LastRun" xml:space="preserve"><value>Last run</value></data>
  <data name="Backup_NextRun" xml:space="preserve"><value>Next run</value></data>
  <data name="Backup_Failures" xml:space="preserve"><value>Failures</value></data>
  <data name="Backup_History" xml:space="preserve"><value>Backup history</value></data>
  <data name="Backup_LogId" xml:space="preserve"><value>#</value></data>
  <data name="Backup_At" xml:space="preserve"><value>Time</value></data>
  <data name="Backup_By" xml:space="preserve"><value>Performed by</value></data>
  <data name="Backup_Schedules" xml:space="preserve"><value>Schedules</value></data>
  <data name="Backup_Tickets" xml:space="preserve"><value>Tickets</value></data>
  <data name="Backup_Payments" xml:space="preserve"><value>Payments</value></data>
  <data name="Backup_ExportTitle" xml:space="preserve"><value>Backup history</value></data>
```

`Strings.vi.resx`:
```xml
  <data name="Menu_Backup" xml:space="preserve"><value>Sao lưu</value></data>
  <data name="Backup_Now" xml:space="preserve"><value>Sao lưu ngay</value></data>
  <data name="Backup_NowHint" xml:space="preserve"><value>Chép toàn bộ lịch chạy, vé và thanh toán sang các bảng sao lưu (giữ 7 bản gần nhất).</value></data>
  <data name="Backup_Done" xml:space="preserve"><value>Đã sao lưu lần #{0}: {1} lịch chạy, {2} vé, {3} thanh toán.</value></data>
  <data name="Backup_DataPumpHint" xml:space="preserve"><value>Sao lưu / khôi phục toàn bộ schema: chạy database\backup.bat hoặc database\restore.bat &lt;file.dmp&gt; (Data Pump, thư mục D:\OracleBackup).</value></data>
  <data name="Backup_Job" xml:space="preserve"><value>Job sao lưu hằng đêm (02:00)</value></data>
  <data name="Backup_JobState" xml:space="preserve"><value>Trạng thái</value></data>
  <data name="Backup_JobEnabled" xml:space="preserve"><value>Đang bật</value></data>
  <data name="Backup_JobDisabled" xml:space="preserve"><value>Đang tắt</value></data>
  <data name="Backup_LastRun" xml:space="preserve"><value>Lần chạy gần nhất</value></data>
  <data name="Backup_NextRun" xml:space="preserve"><value>Lần chạy tiếp theo</value></data>
  <data name="Backup_Failures" xml:space="preserve"><value>Số lần lỗi</value></data>
  <data name="Backup_History" xml:space="preserve"><value>Lịch sử sao lưu</value></data>
  <data name="Backup_LogId" xml:space="preserve"><value>Lần</value></data>
  <data name="Backup_At" xml:space="preserve"><value>Thời điểm</value></data>
  <data name="Backup_By" xml:space="preserve"><value>Người thực hiện</value></data>
  <data name="Backup_Schedules" xml:space="preserve"><value>Lịch chạy</value></data>
  <data name="Backup_Tickets" xml:space="preserve"><value>Vé</value></data>
  <data name="Backup_Payments" xml:space="preserve"><value>Thanh toán</value></data>
  <data name="Backup_ExportTitle" xml:space="preserve"><value>Lịch sử sao lưu</value></data>
```

- [ ] **Step 8: Build, test, commit**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests` → 0 warnings, `failed: 0`.
```bash
git add src/Transport.Presentation.Wpf tests/Transport.UnitTests
git commit -m "feat(ui): add backup screen"
```

---

### Task 12: End-to-end verification (both roles)

**Files:** none created; fix whatever this task uncovers in the files of the task that introduced it (commit each fix separately with a `fix(...)` message).

- [ ] **Step 1: Full automated run**

Run:
```bash
dotnet build TransportManagement.slnx -c Release
dotnet test --project tests/Transport.UnitTests
TRANSPORT_DB_TESTS=1 dotnet test --project tests/Transport.UnitTests
export NLS_LANG=AMERICAN_AMERICA.AL32UTF8
sqlplus -S -L 'transport/"Transport#2026"@//localhost:1521/XEPDB1' @database/09_test_scenarios.sql
```
Expected: Release build 0 warnings; both test runs `failed: 0`; SQL script ends with `ALL TESTS PASSED`.

- [ ] **Step 2: Operator walkthrough** (`operator01` / `Operator#01`; ask the user to perform it or use computer-use if available; note every deviation)

1. Tổng quan: 4 cards show numbers; 7-day bar chart shows bars for the seeded past days.
2. Sidebar shows 11 items in the order of the Global Constraints table.
3. Tuyến xe / Phương tiện / Tài xế / Hành khách: add, edit (double-click), delete (confirm), search, Ctrl+E export opens Save dialog and the `.xlsx` opens in Excel with title, timestamp, formatted columns.
4. Tài xế: Phạm Văn Dũng row is yellow, warning banner visible.
5. Lịch chạy: add trip for tomorrow (price + arrival auto-filled; drivers with expired licence are not offered); "Bắt đầu chạy" → "Hoàn thành" on another trip; "Hủy chuyến" on a trip with tickets → message about refunds.
6. Bán vé: sell + pay + print to PDF (see Task 6 manual check); price filter works.
7. Quản lý vé: the cancelled trip's tickets show "Đã hủy" with reason "Trip cancelled: ..."; cancel a paid ticket → refund message; Ctrl+P prints.
8. Thống kê: change month/route, totals and route chart update; export.
9. Nhân viên: create/lock/unlock/reset password (see Task 10 manual check).
10. Sao lưu: "Sao lưu ngay" → success message with counts; history grows; job shows "Đang bật" and next run at 02:00.
11. Switch language to English on the top bar → every label, menu, grid header, button and message is English; switch back.

- [ ] **Step 3: Seller walkthrough** (`seller01` / `Seller#001`)

1. Sidebar has only: Tổng quan, Bán vé, Quản lý vé, Hành khách, Tuyến xe, Lịch chạy, Thống kê.
2. Tuyến xe and Lịch chạy have no New/Edit/Delete or status buttons.
3. Hành khách: can add and edit, no Delete button.
4. Bán vé and Quản lý vé fully work.

- [ ] **Step 4: Commit any fixes, then tag the milestone**

```bash
git tag -a v1.0-app -m "All screens implemented and verified"
```
