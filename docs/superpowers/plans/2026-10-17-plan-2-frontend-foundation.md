# Plan 2 – Frontend Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Clean Architecture solution (Domain, Application, Infrastructure, Presentation.Wpf, UnitTests) up to a runnable WPF shell: Oracle login, role-aware main window, localization VI/EN, notifications, dialogs, Excel export – everything the screens of Plan 3 plug into.

**Architecture:** Dependencies point inward: Presentation.Wpf → Application → Domain ← Infrastructure. Infrastructure implements Application interfaces with ODP.NET + Dapper and maps Oracle errors to `BusinessRuleException`. Presentation uses MVVM (CommunityToolkit.Mvvm source generators), Generic Host DI, the built-in WPF Fluent theme and `.resx` localization.

**Tech Stack:** .NET SDK 10.0.402, C# 14, WPF (`net10.0-windows`), CommunityToolkit.Mvvm 8.4.2, Microsoft.Extensions.Hosting 10.0.13, Oracle.ManagedDataAccess.Core 23.26.301, Dapper 2.1.89, ClosedXML 0.105.1, xunit.v3 4.0.2 on Microsoft Testing Platform.

**Spec:** `docs/superpowers/specs/2026-10-17-transport-management-design.md` (§9 error table, §12 frontend). **Prerequisite:** Plan 1 completed (database installed, demo accounts exist).

## Global Constraints

- Identifiers in C#/XAML/SQL are **English**; code comments are **Vietnamese**; user-visible text comes **only** from `Resources/Strings.resx` (English) and `Resources/Strings.vi.resx` (Vietnamese) – never hard-code UI text in XAML or C#.
- Every key added to one `.resx` must be added to the other (a unit test enforces it). Every `BusinessErrorCode` value needs an `Err_<number>` key.
- `Nullable` enabled, warnings are errors, file-scoped namespaces, one public type per file (small records/enums of the same feature may share a file only where this plan says so).
- ViewModels never reference `System.Windows.*`. Window code-behind may only call `InitializeComponent()` and assign the injected ViewModel to `DataContext`; dialog/page code-behind only calls `InitializeComponent()`.
- `[ObservableProperty]` partial properties **cannot have initializers** – assign defaults in the constructor.
- Inside `Transport.Presentation.Wpf.*`, the identifier `Application` binds to the namespace `Transport.Application`; always write `System.Windows.Application` for the WPF type.
- Oracle bind variables are always named `:p_<name>`; Dapper anonymous objects use the same `p_<name>` property names. Never concatenate user input into SQL.
- Money format in XAML: `StringFormat={}{0:N0} ₫`; dates: `{}{0:dd/MM/yyyy HH:mm}` (WPF `Language` is forced to `vi-VN` at startup).
- Run all commands from the repo root `D:/DuAn/OracleDB_QuanLyVanTai`. Test command: `dotnet test --project tests/Transport.UnitTests` (Microsoft Testing Platform via `global.json`).

---

### Task 1: Solution skeleton

**Files:**
- Create: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `TransportManagement.slnx`
- Create: `src/Transport.Domain/Transport.Domain.csproj`
- Create: `src/Transport.Application/Transport.Application.csproj`
- Create: `src/Transport.Infrastructure/Transport.Infrastructure.csproj`
- Create: `src/Transport.Presentation.Wpf/Transport.Presentation.Wpf.csproj`, `App.xaml`, `App.xaml.cs`
- Create: `tests/Transport.UnitTests/Transport.UnitTests.csproj`, `tests/Transport.UnitTests/SmokeTests.cs`
- Modify: `.gitignore`

**Interfaces:**
- Produces: buildable solution; namespaces `Transport.Domain`, `Transport.Application`, `Transport.Infrastructure`, `Transport.Presentation.Wpf`, `Transport.UnitTests`.

- [ ] **Step 1: Write `global.json`**

```json
{
  "sdk": {
    "version": "10.0.402",
    "rollForward": "latestFeature"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

- [ ] **Step 2: Write `Directory.Build.props`**

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <!-- Cảnh báo lỗ hổng gói NuGet chỉ hiện cảnh báo, không chặn build -->
    <WarningsNotAsErrors>NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <!-- Chỉ giữ tài nguyên ngôn ngữ en/vi của các gói NuGet -->
    <SatelliteResourceLanguages>en;vi</SatelliteResourceLanguages>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Write `Directory.Packages.props`**

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="ClosedXML" Version="0.105.1" />
    <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.2" />
    <PackageVersion Include="Dapper" Version="2.1.89" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.13" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="10.0.13" />
    <PackageVersion Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="10.0.13" />
    <PackageVersion Include="Oracle.ManagedDataAccess.Core" Version="23.26.301" />
    <PackageVersion Include="xunit.v3" Version="4.0.2" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Write `.editorconfig`**

```ini
root = true

[*]
charset = utf-8
end_of_line = crlf
insert_final_newline = true
indent_style = space
indent_size = 4
trim_trailing_whitespace = true

[*.{json,xml,csproj,props,slnx,resx}]
indent_size = 2

[*.cs]
csharp_style_namespace_declarations = file_scoped:warning
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_prefer_braces = when_multiline:suggestion
dotnet_style_qualification_for_field = false:suggestion
dotnet_sort_system_directives_first = true

# Trường private: _camelCase
dotnet_naming_rule.private_fields_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_underscore.style = underscore_camel
dotnet_naming_rule.private_fields_underscore.severity = suggestion
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_style.underscore_camel.required_prefix = _
dotnet_naming_style.underscore_camel.capitalization = camel_case
```

- [ ] **Step 5: Write the four `src` project files**

`src/Transport.Domain/Transport.Domain.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

`src/Transport.Application/Transport.Application.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Transport.Domain\Transport.Domain.csproj" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>
  <ItemGroup>
    <InternalsVisibleTo Include="Transport.UnitTests" />
  </ItemGroup>
</Project>
```

`src/Transport.Infrastructure/Transport.Infrastructure.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Transport.Application\Transport.Application.csproj" />
    <PackageReference Include="ClosedXML" />
    <PackageReference Include="Dapper" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
    <PackageReference Include="Oracle.ManagedDataAccess.Core" />
  </ItemGroup>
  <ItemGroup>
    <InternalsVisibleTo Include="Transport.UnitTests" />
  </ItemGroup>
</Project>
```

`src/Transport.Presentation.Wpf/Transport.Presentation.Wpf.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Transport.Application\Transport.Application.csproj" />
    <!-- Chỉ dùng ở composition root (App.xaml.cs) để gọi AddInfrastructure() -->
    <ProjectReference Include="..\Transport.Infrastructure\Transport.Infrastructure.csproj" />
    <PackageReference Include="CommunityToolkit.Mvvm" />
    <PackageReference Include="Microsoft.Extensions.Hosting" />
  </ItemGroup>
  <ItemGroup>
    <None Update="appsettings.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
  <ItemGroup>
    <InternalsVisibleTo Include="Transport.UnitTests" />
  </ItemGroup>
</Project>
```

- [ ] **Step 6: Write a minimal WPF app so the project builds**

`src/Transport.Presentation.Wpf/App.xaml`:
```xml
<Application x:Class="Transport.Presentation.Wpf.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources />
</Application>
```

`src/Transport.Presentation.Wpf/App.xaml.cs`:
```csharp
namespace Transport.Presentation.Wpf;

public partial class App : System.Windows.Application
{
}
```

- [ ] **Step 7: Write the test project and a smoke test**

`tests/Transport.UnitTests/Transport.UnitTests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
    <!-- Không bắt buộc truyền TestContext.Current.CancellationToken trong test -->
    <NoWarn>$(NoWarn);xUnit1051</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <ProjectReference Include="..\..\src\Transport.Presentation.Wpf\Transport.Presentation.Wpf.csproj" />
  </ItemGroup>
  <ItemGroup>
    <!-- Chép file .resx ra thư mục test để kiểm tra hai ngôn ngữ có cùng bộ khóa -->
    <None Include="..\..\src\Transport.Presentation.Wpf\Resources\*.resx" LinkBase="Resources" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

`tests/Transport.UnitTests/SmokeTests.cs`:
```csharp
namespace Transport.UnitTests;

public class SmokeTests
{
    [Fact]
    public void TestRunnerWorks() => Assert.True(true);
}
```

- [ ] **Step 8: Create the solution file**

Run:
```bash
dotnet new sln -n TransportManagement --format slnx
dotnet sln TransportManagement.slnx add src/Transport.Domain/Transport.Domain.csproj src/Transport.Application/Transport.Application.csproj src/Transport.Infrastructure/Transport.Infrastructure.csproj src/Transport.Presentation.Wpf/Transport.Presentation.Wpf.csproj --solution-folder src
dotnet sln TransportManagement.slnx add tests/Transport.UnitTests/Transport.UnitTests.csproj --solution-folder tests
```
Expected: `TransportManagement.slnx` lists 5 projects.

- [ ] **Step 9: Extend `.gitignore`**

Append:
```
TestResults/
*.slnx.user
```

- [ ] **Step 10: Build and run the smoke test**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests`
Expected: build succeeds with 0 warnings; test summary `total: 1, failed: 0, succeeded: 1`.

- [ ] **Step 11: Commit**

```bash
git add global.json Directory.Build.props Directory.Packages.props .editorconfig .gitignore TransportManagement.slnx src tests
git commit -m "chore: scaffold Clean Architecture solution"
```

---

### Task 2: Domain layer

**Files:**
- Create: `src/Transport.Domain/Enums/VehicleStatus.cs`, `DriverStatus.cs`, `LicenseClass.cs`, `LicenseState.cs`, `ScheduleStatus.cs`, `TicketStatus.cs`, `PaymentMethod.cs`, `StaffRole.cs`, `StaffStatus.cs`
- Create: `src/Transport.Domain/Entities/Route.cs`, `Vehicle.cs`, `Driver.cs`, `Schedule.cs`, `Passenger.cs`, `StaffMember.cs`
- Create: `src/Transport.Domain/Errors/BusinessErrorCode.cs`, `BusinessRuleException.cs`
- Test: `tests/Transport.UnitTests/Domain/DriverTests.cs`
- Delete: `tests/Transport.UnitTests/SmokeTests.cs`

**Interfaces:**
- Produces (namespace `Transport.Domain.Entities`): records `Route`, `Vehicle`, `Driver`, `Schedule`, `Passenger`, `StaffMember` (properties below); `Driver.GetLicenseState(DateTime today) : LicenseState`, `Driver.CanDrive(DateTime departure) : bool`, `Driver.ExpiryWarningDays = 30`.
- Produces (namespace `Transport.Domain.Enums`): the nine enums below.
- Produces (namespace `Transport.Domain.Errors`): `BusinessErrorCode`, `BusinessRuleException(BusinessErrorCode code, string message)` with `Code`.

- [ ] **Step 1: Write the failing test `tests/Transport.UnitTests/Domain/DriverTests.cs`**

```csharp
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.UnitTests.Domain;

public class DriverTests
{
    private static readonly DateTime Today = new(2026, 10, 17);

    private static Driver CreateDriver(DateTime expiry, DriverStatus status = DriverStatus.Active) => new()
    {
        Id = 1,
        FullName = "Nguyễn Văn An",
        IdCard = "001085012345",
        Phone = "0912345601",
        LicenseNo = "790123456781",
        LicenseClass = LicenseClass.D,
        LicenseExpiry = expiry,
        Status = status,
    };

    [Theory]
    [InlineData(-1, LicenseState.Expired)]
    [InlineData(0, LicenseState.ExpiringSoon)]
    [InlineData(29, LicenseState.ExpiringSoon)]
    [InlineData(30, LicenseState.Valid)]
    public void GetLicenseState_UsesThirtyDayWarningWindow(int daysUntilExpiry, LicenseState expected)
    {
        var driver = CreateDriver(Today.AddDays(daysUntilExpiry));

        Assert.Equal(expected, driver.GetLicenseState(Today));
    }

    [Fact]
    public void CanDrive_RequiresActiveStatusAndLicenseValidOnDepartureDay()
    {
        var driver = CreateDriver(Today.AddDays(10));

        Assert.True(driver.CanDrive(Today.AddDays(10).AddHours(20)));
        Assert.False(driver.CanDrive(Today.AddDays(11)));
        Assert.False((driver with { Status = DriverStatus.Inactive }).CanDrive(Today));
    }
}
```

Delete `tests/Transport.UnitTests/SmokeTests.cs`.

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build error `The type or namespace name 'Entities' does not exist in the namespace 'Transport.Domain'`.

- [ ] **Step 3: Write the enums** (one file each, namespace `Transport.Domain.Enums`)

```csharp
namespace Transport.Domain.Enums;

public enum VehicleStatus
{
    Active,
    Maintenance,
    Retired,
}
```

```csharp
namespace Transport.Domain.Enums;

public enum DriverStatus
{
    Active,
    Inactive,
}
```

```csharp
namespace Transport.Domain.Enums;

// D1/D2/D theo Luật TTATGTĐB 2024, E là hạng bằng cũ còn hiệu lực
public enum LicenseClass
{
    D1,
    D2,
    D,
    E,
}
```

```csharp
namespace Transport.Domain.Enums;

public enum LicenseState
{
    Valid,
    ExpiringSoon,
    Expired,
}
```

```csharp
namespace Transport.Domain.Enums;

public enum ScheduleStatus
{
    Scheduled,
    Running,
    Completed,
    Cancelled,
}
```

```csharp
namespace Transport.Domain.Enums;

public enum TicketStatus
{
    Booked,
    Paid,
    Cancelled,
}
```

```csharp
namespace Transport.Domain.Enums;

public enum PaymentMethod
{
    Cash,
    Transfer,
    Card,
}
```

```csharp
namespace Transport.Domain.Enums;

// Ánh xạ tới role Oracle ROLE_OPERATOR / ROLE_TICKET_SELLER
public enum StaffRole
{
    Operator,
    TicketSeller,
}
```

```csharp
namespace Transport.Domain.Enums;

public enum StaffStatus
{
    Active,
    Locked,
}
```

- [ ] **Step 4: Write the entities** (namespace `Transport.Domain.Entities`)

`Route.cs`:
```csharp
namespace Transport.Domain.Entities;

// Tuyến xe
public sealed record Route
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public int? DistanceKm { get; init; }
    public int? DurationMin { get; init; }
    public decimal BasePrice { get; init; }
}
```

`Vehicle.cs`:
```csharp
using Transport.Domain.Enums;

namespace Transport.Domain.Entities;

// Phương tiện
public sealed record Vehicle
{
    public int Id { get; init; }
    public required string PlateNumber { get; init; }
    public required string VehicleType { get; init; }
    public int SeatCount { get; init; }
    public VehicleStatus Status { get; init; }
}
```

`Driver.cs`:
```csharp
using Transport.Domain.Enums;

namespace Transport.Domain.Entities;

// Tài xế
public sealed record Driver
{
    // Cảnh báo khi bằng lái còn dưới số ngày này
    public const int ExpiryWarningDays = 30;

    public int Id { get; init; }
    public required string FullName { get; init; }
    public required string IdCard { get; init; }
    public required string Phone { get; init; }
    public required string LicenseNo { get; init; }
    public LicenseClass LicenseClass { get; init; }
    public DateTime LicenseExpiry { get; init; }
    public DriverStatus Status { get; init; }

    // Tình trạng bằng lái so với ngày tham chiếu
    public LicenseState GetLicenseState(DateTime today) =>
        LicenseExpiry.Date < today.Date ? LicenseState.Expired
        : LicenseExpiry.Date < today.Date.AddDays(ExpiryWarningDays) ? LicenseState.ExpiringSoon
        : LicenseState.Valid;

    // Có được phân công chạy chuyến khởi hành vào thời điểm này không
    public bool CanDrive(DateTime departure) =>
        Status == DriverStatus.Active && LicenseExpiry.Date >= departure.Date;
}
```

`Schedule.cs`:
```csharp
using Transport.Domain.Enums;

namespace Transport.Domain.Entities;

// Lịch chạy (dùng khi thêm/sửa; danh sách đọc từ V_TRIP_STATISTICS)
public sealed record Schedule
{
    public int Id { get; init; }
    public int RouteId { get; init; }
    public int VehicleId { get; init; }
    public int DriverId { get; init; }
    public DateTime DepartureTime { get; init; }
    public DateTime ArrivalTime { get; init; }
    public decimal TicketPrice { get; init; }
    public ScheduleStatus Status { get; init; }
}
```

`Passenger.cs`:
```csharp
namespace Transport.Domain.Entities;

// Hành khách
public sealed record Passenger
{
    public int Id { get; init; }
    public required string FullName { get; init; }
    public required string Phone { get; init; }
    public string? IdCard { get; init; }
    public string? Email { get; init; }
}
```

`StaffMember.cs`:
```csharp
using Transport.Domain.Enums;

namespace Transport.Domain.Entities;

// Nhân viên = một user Oracle (bảng STAFF)
public sealed record StaffMember
{
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public string? Phone { get; init; }
    public StaffRole Role { get; init; }
    public StaffStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public required string CreatedBy { get; init; }
}
```

- [ ] **Step 5: Write the error types** (namespace `Transport.Domain.Errors`)

`BusinessErrorCode.cs`:
```csharp
namespace Transport.Domain.Errors;

// Mã lỗi nghiệp vụ. Giá trị trùng mã lỗi Oracle (dạng dương) để frontend tra bản dịch Err_<mã>.
public enum BusinessErrorCode
{
    // Lỗi từ RAISE_APPLICATION_ERROR (spec mục 9)
    SeatNotFound = 20001,
    ScheduleNotOpenForSale = 20002,
    SeatAlreadyBooked = 20003,
    ScheduleDepartedForBooking = 20010,
    ScheduleNotFoundForBooking = 20011,
    TicketNotPayable = 20020,
    TicketNotFoundForPayment = 20021,
    TicketAlreadyCancelled = 20030,
    ScheduleDepartedForCancel = 20031,
    TicketNotFoundForCancel = 20032,
    InvalidUsername = 20040,
    InvalidRole = 20041,
    StaffNotFound = 20042,
    CannotChangeOwnAccount = 20043,
    InvalidPassword = 20044,
    ScheduleNotFound = 20050,
    ScheduleNotCancellable = 20051,
    ScheduleHasActiveTickets = 20052,

    // Lỗi hệ thống Oracle hay gặp (giá trị = số ORA)
    DuplicateValue = 1,
    InvalidCredentials = 1017,
    InsufficientPrivileges = 1031,
    UsernameAlreadyExists = 1920,
    ChildRecordExists = 2292,
    CheckConstraintViolated = 2290,
    AccountLocked = 28000,
    DatabaseUnavailable = 12541,

    // Lỗi kiểm tra phía client, không đi tới Oracle
    RequiredField = 90001,
    InvalidPhone = 90002,
    InvalidSeat = 90003,
    InvalidTimeRange = 90004,
    InvalidPrice = 90005,
    OperatorOnly = 90006,
}
```

`BusinessRuleException.cs`:
```csharp
namespace Transport.Domain.Errors;

// Vi phạm luật nghiệp vụ; Message là thông báo tiếng Anh, giao diện dịch theo Code
public sealed class BusinessRuleException(BusinessErrorCode code, string message) : Exception(message)
{
    public BusinessErrorCode Code { get; } = code;
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `total: 5, failed: 0`.

- [ ] **Step 7: Commit**

```bash
git add src/Transport.Domain tests/Transport.UnitTests
git commit -m "feat(domain): add entities, enums and business errors"
```

---

### Task 3: Application abstractions, models and TicketService

**Files:**
- Create: `src/Transport.Application/Abstractions/ICrudRepository.cs`, `IRouteRepository.cs`, `IVehicleRepository.cs`, `IDriverRepository.cs`, `IPassengerRepository.cs`, `IScheduleRepository.cs`, `ITicketRepository.cs`, `IStaffRepository.cs`, `IStatisticsQueries.cs`, `IBackupRepository.cs`, `IAuthService.cs`, `IUserSession.cs`, `IExcelExporter.cs`
- Create: `src/Transport.Application/Models/TripStatistic.cs`, `TripSearchCriteria.cs`, `BookingRequest.cs`, `TicketSearchCriteria.cs`, `TicketListItem.cs`, `TicketPrintModel.cs`, `DailyRevenue.cs`, `DashboardSummary.cs`, `BackupLog.cs`, `BackupJobStatus.cs`, `NewStaffRequest.cs`, `ExportColumn.cs`
- Create: `src/Transport.Application/Services/Rules.cs`, `TicketService.cs`
- Test: `tests/Transport.UnitTests/Fakes/FakeTicketRepository.cs`, `tests/Transport.UnitTests/Application/TicketServiceTests.cs`

**Interfaces:**
- Consumes: Domain types (Task 2).
- Produces: every interface/model below exactly as written (later tasks implement or consume them). `TicketService` public API:
  - `Task<IReadOnlySet<int>> GetBookedSeatsAsync(int scheduleId, CancellationToken ct = default)`
  - `Task<long> BookAsync(BookingRequest request, CancellationToken ct = default)`
  - `Task PayAsync(long ticketId, PaymentMethod method, CancellationToken ct = default)`
  - `Task CancelAsync(long ticketId, string reason, CancellationToken ct = default)`
  - `Task<IReadOnlyList<TicketListItem>> SearchAsync(TicketSearchCriteria criteria, CancellationToken ct = default)`
  - `Task<TicketPrintModel?> GetPrintModelAsync(long ticketId, CancellationToken ct = default)`

- [ ] **Step 1: Write the abstractions** (namespace `Transport.Application.Abstractions`)

`ICrudRepository.cs`:
```csharp
namespace Transport.Application.Abstractions;

// CRUD chung cho các danh mục (tuyến, xe, tài xế, hành khách)
public interface ICrudRepository<TEntity>
{
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
```

`IRouteRepository.cs`:
```csharp
using Transport.Domain.Entities;

namespace Transport.Application.Abstractions;

public interface IRouteRepository : ICrudRepository<Route>
{
}
```

`IVehicleRepository.cs`:
```csharp
using Transport.Domain.Entities;

namespace Transport.Application.Abstractions;

public interface IVehicleRepository : ICrudRepository<Vehicle>
{
}
```

`IDriverRepository.cs`:
```csharp
using Transport.Domain.Entities;

namespace Transport.Application.Abstractions;

public interface IDriverRepository : ICrudRepository<Driver>
{
}
```

`IPassengerRepository.cs`:
```csharp
using Transport.Domain.Entities;

namespace Transport.Application.Abstractions;

public interface IPassengerRepository : ICrudRepository<Passenger>
{
    Task<Passenger?> FindByPhoneAsync(string phone, CancellationToken ct = default);
}
```

`IScheduleRepository.cs`:
```csharp
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.Application.Abstractions;

// Ghi lịch chạy; đọc danh sách qua IStatisticsQueries (view V_TRIP_STATISTICS)
public interface IScheduleRepository
{
    Task AddAsync(Schedule schedule, CancellationToken ct = default);
    Task UpdateAsync(Schedule schedule, CancellationToken ct = default);
    Task DeleteAsync(int scheduleId, CancellationToken ct = default);
    Task SetStatusAsync(int scheduleId, ScheduleStatus status, CancellationToken ct = default);
    // PKG_SCHEDULE.CANCEL_SCHEDULE: hủy vé, hoàn tiền rồi hủy chuyến
    Task CancelAsync(int scheduleId, string reason, CancellationToken ct = default);
}
```

`ITicketRepository.cs`:
```csharp
using Transport.Application.Models;
using Transport.Domain.Enums;

namespace Transport.Application.Abstractions;

public interface ITicketRepository
{
    Task<IReadOnlySet<int>> GetBookedSeatsAsync(int scheduleId, CancellationToken ct = default);
    // PKG_TICKET.BOOK_TICKET, trả về mã vé
    Task<long> BookAsync(BookingRequest request, CancellationToken ct = default);
    Task PayAsync(long ticketId, PaymentMethod method, CancellationToken ct = default);
    Task CancelAsync(long ticketId, string reason, CancellationToken ct = default);
    Task<IReadOnlyList<TicketListItem>> SearchAsync(TicketSearchCriteria criteria, CancellationToken ct = default);
    Task<TicketPrintModel?> GetPrintModelAsync(long ticketId, CancellationToken ct = default);
}
```

`IStaffRepository.cs`:
```csharp
using Transport.Application.Models;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.Application.Abstractions;

// Gọi PKG_STAFF
public interface IStaffRepository
{
    Task<IReadOnlyList<StaffMember>> GetAllAsync(CancellationToken ct = default);
    Task CreateAsync(NewStaffRequest request, CancellationToken ct = default);
    Task UpdateInfoAsync(string username, string fullName, string? phone, CancellationToken ct = default);
    Task ChangeRoleAsync(string username, StaffRole role, CancellationToken ct = default);
    Task ResetPasswordAsync(string username, string password, CancellationToken ct = default);
    Task SetLockedAsync(string username, bool locked, CancellationToken ct = default);
}
```

`IStatisticsQueries.cs`:
```csharp
using Transport.Application.Models;

namespace Transport.Application.Abstractions;

// Đọc view V_TRIP_STATISTICS
public interface IStatisticsQueries
{
    Task<IReadOnlyList<TripStatistic>> GetTripsAsync(TripSearchCriteria criteria, CancellationToken ct = default);
    // Doanh thu theo ngày khởi hành trong [from, to)
    Task<IReadOnlyList<DailyRevenue>> GetDailyRevenueAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
```

`IBackupRepository.cs`:
```csharp
using Transport.Application.Models;

namespace Transport.Application.Abstractions;

public interface IBackupRepository
{
    Task RunBackupAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BackupLog>> GetLogsAsync(CancellationToken ct = default);
    Task<BackupJobStatus?> GetJobStatusAsync(CancellationToken ct = default);
}
```

`IAuthService.cs`:
```csharp
namespace Transport.Application.Abstractions;

public interface IAuthService
{
    // Đăng nhập bằng user Oracle; ném BusinessRuleException khi sai mật khẩu/bị khóa/không có quyền
    Task LoginAsync(string username, string password, CancellationToken ct = default);
    void Logout();
}
```

`IUserSession.cs`:
```csharp
namespace Transport.Application.Abstractions;

// Người đang đăng nhập
public interface IUserSession
{
    string Username { get; }
    string FullName { get; }
    bool IsOperator { get; }
    bool IsAuthenticated { get; }
}
```

`IExcelExporter.cs`:
```csharp
using Transport.Application.Models;

namespace Transport.Application.Abstractions;

public interface IExcelExporter
{
    Task ExportAsync<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string title, string filePath,
        CancellationToken ct = default);
}
```

- [ ] **Step 2: Write the models** (namespace `Transport.Application.Models`)

`TripStatistic.cs`:
```csharp
using Transport.Domain.Enums;

namespace Transport.Application.Models;

// Một dòng của V_TRIP_STATISTICS
public sealed record TripStatistic
{
    public int ScheduleId { get; init; }
    public int RouteId { get; init; }
    public int VehicleId { get; init; }
    public int DriverId { get; init; }
    public required string RouteName { get; init; }
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public DateTime DepartureTime { get; init; }
    public DateTime ArrivalTime { get; init; }
    public decimal TicketPrice { get; init; }
    public ScheduleStatus Status { get; init; }
    public required string PlateNumber { get; init; }
    public required string VehicleType { get; init; }
    public required string DriverName { get; init; }
    public int SeatCount { get; init; }
    public int SeatsSold { get; init; }
    public int SeatsAvailable { get; init; }
    public decimal OccupancyPct { get; init; }
    public int TicketsCancelled { get; init; }
    public decimal Revenue { get; init; }
}
```

`TripSearchCriteria.cs`:
```csharp
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
```

`BookingRequest.cs`:
```csharp
namespace Transport.Application.Models;

public sealed record BookingRequest(int ScheduleId, string FullName, string Phone, string? IdCard, int SeatNo);
```

`TicketSearchCriteria.cs`:
```csharp
using Transport.Domain.Enums;

namespace Transport.Application.Models;

public sealed record TicketSearchCriteria
{
    // Mã vé, SĐT hoặc một phần tên khách
    public string? Keyword { get; init; }
    public TicketStatus? Status { get; init; }
    public int? ScheduleId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}
```

`TicketListItem.cs`:
```csharp
using Transport.Domain.Enums;

namespace Transport.Application.Models;

public sealed record TicketListItem
{
    public long TicketId { get; init; }
    public int ScheduleId { get; init; }
    public required string RouteName { get; init; }
    public DateTime DepartureTime { get; init; }
    public required string PlateNumber { get; init; }
    public int SeatNo { get; init; }
    public required string PassengerName { get; init; }
    public required string Phone { get; init; }
    public decimal Price { get; init; }
    public TicketStatus Status { get; init; }
    public DateTime BookedAt { get; init; }
    public required string SoldBy { get; init; }
    public string? CancelReason { get; init; }
}
```

`TicketPrintModel.cs`:
```csharp
using Transport.Domain.Enums;

namespace Transport.Application.Models;

// Dữ liệu in lên vé
public sealed record TicketPrintModel
{
    public long TicketId { get; init; }
    public required string RouteName { get; init; }
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public DateTime DepartureTime { get; init; }
    public DateTime ArrivalTime { get; init; }
    public required string PlateNumber { get; init; }
    public required string VehicleType { get; init; }
    public int SeatNo { get; init; }
    public required string PassengerName { get; init; }
    public required string Phone { get; init; }
    public decimal Price { get; init; }
    public TicketStatus Status { get; init; }
    public required string SoldBy { get; init; }
    public DateTime BookedAt { get; init; }
}
```

`DailyRevenue.cs`:
```csharp
namespace Transport.Application.Models;

public sealed record DailyRevenue
{
    public DateTime RevenueDate { get; init; }
    public decimal Revenue { get; init; }
    public int Tickets { get; init; }
}
```

`DashboardSummary.cs`:
```csharp
namespace Transport.Application.Models;

public sealed record DashboardSummary(
    int TripsToday,
    int TicketsSoldToday,
    decimal RevenueToday,
    decimal AverageOccupancyPct,
    IReadOnlyList<DailyRevenue> Last7Days);
```

`BackupLog.cs`:
```csharp
namespace Transport.Application.Models;

public sealed record BackupLog
{
    public int LogId { get; init; }
    public DateTime BackupAt { get; init; }
    public required string PerformedBy { get; init; }
    public int? ScheduleRows { get; init; }
    public int? TicketRows { get; init; }
    public int? PaymentRows { get; init; }
}
```

`BackupJobStatus.cs`:
```csharp
namespace Transport.Application.Models;

// Trạng thái JOB_NIGHTLY_BACKUP (view V_BACKUP_JOB_STATUS)
public sealed record BackupJobStatus
{
    public required string JobName { get; init; }
    // 'TRUE' / 'FALSE' như Oracle trả về
    public required string Enabled { get; init; }
    public string? State { get; init; }
    public DateTime? LastStartDate { get; init; }
    public DateTime? NextRunDate { get; init; }
    public int? FailureCount { get; init; }

    public bool IsEnabled => Enabled == "TRUE";
}
```

`NewStaffRequest.cs`:
```csharp
using Transport.Domain.Enums;

namespace Transport.Application.Models;

public sealed record NewStaffRequest(string Username, string Password, string FullName, string? Phone, StaffRole Role);
```

`ExportColumn.cs`:
```csharp
namespace Transport.Application.Models;

// Một cột khi xuất Excel; Format là định dạng số/ngày của Excel (VD "#,##0")
public sealed record ExportColumn<T>(string Header, Func<T, object?> Value, string? Format = null);
```

- [ ] **Step 3: Write the fake repository `tests/Transport.UnitTests/Fakes/FakeTicketRepository.cs`**

```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;
using Transport.Domain.Errors;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeTicketRepository : ITicketRepository
{
    public HashSet<int> BookedSeats { get; } = [];
    public long NextTicketId { get; set; } = 1;
    public BookingRequest? LastBooking { get; private set; }
    public BusinessRuleException? BookError { get; set; }
    public List<(long TicketId, PaymentMethod Method)> Payments { get; } = [];
    public List<(long TicketId, string Reason)> Cancellations { get; } = [];
    public TicketSearchCriteria? LastSearch { get; private set; }
    public List<TicketListItem> SearchResult { get; } = [];
    public TicketPrintModel? PrintModel { get; set; }

    public Task<IReadOnlySet<int>> GetBookedSeatsAsync(int scheduleId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlySet<int>>(BookedSeats.ToHashSet());

    public Task<long> BookAsync(BookingRequest request, CancellationToken ct = default)
    {
        if (BookError is not null)
        {
            throw BookError;
        }

        LastBooking = request;
        BookedSeats.Add(request.SeatNo);
        return Task.FromResult(NextTicketId++);
    }

    public Task PayAsync(long ticketId, PaymentMethod method, CancellationToken ct = default)
    {
        Payments.Add((ticketId, method));
        return Task.CompletedTask;
    }

    public Task CancelAsync(long ticketId, string reason, CancellationToken ct = default)
    {
        Cancellations.Add((ticketId, reason));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TicketListItem>> SearchAsync(TicketSearchCriteria criteria, CancellationToken ct = default)
    {
        LastSearch = criteria;
        return Task.FromResult<IReadOnlyList<TicketListItem>>(SearchResult.ToList());
    }

    public Task<TicketPrintModel?> GetPrintModelAsync(long ticketId, CancellationToken ct = default) =>
        Task.FromResult(PrintModel);
}
```

- [ ] **Step 4: Write the failing tests `tests/Transport.UnitTests/Application/TicketServiceTests.cs`**

```csharp
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Errors;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class TicketServiceTests
{
    private readonly FakeTicketRepository _repository = new() { NextTicketId = 42 };
    private readonly TicketService _service;

    public TicketServiceTests() => _service = new TicketService(_repository);

    [Fact]
    public async Task BookAsync_TrimsInputAndReturnsTicketId()
    {
        var id = await _service.BookAsync(new BookingRequest(7, "  Nguyễn Văn A ", " 0912345678 ", "   ", 5));

        Assert.Equal(42, id);
        Assert.Equal(new BookingRequest(7, "Nguyễn Văn A", "0912345678", null, 5), _repository.LastBooking);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1912345678")]
    [InlineData("09123456789")]
    [InlineData("09123abc78")]
    public async Task BookAsync_RejectsInvalidPhone(string phone)
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.BookAsync(new BookingRequest(7, "Nguyễn Văn A", phone, null, 5)));

        Assert.Equal(BusinessErrorCode.InvalidPhone, ex.Code);
        Assert.Null(_repository.LastBooking);
    }

    [Fact]
    public async Task BookAsync_RejectsEmptyName()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.BookAsync(new BookingRequest(7, " ", "0912345678", null, 5)));

        Assert.Equal(BusinessErrorCode.RequiredField, ex.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task BookAsync_RejectsNonPositiveSeat(int seat)
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.BookAsync(new BookingRequest(7, "Nguyễn Văn A", "0912345678", null, seat)));

        Assert.Equal(BusinessErrorCode.InvalidSeat, ex.Code);
    }

    [Fact]
    public async Task CancelAsync_RequiresReasonAndTrimsIt()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CancelAsync(5, "  "));
        Assert.Equal(BusinessErrorCode.RequiredField, ex.Code);

        await _service.CancelAsync(5, " Khách đổi ý ");
        Assert.Equal((5L, "Khách đổi ý"), Assert.Single(_repository.Cancellations));
    }

    [Fact]
    public async Task SearchAsync_TurnsBlankKeywordIntoNull()
    {
        await _service.SearchAsync(new TicketSearchCriteria { Keyword = "   " });

        Assert.Null(_repository.LastSearch!.Keyword);
    }
}
```

- [ ] **Step 5: Run to verify they fail**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build error `The type or namespace name 'TicketService' could not be found`.

- [ ] **Step 6: Write `src/Transport.Application/Services/Rules.cs`**

```csharp
using System.Text.RegularExpressions;
using Transport.Application.Abstractions;
using Transport.Domain.Errors;

namespace Transport.Application.Services;

// Luật kiểm tra dữ liệu phía client, chạy trước khi gọi Oracle
internal static partial class Rules
{
    public const int MinPasswordLength = 8;

    public static void Required(params string?[] values)
    {
        if (values.Any(string.IsNullOrWhiteSpace))
        {
            throw new BusinessRuleException(BusinessErrorCode.RequiredField, "A required field is empty.");
        }
    }

    public static void Phone(string? phone)
    {
        if (phone is null || !PhonePattern().IsMatch(phone.Trim()))
        {
            throw new BusinessRuleException(BusinessErrorCode.InvalidPhone, "Phone must be 10 digits starting with 0.");
        }
    }

    public static void OptionalPhone(string? phone)
    {
        if (!string.IsNullOrWhiteSpace(phone))
        {
            Phone(phone);
        }
    }

    public static void Operator(IUserSession session)
    {
        if (!session.IsOperator)
        {
            throw new BusinessRuleException(BusinessErrorCode.OperatorOnly, "Only operators can perform this action.");
        }
    }

    // Cùng luật với PKG_STAFF.valid_username
    public static void Username(string? username)
    {
        if (username is null || !UsernamePattern().IsMatch(username))
        {
            throw new BusinessRuleException(BusinessErrorCode.InvalidUsername, "Invalid username.");
        }
    }

    // Cùng luật với PKG_STAFF.check_password
    public static void Password(string? password)
    {
        if (password is null || password.Length < MinPasswordLength || password.Contains('"'))
        {
            throw new BusinessRuleException(BusinessErrorCode.InvalidPassword,
                "Password must be at least 8 characters and must not contain double quotes.");
        }
    }

    public static string? TrimToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex(@"^0\d{9}$")]
    private static partial Regex PhonePattern();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{2,29}$")]
    private static partial Regex UsernamePattern();
}
```

- [ ] **Step 7: Write `src/Transport.Application/Services/TicketService.cs`**

```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;
using Transport.Domain.Errors;

namespace Transport.Application.Services;

// Bán vé: kiểm tra dữ liệu rồi gọi PKG_TICKET qua repository
public sealed class TicketService(ITicketRepository tickets)
{
    public Task<IReadOnlySet<int>> GetBookedSeatsAsync(int scheduleId, CancellationToken ct = default) =>
        tickets.GetBookedSeatsAsync(scheduleId, ct);

    public async Task<long> BookAsync(BookingRequest request, CancellationToken ct = default)
    {
        Rules.Required(request.FullName);
        Rules.Phone(request.Phone);
        if (request.SeatNo <= 0)
        {
            throw new BusinessRuleException(BusinessErrorCode.InvalidSeat, "Seat number must be positive.");
        }

        var normalized = request with
        {
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            IdCard = Rules.TrimToNull(request.IdCard),
        };
        return await tickets.BookAsync(normalized, ct);
    }

    public Task PayAsync(long ticketId, PaymentMethod method, CancellationToken ct = default) =>
        tickets.PayAsync(ticketId, method, ct);

    public async Task CancelAsync(long ticketId, string reason, CancellationToken ct = default)
    {
        Rules.Required(reason);
        await tickets.CancelAsync(ticketId, reason.Trim(), ct);
    }

    public Task<IReadOnlyList<TicketListItem>> SearchAsync(TicketSearchCriteria criteria, CancellationToken ct = default) =>
        tickets.SearchAsync(criteria with { Keyword = Rules.TrimToNull(criteria.Keyword) }, ct);

    public Task<TicketPrintModel?> GetPrintModelAsync(long ticketId, CancellationToken ct = default) =>
        tickets.GetPrintModelAsync(ticketId, ct);
}
```

- [ ] **Step 8: Run tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: all tests pass (`failed: 0`).

- [ ] **Step 9: Commit**

```bash
git add src/Transport.Application tests/Transport.UnitTests
git commit -m "feat(application): add abstractions, models and ticket service"
```

---

### Task 4: Schedule, staff, backup and dashboard services

**Files:**
- Create: `src/Transport.Application/Services/ScheduleService.cs`, `StaffService.cs`, `BackupService.cs`, `DashboardService.cs`, `src/Transport.Application/DependencyInjection.cs`
- Test: `tests/Transport.UnitTests/Fakes/FakeUserSession.cs`, `FakeScheduleRepository.cs`, `FakeStaffRepository.cs`, `FakeBackupRepository.cs`, `FakeStatisticsQueries.cs`, `FixedTimeProvider.cs`
- Test: `tests/Transport.UnitTests/Application/ScheduleServiceTests.cs`, `StaffServiceTests.cs`, `BackupServiceTests.cs`, `DashboardServiceTests.cs`

**Interfaces:**
- Consumes: Task 3 abstractions/models, `Rules`.
- Produces:
  - `ScheduleService(IScheduleRepository, IUserSession)`: `AddAsync(Schedule, ct)`, `UpdateAsync(Schedule, ct)`, `DeleteAsync(int, ct)`, `MarkRunningAsync(int, ct)`, `MarkCompletedAsync(int, ct)`, `CancelAsync(int, string reason, ct)`
  - `StaffService(IStaffRepository, IUserSession)`: `GetAllAsync(ct)`, `CreateAsync(NewStaffRequest, ct)`, `UpdateInfoAsync(string username, string fullName, string? phone, ct)`, `ChangeRoleAsync(string, StaffRole, ct)`, `ResetPasswordAsync(string, string, ct)`, `SetLockedAsync(string, bool, ct)`
  - `BackupService(IBackupRepository, IUserSession)`: `RunAsync(ct)`, `GetLogsAsync(ct)`, `GetJobStatusAsync(ct)`
  - `DashboardService(IStatisticsQueries, TimeProvider)`: `Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)`
  - `DependencyInjection.AddApplication(this IServiceCollection)`
  - Test fakes `FakeUserSession { Username, FullName, IsOperator, IsAuthenticated }` (settable), `FakeStatisticsQueries { Trips, Daily, LastCriteria }`, `FixedTimeProvider(DateTimeOffset now)`.

- [ ] **Step 1: Write the fakes**

`tests/Transport.UnitTests/Fakes/FakeUserSession.cs`:
```csharp
using Transport.Application.Abstractions;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeUserSession : IUserSession
{
    public string Username { get; set; } = "OPERATOR01";
    public string FullName { get; set; } = "Nguyễn Văn Điều";
    public bool IsOperator { get; set; } = true;
    public bool IsAuthenticated { get; set; } = true;

    public static FakeUserSession Seller() => new() { Username = "SELLER01", FullName = "Trần Thị Bán", IsOperator = false };
}
```

`tests/Transport.UnitTests/Fakes/FakeScheduleRepository.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeScheduleRepository : IScheduleRepository
{
    public List<string> Calls { get; } = [];
    public Schedule? LastSchedule { get; private set; }

    public Task AddAsync(Schedule schedule, CancellationToken ct = default) { LastSchedule = schedule; Calls.Add("add"); return Task.CompletedTask; }
    public Task UpdateAsync(Schedule schedule, CancellationToken ct = default) { LastSchedule = schedule; Calls.Add("update"); return Task.CompletedTask; }
    public Task DeleteAsync(int scheduleId, CancellationToken ct = default) { Calls.Add($"delete:{scheduleId}"); return Task.CompletedTask; }
    public Task SetStatusAsync(int scheduleId, ScheduleStatus status, CancellationToken ct = default) { Calls.Add($"status:{scheduleId}:{status}"); return Task.CompletedTask; }
    public Task CancelAsync(int scheduleId, string reason, CancellationToken ct = default) { Calls.Add($"cancel:{scheduleId}:{reason}"); return Task.CompletedTask; }
}
```

`tests/Transport.UnitTests/Fakes/FakeStaffRepository.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Entities;
using Transport.Domain.Enums;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeStaffRepository : IStaffRepository
{
    public List<StaffMember> Staff { get; } = [];
    public List<string> Calls { get; } = [];
    public NewStaffRequest? LastCreate { get; private set; }

    public Task<IReadOnlyList<StaffMember>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<StaffMember>>(Staff.ToList());
    public Task CreateAsync(NewStaffRequest request, CancellationToken ct = default) { LastCreate = request; Calls.Add($"create:{request.Username}"); return Task.CompletedTask; }
    public Task UpdateInfoAsync(string username, string fullName, string? phone, CancellationToken ct = default) { Calls.Add($"update:{username}:{fullName}:{phone}"); return Task.CompletedTask; }
    public Task ChangeRoleAsync(string username, StaffRole role, CancellationToken ct = default) { Calls.Add($"role:{username}:{role}"); return Task.CompletedTask; }
    public Task ResetPasswordAsync(string username, string password, CancellationToken ct = default) { Calls.Add($"password:{username}"); return Task.CompletedTask; }
    public Task SetLockedAsync(string username, bool locked, CancellationToken ct = default) { Calls.Add($"lock:{username}:{locked}"); return Task.CompletedTask; }
}
```

`tests/Transport.UnitTests/Fakes/FakeBackupRepository.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeBackupRepository : IBackupRepository
{
    public int RunCount { get; private set; }
    public List<BackupLog> Logs { get; } = [];
    public BackupJobStatus? JobStatus { get; set; }

    public Task RunBackupAsync(CancellationToken ct = default) { RunCount++; return Task.CompletedTask; }
    public Task<IReadOnlyList<BackupLog>> GetLogsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<BackupLog>>(Logs.ToList());
    public Task<BackupJobStatus?> GetJobStatusAsync(CancellationToken ct = default) => Task.FromResult(JobStatus);
}
```

`tests/Transport.UnitTests/Fakes/FakeStatisticsQueries.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeStatisticsQueries : IStatisticsQueries
{
    public List<TripStatistic> Trips { get; } = [];
    public List<DailyRevenue> Daily { get; } = [];
    public TripSearchCriteria? LastCriteria { get; private set; }
    public (DateTime From, DateTime To)? LastDailyRange { get; private set; }

    public Task<IReadOnlyList<TripStatistic>> GetTripsAsync(TripSearchCriteria criteria, CancellationToken ct = default)
    {
        LastCriteria = criteria;
        return Task.FromResult<IReadOnlyList<TripStatistic>>(Trips.ToList());
    }

    public Task<IReadOnlyList<DailyRevenue>> GetDailyRevenueAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        LastDailyRange = (from, to);
        return Task.FromResult<IReadOnlyList<DailyRevenue>>(Daily.ToList());
    }

    // Tạo nhanh một chuyến cho test
    public static TripStatistic Trip(int id, int seatsSold = 0, int seatCount = 40, decimal revenue = 0,
        ScheduleStatus status = ScheduleStatus.Scheduled, decimal price = 280000, DateTime? departure = null,
        int routeId = 1, string routeName = "Hà Nội - Lào Cai") => new()
    {
        ScheduleId = id,
        RouteId = routeId,
        VehicleId = 1,
        DriverId = 1,
        RouteName = routeName,
        Origin = "Hà Nội",
        Destination = "Lào Cai",
        DepartureTime = departure ?? new DateTime(2026, 10, 17, 20, 0, 0),
        ArrivalTime = (departure ?? new DateTime(2026, 10, 17, 20, 0, 0)).AddHours(5.5),
        TicketPrice = price,
        Status = status,
        PlateNumber = "29B-196.88",
        VehicleType = "Giường nằm 40 chỗ",
        DriverName = "Nguyễn Văn An",
        SeatCount = seatCount,
        SeatsSold = seatsSold,
        SeatsAvailable = seatCount - seatsSold,
        OccupancyPct = Math.Round(100m * seatsSold / seatCount, 1),
        Revenue = revenue,
    };
}
```

`tests/Transport.UnitTests/Fakes/FixedTimeProvider.cs`:
```csharp
namespace Transport.UnitTests.Fakes;

// Đồng hồ cố định cho test (múi giờ UTC để GetLocalNow() = now)
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
```

- [ ] **Step 2: Write the failing tests**

`tests/Transport.UnitTests/Application/ScheduleServiceTests.cs`:
```csharp
using Transport.Application.Services;
using Transport.Domain.Entities;
using Transport.Domain.Errors;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class ScheduleServiceTests
{
    private readonly FakeScheduleRepository _repository = new();

    private static Schedule ValidSchedule() => new()
    {
        RouteId = 1,
        VehicleId = 2,
        DriverId = 3,
        DepartureTime = new DateTime(2026, 10, 20, 7, 0, 0),
        ArrivalTime = new DateTime(2026, 10, 20, 12, 30, 0),
        TicketPrice = 280000,
    };

    [Fact]
    public async Task AddAsync_SavesValidSchedule()
    {
        await new ScheduleService(_repository, new FakeUserSession()).AddAsync(ValidSchedule());

        Assert.Equal(new[] { "add" }, _repository.Calls);
    }

    [Fact]
    public async Task AddAsync_RejectsArrivalNotAfterDeparture()
    {
        var schedule = ValidSchedule() with { ArrivalTime = new DateTime(2026, 10, 20, 7, 0, 0) };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => new ScheduleService(_repository, new FakeUserSession()).AddAsync(schedule));

        Assert.Equal(BusinessErrorCode.InvalidTimeRange, ex.Code);
        Assert.Empty(_repository.Calls);
    }

    [Fact]
    public async Task UpdateAsync_RejectsNonPositivePriceAndMissingDriver()
    {
        var service = new ScheduleService(_repository, new FakeUserSession());

        var price = await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(ValidSchedule() with { TicketPrice = 0 }));
        var driver = await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(ValidSchedule() with { DriverId = 0 }));

        Assert.Equal(BusinessErrorCode.InvalidPrice, price.Code);
        Assert.Equal(BusinessErrorCode.RequiredField, driver.Code);
    }

    [Fact]
    public async Task Seller_CannotChangeSchedules()
    {
        var service = new ScheduleService(_repository, FakeUserSession.Seller());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CancelAsync(5, "Xe hỏng"));

        Assert.Equal(BusinessErrorCode.OperatorOnly, ex.Code);
    }

    [Fact]
    public async Task StatusAndCancelCallsReachRepository()
    {
        var service = new ScheduleService(_repository, new FakeUserSession());

        await service.MarkRunningAsync(5);
        await service.MarkCompletedAsync(5);
        await service.CancelAsync(6, " Xe hỏng ");
        await service.DeleteAsync(7);

        Assert.Equal(new[] { "status:5:Running", "status:5:Completed", "cancel:6:Xe hỏng", "delete:7" }, _repository.Calls);
    }
}
```

`tests/Transport.UnitTests/Application/StaffServiceTests.cs`:
```csharp
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.Domain.Errors;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class StaffServiceTests
{
    private readonly FakeStaffRepository _repository = new();
    private readonly StaffService _service;

    public StaffServiceTests() => _service = new StaffService(_repository, new FakeUserSession { Username = "OPERATOR01" });

    [Fact]
    public async Task CreateAsync_NormalizesAndCreates()
    {
        await _service.CreateAsync(new NewStaffRequest("seller03", "Seller#003", " Phạm Thị C ", " ", StaffRole.TicketSeller));

        Assert.Equal(new NewStaffRequest("seller03", "Seller#003", "Phạm Thị C", null, StaffRole.TicketSeller), _repository.LastCreate);
    }

    [Theory]
    [InlineData("ab", "Password1", BusinessErrorCode.InvalidUsername)]
    [InlineData("1seller", "Password1", BusinessErrorCode.InvalidUsername)]
    [InlineData("x; drop", "Password1", BusinessErrorCode.InvalidUsername)]
    [InlineData("seller03", "short", BusinessErrorCode.InvalidPassword)]
    [InlineData("seller03", "has\"quote1", BusinessErrorCode.InvalidPassword)]
    public async Task CreateAsync_EnforcesSameRulesAsPkgStaff(string username, string password, BusinessErrorCode expected)
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.CreateAsync(new NewStaffRequest(username, password, "Tên", null, StaffRole.Operator)));

        Assert.Equal(expected, ex.Code);
        Assert.Empty(_repository.Calls);
    }

    [Fact]
    public async Task CannotLockOrChangeRoleOfOwnAccount()
    {
        var lockSelf = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.SetLockedAsync("operator01", true));
        var roleSelf = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ChangeRoleAsync("OPERATOR01", StaffRole.TicketSeller));

        Assert.Equal(BusinessErrorCode.CannotChangeOwnAccount, lockSelf.Code);
        Assert.Equal(BusinessErrorCode.CannotChangeOwnAccount, roleSelf.Code);
    }

    [Fact]
    public async Task OtherOperationsReachRepository()
    {
        await _service.UpdateInfoAsync("SELLER01", " Trần Thị Bán ", "0912000002");
        await _service.ChangeRoleAsync("SELLER01", StaffRole.Operator);
        await _service.ResetPasswordAsync("SELLER01", "NewPass#01");
        await _service.SetLockedAsync("SELLER01", true);

        Assert.Equal(
            new[] { "update:SELLER01:Trần Thị Bán:0912000002", "role:SELLER01:Operator", "password:SELLER01", "lock:SELLER01:True" },
            _repository.Calls);
    }

    [Fact]
    public async Task Seller_CannotManageStaff()
    {
        var service = new StaffService(_repository, FakeUserSession.Seller());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetAllAsync());

        Assert.Equal(BusinessErrorCode.OperatorOnly, ex.Code);
    }
}
```

`tests/Transport.UnitTests/Application/BackupServiceTests.cs`:
```csharp
using Transport.Application.Services;
using Transport.Domain.Errors;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class BackupServiceTests
{
    [Fact]
    public async Task Operator_CanRunBackup()
    {
        var repository = new FakeBackupRepository();

        await new BackupService(repository, new FakeUserSession()).RunAsync();

        Assert.Equal(1, repository.RunCount);
    }

    [Fact]
    public async Task Seller_CannotRunBackup()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => new BackupService(new FakeBackupRepository(), FakeUserSession.Seller()).RunAsync());

        Assert.Equal(BusinessErrorCode.OperatorOnly, ex.Code);
    }
}
```

`tests/Transport.UnitTests/Application/DashboardServiceTests.cs`:
```csharp
using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Enums;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class DashboardServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetSummaryAsync_AggregatesTodayAndFillsSevenDays()
    {
        var queries = new FakeStatisticsQueries();
        queries.Trips.Add(FakeStatisticsQueries.Trip(1, seatsSold: 20, seatCount: 40, revenue: 5_600_000));
        queries.Trips.Add(FakeStatisticsQueries.Trip(2, seatsSold: 4, seatCount: 16, revenue: 400_000));
        queries.Trips.Add(FakeStatisticsQueries.Trip(3, seatsSold: 0, seatCount: 45, status: ScheduleStatus.Cancelled));
        queries.Daily.Add(new DailyRevenue { RevenueDate = new DateTime(2026, 10, 15), Revenue = 1_000_000, Tickets = 5 });

        var summary = await new DashboardService(queries, new FixedTimeProvider(Now)).GetSummaryAsync();

        Assert.Equal(new DateTime(2026, 10, 17), queries.LastCriteria!.From);
        Assert.Equal(new DateTime(2026, 10, 18), queries.LastCriteria.To);
        Assert.Equal(2, summary.TripsToday);
        Assert.Equal(24, summary.TicketsSoldToday);
        Assert.Equal(6_000_000, summary.RevenueToday);
        Assert.Equal(37.5m, summary.AverageOccupancyPct);
        Assert.Equal(7, summary.Last7Days.Count);
        Assert.Equal(new DateTime(2026, 10, 11), summary.Last7Days[0].RevenueDate);
        Assert.Equal(1_000_000, summary.Last7Days[4].Revenue);
        Assert.Equal(0, summary.Last7Days[6].Revenue);
    }
}
```

- [ ] **Step 3: Run to verify they fail**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build errors for `ScheduleService`, `StaffService`, `BackupService`, `DashboardService`.

- [ ] **Step 4: Write the services**

`src/Transport.Application/Services/ScheduleService.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Domain.Entities;
using Transport.Domain.Enums;
using Transport.Domain.Errors;

namespace Transport.Application.Services;

// Lịch chạy: chỉ điều hành được thay đổi
public sealed class ScheduleService(IScheduleRepository schedules, IUserSession session)
{
    public async Task AddAsync(Schedule schedule, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Validate(schedule);
        await schedules.AddAsync(schedule, ct);
    }

    public async Task UpdateAsync(Schedule schedule, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Validate(schedule);
        await schedules.UpdateAsync(schedule, ct);
    }

    public async Task DeleteAsync(int scheduleId, CancellationToken ct = default)
    {
        Rules.Operator(session);
        await schedules.DeleteAsync(scheduleId, ct);
    }

    public async Task MarkRunningAsync(int scheduleId, CancellationToken ct = default)
    {
        Rules.Operator(session);
        await schedules.SetStatusAsync(scheduleId, ScheduleStatus.Running, ct);
    }

    public async Task MarkCompletedAsync(int scheduleId, CancellationToken ct = default)
    {
        Rules.Operator(session);
        await schedules.SetStatusAsync(scheduleId, ScheduleStatus.Completed, ct);
    }

    public async Task CancelAsync(int scheduleId, string reason, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Rules.Required(reason);
        await schedules.CancelAsync(scheduleId, reason.Trim(), ct);
    }

    private static void Validate(Schedule schedule)
    {
        if (schedule.RouteId <= 0 || schedule.VehicleId <= 0 || schedule.DriverId <= 0)
        {
            throw new BusinessRuleException(BusinessErrorCode.RequiredField, "Route, vehicle and driver are required.");
        }

        if (schedule.ArrivalTime <= schedule.DepartureTime)
        {
            throw new BusinessRuleException(BusinessErrorCode.InvalidTimeRange, "Arrival time must be after departure time.");
        }

        if (schedule.TicketPrice <= 0)
        {
            throw new BusinessRuleException(BusinessErrorCode.InvalidPrice, "Ticket price must be greater than 0.");
        }
    }
}
```

`src/Transport.Application/Services/StaffService.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Entities;
using Transport.Domain.Enums;
using Transport.Domain.Errors;

namespace Transport.Application.Services;

// Quản lý nhân viên (user Oracle) – chỉ điều hành
public sealed class StaffService(IStaffRepository staff, IUserSession session)
{
    public async Task<IReadOnlyList<StaffMember>> GetAllAsync(CancellationToken ct = default)
    {
        Rules.Operator(session);
        return await staff.GetAllAsync(ct);
    }

    public async Task CreateAsync(NewStaffRequest request, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Rules.Username(request.Username);
        Rules.Password(request.Password);
        Rules.Required(request.FullName);
        Rules.OptionalPhone(request.Phone);
        await staff.CreateAsync(request with { FullName = request.FullName.Trim(), Phone = Rules.TrimToNull(request.Phone) }, ct);
    }

    public async Task UpdateInfoAsync(string username, string fullName, string? phone, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Rules.Required(fullName);
        Rules.OptionalPhone(phone);
        await staff.UpdateInfoAsync(username, fullName.Trim(), Rules.TrimToNull(phone), ct);
    }

    public async Task ChangeRoleAsync(string username, StaffRole role, CancellationToken ct = default)
    {
        Rules.Operator(session);
        NotSelf(username);
        await staff.ChangeRoleAsync(username, role, ct);
    }

    public async Task ResetPasswordAsync(string username, string password, CancellationToken ct = default)
    {
        Rules.Operator(session);
        Rules.Password(password);
        await staff.ResetPasswordAsync(username, password, ct);
    }

    public async Task SetLockedAsync(string username, bool locked, CancellationToken ct = default)
    {
        Rules.Operator(session);
        NotSelf(username);
        await staff.SetLockedAsync(username, locked, ct);
    }

    // Không cho tự khóa/tự đổi quyền chính mình (giống PKG_STAFF.not_self)
    private void NotSelf(string username)
    {
        if (string.Equals(username, session.Username, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException(BusinessErrorCode.CannotChangeOwnAccount, "You cannot lock or change your own account.");
        }
    }
}
```

`src/Transport.Application/Services/BackupService.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;

namespace Transport.Application.Services;

// Sao lưu dữ liệu vận hành – chỉ điều hành
public sealed class BackupService(IBackupRepository backups, IUserSession session)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        Rules.Operator(session);
        await backups.RunBackupAsync(ct);
    }

    public async Task<IReadOnlyList<BackupLog>> GetLogsAsync(CancellationToken ct = default)
    {
        Rules.Operator(session);
        return await backups.GetLogsAsync(ct);
    }

    public async Task<BackupJobStatus?> GetJobStatusAsync(CancellationToken ct = default)
    {
        Rules.Operator(session);
        return await backups.GetJobStatusAsync(ct);
    }
}
```

`src/Transport.Application/Services/DashboardService.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;

namespace Transport.Application.Services;

// Số liệu màn Tổng quan: hôm nay + doanh thu 7 ngày gần nhất
public sealed class DashboardService(IStatisticsQueries statistics, TimeProvider time)
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var today = time.GetLocalNow().Date;
        var trips = await statistics.GetTripsAsync(new TripSearchCriteria { From = today, To = today.AddDays(1) }, ct);
        var active = trips.Where(t => t.Status != ScheduleStatus.Cancelled).ToList();

        var daily = await statistics.GetDailyRevenueAsync(today.AddDays(-6), today.AddDays(1), ct);
        var last7Days = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(i - 6))
            .Select(day => daily.FirstOrDefault(d => d.RevenueDate.Date == day) ?? new DailyRevenue { RevenueDate = day })
            .ToList();

        return new DashboardSummary(
            active.Count,
            active.Sum(t => t.SeatsSold),
            active.Sum(t => t.Revenue),
            active.Count == 0 ? 0 : Math.Round(active.Average(t => t.OccupancyPct), 1),
            last7Days);
    }
}
```

`src/Transport.Application/DependencyInjection.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Transport.Application.Services;

namespace Transport.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<TicketService>();
        services.AddSingleton<ScheduleService>();
        services.AddSingleton<StaffService>();
        services.AddSingleton<BackupService>();
        services.AddSingleton<DashboardService>();
        return services;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 6: Commit**

```bash
git add src/Transport.Application tests/Transport.UnitTests
git commit -m "feat(application): add schedule, staff, backup and dashboard services"
```

---

### Task 5: Infrastructure core – error mapping, connection, session, login

**Files:**
- Create: `src/Transport.Infrastructure/Persistence/DbEnum.cs`, `OracleErrorMapper.cs`, `DatabaseOptions.cs`, `UserSession.cs`, `OracleConnectionFactory.cs`, `OracleRepository.cs`, `OracleAuthService.cs`
- Create: `src/Transport.Infrastructure/DependencyInjection.cs`
- Test: `tests/Transport.UnitTests/Infrastructure/DbEnumTests.cs`, `OracleErrorMapperTests.cs`

**Interfaces:**
- Consumes: Application abstractions, Domain errors.
- Produces (namespace `Transport.Infrastructure.Persistence`, all `internal` except `DatabaseOptions`):
  - `DbEnum.ToDb<TEnum>(TEnum) : string` (PascalCase → UPPER_SNAKE), `DbEnum.RoleToDb(StaffRole) : string` (`ROLE_...`)
  - `OracleErrorMapper.Map(int number, string message) : BusinessRuleException?`, `OracleErrorMapper.CleanMessage(string) : string`
  - `DatabaseOptions { const SectionName = "Database"; DataSource; Schema }`
  - `UserSession : IUserSession` with `internal Password`, `internal Start(username, password, fullName, isOperator)`, `internal Clear()`
  - `OracleConnectionFactory.OpenAsync(ct)` and `OpenAsync(username, password, ct)`
  - `abstract class OracleRepository(OracleConnectionFactory)` with protected `QueryAsync<T>(sql, args, ct)`, `QuerySingleOrDefaultAsync<T>(sql, args, ct)`, `ExecuteAsync(sql, args, ct)`, `CallAsync(procedure, args, ct)`, `RunAsync<T>(Func<OracleConnection, Task<T>>, ct)`
  - `OracleAuthService : IAuthService`
  - `Transport.Infrastructure.DependencyInjection.AddInfrastructure(this IServiceCollection, IConfiguration)`

- [ ] **Step 1: Write the failing tests**

`tests/Transport.UnitTests/Infrastructure/DbEnumTests.cs`:
```csharp
using Transport.Domain.Enums;
using Transport.Infrastructure.Persistence;

namespace Transport.UnitTests.Infrastructure;

public class DbEnumTests
{
    [Fact]
    public void ToDb_ConvertsPascalCaseToUpperSnakeCase()
    {
        Assert.Equal("SCHEDULED", DbEnum.ToDb(ScheduleStatus.Scheduled));
        Assert.Equal("TICKET_SELLER", DbEnum.ToDb(StaffRole.TicketSeller));
        Assert.Equal("TRANSFER", DbEnum.ToDb(PaymentMethod.Transfer));
        Assert.Equal("MAINTENANCE", DbEnum.ToDb(VehicleStatus.Maintenance));
        Assert.Equal("D1", DbEnum.ToDb(LicenseClass.D1));
    }

    [Fact]
    public void RoleToDb_AddsRolePrefix()
    {
        Assert.Equal("ROLE_OPERATOR", DbEnum.RoleToDb(StaffRole.Operator));
        Assert.Equal("ROLE_TICKET_SELLER", DbEnum.RoleToDb(StaffRole.TicketSeller));
    }
}
```

`tests/Transport.UnitTests/Infrastructure/OracleErrorMapperTests.cs`:
```csharp
using Transport.Domain.Errors;
using Transport.Infrastructure.Persistence;

namespace Transport.UnitTests.Infrastructure;

public class OracleErrorMapperTests
{
    [Theory]
    [InlineData(20001)] [InlineData(20002)] [InlineData(20003)] [InlineData(20010)] [InlineData(20011)]
    [InlineData(20020)] [InlineData(20021)] [InlineData(20030)] [InlineData(20031)] [InlineData(20032)]
    [InlineData(20040)] [InlineData(20041)] [InlineData(20042)] [InlineData(20043)] [InlineData(20044)]
    [InlineData(20050)] [InlineData(20051)] [InlineData(20052)]
    public void Map_ApplicationErrors_KeepTheirNumber(int number)
    {
        var ex = OracleErrorMapper.Map(number, $"ORA-{number}: Something.");

        Assert.NotNull(ex);
        Assert.Equal(number, (int)ex.Code);
        Assert.Equal("Something.", ex.Message);
    }

    [Theory]
    [InlineData(1, BusinessErrorCode.DuplicateValue)]
    [InlineData(1017, BusinessErrorCode.InvalidCredentials)]
    [InlineData(1031, BusinessErrorCode.InsufficientPrivileges)]
    [InlineData(1920, BusinessErrorCode.UsernameAlreadyExists)]
    [InlineData(2292, BusinessErrorCode.ChildRecordExists)]
    [InlineData(2290, BusinessErrorCode.CheckConstraintViolated)]
    [InlineData(28000, BusinessErrorCode.AccountLocked)]
    [InlineData(12541, BusinessErrorCode.DatabaseUnavailable)]
    [InlineData(12514, BusinessErrorCode.DatabaseUnavailable)]
    public void Map_KnownSystemErrors(int number, BusinessErrorCode expected)
    {
        Assert.Equal(expected, OracleErrorMapper.Map(number, "ORA-00000: x")!.Code);
    }

    [Theory]
    [InlineData(942)]
    [InlineData(20999)]
    [InlineData(6550)]
    public void Map_UnknownErrors_ReturnNull(int number)
    {
        Assert.Null(OracleErrorMapper.Map(number, "ORA-00942: table or view does not exist"));
    }

    [Fact]
    public void CleanMessage_KeepsFirstLineWithoutOraPrefix()
    {
        const string raw = "ORA-20003: Seat 5 is already booked.\nORA-06512: at \"TRANSPORT.TRG_TICKETS_CHECK_SEAT\", line 40\nhttps://docs.oracle.com/error-help/db/ora-20003/";

        Assert.Equal("Seat 5 is already booked.", OracleErrorMapper.CleanMessage(raw));
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build error `The type or namespace name 'Persistence' does not exist in the namespace 'Transport.Infrastructure'`.

- [ ] **Step 3: Write `DbEnum.cs` and `OracleErrorMapper.cs`**

`src/Transport.Infrastructure/Persistence/DbEnum.cs`:
```csharp
using System.Text.RegularExpressions;
using Transport.Domain.Enums;

namespace Transport.Infrastructure.Persistence;

// Chuyển enum C# sang giá trị trạng thái trong Oracle (VD TicketSeller -> TICKET_SELLER).
// Chiều đọc do Dapper tự Enum.Parse (không phân biệt hoa thường).
internal static partial class DbEnum
{
    public static string ToDb<TEnum>(TEnum value) where TEnum : struct, Enum =>
        WordBoundary().Replace(value.ToString(), "$1_$2").ToUpperInvariant();

    public static string RoleToDb(StaffRole role) => "ROLE_" + ToDb(role);

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex WordBoundary();
}
```

`src/Transport.Infrastructure/Persistence/OracleErrorMapper.cs`:
```csharp
using System.Text.RegularExpressions;
using Transport.Domain.Errors;

namespace Transport.Infrastructure.Persistence;

// Chuyển lỗi Oracle thành BusinessRuleException theo bảng mã lỗi (spec mục 9)
internal static partial class OracleErrorMapper
{
    private static readonly Dictionary<int, BusinessErrorCode> SystemErrors = new()
    {
        [1] = BusinessErrorCode.DuplicateValue,
        [1017] = BusinessErrorCode.InvalidCredentials,
        [1031] = BusinessErrorCode.InsufficientPrivileges,
        [1920] = BusinessErrorCode.UsernameAlreadyExists,
        [2292] = BusinessErrorCode.ChildRecordExists,
        [2290] = BusinessErrorCode.CheckConstraintViolated,
        [28000] = BusinessErrorCode.AccountLocked,
        [12541] = BusinessErrorCode.DatabaseUnavailable,
        [12514] = BusinessErrorCode.DatabaseUnavailable,
    };

    // Trả null nếu không phải lỗi đã biết (để exception gốc được ném tiếp)
    public static BusinessRuleException? Map(int number, string message)
    {
        var text = CleanMessage(message);
        if (number is >= 20000 and <= 20999 && Enum.IsDefined((BusinessErrorCode)number))
        {
            return new BusinessRuleException((BusinessErrorCode)number, text);
        }

        return SystemErrors.TryGetValue(number, out var code) ? new BusinessRuleException(code, text) : null;
    }

    // "ORA-20003: Seat 5 is already booked.\nORA-06512: ..." -> "Seat 5 is already booked."
    public static string CleanMessage(string message)
    {
        var firstLine = message.Split('\n', 2)[0].Trim();
        return OraPrefix().Replace(firstLine, string.Empty);
    }

    [GeneratedRegex(@"^ORA-\d{5}:\s*")]
    private static partial Regex OraPrefix();
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 5: Write options, session, connection factory and repository base**

`src/Transport.Infrastructure/Persistence/DatabaseOptions.cs`:
```csharp
namespace Transport.Infrastructure.Persistence;

// Mục "Database" trong appsettings.json
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string DataSource { get; set; } = "localhost:1521/XEPDB1";

    // Schema chứa bảng/package; mọi phiên được ALTER SESSION SET CURRENT_SCHEMA về đây
    public string Schema { get; set; } = "TRANSPORT";
}
```

`src/Transport.Infrastructure/Persistence/UserSession.cs`:
```csharp
using Transport.Application.Abstractions;

namespace Transport.Infrastructure.Persistence;

// Thông tin đăng nhập của phiên; mật khẩu chỉ giữ trong bộ nhớ để mở kết nối từ pool
internal sealed class UserSession : IUserSession
{
    public string Username { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public bool IsOperator { get; private set; }
    public bool IsAuthenticated => Username.Length > 0;

    internal string Password { get; private set; } = string.Empty;

    internal void Start(string username, string password, string fullName, bool isOperator)
    {
        Username = username;
        Password = password;
        FullName = fullName;
        IsOperator = isOperator;
    }

    internal void Clear() => Start(string.Empty, string.Empty, string.Empty, false);
}
```

`src/Transport.Infrastructure/Persistence/OracleConnectionFactory.cs`:
```csharp
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace Transport.Infrastructure.Persistence;

// Mở kết nối (lấy từ pool) bằng tài khoản Oracle của nhân viên đang đăng nhập
internal sealed partial class OracleConnectionFactory(IOptions<DatabaseOptions> options, UserSession session)
{
    public Task<OracleConnection> OpenAsync(CancellationToken ct) =>
        session.IsAuthenticated
            ? OpenAsync(session.Username, session.Password, ct)
            : throw new InvalidOperationException("No user is signed in.");

    public async Task<OracleConnection> OpenAsync(string username, string password, CancellationToken ct)
    {
        var settings = options.Value;
        if (!SchemaPattern().IsMatch(settings.Schema))
        {
            throw new InvalidOperationException($"Invalid schema name '{settings.Schema}'.");
        }

        var builder = new OracleConnectionStringBuilder
        {
            DataSource = settings.DataSource,
            UserID = username,
            Password = password,
        };
        var connection = new OracleConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            // Nhờ vậy câu SQL trong app không cần tiền tố "transport."
            command.CommandText = $"ALTER SESSION SET CURRENT_SCHEMA = {settings.Schema}";
            await command.ExecuteNonQueryAsync(ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{0,29}$")]
    private static partial Regex SchemaPattern();
}
```

`src/Transport.Infrastructure/Persistence/OracleRepository.cs`:
```csharp
using System.Data;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace Transport.Infrastructure.Persistence;

// Lớp nền cho repository: mở kết nối, chạy lệnh, dịch lỗi Oracle
internal abstract class OracleRepository(OracleConnectionFactory factory)
{
    static OracleRepository()
    {
        // Cột route_name <-> thuộc tính RouteName
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    protected Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? args, CancellationToken ct) =>
        RunAsync<IReadOnlyList<T>>(
            async c => (await c.QueryAsync<T>(new CommandDefinition(sql, args, cancellationToken: ct))).AsList(), ct);

    protected Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? args, CancellationToken ct) =>
        RunAsync(c => c.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sql, args, cancellationToken: ct)), ct);

    protected Task ExecuteAsync(string sql, object? args, CancellationToken ct) =>
        RunAsync(c => c.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: ct)), ct);

    // Gọi procedure/package, VD "pkg_ticket.pay_ticket"
    protected Task CallAsync(string procedure, object? args, CancellationToken ct) =>
        RunAsync(c => c.ExecuteAsync(new CommandDefinition(procedure, args,
            commandType: CommandType.StoredProcedure, cancellationToken: ct)), ct);

    protected async Task<T> RunAsync<T>(Func<OracleConnection, Task<T>> work, CancellationToken ct)
    {
        try
        {
            await using var connection = await factory.OpenAsync(ct);
            return await work(connection);
        }
        catch (OracleException ex) when (OracleErrorMapper.Map(ex.Number, ex.Message) is { } mapped)
        {
            throw mapped;
        }
    }
}
```

- [ ] **Step 6: Write `OracleAuthService.cs` and the DI extension**

`src/Transport.Infrastructure/Persistence/OracleAuthService.cs`:
```csharp
using Dapper;
using Oracle.ManagedDataAccess.Client;
using Transport.Application.Abstractions;
using Transport.Domain.Errors;

namespace Transport.Infrastructure.Persistence;

// Đăng nhập = mở kết nối Oracle bằng tài khoản nhân viên, đọc role đang bật và họ tên trong STAFF
internal sealed class OracleAuthService(OracleConnectionFactory factory, UserSession session) : IAuthService
{
    private const string OperatorRole = "ROLE_OPERATOR";
    private const string SellerRole = "ROLE_TICKET_SELLER";

    public async Task LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = username.Trim().ToUpperInvariant();
        try
        {
            await using var connection = await factory.OpenAsync(user, password, ct);
            var roles = (await connection.QueryAsync<string>(
                    new CommandDefinition("SELECT role FROM session_roles", cancellationToken: ct)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var fullName = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(
                "SELECT full_name FROM staff WHERE username = :p_username", new { p_username = user }, cancellationToken: ct));

            // Chỉ nhân viên có trong STAFF và có một trong hai role mới vào được app (chặn cả TRANSPORT)
            var isOperator = roles.Contains(OperatorRole);
            if (fullName is null || (!isOperator && !roles.Contains(SellerRole)))
            {
                throw new BusinessRuleException(BusinessErrorCode.InsufficientPrivileges, "This account cannot use the application.");
            }

            session.Start(user, password, fullName, isOperator);
        }
        catch (OracleException ex) when (OracleErrorMapper.Map(ex.Number, ex.Message) is { } mapped)
        {
            throw mapped;
        }
    }

    public void Logout()
    {
        session.Clear();
        // Bỏ các kết nối trong pool mang thông tin đăng nhập cũ
        OracleConnection.ClearAllPools();
    }
}
```

`src/Transport.Infrastructure/DependencyInjection.cs`:
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Transport.Application.Abstractions;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddSingleton<UserSession>();
        services.AddSingleton<IUserSession>(sp => sp.GetRequiredService<UserSession>());
        services.AddSingleton<OracleConnectionFactory>();
        services.AddSingleton<IAuthService, OracleAuthService>();
        return services;
    }
}
```

- [ ] **Step 7: Build and test**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests`
Expected: 0 warnings, `failed: 0`.

- [ ] **Step 8: Commit**

```bash
git add src/Transport.Infrastructure tests/Transport.UnitTests
git commit -m "feat(infrastructure): add Oracle connection, error mapping and login"
```

---

### Task 6: Oracle repositories + integration tests

**Files:**
- Create: `src/Transport.Infrastructure/Repositories/RouteRepository.cs`, `VehicleRepository.cs`, `DriverRepository.cs`, `PassengerRepository.cs`, `ScheduleRepository.cs`, `TicketRepository.cs`, `StaffRepository.cs`, `StatisticsQueries.cs`, `BackupRepository.cs`
- Modify: `src/Transport.Infrastructure/DependencyInjection.cs`
- Test: `tests/Transport.UnitTests/Infrastructure/OracleIntegrationTests.cs`

**Interfaces:**
- Consumes: `OracleRepository`, `DbEnum`, all Application repository interfaces (Task 3).
- Produces: DI registrations for `IRouteRepository, IVehicleRepository, IDriverRepository, IPassengerRepository, IScheduleRepository, ITicketRepository, IStaffRepository, IStatisticsQueries, IBackupRepository`.

- [ ] **Step 1: Write the failing integration tests `tests/Transport.UnitTests/Infrastructure/OracleIntegrationTests.cs`**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Transport.Application;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Errors;
using Transport.Infrastructure;

namespace Transport.UnitTests.Infrastructure;

// Chạy với Oracle thật đã cài bằng Plan 1. Bật bằng biến môi trường TRANSPORT_DB_TESTS=1.
public class OracleIntegrationTests
{
    private static void RequireDatabase() =>
        Assert.SkipUnless(Environment.GetEnvironmentVariable("TRANSPORT_DB_TESTS") == "1",
            "Set TRANSPORT_DB_TESTS=1 to run Oracle integration tests.");

    private static ServiceProvider CreateServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Database:DataSource"] = "localhost:1521/XEPDB1" })
            .Build();
        return new ServiceCollection().AddApplication().AddInfrastructure(configuration).BuildServiceProvider();
    }

    private static async Task<ServiceProvider> SignInAsync(string username, string password)
    {
        var services = CreateServices();
        await services.GetRequiredService<IAuthService>().LoginAsync(username, password);
        return services;
    }

    private static Task<IReadOnlyList<TripStatistic>> OpenTripsAsync(ServiceProvider services) =>
        services.GetRequiredService<IStatisticsQueries>().GetTripsAsync(new TripSearchCriteria
        {
            From = DateTime.Today,
            To = DateTime.Today.AddDays(8),
            OnlyOpenForSale = true,
        });

    [Fact]
    public async Task Operator_ReadsSeededCatalogs()
    {
        RequireDatabase();
        using var services = await SignInAsync("operator01", "Operator#01");

        Assert.True(services.GetRequiredService<IUserSession>().IsOperator);
        Assert.Equal(5, (await services.GetRequiredService<IRouteRepository>().GetAllAsync()).Count);
        Assert.Equal(6, (await services.GetRequiredService<IVehicleRepository>().GetAllAsync()).Count);
        Assert.Equal(6, (await services.GetRequiredService<IDriverRepository>().GetAllAsync()).Count);
        Assert.True((await services.GetRequiredService<IPassengerRepository>().GetAllAsync()).Count >= 30);
        Assert.True((await services.GetRequiredService<IStaffRepository>().GetAllAsync()).Count >= 3);
        Assert.NotEmpty(await services.GetRequiredService<IStatisticsQueries>()
            .GetDailyRevenueAsync(DateTime.Today.AddDays(-7), DateTime.Today));
        Assert.True((await services.GetRequiredService<IBackupRepository>().GetJobStatusAsync())!.IsEnabled);
    }

    [Fact]
    public async Task Seller_SeesOpenTripsAndBookedSeats()
    {
        RequireDatabase();
        using var services = await SignInAsync("seller01", "Seller#001");

        var trips = await OpenTripsAsync(services);
        var seats = await services.GetRequiredService<ITicketRepository>().GetBookedSeatsAsync(trips[0].ScheduleId);

        Assert.False(services.GetRequiredService<IUserSession>().IsOperator);
        Assert.Equal("Trần Thị Bán", services.GetRequiredService<IUserSession>().FullName);
        Assert.All(seats, seat => Assert.InRange(seat, 1, trips[0].SeatCount));
        Assert.NotEmpty(await services.GetRequiredService<ITicketRepository>()
            .SearchAsync(new TicketSearchCriteria { From = DateTime.Today.AddDays(-8), To = DateTime.Today }));
    }

    [Fact]
    public async Task BookingSeatBeyondCapacity_IsMappedToSeatNotFound()
    {
        RequireDatabase();
        using var services = await SignInAsync("seller01", "Seller#001");
        var trip = (await OpenTripsAsync(services))[0];

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => services.GetRequiredService<ITicketRepository>()
            .BookAsync(new BookingRequest(trip.ScheduleId, "Integration Test", "0999111222", null, 99)));

        Assert.Equal(BusinessErrorCode.SeatNotFound, ex.Code);
    }

    [Fact]
    public async Task UnknownUser_IsMappedToInvalidCredentials()
    {
        RequireDatabase();
        // Dùng user không tồn tại để không làm khóa tài khoản demo do nhập sai nhiều lần
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => SignInAsync("nobody_xyz", "wrong-password"));

        Assert.Equal(BusinessErrorCode.InvalidCredentials, ex.Code);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `TRANSPORT_DB_TESTS=1 dotnet test --project tests/Transport.UnitTests`
Expected: `UnknownUser_IsMappedToInvalidCredentials` passes, the other three fail with `No service for type 'Transport.Application.Abstractions.IRouteRepository'` (or `IStatisticsQueries`).

- [ ] **Step 3: Write the catalog repositories** (namespace `Transport.Infrastructure.Repositories`)

`RouteRepository.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Domain.Entities;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class RouteRepository(OracleConnectionFactory factory) : OracleRepository(factory), IRouteRepository
{
    public Task<IReadOnlyList<Route>> GetAllAsync(CancellationToken ct = default) => QueryAsync<Route>(
        """
        SELECT route_id AS id, route_name AS name, origin, destination, distance_km, duration_min, base_price
          FROM routes
         ORDER BY route_name
        """, null, ct);

    public Task AddAsync(Route route, CancellationToken ct = default) => ExecuteAsync(
        """
        INSERT INTO routes (route_name, origin, destination, distance_km, duration_min, base_price)
        VALUES (:p_name, :p_origin, :p_destination, :p_distance, :p_duration, :p_price)
        """, Args(route), ct);

    public Task UpdateAsync(Route route, CancellationToken ct = default) => ExecuteAsync(
        """
        UPDATE routes
           SET route_name = :p_name, origin = :p_origin, destination = :p_destination,
               distance_km = :p_distance, duration_min = :p_duration, base_price = :p_price
         WHERE route_id = :p_id
        """, Args(route), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default) =>
        ExecuteAsync("DELETE FROM routes WHERE route_id = :p_id", new { p_id = id }, ct);

    private static object Args(Route route) => new
    {
        p_id = route.Id,
        p_name = route.Name,
        p_origin = route.Origin,
        p_destination = route.Destination,
        p_distance = route.DistanceKm,
        p_duration = route.DurationMin,
        p_price = route.BasePrice,
    };
}
```

`VehicleRepository.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Domain.Entities;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class VehicleRepository(OracleConnectionFactory factory) : OracleRepository(factory), IVehicleRepository
{
    public Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken ct = default) => QueryAsync<Vehicle>(
        """
        SELECT vehicle_id AS id, plate_number, vehicle_type, seat_count, status
          FROM vehicles
         ORDER BY plate_number
        """, null, ct);

    public Task AddAsync(Vehicle vehicle, CancellationToken ct = default) => ExecuteAsync(
        """
        INSERT INTO vehicles (plate_number, vehicle_type, seat_count, status)
        VALUES (:p_plate, :p_type, :p_seats, :p_status)
        """, Args(vehicle), ct);

    public Task UpdateAsync(Vehicle vehicle, CancellationToken ct = default) => ExecuteAsync(
        """
        UPDATE vehicles
           SET plate_number = :p_plate, vehicle_type = :p_type, seat_count = :p_seats, status = :p_status
         WHERE vehicle_id = :p_id
        """, Args(vehicle), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default) =>
        ExecuteAsync("DELETE FROM vehicles WHERE vehicle_id = :p_id", new { p_id = id }, ct);

    private static object Args(Vehicle vehicle) => new
    {
        p_id = vehicle.Id,
        p_plate = vehicle.PlateNumber,
        p_type = vehicle.VehicleType,
        p_seats = vehicle.SeatCount,
        p_status = DbEnum.ToDb(vehicle.Status),
    };
}
```

`DriverRepository.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Domain.Entities;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class DriverRepository(OracleConnectionFactory factory) : OracleRepository(factory), IDriverRepository
{
    public Task<IReadOnlyList<Driver>> GetAllAsync(CancellationToken ct = default) => QueryAsync<Driver>(
        """
        SELECT driver_id AS id, full_name, id_card, phone, license_no, license_class, license_expiry, status
          FROM drivers
         ORDER BY full_name
        """, null, ct);

    public Task AddAsync(Driver driver, CancellationToken ct = default) => ExecuteAsync(
        """
        INSERT INTO drivers (full_name, id_card, phone, license_no, license_class, license_expiry, status)
        VALUES (:p_name, :p_id_card, :p_phone, :p_license_no, :p_class, :p_expiry, :p_status)
        """, Args(driver), ct);

    public Task UpdateAsync(Driver driver, CancellationToken ct = default) => ExecuteAsync(
        """
        UPDATE drivers
           SET full_name = :p_name, id_card = :p_id_card, phone = :p_phone, license_no = :p_license_no,
               license_class = :p_class, license_expiry = :p_expiry, status = :p_status
         WHERE driver_id = :p_id
        """, Args(driver), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default) =>
        ExecuteAsync("DELETE FROM drivers WHERE driver_id = :p_id", new { p_id = id }, ct);

    private static object Args(Driver driver) => new
    {
        p_id = driver.Id,
        p_name = driver.FullName,
        p_id_card = driver.IdCard,
        p_phone = driver.Phone,
        p_license_no = driver.LicenseNo,
        p_class = DbEnum.ToDb(driver.LicenseClass),
        p_expiry = driver.LicenseExpiry.Date,
        p_status = DbEnum.ToDb(driver.Status),
    };
}
```

`PassengerRepository.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Domain.Entities;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class PassengerRepository(OracleConnectionFactory factory) : OracleRepository(factory), IPassengerRepository
{
    private const string SelectColumns = "SELECT passenger_id AS id, full_name, phone, id_card, email FROM passengers";

    public Task<IReadOnlyList<Passenger>> GetAllAsync(CancellationToken ct = default) =>
        QueryAsync<Passenger>($"{SelectColumns} ORDER BY full_name", null, ct);

    public Task<Passenger?> FindByPhoneAsync(string phone, CancellationToken ct = default) =>
        QuerySingleOrDefaultAsync<Passenger>($"{SelectColumns} WHERE phone = :p_phone", new { p_phone = phone }, ct);

    public Task AddAsync(Passenger passenger, CancellationToken ct = default) => ExecuteAsync(
        """
        INSERT INTO passengers (full_name, phone, id_card, email)
        VALUES (:p_name, :p_phone, :p_id_card, :p_email)
        """, Args(passenger), ct);

    public Task UpdateAsync(Passenger passenger, CancellationToken ct = default) => ExecuteAsync(
        """
        UPDATE passengers
           SET full_name = :p_name, phone = :p_phone, id_card = :p_id_card, email = :p_email
         WHERE passenger_id = :p_id
        """, Args(passenger), ct);

    public Task DeleteAsync(int id, CancellationToken ct = default) =>
        ExecuteAsync("DELETE FROM passengers WHERE passenger_id = :p_id", new { p_id = id }, ct);

    private static object Args(Passenger passenger) => new
    {
        p_id = passenger.Id,
        p_name = passenger.FullName,
        p_phone = passenger.Phone,
        p_id_card = passenger.IdCard,
        p_email = passenger.Email,
    };
}
```

- [ ] **Step 4: Write schedule, ticket and staff repositories**

`ScheduleRepository.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Domain.Entities;
using Transport.Domain.Enums;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class ScheduleRepository(OracleConnectionFactory factory) : OracleRepository(factory), IScheduleRepository
{
    public Task AddAsync(Schedule schedule, CancellationToken ct = default) => ExecuteAsync(
        """
        INSERT INTO schedules (route_id, vehicle_id, driver_id, departure_time, arrival_time, ticket_price)
        VALUES (:p_route, :p_vehicle, :p_driver, :p_departure, :p_arrival, :p_price)
        """, Args(schedule), ct);

    public Task UpdateAsync(Schedule schedule, CancellationToken ct = default) => ExecuteAsync(
        """
        UPDATE schedules
           SET route_id = :p_route, vehicle_id = :p_vehicle, driver_id = :p_driver,
               departure_time = :p_departure, arrival_time = :p_arrival, ticket_price = :p_price
         WHERE schedule_id = :p_id
        """, Args(schedule), ct);

    public Task DeleteAsync(int scheduleId, CancellationToken ct = default) =>
        ExecuteAsync("DELETE FROM schedules WHERE schedule_id = :p_id", new { p_id = scheduleId }, ct);

    // Hủy chuyến phải qua CancelAsync (trigger TRG_SCHEDULES_CHECK_CANCEL chặn UPDATE trực tiếp khi còn vé)
    public Task SetStatusAsync(int scheduleId, ScheduleStatus status, CancellationToken ct = default) => ExecuteAsync(
        "UPDATE schedules SET status = :p_status WHERE schedule_id = :p_id",
        new { p_id = scheduleId, p_status = DbEnum.ToDb(status) }, ct);

    public Task CancelAsync(int scheduleId, string reason, CancellationToken ct = default) =>
        CallAsync("pkg_schedule.cancel_schedule", new { p_schedule_id = scheduleId, p_reason = reason }, ct);

    private static object Args(Schedule schedule) => new
    {
        p_id = schedule.Id,
        p_route = schedule.RouteId,
        p_vehicle = schedule.VehicleId,
        p_driver = schedule.DriverId,
        p_departure = schedule.DepartureTime,
        p_arrival = schedule.ArrivalTime,
        p_price = schedule.TicketPrice,
    };
}
```

`TicketRepository.cs`:
```csharp
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class TicketRepository(OracleConnectionFactory factory) : OracleRepository(factory), ITicketRepository
{
    public async Task<IReadOnlySet<int>> GetBookedSeatsAsync(int scheduleId, CancellationToken ct = default)
    {
        var seats = await QueryAsync<int>(
            "SELECT seat_no FROM tickets WHERE schedule_id = :p_schedule_id AND status <> 'CANCELLED'",
            new { p_schedule_id = scheduleId }, ct);
        return seats.ToHashSet();
    }

    // Dùng OracleCommand trực tiếp vì cần đọc tham số OUT o_ticket_id
    public Task<long> BookAsync(BookingRequest request, CancellationToken ct = default) => RunAsync(async connection =>
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "pkg_ticket.book_ticket";
        command.CommandType = CommandType.StoredProcedure;
        command.BindByName = true;
        command.Parameters.Add("p_schedule_id", OracleDbType.Int32).Value = request.ScheduleId;
        command.Parameters.Add("p_full_name", OracleDbType.Varchar2).Value = request.FullName;
        command.Parameters.Add("p_phone", OracleDbType.Varchar2).Value = request.Phone;
        command.Parameters.Add("p_id_card", OracleDbType.Varchar2).Value = (object?)request.IdCard ?? DBNull.Value;
        command.Parameters.Add("p_seat_no", OracleDbType.Int32).Value = request.SeatNo;
        var ticketId = command.Parameters.Add("o_ticket_id", OracleDbType.Int64, ParameterDirection.Output);
        await command.ExecuteNonQueryAsync(ct);
        return ((OracleDecimal)ticketId.Value).ToInt64();
    }, ct);

    public Task PayAsync(long ticketId, PaymentMethod method, CancellationToken ct = default) =>
        CallAsync("pkg_ticket.pay_ticket", new { p_ticket_id = ticketId, p_method = DbEnum.ToDb(method) }, ct);

    public Task CancelAsync(long ticketId, string reason, CancellationToken ct = default) =>
        CallAsync("pkg_ticket.cancel_ticket", new { p_ticket_id = ticketId, p_reason = reason }, ct);

    public Task<IReadOnlyList<TicketListItem>> SearchAsync(TicketSearchCriteria criteria, CancellationToken ct = default) =>
        QueryAsync<TicketListItem>(
            """
            SELECT t.ticket_id, t.schedule_id, r.route_name, s.departure_time, v.plate_number, t.seat_no,
                   p.full_name AS passenger_name, p.phone, t.price, t.status, t.booked_at, t.sold_by, t.cancel_reason
              FROM tickets t
              JOIN schedules s  ON s.schedule_id  = t.schedule_id
              JOIN routes r     ON r.route_id     = s.route_id
              JOIN vehicles v   ON v.vehicle_id   = s.vehicle_id
              JOIN passengers p ON p.passenger_id = t.passenger_id
             WHERE (:p_keyword IS NULL
                    OR p.phone = :p_keyword
                    OR TO_CHAR(t.ticket_id) = :p_keyword
                    OR UPPER(p.full_name) LIKE '%' || UPPER(:p_keyword) || '%')
               AND (:p_status IS NULL OR t.status = :p_status)
               AND (:p_schedule_id IS NULL OR t.schedule_id = :p_schedule_id)
               AND (:p_from IS NULL OR s.departure_time >= :p_from)
               AND (:p_to IS NULL OR s.departure_time < :p_to)
             ORDER BY t.booked_at DESC
             FETCH FIRST 500 ROWS ONLY
            """,
            new
            {
                p_keyword = criteria.Keyword,
                p_status = criteria.Status is { } status ? DbEnum.ToDb(status) : null,
                p_schedule_id = criteria.ScheduleId,
                p_from = criteria.From,
                p_to = criteria.To,
            }, ct);

    public Task<TicketPrintModel?> GetPrintModelAsync(long ticketId, CancellationToken ct = default) =>
        QuerySingleOrDefaultAsync<TicketPrintModel>(
            """
            SELECT t.ticket_id, r.route_name, r.origin, r.destination, s.departure_time, s.arrival_time,
                   v.plate_number, v.vehicle_type, t.seat_no, p.full_name AS passenger_name, p.phone,
                   t.price, t.status, t.sold_by, t.booked_at
              FROM tickets t
              JOIN schedules s  ON s.schedule_id  = t.schedule_id
              JOIN routes r     ON r.route_id     = s.route_id
              JOIN vehicles v   ON v.vehicle_id   = s.vehicle_id
              JOIN passengers p ON p.passenger_id = t.passenger_id
             WHERE t.ticket_id = :p_ticket_id
            """, new { p_ticket_id = ticketId }, ct);
}
```

`StaffRepository.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Entities;
using Transport.Domain.Enums;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

// Mọi thay đổi đi qua PKG_STAFF (tạo/khóa user Oracle)
internal sealed class StaffRepository(OracleConnectionFactory factory) : OracleRepository(factory), IStaffRepository
{
    // 'ROLE_TICKET_SELLER' -> 'TICKETSELLER' để Dapper parse thành StaffRole.TicketSeller
    public Task<IReadOnlyList<StaffMember>> GetAllAsync(CancellationToken ct = default) => QueryAsync<StaffMember>(
        """
        SELECT username, full_name, phone, REPLACE(SUBSTR(role_name, 6), '_', '') AS role,
               status, created_at, created_by
          FROM staff
         ORDER BY username
        """, null, ct);

    public Task CreateAsync(NewStaffRequest request, CancellationToken ct = default) => CallAsync("pkg_staff.create_staff", new
    {
        p_username = request.Username,
        p_password = request.Password,
        p_full_name = request.FullName,
        p_phone = request.Phone,
        p_role = DbEnum.RoleToDb(request.Role),
    }, ct);

    public Task UpdateInfoAsync(string username, string fullName, string? phone, CancellationToken ct = default) =>
        CallAsync("pkg_staff.update_staff_info", new { p_username = username, p_full_name = fullName, p_phone = phone }, ct);

    public Task ChangeRoleAsync(string username, StaffRole role, CancellationToken ct = default) =>
        CallAsync("pkg_staff.change_role", new { p_username = username, p_role = DbEnum.RoleToDb(role) }, ct);

    public Task ResetPasswordAsync(string username, string password, CancellationToken ct = default) =>
        CallAsync("pkg_staff.reset_password", new { p_username = username, p_password = password }, ct);

    public Task SetLockedAsync(string username, bool locked, CancellationToken ct = default) =>
        CallAsync("pkg_staff.set_locked", new { p_username = username, p_locked = locked ? 1 : 0 }, ct);
}
```

- [ ] **Step 5: Write statistics and backup queries**

`StatisticsQueries.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class StatisticsQueries(OracleConnectionFactory factory) : OracleRepository(factory), IStatisticsQueries
{
    public Task<IReadOnlyList<TripStatistic>> GetTripsAsync(TripSearchCriteria criteria, CancellationToken ct = default) =>
        QueryAsync<TripStatistic>(
            """
            SELECT schedule_id, route_id, vehicle_id, driver_id, route_name, origin, destination,
                   departure_time, arrival_time, ticket_price, schedule_status AS status, plate_number,
                   vehicle_type, driver_name, seat_count, seats_sold, seats_available, occupancy_pct,
                   tickets_cancelled, revenue
              FROM v_trip_statistics
             WHERE departure_time >= :p_from AND departure_time < :p_to
               AND (:p_route_id IS NULL OR route_id = :p_route_id)
               AND (:p_origin IS NULL OR UPPER(origin) LIKE '%' || UPPER(:p_origin) || '%')
               AND (:p_destination IS NULL OR UPPER(destination) LIKE '%' || UPPER(:p_destination) || '%')
               AND (:p_min_price IS NULL OR ticket_price >= :p_min_price)
               AND (:p_max_price IS NULL OR ticket_price <= :p_max_price)
               AND (:p_only_open = 0 OR (schedule_status = 'SCHEDULED' AND departure_time > SYSDATE))
             ORDER BY departure_time
            """,
            new
            {
                p_from = criteria.From,
                p_to = criteria.To,
                p_route_id = criteria.RouteId,
                p_origin = string.IsNullOrWhiteSpace(criteria.Origin) ? null : criteria.Origin.Trim(),
                p_destination = string.IsNullOrWhiteSpace(criteria.Destination) ? null : criteria.Destination.Trim(),
                p_min_price = criteria.MinPrice,
                p_max_price = criteria.MaxPrice,
                p_only_open = criteria.OnlyOpenForSale ? 1 : 0,
            }, ct);

    public Task<IReadOnlyList<DailyRevenue>> GetDailyRevenueAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        QueryAsync<DailyRevenue>(
            """
            SELECT TRUNC(departure_time) AS revenue_date, SUM(revenue) AS revenue, SUM(seats_sold) AS tickets
              FROM v_trip_statistics
             WHERE departure_time >= :p_from AND departure_time < :p_to AND schedule_status <> 'CANCELLED'
             GROUP BY TRUNC(departure_time)
             ORDER BY revenue_date
            """, new { p_from = from, p_to = to }, ct);
}
```

`BackupRepository.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Infrastructure.Persistence;

namespace Transport.Infrastructure.Repositories;

internal sealed class BackupRepository(OracleConnectionFactory factory) : OracleRepository(factory), IBackupRepository
{
    public Task RunBackupAsync(CancellationToken ct = default) => CallAsync("backup_operational_data", null, ct);

    public Task<IReadOnlyList<BackupLog>> GetLogsAsync(CancellationToken ct = default) => QueryAsync<BackupLog>(
        """
        SELECT log_id, backup_at, performed_by, schedule_rows, ticket_rows, payment_rows
          FROM backup_logs
         ORDER BY log_id DESC
         FETCH FIRST 50 ROWS ONLY
        """, null, ct);

    // CAST bỏ múi giờ của TIMESTAMP WITH TIME ZONE để đọc ra DateTime
    public Task<BackupJobStatus?> GetJobStatusAsync(CancellationToken ct = default) => QuerySingleOrDefaultAsync<BackupJobStatus>(
        """
        SELECT job_name, enabled, state,
               CAST(last_start_date AS TIMESTAMP) AS last_start_date,
               CAST(next_run_date AS TIMESTAMP) AS next_run_date,
               failure_count
          FROM v_backup_job_status
        """, null, ct);
}
```

- [ ] **Step 6: Register the repositories – replace the body of `AddInfrastructure` in `src/Transport.Infrastructure/DependencyInjection.cs`**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Transport.Application.Abstractions;
using Transport.Infrastructure.Persistence;
using Transport.Infrastructure.Repositories;

namespace Transport.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddSingleton<UserSession>();
        services.AddSingleton<IUserSession>(sp => sp.GetRequiredService<UserSession>());
        services.AddSingleton<OracleConnectionFactory>();
        services.AddSingleton<IAuthService, OracleAuthService>();

        services.AddSingleton<IRouteRepository, RouteRepository>();
        services.AddSingleton<IVehicleRepository, VehicleRepository>();
        services.AddSingleton<IDriverRepository, DriverRepository>();
        services.AddSingleton<IPassengerRepository, PassengerRepository>();
        services.AddSingleton<IScheduleRepository, ScheduleRepository>();
        services.AddSingleton<ITicketRepository, TicketRepository>();
        services.AddSingleton<IStaffRepository, StaffRepository>();
        services.AddSingleton<IStatisticsQueries, StatisticsQueries>();
        services.AddSingleton<IBackupRepository, BackupRepository>();
        return services;
    }
}
```

- [ ] **Step 7: Run unit + integration tests**

Run: `dotnet test --project tests/Transport.UnitTests && TRANSPORT_DB_TESTS=1 dotnet test --project tests/Transport.UnitTests`
Expected: first run – integration tests reported as skipped, rest pass; second run – everything passes (`failed: 0`, `skipped: 0`).

- [ ] **Step 8: Commit**

```bash
git add src/Transport.Infrastructure tests/Transport.UnitTests
git commit -m "feat(infrastructure): add Oracle repositories and integration tests"
```

---

### Task 7: Excel exporter

**Files:**
- Create: `src/Transport.Infrastructure/Export/ClosedXmlExcelExporter.cs`
- Modify: `src/Transport.Infrastructure/DependencyInjection.cs` (register exporter)
- Test: `tests/Transport.UnitTests/Infrastructure/ClosedXmlExcelExporterTests.cs`

**Interfaces:**
- Consumes: `IExcelExporter`, `ExportColumn<T>`, `IUserSession`, `TimeProvider`.
- Produces: `ClosedXmlExcelExporter : IExcelExporter` (internal) registered as singleton. Sheet layout: A1 title (bold 14pt), A2 `dd/MM/yyyy HH:mm - FullName (USERNAME)`, row 4 headers, data from row 5 as an Excel table.

- [ ] **Step 1: Write the failing test**

```csharp
using ClosedXML.Excel;
using Transport.Application.Models;
using Transport.Infrastructure.Export;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Infrastructure;

public class ClosedXmlExcelExporterTests
{
    private sealed record Row(string Name, decimal Price, DateTime Departure);

    [Fact]
    public async Task ExportAsync_WritesTitleHeadersAndFormattedRows()
    {
        var path = Path.Combine(Path.GetTempPath(), $"export-test-{Guid.NewGuid():N}.xlsx");
        var exporter = new ClosedXmlExcelExporter(new FakeUserSession(),
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 17, 9, 30, 0, TimeSpan.Zero)));
        var columns = new[]
        {
            new ExportColumn<Row>("Tuyến", r => r.Name),
            new ExportColumn<Row>("Giá", r => r.Price, "#,##0"),
            new ExportColumn<Row>("Khởi hành", r => r.Departure, "dd/MM/yyyy HH:mm"),
        };
        var rows = new[] { new Row("Hà Nội - Lào Cai", 280000m, new DateTime(2026, 10, 20, 7, 0, 0)) };

        try
        {
            await exporter.ExportAsync(rows, columns, "Danh sách chuyến", path);

            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheet(1);
            Assert.Equal("Danh sách chuyến", sheet.Cell(1, 1).GetString());
            Assert.Equal("17/10/2026 09:30 - Nguyễn Văn Điều (OPERATOR01)", sheet.Cell(2, 1).GetString());
            Assert.Equal("Giá", sheet.Cell(4, 2).GetString());
            Assert.Equal("Hà Nội - Lào Cai", sheet.Cell(5, 1).GetString());
            Assert.Equal(280000d, sheet.Cell(5, 2).GetDouble());
            Assert.Equal("#,##0", sheet.Cell(5, 2).Style.NumberFormat.Format);
            Assert.Equal(new DateTime(2026, 10, 20, 7, 0, 0), sheet.Cell(5, 3).GetDateTime());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build error `The type or namespace name 'Export' does not exist in the namespace 'Transport.Infrastructure'`.

- [ ] **Step 3: Write `src/Transport.Infrastructure/Export/ClosedXmlExcelExporter.cs`**

```csharp
using System.Globalization;
using ClosedXML.Excel;
using Transport.Application.Abstractions;
using Transport.Application.Models;

namespace Transport.Infrastructure.Export;

// Xuất danh sách ra .xlsx: dòng 1 tiêu đề, dòng 2 thời điểm + người xuất, từ dòng 4 là bảng
internal sealed class ClosedXmlExcelExporter(IUserSession session, TimeProvider time) : IExcelExporter
{
    private const int HeaderRow = 4;

    public Task ExportAsync<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string title, string filePath,
        CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Data");

        var titleCell = sheet.Cell(1, 1);
        titleCell.Value = title;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 14;
        var exportedAt = time.GetLocalNow().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        sheet.Cell(2, 1).Value = $"{exportedAt} - {session.FullName} ({session.Username})";

        for (var c = 0; c < columns.Count; c++)
        {
            sheet.Cell(HeaderRow, c + 1).Value = columns[c].Header;
        }

        var row = HeaderRow + 1;
        foreach (var item in rows)
        {
            ct.ThrowIfCancellationRequested();
            for (var c = 0; c < columns.Count; c++)
            {
                var cell = sheet.Cell(row, c + 1);
                cell.Value = ToCellValue(columns[c].Value(item));
                if (columns[c].Format is { } format)
                {
                    cell.Style.NumberFormat.Format = format;
                }
            }

            row++;
        }

        if (row > HeaderRow + 1)
        {
            sheet.Range(HeaderRow, 1, row - 1, columns.Count).CreateTable();
        }
        else
        {
            sheet.Row(HeaderRow).Style.Font.Bold = true;
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
        return Task.CompletedTask;
    }

    private static XLCellValue ToCellValue(object? value) => value switch
    {
        null => Blank.Value,
        string text => text,
        DateTime date => date,
        decimal number => (double)number,
        int number => (double)number,
        long number => (double)number,
        double number => number,
        bool flag => flag,
        _ => value.ToString() ?? string.Empty,
    };
}
```

- [ ] **Step 4: Register it** – in `AddInfrastructure`, add before `return services;`:

```csharp
        services.AddSingleton<IExcelExporter, Export.ClosedXmlExcelExporter>();
```

- [ ] **Step 5: Run tests**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 6: Commit**

```bash
git add src/Transport.Infrastructure tests/Transport.UnitTests
git commit -m "feat(infrastructure): add ClosedXML Excel exporter"
```

---

### Task 8: Localization (resx + service + markup extension)

**Files:**
- Create: `src/Transport.Presentation.Wpf/Resources/Strings.resx`, `Strings.vi.resx`
- Create: `src/Transport.Presentation.Wpf/Localization/ILocalizationService.cs`, `LocalizationService.cs`, `LocExtension.cs`, `LocalizeConverter.cs`
- Test: `tests/Transport.UnitTests/Presentation/ResourceParityTests.cs`, `LocalizationServiceTests.cs`

**Interfaces:**
- Consumes: `BusinessRuleException`, `BusinessErrorCode`.
- Produces (namespace `Transport.Presentation.Wpf.Localization`):
  - `ILocalizationService : INotifyPropertyChanged` – `string this[string key]`, `string CurrentLanguage` (`"vi"`/`"en"`), `void SetLanguage(string)`, `string Format(string key, params object[] args)`, `string Translate(BusinessRuleException, params object[] args)`
  - `LocalizationService.Instance` (singleton used by XAML and DI)
  - XAML: `{l:Loc Key}`; converter `LocalizeConverter` (key = `ConverterParameter` prefix + value)
  - Resource keys of this task (later tasks append more).

- [ ] **Step 1: Write the failing tests**

`tests/Transport.UnitTests/Presentation/ResourceParityTests.cs`:
```csharp
using System.Xml.Linq;
using Transport.Domain.Errors;

namespace Transport.UnitTests.Presentation;

public class ResourceParityTests
{
    private static HashSet<string> Keys(string fileName) =>
        XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Resources", fileName))
            .Root!.Elements("data")
            .Select(e => (string)e.Attribute("name")!)
            .ToHashSet();

    [Fact]
    public void EnglishAndVietnameseHaveTheSameKeys()
    {
        var en = Keys("Strings.resx");
        var vi = Keys("Strings.vi.resx");

        Assert.Empty(en.Except(vi));
        Assert.Empty(vi.Except(en));
    }

    [Fact]
    public void EveryBusinessErrorCodeHasATranslation()
    {
        var en = Keys("Strings.resx");

        var missing = Enum.GetValues<BusinessErrorCode>().Select(c => $"Err_{(int)c}").Where(k => !en.Contains(k));

        Assert.Empty(missing);
    }
}
```

`tests/Transport.UnitTests/Presentation/LocalizationServiceTests.cs`:
```csharp
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Localization;

namespace Transport.UnitTests.Presentation;

public class LocalizationServiceTests
{
    [Fact]
    public void Indexer_ReturnsTextForCurrentLanguage()
    {
        var localization = new LocalizationService();

        localization.SetLanguage("vi");
        Assert.Equal("Đăng nhập", localization["Login_Title"]);

        localization.SetLanguage("en");
        Assert.Equal("Sign in", localization["Login_Title"]);
        Assert.Equal("en", localization.CurrentLanguage);
    }

    [Fact]
    public void Indexer_ReturnsKeyWhenMissing()
    {
        Assert.Equal("No_Such_Key", new LocalizationService()["No_Such_Key"]);
    }

    [Fact]
    public void SetLanguage_RaisesIndexerChange()
    {
        var localization = new LocalizationService();
        var changed = new List<string?>();
        localization.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        localization.SetLanguage("en");

        Assert.Contains("Item[]", changed);
    }

    [Fact]
    public void Translate_FormatsKnownErrorAndFallsBackToEnglishMessage()
    {
        var localization = new LocalizationService();
        localization.SetLanguage("vi");

        Assert.Equal("Ghế 5 đã có người đặt.",
            localization.Translate(new BusinessRuleException(BusinessErrorCode.SeatAlreadyBooked, "Seat 5 is already booked."), 5));
        Assert.Equal("Raw message",
            localization.Translate(new BusinessRuleException((BusinessErrorCode)12345, "Raw message")));
    }

    [Fact]
    public void Format_ReturnsTemplateWhenArgumentsAreMissing()
    {
        var localization = new LocalizationService();
        localization.SetLanguage("en");

        Assert.Equal("Exported to {0}", localization.Format("Msg_Exported"));
        Assert.Equal("Exported to C:\\a.xlsx", localization.Format("Msg_Exported", "C:\\a.xlsx"));
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build error `The type or namespace name 'Localization' does not exist in the namespace 'Transport.Presentation.Wpf'`.

- [ ] **Step 3: Write `src/Transport.Presentation.Wpf/Resources/Strings.resx` (English, neutral)**

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <data name="App_Title" xml:space="preserve"><value>Transport &amp; Ticketing Management</value></data>
  <data name="App_Subtitle" xml:space="preserve"><value>Bus transport &amp; ticketing</value></data>
  <data name="Login_Title" xml:space="preserve"><value>Sign in</value></data>
  <data name="Login_Hint" xml:space="preserve"><value>Sign in with your Oracle staff account.</value></data>
  <data name="Login_Username" xml:space="preserve"><value>Username</value></data>
  <data name="Login_Password" xml:space="preserve"><value>Password</value></data>
  <data name="Login_Language" xml:space="preserve"><value>Language</value></data>
  <data name="Login_Button" xml:space="preserve"><value>Sign in</value></data>
  <data name="Lang_vi" xml:space="preserve"><value>Tiếng Việt</value></data>
  <data name="Lang_en" xml:space="preserve"><value>English</value></data>
  <data name="Role_Operator" xml:space="preserve"><value>Operator</value></data>
  <data name="Role_TicketSeller" xml:space="preserve"><value>Ticket seller</value></data>
  <data name="Btn_SwitchLanguage" xml:space="preserve"><value>Tiếng Việt</value></data>
  <data name="Btn_Logout" xml:space="preserve"><value>Sign out</value></data>
  <data name="Btn_OK" xml:space="preserve"><value>OK</value></data>
  <data name="Btn_Cancel" xml:space="preserve"><value>Cancel</value></data>
  <data name="Btn_Close" xml:space="preserve"><value>Close</value></data>
  <data name="Msg_ConfirmLogout" xml:space="preserve"><value>Do you want to sign out?</value></data>
  <data name="Msg_Saved" xml:space="preserve"><value>Saved successfully.</value></data>
  <data name="Msg_Deleted" xml:space="preserve"><value>Deleted successfully.</value></data>
  <data name="Msg_ConfirmDelete" xml:space="preserve"><value>Delete {0}?</value></data>
  <data name="Msg_FixErrors" xml:space="preserve"><value>Please correct the highlighted fields.</value></data>
  <data name="Msg_Exported" xml:space="preserve"><value>Exported to {0}</value></data>
  <data name="Dialog_Payment_Title" xml:space="preserve"><value>Payment</value></data>
  <data name="Dialog_Payment_Amount" xml:space="preserve"><value>Amount to pay</value></data>
  <data name="Dialog_Payment_Method" xml:space="preserve"><value>Payment method</value></data>
  <data name="Dialog_Password_Label" xml:space="preserve"><value>New password (at least 8 characters)</value></data>
  <data name="PaymentMethod_Cash" xml:space="preserve"><value>Cash</value></data>
  <data name="PaymentMethod_Transfer" xml:space="preserve"><value>Bank transfer</value></data>
  <data name="PaymentMethod_Card" xml:space="preserve"><value>Card</value></data>
  <data name="Val_Required" xml:space="preserve"><value>This field is required.</value></data>
  <data name="Val_Phone" xml:space="preserve"><value>10 digits starting with 0.</value></data>
  <data name="Val_Positive" xml:space="preserve"><value>Must be greater than 0.</value></data>
  <data name="Err_20001" xml:space="preserve"><value>Seat {0} does not exist on this vehicle.</value></data>
  <data name="Err_20002" xml:space="preserve"><value>This trip is no longer open for sale.</value></data>
  <data name="Err_20003" xml:space="preserve"><value>Seat {0} is already booked.</value></data>
  <data name="Err_20010" xml:space="preserve"><value>The trip has departed; booking is not allowed.</value></data>
  <data name="Err_20011" xml:space="preserve"><value>Trip not found.</value></data>
  <data name="Err_20020" xml:space="preserve"><value>Only unpaid (booked) tickets can be paid.</value></data>
  <data name="Err_20021" xml:space="preserve"><value>Ticket not found.</value></data>
  <data name="Err_20030" xml:space="preserve"><value>The ticket is already cancelled.</value></data>
  <data name="Err_20031" xml:space="preserve"><value>The trip has departed; cancellation is not allowed.</value></data>
  <data name="Err_20032" xml:space="preserve"><value>Ticket not found.</value></data>
  <data name="Err_20040" xml:space="preserve"><value>Invalid username (3–30 letters, digits or _, starting with a letter).</value></data>
  <data name="Err_20041" xml:space="preserve"><value>Invalid role.</value></data>
  <data name="Err_20042" xml:space="preserve"><value>Staff member not found.</value></data>
  <data name="Err_20043" xml:space="preserve"><value>You cannot lock or change your own account.</value></data>
  <data name="Err_20044" xml:space="preserve"><value>Password must be at least 8 characters and must not contain double quotes.</value></data>
  <data name="Err_20050" xml:space="preserve"><value>Trip not found.</value></data>
  <data name="Err_20051" xml:space="preserve"><value>Only scheduled trips can be cancelled.</value></data>
  <data name="Err_20052" xml:space="preserve"><value>This trip still has active tickets; use "Cancel trip".</value></data>
  <data name="Err_1" xml:space="preserve"><value>This value already exists (plate number, phone, ID card, licence number…).</value></data>
  <data name="Err_1017" xml:space="preserve"><value>Wrong username or password.</value></data>
  <data name="Err_1031" xml:space="preserve"><value>You do not have permission to perform this action.</value></data>
  <data name="Err_1920" xml:space="preserve"><value>This username already exists.</value></data>
  <data name="Err_2292" xml:space="preserve"><value>Cannot delete because related data exists.</value></data>
  <data name="Err_2290" xml:space="preserve"><value>Invalid data (for example origin equals destination, or seat count outside 1–60).</value></data>
  <data name="Err_28000" xml:space="preserve"><value>This account is locked.</value></data>
  <data name="Err_12541" xml:space="preserve"><value>Cannot connect to Oracle. Check that the database service is running.</value></data>
  <data name="Err_90001" xml:space="preserve"><value>Please fill in all required fields.</value></data>
  <data name="Err_90002" xml:space="preserve"><value>Phone number must be 10 digits starting with 0.</value></data>
  <data name="Err_90003" xml:space="preserve"><value>Please choose a valid seat.</value></data>
  <data name="Err_90004" xml:space="preserve"><value>Arrival time must be after departure time.</value></data>
  <data name="Err_90005" xml:space="preserve"><value>Price must be greater than 0.</value></data>
  <data name="Err_90006" xml:space="preserve"><value>Only operators can perform this action.</value></data>
</root>
```

- [ ] **Step 4: Write `src/Transport.Presentation.Wpf/Resources/Strings.vi.resx` (Vietnamese)**

Same four `resheader` lines, then:

```xml
  <data name="App_Title" xml:space="preserve"><value>Quản lý vận tải và đặt vé xe</value></data>
  <data name="App_Subtitle" xml:space="preserve"><value>Vận tải &amp; đặt vé xe khách</value></data>
  <data name="Login_Title" xml:space="preserve"><value>Đăng nhập</value></data>
  <data name="Login_Hint" xml:space="preserve"><value>Đăng nhập bằng tài khoản Oracle của nhân viên.</value></data>
  <data name="Login_Username" xml:space="preserve"><value>Tên đăng nhập</value></data>
  <data name="Login_Password" xml:space="preserve"><value>Mật khẩu</value></data>
  <data name="Login_Language" xml:space="preserve"><value>Ngôn ngữ</value></data>
  <data name="Login_Button" xml:space="preserve"><value>Đăng nhập</value></data>
  <data name="Lang_vi" xml:space="preserve"><value>Tiếng Việt</value></data>
  <data name="Lang_en" xml:space="preserve"><value>English</value></data>
  <data name="Role_Operator" xml:space="preserve"><value>Điều hành</value></data>
  <data name="Role_TicketSeller" xml:space="preserve"><value>Nhân viên bán vé</value></data>
  <data name="Btn_SwitchLanguage" xml:space="preserve"><value>English</value></data>
  <data name="Btn_Logout" xml:space="preserve"><value>Đăng xuất</value></data>
  <data name="Btn_OK" xml:space="preserve"><value>Đồng ý</value></data>
  <data name="Btn_Cancel" xml:space="preserve"><value>Hủy bỏ</value></data>
  <data name="Btn_Close" xml:space="preserve"><value>Đóng</value></data>
  <data name="Msg_ConfirmLogout" xml:space="preserve"><value>Bạn có muốn đăng xuất không?</value></data>
  <data name="Msg_Saved" xml:space="preserve"><value>Đã lưu thành công.</value></data>
  <data name="Msg_Deleted" xml:space="preserve"><value>Đã xóa thành công.</value></data>
  <data name="Msg_ConfirmDelete" xml:space="preserve"><value>Bạn có chắc muốn xóa {0}?</value></data>
  <data name="Msg_FixErrors" xml:space="preserve"><value>Vui lòng sửa các ô đang báo lỗi.</value></data>
  <data name="Msg_Exported" xml:space="preserve"><value>Đã xuất file: {0}</value></data>
  <data name="Dialog_Payment_Title" xml:space="preserve"><value>Thanh toán</value></data>
  <data name="Dialog_Payment_Amount" xml:space="preserve"><value>Số tiền cần thanh toán</value></data>
  <data name="Dialog_Payment_Method" xml:space="preserve"><value>Phương thức thanh toán</value></data>
  <data name="Dialog_Password_Label" xml:space="preserve"><value>Mật khẩu mới (tối thiểu 8 ký tự)</value></data>
  <data name="PaymentMethod_Cash" xml:space="preserve"><value>Tiền mặt</value></data>
  <data name="PaymentMethod_Transfer" xml:space="preserve"><value>Chuyển khoản</value></data>
  <data name="PaymentMethod_Card" xml:space="preserve"><value>Thẻ</value></data>
  <data name="Val_Required" xml:space="preserve"><value>Không được để trống.</value></data>
  <data name="Val_Phone" xml:space="preserve"><value>Gồm 10 chữ số, bắt đầu bằng 0.</value></data>
  <data name="Val_Positive" xml:space="preserve"><value>Phải lớn hơn 0.</value></data>
  <data name="Err_20001" xml:space="preserve"><value>Ghế {0} không có trên xe này.</value></data>
  <data name="Err_20002" xml:space="preserve"><value>Chuyến này không còn mở bán.</value></data>
  <data name="Err_20003" xml:space="preserve"><value>Ghế {0} đã có người đặt.</value></data>
  <data name="Err_20010" xml:space="preserve"><value>Chuyến đã khởi hành, không thể đặt vé.</value></data>
  <data name="Err_20011" xml:space="preserve"><value>Không tìm thấy chuyến xe.</value></data>
  <data name="Err_20020" xml:space="preserve"><value>Chỉ thanh toán được vé đang chờ thanh toán.</value></data>
  <data name="Err_20021" xml:space="preserve"><value>Không tìm thấy vé.</value></data>
  <data name="Err_20030" xml:space="preserve"><value>Vé đã bị hủy trước đó.</value></data>
  <data name="Err_20031" xml:space="preserve"><value>Chuyến đã khởi hành, không thể hủy vé.</value></data>
  <data name="Err_20032" xml:space="preserve"><value>Không tìm thấy vé.</value></data>
  <data name="Err_20040" xml:space="preserve"><value>Tên đăng nhập không hợp lệ (3–30 ký tự chữ, số hoặc _, bắt đầu bằng chữ).</value></data>
  <data name="Err_20041" xml:space="preserve"><value>Vai trò không hợp lệ.</value></data>
  <data name="Err_20042" xml:space="preserve"><value>Không tìm thấy nhân viên.</value></data>
  <data name="Err_20043" xml:space="preserve"><value>Bạn không thể khóa hoặc đổi quyền tài khoản của chính mình.</value></data>
  <data name="Err_20044" xml:space="preserve"><value>Mật khẩu tối thiểu 8 ký tự và không chứa dấu nháy kép.</value></data>
  <data name="Err_20050" xml:space="preserve"><value>Không tìm thấy chuyến xe.</value></data>
  <data name="Err_20051" xml:space="preserve"><value>Chỉ hủy được chuyến chưa chạy.</value></data>
  <data name="Err_20052" xml:space="preserve"><value>Chuyến còn vé hiệu lực, hãy dùng chức năng "Hủy chuyến".</value></data>
  <data name="Err_1" xml:space="preserve"><value>Dữ liệu bị trùng (biển số, SĐT, CCCD, số bằng lái…).</value></data>
  <data name="Err_1017" xml:space="preserve"><value>Sai tên đăng nhập hoặc mật khẩu.</value></data>
  <data name="Err_1031" xml:space="preserve"><value>Bạn không có quyền thực hiện thao tác này.</value></data>
  <data name="Err_1920" xml:space="preserve"><value>Tên đăng nhập đã tồn tại.</value></data>
  <data name="Err_2292" xml:space="preserve"><value>Không xóa được vì đang có dữ liệu liên quan.</value></data>
  <data name="Err_2290" xml:space="preserve"><value>Dữ liệu không hợp lệ (ví dụ điểm đi trùng điểm đến, số ghế ngoài khoảng 1–60).</value></data>
  <data name="Err_28000" xml:space="preserve"><value>Tài khoản đã bị khóa.</value></data>
  <data name="Err_12541" xml:space="preserve"><value>Không kết nối được Oracle, hãy kiểm tra dịch vụ CSDL.</value></data>
  <data name="Err_90001" xml:space="preserve"><value>Vui lòng nhập đầy đủ thông tin bắt buộc.</value></data>
  <data name="Err_90002" xml:space="preserve"><value>Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0.</value></data>
  <data name="Err_90003" xml:space="preserve"><value>Vui lòng chọn ghế hợp lệ.</value></data>
  <data name="Err_90004" xml:space="preserve"><value>Giờ đến phải sau giờ khởi hành.</value></data>
  <data name="Err_90005" xml:space="preserve"><value>Giá phải lớn hơn 0.</value></data>
  <data name="Err_90006" xml:space="preserve"><value>Chỉ điều hành mới được thực hiện thao tác này.</value></data>
</root>
```

(The file starts with `<?xml version="1.0" encoding="utf-8"?>`, `<root>` and the four `resheader` lines exactly as in Step 3.)

- [ ] **Step 5: Write the localization classes**

`ILocalizationService.cs`:
```csharp
using System.ComponentModel;
using Transport.Domain.Errors;

namespace Transport.Presentation.Wpf.Localization;

public interface ILocalizationService : INotifyPropertyChanged
{
    // Chuỗi đã dịch; thiếu khóa thì trả về chính khóa
    string this[string key] { get; }

    // "vi" hoặc "en"
    string CurrentLanguage { get; }

    void SetLanguage(string languageCode);

    string Format(string key, params object[] args);

    // Dịch lỗi theo khóa Err_<mã>; không có bản dịch thì dùng message tiếng Anh
    string Translate(BusinessRuleException exception, params object[] args);
}
```

`LocalizationService.cs`:
```csharp
using System.Globalization;
using System.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using Transport.Domain.Errors;

namespace Transport.Presentation.Wpf.Localization;

public sealed class LocalizationService : ObservableObject, ILocalizationService
{
    private static readonly ResourceManager Strings =
        new("Transport.Presentation.Wpf.Resources.Strings", typeof(LocalizationService).Assembly);

    private CultureInfo _culture = CultureInfo.GetCultureInfo("vi");

    // Dùng chung cho XAML ({l:Loc ...}) và DI
    public static LocalizationService Instance { get; } = new();

    public string CurrentLanguage => _culture.TwoLetterISOLanguageName;

    public string this[string key] => Strings.GetString(key, _culture) ?? key;

    public void SetLanguage(string languageCode)
    {
        _culture = CultureInfo.GetCultureInfo(languageCode);
        // "Item[]" báo cho mọi binding dạng [Key] cập nhật lại
        OnPropertyChanged("Item[]");
        OnPropertyChanged(nameof(CurrentLanguage));
    }

    public string Format(string key, params object[] args) => SafeFormat(this[key], args);

    public string Translate(BusinessRuleException exception, params object[] args)
    {
        var template = Strings.GetString($"Err_{(int)exception.Code}", _culture);
        return template is null ? exception.Message : SafeFormat(template, args);
    }

    private string SafeFormat(string template, object[] args)
    {
        try
        {
            return string.Format(_culture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }
}
```

`LocExtension.cs`:
```csharp
using System.Windows.Data;
using System.Windows.Markup;

namespace Transport.Presentation.Wpf.Localization;

// {l:Loc Key}: chuỗi đã dịch, tự cập nhật khi đổi ngôn ngữ
[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key) => Key = key;

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]") { Source = LocalizationService.Instance, Mode = BindingMode.OneWay }
            .ProvideValue(serviceProvider);
}
```

`LocalizeConverter.cs`:
```csharp
using System.Globalization;
using System.Windows.Data;

namespace Transport.Presentation.Wpf.Localization;

// Dịch giá trị động: khóa = ConverterParameter (tiền tố) + giá trị, VD "TicketStatus_" + Paid
public sealed class LocalizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? string.Empty : LocalizationService.Instance[$"{parameter}{value}"];

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`. If `EveryBusinessErrorCodeHasATranslation` fails, the message lists the missing `Err_` keys – add them to both files.

- [ ] **Step 7: Commit**

```bash
git add src/Transport.Presentation.Wpf tests/Transport.UnitTests
git commit -m "feat(ui): add resx localization with runtime language switch"
```

---

### Task 9: UI shell services, theme, dialogs and page base class

**Files:**
- Create: `src/Transport.Presentation.Wpf/Themes/Colors.xaml`, `Themes/Controls.xaml`
- Modify: `src/Transport.Presentation.Wpf/App.xaml` (merge Fluent + theme dictionaries)
- Create: `src/Transport.Presentation.Wpf/Converters/NullToVisibilityConverter.cs`, `InverseBooleanToVisibilityConverter.cs`
- Create: `src/Transport.Presentation.Wpf/Controls/PasswordBoxAssistant.cs`
- Create: `src/Transport.Presentation.Wpf/Services/NotificationSeverity.cs`, `INotificationService.cs`, `NotificationService.cs`, `IDialogService.cs`, `DialogService.cs`, `INavigationService.cs`, `NavigationService.cs`, `PageDescriptor.cs`
- Create: `src/Transport.Presentation.Wpf/ViewModels/PageServices.cs`, `PageViewModel.cs`
- Create: `src/Transport.Presentation.Wpf/ViewModels/Dialogs/IDialogViewModel.cs`, `TextPromptViewModel.cs`, `PasswordPromptViewModel.cs`, `PaymentDialogViewModel.cs`
- Create: `src/Transport.Presentation.Wpf/Views/Dialogs/TextPromptDialog.xaml(.cs)`, `PasswordPromptDialog.xaml(.cs)`, `PaymentDialog.xaml(.cs)`
- Test: `tests/Transport.UnitTests/Fakes/FakeLocalizationService.cs`, `FakeNotificationService.cs`, `FakeDialogService.cs`, `FakeExcelExporter.cs`, `PageTestContext.cs`
- Test: `tests/Transport.UnitTests/Presentation/NotificationServiceTests.cs`, `PageViewModelTests.cs`, `NavigationServiceTests.cs`, `DialogViewModelTests.cs`

**Interfaces:**
- Produces (namespace `Transport.Presentation.Wpf.Services`):
  - `enum NotificationSeverity { Success, Warning, Error }`
  - `INotificationService { void ShowSuccess(string); void ShowWarning(string); void ShowError(string); }`; `NotificationService` (+ `Message`, `Severity`, `IsOpen`, `CloseCommand`)
  - `IDialogService { bool Confirm(string message); string? ShowSaveFile(string suggestedName); string? PromptText(string title, string label); string? PromptPassword(string title); PaymentMethod? ChoosePaymentMethod(decimal amount); }`
  - `INavigationService : INotifyPropertyChanged { PageViewModel? CurrentPage { get; } Task NavigateToAsync(Type viewModelType); }`
  - `record PageDescriptor(string TitleKey, string Glyph, Type ViewModelType, bool OperatorOnly, int Order)`
- Produces (namespace `Transport.Presentation.Wpf.ViewModels`):
  - `record PageServices(ILocalizationService Localization, INotificationService Notifications, IDialogService Dialogs, IUserSession Session, IExcelExporter Exporter)`
  - `abstract class PageViewModel : ObservableValidator` with `IsBusy`, `IsOperator`, `virtual Task LoadAsync()`, protected `Loc`, `Notify`, `Dialogs`, `Session`, `RunBusyAsync(Func<Task>, params object[] errorArgs)`, `ExportAsync<T>(IEnumerable<T>, IReadOnlyList<ExportColumn<T>>, string titleKey)`, `FirstValidationError()`
- XAML resource keys (Controls.xaml): `SymbolFont`, `BoolToVisibility`, `InverseBoolToVisibility`, `NullToCollapsed`, `Localize`, `PageTitleStyle`, `SectionTitleStyle`, `FieldLabelStyle`, `CardStyle`, `PrimaryButtonStyle`, `SidebarListBoxStyle`, `SidebarItemStyle`, `SidebarButtonStyle`, `InfoBarStyle`; Colors.xaml: `HaUiNavyBrush`, `HaUiNavyDarkBrush`, `HaUiGoldBrush`, `SeatFreeBrush`, `SeatBookedBrush`, `SeatSelectedBrush`, `SuccessBrush`, `WarningBrush`, `ErrorBrush`, `SuccessBackgroundBrush`, `WarningBackgroundBrush`, `ErrorBackgroundBrush`.
- Test helpers: `PageTestContext` (`Localization`, `Notifications`, `Dialogs`, `Session`, `Exporter`, `Services`, `static ForSeller()`).

- [ ] **Step 1: Write the test fakes**

`tests/Transport.UnitTests/Fakes/FakeLocalizationService.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Localization;

namespace Transport.UnitTests.Fakes;

// Trả về chính khóa để test kiểm tra "đã dùng đúng khóa"
internal sealed class FakeLocalizationService : ObservableObject, ILocalizationService
{
    public string CurrentLanguage { get; private set; } = "vi";

    public string this[string key] => key;

    public void SetLanguage(string languageCode)
    {
        CurrentLanguage = languageCode;
        OnPropertyChanged(nameof(CurrentLanguage));
    }

    public string Format(string key, params object[] args) =>
        args.Length == 0 ? key : $"{key}|{string.Join(",", args)}";

    public string Translate(BusinessRuleException exception, params object[] args) =>
        Format($"Err_{(int)exception.Code}", args);
}
```

`tests/Transport.UnitTests/Fakes/FakeNotificationService.cs`:
```csharp
using Transport.Presentation.Wpf.Services;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeNotificationService : INotificationService
{
    public List<string> Successes { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> Errors { get; } = [];

    public void ShowSuccess(string message) => Successes.Add(message);
    public void ShowWarning(string message) => Warnings.Add(message);
    public void ShowError(string message) => Errors.Add(message);
}
```

`tests/Transport.UnitTests/Fakes/FakeDialogService.cs`:
```csharp
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.Services;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeDialogService : IDialogService
{
    public bool ConfirmResult { get; set; } = true;
    public List<string> ConfirmMessages { get; } = [];
    public string? SaveFilePath { get; set; } = @"C:\temp\export.xlsx";
    public string? PromptTextResult { get; set; }
    public string? PromptPasswordResult { get; set; }
    public PaymentMethod? PaymentResult { get; set; }
    public List<string> Prompts { get; } = [];

    public bool Confirm(string message)
    {
        ConfirmMessages.Add(message);
        return ConfirmResult;
    }

    public string? ShowSaveFile(string suggestedName) => SaveFilePath;

    public string? PromptText(string title, string label)
    {
        Prompts.Add(title);
        return PromptTextResult;
    }

    public string? PromptPassword(string title)
    {
        Prompts.Add(title);
        return PromptPasswordResult;
    }

    public PaymentMethod? ChoosePaymentMethod(decimal amount) => PaymentResult;
}
```

`tests/Transport.UnitTests/Fakes/FakeExcelExporter.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Application.Models;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeExcelExporter : IExcelExporter
{
    public string? Title { get; private set; }
    public string? FilePath { get; private set; }
    public List<string> Headers { get; } = [];
    public int RowCount { get; private set; }

    public Task ExportAsync<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string title, string filePath,
        CancellationToken ct = default)
    {
        Title = title;
        FilePath = filePath;
        Headers.AddRange(columns.Select(c => c.Header));
        RowCount = rows.Count();
        return Task.CompletedTask;
    }
}
```

`tests/Transport.UnitTests/Fakes/PageTestContext.cs`:
```csharp
using Transport.Presentation.Wpf.ViewModels;

namespace Transport.UnitTests.Fakes;

// Gom các dịch vụ giả mà mọi ViewModel trang cần
internal sealed class PageTestContext
{
    public FakeLocalizationService Localization { get; } = new();
    public FakeNotificationService Notifications { get; } = new();
    public FakeDialogService Dialogs { get; } = new();
    public FakeUserSession Session { get; init; } = new();
    public FakeExcelExporter Exporter { get; } = new();

    public PageServices Services => new(Localization, Notifications, Dialogs, Session, Exporter);

    public static PageTestContext ForSeller() => new() { Session = FakeUserSession.Seller() };
}
```

- [ ] **Step 2: Write the failing tests**

`tests/Transport.UnitTests/Presentation/NotificationServiceTests.cs`:
```csharp
using Transport.Presentation.Wpf.Services;

namespace Transport.UnitTests.Presentation;

public class NotificationServiceTests
{
    [Fact]
    public void ShowError_OpensBarWithMessageAndSeverity()
    {
        var notifications = new NotificationService();

        notifications.ShowError("Ghế 5 đã có người đặt.");

        Assert.True(notifications.IsOpen);
        Assert.Equal("Ghế 5 đã có người đặt.", notifications.Message);
        Assert.Equal(NotificationSeverity.Error, notifications.Severity);
    }

    [Fact]
    public void CloseCommand_HidesBar_AndNewMessageReplacesOld()
    {
        var notifications = new NotificationService();
        notifications.ShowSuccess("A");

        notifications.CloseCommand.Execute(null);
        Assert.False(notifications.IsOpen);

        notifications.ShowWarning("B");
        Assert.True(notifications.IsOpen);
        Assert.Equal("B", notifications.Message);
        Assert.Equal(NotificationSeverity.Warning, notifications.Severity);
    }
}
```

`tests/Transport.UnitTests/Presentation/PageViewModelTests.cs`:
```csharp
using Transport.Application.Models;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.ViewModels;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class PageViewModelTests
{
    private sealed class TestPage(PageServices services) : PageViewModel(services)
    {
        public Task Run(Func<Task> action, params object[] args) => RunBusyAsync(action, args);

        public Task Export(IEnumerable<int> rows) =>
            ExportAsync(rows, new[] { new ExportColumn<int>("Col_Number", x => x) }, "Title_Numbers");
    }

    private readonly PageTestContext _context = new();

    [Fact]
    public async Task RunBusyAsync_ShowsTranslatedBusinessErrorAndResetsBusy()
    {
        var page = new TestPage(_context.Services);
        var busyDuringAction = false;

        await page.Run(() =>
        {
            busyDuringAction = page.IsBusy;
            throw new BusinessRuleException(BusinessErrorCode.SeatAlreadyBooked, "Seat 5 is already booked.");
        }, 5);

        Assert.True(busyDuringAction);
        Assert.False(page.IsBusy);
        Assert.Equal(new[] { "Err_20003|5" }, _context.Notifications.Errors);
    }

    [Fact]
    public async Task ExportAsync_TranslatesTitleAndHeaders()
    {
        await new TestPage(_context.Services).Export([1, 2, 3]);

        Assert.Equal("Title_Numbers", _context.Exporter.Title);
        Assert.Equal(new[] { "Col_Number" }, _context.Exporter.Headers);
        Assert.Equal(3, _context.Exporter.RowCount);
        Assert.Equal(new[] { @"Msg_Exported|C:\temp\export.xlsx" }, _context.Notifications.Successes);
    }

    [Fact]
    public async Task ExportAsync_DoesNothingWhenSaveDialogIsCancelled()
    {
        _context.Dialogs.SaveFilePath = null;

        await new TestPage(_context.Services).Export([1]);

        Assert.Null(_context.Exporter.Title);
    }
}
```

`tests/Transport.UnitTests/Presentation/NavigationServiceTests.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Transport.Presentation.Wpf.Services;
using Transport.Presentation.Wpf.ViewModels;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class NavigationServiceTests
{
    private sealed class LoadingPage(PageServices services) : PageViewModel(services)
    {
        public bool Loaded { get; private set; }

        public override Task LoadAsync()
        {
            Loaded = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task NavigateToAsync_CreatesPageFromContainerAndLoadsIt()
    {
        var provider = new ServiceCollection()
            .AddSingleton(new PageTestContext().Services)
            .AddTransient<LoadingPage>()
            .BuildServiceProvider();
        var navigation = new NavigationService(provider);

        await navigation.NavigateToAsync(typeof(LoadingPage));

        var page = Assert.IsType<LoadingPage>(navigation.CurrentPage);
        Assert.True(page.Loaded);
    }
}
```

`tests/Transport.UnitTests/Presentation/DialogViewModelTests.cs`:
```csharp
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.ViewModels.Dialogs;

namespace Transport.UnitTests.Presentation;

public class DialogViewModelTests
{
    [Fact]
    public void TextPrompt_OkNeedsTextAndClosesWithTrue()
    {
        var dialog = new TextPromptViewModel("Hủy vé", "Lý do");
        bool? result = null;
        dialog.CloseRequested += r => result = r;

        Assert.False(dialog.OkCommand.CanExecute(null));
        dialog.Text = "Khách đổi ý";
        Assert.True(dialog.OkCommand.CanExecute(null));
        dialog.OkCommand.Execute(null);

        Assert.True(result);
    }

    [Fact]
    public void Payment_DefaultsToCashAndListsAllMethods()
    {
        var dialog = new PaymentDialogViewModel(280000);

        Assert.Equal(PaymentMethod.Cash, dialog.SelectedMethod);
        Assert.Equal(Enum.GetValues<PaymentMethod>(), dialog.Methods);
        Assert.Equal(280000, dialog.Amount);
    }

    [Fact]
    public void PasswordPrompt_OkNeedsPassword()
    {
        var dialog = new PasswordPromptViewModel("Đặt lại mật khẩu", "Mật khẩu mới");

        Assert.False(dialog.OkCommand.CanExecute(null));
        dialog.Password = "NewPass#01";
        Assert.True(dialog.OkCommand.CanExecute(null));
    }
}
```

- [ ] **Step 3: Run to verify they fail**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build errors for `Transport.Presentation.Wpf.Services` / `ViewModels`.

- [ ] **Step 4: Write services, page base and dialog view models**

`Services/NotificationSeverity.cs`:
```csharp
namespace Transport.Presentation.Wpf.Services;

public enum NotificationSeverity
{
    Success,
    Warning,
    Error,
}
```

`Services/INotificationService.cs`:
```csharp
namespace Transport.Presentation.Wpf.Services;

// Thanh thông báo (InfoBar) dưới thanh tiêu đề của cửa sổ chính
public interface INotificationService
{
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);
}
```

`Services/NotificationService.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Transport.Presentation.Wpf.Services;

public sealed partial class NotificationService : ObservableObject, INotificationService
{
    private CancellationTokenSource? _autoHide;

    public NotificationService() => Message = string.Empty;

    [ObservableProperty]
    public partial string Message { get; set; }

    [ObservableProperty]
    public partial NotificationSeverity Severity { get; set; }

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    public void ShowSuccess(string message) => Show(message, NotificationSeverity.Success, TimeSpan.FromSeconds(4));

    public void ShowWarning(string message) => Show(message, NotificationSeverity.Warning, TimeSpan.FromSeconds(6));

    public void ShowError(string message) => Show(message, NotificationSeverity.Error, TimeSpan.FromSeconds(8));

    [RelayCommand]
    private void Close()
    {
        _autoHide?.Cancel();
        IsOpen = false;
    }

    private void Show(string message, NotificationSeverity severity, TimeSpan duration)
    {
        _autoHide?.Cancel();
        _autoHide = new CancellationTokenSource();
        Message = message;
        Severity = severity;
        IsOpen = true;
        _ = HideLaterAsync(duration, _autoHide.Token);
    }

    // Tự ẩn sau một lúc; thông báo mới sẽ hủy lần ẩn của thông báo cũ
    private async Task HideLaterAsync(TimeSpan delay, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, token);
            IsOpen = false;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
```

`Services/IDialogService.cs`:
```csharp
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.Services;

public interface IDialogService
{
    // Hộp thoại Có/Không
    bool Confirm(string message);

    // Chọn nơi lưu file .xlsx; null nếu người dùng hủy
    string? ShowSaveFile(string suggestedName);

    // Nhập một dòng chữ (VD lý do hủy); null nếu hủy
    string? PromptText(string title, string label);

    string? PromptPassword(string title);

    PaymentMethod? ChoosePaymentMethod(decimal amount);
}
```

`Services/INavigationService.cs`:
```csharp
using System.ComponentModel;
using Transport.Presentation.Wpf.ViewModels;

namespace Transport.Presentation.Wpf.Services;

public interface INavigationService : INotifyPropertyChanged
{
    PageViewModel? CurrentPage { get; }

    // Tạo ViewModel trang mới từ DI rồi gọi LoadAsync
    Task NavigateToAsync(Type viewModelType);
}
```

`Services/NavigationService.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Transport.Presentation.Wpf.ViewModels;

namespace Transport.Presentation.Wpf.Services;

public sealed partial class NavigationService(IServiceProvider services) : ObservableObject, INavigationService
{
    [ObservableProperty]
    public partial PageViewModel? CurrentPage { get; set; }

    public async Task NavigateToAsync(Type viewModelType)
    {
        var page = (PageViewModel)services.GetRequiredService(viewModelType);
        CurrentPage = page;
        await page.LoadAsync();
    }
}
```

`Services/PageDescriptor.cs`:
```csharp
namespace Transport.Presentation.Wpf.Services;

// Một mục menu: khóa tiêu đề, ký tự icon (Segoe Fluent Icons), ViewModel của trang
public sealed record PageDescriptor(string TitleKey, string Glyph, Type ViewModelType, bool OperatorOnly, int Order);
```

`ViewModels/PageServices.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf.ViewModels;

// Các dịch vụ giao diện dùng chung, gom lại để constructor của trang gọn
public sealed record PageServices(
    ILocalizationService Localization,
    INotificationService Notifications,
    IDialogService Dialogs,
    IUserSession Session,
    IExcelExporter Exporter);
```

`ViewModels/PageViewModel.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf.ViewModels;

// Lớp nền của mọi trang trong cửa sổ chính
public abstract partial class PageViewModel : ObservableValidator
{
    protected PageViewModel(PageServices services) => Services = services;

    public bool IsOperator => Session.IsOperator;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    protected PageServices Services { get; }

    protected ILocalizationService Loc => Services.Localization;

    protected INotificationService Notify => Services.Notifications;

    protected IDialogService Dialogs => Services.Dialogs;

    protected IUserSession Session => Services.Session;

    // Gọi mỗi lần điều hướng tới trang
    public virtual Task LoadAsync() => Task.CompletedTask;

    // Chạy thao tác với trạng thái "đang xử lý"; lỗi nghiệp vụ được dịch và hiện ở thanh thông báo
    protected async Task RunBusyAsync(Func<Task> action, params object[] errorArgs)
    {
        IsBusy = true;
        try
        {
            await action();
        }
        catch (BusinessRuleException ex)
        {
            Notify.ShowError(Loc.Translate(ex, errorArgs));
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Xuất danh sách ra Excel; tiêu đề và tên cột là khóa resource
    protected Task ExportAsync<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string titleKey) =>
        RunBusyAsync(async () =>
        {
            var title = Loc[titleKey];
            var path = Dialogs.ShowSaveFile(title);
            if (path is null)
            {
                return;
            }

            var localized = columns.Select(c => c with { Header = Loc[c.Header] }).ToList();
            await Services.Exporter.ExportAsync(rows, localized, title, path);
            Notify.ShowSuccess(Loc.Format("Msg_Exported", path));
        });

    // Lỗi kiểm tra dữ liệu đầu tiên (đã dịch), dùng sau ValidateAllProperties()
    protected string FirstValidationError() =>
        GetErrors().Select(e => e.ErrorMessage).FirstOrDefault(m => m is not null) is { } key
            ? Loc[key]
            : Loc["Msg_FixErrors"];
}
```

`ViewModels/Dialogs/IDialogViewModel.cs`:
```csharp
namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

// ViewModel của hộp thoại báo đóng (true = đồng ý)
public interface IDialogViewModel
{
    event Action<bool>? CloseRequested;
}
```

`ViewModels/Dialogs/TextPromptViewModel.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

public sealed partial class TextPromptViewModel : ObservableObject, IDialogViewModel
{
    public TextPromptViewModel(string title, string label)
    {
        Title = title;
        Label = label;
        Text = string.Empty;
    }

    public event Action<bool>? CloseRequested;

    public string Title { get; }

    public string Label { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    public partial string Text { get; set; }

    [RelayCommand(CanExecute = nameof(CanOk))]
    private void Ok() => CloseRequested?.Invoke(true);

    private bool CanOk() => !string.IsNullOrWhiteSpace(Text);
}
```

`ViewModels/Dialogs/PasswordPromptViewModel.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

public sealed partial class PasswordPromptViewModel : ObservableObject, IDialogViewModel
{
    public PasswordPromptViewModel(string title, string label)
    {
        Title = title;
        Label = label;
        Password = string.Empty;
    }

    public event Action<bool>? CloseRequested;

    public string Title { get; }

    public string Label { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    public partial string Password { get; set; }

    [RelayCommand(CanExecute = nameof(CanOk))]
    private void Ok() => CloseRequested?.Invoke(true);

    private bool CanOk() => Password.Length > 0;
}
```

`ViewModels/Dialogs/PaymentDialogViewModel.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Domain.Enums;

namespace Transport.Presentation.Wpf.ViewModels.Dialogs;

public sealed partial class PaymentDialogViewModel : ObservableObject, IDialogViewModel
{
    public PaymentDialogViewModel(decimal amount)
    {
        Amount = amount;
        SelectedMethod = PaymentMethod.Cash;
    }

    public event Action<bool>? CloseRequested;

    public decimal Amount { get; }

    public IReadOnlyList<PaymentMethod> Methods { get; } = Enum.GetValues<PaymentMethod>();

    [ObservableProperty]
    public partial PaymentMethod SelectedMethod { get; set; }

    [RelayCommand]
    private void Ok() => CloseRequested?.Invoke(true);
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 6: Write the theme dictionaries**

`Themes/Colors.xaml`:
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- Màu nhận diện HaUI -->
    <Color x:Key="HaUiNavyColor">#0B3D91</Color>
    <Color x:Key="HaUiNavyDarkColor">#082C69</Color>
    <Color x:Key="HaUiGoldColor">#F5B700</Color>

    <SolidColorBrush x:Key="HaUiNavyBrush" Color="{StaticResource HaUiNavyColor}" />
    <SolidColorBrush x:Key="HaUiNavyDarkBrush" Color="{StaticResource HaUiNavyDarkColor}" />
    <SolidColorBrush x:Key="HaUiGoldBrush" Color="{StaticResource HaUiGoldColor}" />

    <!-- Sơ đồ ghế -->
    <SolidColorBrush x:Key="SeatFreeBrush" Color="#2E7D32" />
    <SolidColorBrush x:Key="SeatBookedBrush" Color="#C62828" />
    <SolidColorBrush x:Key="SeatSelectedBrush" Color="#1565C0" />

    <!-- Thanh thông báo -->
    <SolidColorBrush x:Key="SuccessBrush" Color="#2E7D32" />
    <SolidColorBrush x:Key="WarningBrush" Color="#B26A00" />
    <SolidColorBrush x:Key="ErrorBrush" Color="#C62828" />
    <SolidColorBrush x:Key="SuccessBackgroundBrush" Color="#E8F5E9" />
    <SolidColorBrush x:Key="WarningBackgroundBrush" Color="#FFF4E5" />
    <SolidColorBrush x:Key="ErrorBackgroundBrush" Color="#FDECEA" />
</ResourceDictionary>
```

`Themes/Controls.xaml`:
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
                    xmlns:conv="clr-namespace:Transport.Presentation.Wpf.Converters"
                    xmlns:svc="clr-namespace:Transport.Presentation.Wpf.Services">

    <FontFamily x:Key="SymbolFont">Segoe Fluent Icons, Segoe MDL2 Assets</FontFamily>

    <BooleanToVisibilityConverter x:Key="BoolToVisibility" />
    <conv:InverseBooleanToVisibilityConverter x:Key="InverseBoolToVisibility" />
    <conv:NullToVisibilityConverter x:Key="NullToCollapsed" />
    <l:LocalizeConverter x:Key="Localize" />

    <!-- Chữ -->
    <Style x:Key="PageTitleStyle" TargetType="TextBlock">
        <Setter Property="FontSize" Value="26" />
        <Setter Property="FontWeight" Value="SemiBold" />
        <Setter Property="Foreground" Value="{DynamicResource TextFillColorPrimaryBrush}" />
    </Style>
    <Style x:Key="SectionTitleStyle" TargetType="TextBlock">
        <Setter Property="FontSize" Value="16" />
        <Setter Property="FontWeight" Value="SemiBold" />
        <Setter Property="Margin" Value="0,0,0,8" />
        <Setter Property="Foreground" Value="{DynamicResource TextFillColorPrimaryBrush}" />
    </Style>
    <Style x:Key="FieldLabelStyle" TargetType="TextBlock">
        <Setter Property="Margin" Value="0,12,0,4" />
        <Setter Property="Foreground" Value="{DynamicResource TextFillColorSecondaryBrush}" />
    </Style>

    <!-- Thẻ nội dung (nền/viền lấy từ Fluent theme nên đúng cả sáng lẫn tối) -->
    <Style x:Key="CardStyle" TargetType="Border">
        <Setter Property="Background" Value="{DynamicResource CardBackgroundFillColorDefaultBrush}" />
        <Setter Property="BorderBrush" Value="{DynamicResource CardStrokeColorDefaultBrush}" />
        <Setter Property="BorderThickness" Value="1" />
        <Setter Property="CornerRadius" Value="8" />
        <Setter Property="Padding" Value="16" />
    </Style>

    <!-- Nút chính màu navy HaUI -->
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="Foreground" Value="White" />
        <Setter Property="Background" Value="{StaticResource HaUiNavyBrush}" />
        <Setter Property="Padding" Value="16,8" />
        <Setter Property="MinHeight" Value="34" />
        <Setter Property="FontWeight" Value="SemiBold" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="Root" Background="{TemplateBinding Background}" CornerRadius="4"
                            BorderThickness="2" BorderBrush="Transparent" Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" RecognizesAccessKey="True" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Root" Property="Background" Value="{StaticResource HaUiNavyDarkBrush}" />
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="Root" Property="Opacity" Value="0.85" />
                        </Trigger>
                        <Trigger Property="IsKeyboardFocused" Value="True">
                            <Setter TargetName="Root" Property="BorderBrush" Value="{StaticResource HaUiGoldBrush}" />
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter TargetName="Root" Property="Opacity" Value="0.45" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- Sidebar -->
    <Style x:Key="SidebarListBoxStyle" TargetType="ListBox">
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="BorderThickness" Value="0" />
        <Setter Property="ScrollViewer.HorizontalScrollBarVisibility" Value="Disabled" />
    </Style>

    <Style x:Key="SidebarItemStyle" TargetType="ListBoxItem">
        <Setter Property="Margin" Value="8,2" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ListBoxItem">
                    <Grid>
                        <Border x:Name="Bg" Background="Transparent" CornerRadius="6" BorderThickness="1" BorderBrush="Transparent" />
                        <Rectangle x:Name="Marker" Width="4" HorizontalAlignment="Left" Margin="0,8" RadiusX="2" RadiusY="2"
                                   Fill="{StaticResource HaUiGoldBrush}" Visibility="Collapsed" />
                        <ContentPresenter Margin="16,10,12,10" VerticalAlignment="Center" />
                    </Grid>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Bg" Property="Background" Value="#22FFFFFF" />
                        </Trigger>
                        <Trigger Property="IsSelected" Value="True">
                            <Setter TargetName="Bg" Property="Background" Value="#33FFFFFF" />
                            <Setter TargetName="Marker" Property="Visibility" Value="Visible" />
                        </Trigger>
                        <Trigger Property="IsKeyboardFocused" Value="True">
                            <Setter TargetName="Bg" Property="BorderBrush" Value="{StaticResource HaUiGoldBrush}" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style x:Key="SidebarButtonStyle" TargetType="Button">
        <Setter Property="Foreground" Value="White" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Margin" Value="8,4,8,16" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="Root" Background="Transparent" CornerRadius="6" Padding="16,10" BorderThickness="1" BorderBrush="Transparent">
                        <ContentPresenter VerticalAlignment="Center" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Root" Property="Background" Value="#22FFFFFF" />
                        </Trigger>
                        <Trigger Property="IsKeyboardFocused" Value="True">
                            <Setter TargetName="Root" Property="BorderBrush" Value="{StaticResource HaUiGoldBrush}" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- Thanh thông báo kiểu InfoBar; DataContext = NotificationService -->
    <Style x:Key="InfoBarStyle" TargetType="Border">
        <Setter Property="Margin" Value="24,0,24,8" />
        <Setter Property="Padding" Value="16,10" />
        <Setter Property="CornerRadius" Value="6" />
        <Setter Property="BorderThickness" Value="1" />
        <Setter Property="Background" Value="{StaticResource SuccessBackgroundBrush}" />
        <Setter Property="BorderBrush" Value="{StaticResource SuccessBrush}" />
        <Setter Property="TextElement.Foreground" Value="{StaticResource SuccessBrush}" />
        <Setter Property="Visibility" Value="{Binding IsOpen, Converter={StaticResource BoolToVisibility}}" />
        <Style.Triggers>
            <DataTrigger Binding="{Binding Severity}" Value="{x:Static svc:NotificationSeverity.Warning}">
                <Setter Property="Background" Value="{StaticResource WarningBackgroundBrush}" />
                <Setter Property="BorderBrush" Value="{StaticResource WarningBrush}" />
                <Setter Property="TextElement.Foreground" Value="{StaticResource WarningBrush}" />
            </DataTrigger>
            <DataTrigger Binding="{Binding Severity}" Value="{x:Static svc:NotificationSeverity.Error}">
                <Setter Property="Background" Value="{StaticResource ErrorBackgroundBrush}" />
                <Setter Property="BorderBrush" Value="{StaticResource ErrorBrush}" />
                <Setter Property="TextElement.Foreground" Value="{StaticResource ErrorBrush}" />
            </DataTrigger>
        </Style.Triggers>
    </Style>

    <!-- Ô nhập: tooltip hiện lỗi kiểm tra dữ liệu đã dịch (ErrorMessage là khóa resource) -->
    <Style TargetType="TextBox" BasedOn="{StaticResource {x:Type TextBox}}">
        <Style.Triggers>
            <Trigger Property="Validation.HasError" Value="True">
                <Setter Property="ToolTip"
                        Value="{Binding RelativeSource={RelativeSource Self}, Path=(Validation.Errors)[0].ErrorContent, Converter={StaticResource Localize}}" />
            </Trigger>
        </Style.Triggers>
    </Style>
</ResourceDictionary>
```

- [ ] **Step 7: Merge the dictionaries in `App.xaml`**

```xml
<Application x:Class="Transport.Presentation.Wpf.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- Fluent theme có sẵn của WPF (.NET 9+), theo chế độ sáng/tối của Windows -->
                <ResourceDictionary Source="pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.xaml" />
                <ResourceDictionary Source="Themes/Colors.xaml" />
                <ResourceDictionary Source="Themes/Controls.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

- [ ] **Step 8: Write converters and the PasswordBox helper**

`Converters/NullToVisibilityConverter.cs`:
```csharp
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Transport.Presentation.Wpf.Converters;

// null hoặc chuỗi rỗng -> Collapsed
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null or string { Length: 0 } ? Visibility.Collapsed : Visibility.Visible;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
```

`Converters/InverseBooleanToVisibilityConverter.cs`:
```csharp
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Transport.Presentation.Wpf.Converters;

// true -> Collapsed, false -> Visible
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
```

`Controls/PasswordBoxAssistant.cs`:
```csharp
using System.Windows;
using System.Windows.Controls;

namespace Transport.Presentation.Wpf.Controls;

// PasswordBox.Password không bind được; thuộc tính gắn kèm này cho phép bind hai chiều với ViewModel
public static class PasswordBoxAssistant
{
    public static readonly DependencyProperty BoundPasswordProperty = DependencyProperty.RegisterAttached(
        "BoundPassword", typeof(string), typeof(PasswordBoxAssistant),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

    private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached(
        "IsUpdating", typeof(bool), typeof(PasswordBoxAssistant));

    public static string? GetBoundPassword(DependencyObject element) => (string?)element.GetValue(BoundPasswordProperty);

    public static void SetBoundPassword(DependencyObject element, string? value) => element.SetValue(BoundPasswordProperty, value);

    // Giá trị mặc định là null nên lần bind đầu (chuỗi rỗng) cũng chạy vào đây và gắn sự kiện
    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box)
        {
            return;
        }

        box.PasswordChanged -= OnPasswordChanged;
        if (!(bool)box.GetValue(IsUpdatingProperty))
        {
            box.Password = (string?)e.NewValue ?? string.Empty;
        }

        box.PasswordChanged += OnPasswordChanged;
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        var box = (PasswordBox)sender;
        box.SetValue(IsUpdatingProperty, true);
        SetBoundPassword(box, box.Password);
        box.SetValue(IsUpdatingProperty, false);
    }
}
```

- [ ] **Step 9: Write the dialogs**

`Views/Dialogs/TextPromptDialog.xaml`:
```xml
<Window x:Class="Transport.Presentation.Wpf.Views.Dialogs.TextPromptDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
        xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Dialogs"
        mc:Ignorable="d"
        d:DataContext="{d:DesignInstance Type=vm:TextPromptViewModel}"
        Title="{Binding Title}" Width="440" SizeToContent="Height" ResizeMode="NoResize"
        WindowStartupLocation="CenterOwner" ShowInTaskbar="False"
        FocusManager.FocusedElement="{Binding ElementName=InputBox}">
    <StackPanel Margin="24">
        <TextBlock Text="{Binding Label}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
        <TextBox x:Name="InputBox" Text="{Binding Text, UpdateSourceTrigger=PropertyChanged}"
                 TextWrapping="Wrap" MinHeight="64" AutomationProperties.Name="{Binding Label}" />
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0">
            <Button Content="{l:Loc Btn_Cancel}" IsCancel="True" Margin="0,0,8,0" MinWidth="96" />
            <Button Content="{l:Loc Btn_OK}" Command="{Binding OkCommand}" IsDefault="True"
                    Style="{StaticResource PrimaryButtonStyle}" MinWidth="96" />
        </StackPanel>
    </StackPanel>
</Window>
```

`Views/Dialogs/PasswordPromptDialog.xaml`:
```xml
<Window x:Class="Transport.Presentation.Wpf.Views.Dialogs.PasswordPromptDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
        xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls"
        xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Dialogs"
        mc:Ignorable="d"
        d:DataContext="{d:DesignInstance Type=vm:PasswordPromptViewModel}"
        Title="{Binding Title}" Width="420" SizeToContent="Height" ResizeMode="NoResize"
        WindowStartupLocation="CenterOwner" ShowInTaskbar="False"
        FocusManager.FocusedElement="{Binding ElementName=PasswordInput}">
    <StackPanel Margin="24">
        <TextBlock Text="{Binding Label}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
        <PasswordBox x:Name="PasswordInput" c:PasswordBoxAssistant.BoundPassword="{Binding Password}"
                     AutomationProperties.Name="{Binding Label}" />
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0">
            <Button Content="{l:Loc Btn_Cancel}" IsCancel="True" Margin="0,0,8,0" MinWidth="96" />
            <Button Content="{l:Loc Btn_OK}" Command="{Binding OkCommand}" IsDefault="True"
                    Style="{StaticResource PrimaryButtonStyle}" MinWidth="96" />
        </StackPanel>
    </StackPanel>
</Window>
```

`Views/Dialogs/PaymentDialog.xaml`:
```xml
<Window x:Class="Transport.Presentation.Wpf.Views.Dialogs.PaymentDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
        xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels.Dialogs"
        mc:Ignorable="d"
        d:DataContext="{d:DesignInstance Type=vm:PaymentDialogViewModel}"
        Title="{l:Loc Dialog_Payment_Title}" Width="400" SizeToContent="Height" ResizeMode="NoResize"
        WindowStartupLocation="CenterOwner" ShowInTaskbar="False">
    <StackPanel Margin="24">
        <TextBlock Text="{l:Loc Dialog_Payment_Amount}" Style="{StaticResource FieldLabelStyle}" Margin="0,0,0,4" />
        <TextBlock Text="{Binding Amount, StringFormat={}{0:N0} ₫}" FontSize="28" FontWeight="SemiBold"
                   Foreground="{StaticResource HaUiNavyBrush}" />
        <TextBlock Text="{l:Loc Dialog_Payment_Method}" Style="{StaticResource FieldLabelStyle}" />
        <ComboBox ItemsSource="{Binding Methods}" SelectedItem="{Binding SelectedMethod}"
                  AutomationProperties.Name="{l:Loc Dialog_Payment_Method}">
            <ComboBox.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding Converter={StaticResource Localize}, ConverterParameter=PaymentMethod_}" />
                </DataTemplate>
            </ComboBox.ItemTemplate>
        </ComboBox>
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0">
            <Button Content="{l:Loc Btn_Cancel}" IsCancel="True" Margin="0,0,8,0" MinWidth="96" />
            <Button Content="{l:Loc Btn_OK}" Command="{Binding OkCommand}" IsDefault="True"
                    Style="{StaticResource PrimaryButtonStyle}" MinWidth="96" />
        </StackPanel>
    </StackPanel>
</Window>
```

Code-behind of each dialog (same pattern, change the class name):
```csharp
using System.Windows;

namespace Transport.Presentation.Wpf.Views.Dialogs;

public partial class TextPromptDialog : Window
{
    public TextPromptDialog() => InitializeComponent();
}
```

- [ ] **Step 10: Write `Services/DialogService.cs`**

```csharp
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Transport.Domain.Enums;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.ViewModels.Dialogs;
using Transport.Presentation.Wpf.Views.Dialogs;

namespace Transport.Presentation.Wpf.Services;

public sealed class DialogService(ILocalizationService localization) : IDialogService
{
    public bool Confirm(string message)
    {
        var owner = ActiveWindow();
        var title = localization["App_Title"];
        var result = owner is null
            ? MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
            : MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public string? ShowSaveFile(string suggestedName)
    {
        var dialog = new SaveFileDialog
        {
            FileName = $"{SafeFileName(suggestedName)}_{DateTime.Now:yyyyMMdd_HHmm}",
            DefaultExt = ".xlsx",
            AddExtension = true,
            Filter = "Excel (*.xlsx)|*.xlsx",
        };
        var owner = ActiveWindow();
        var accepted = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        return accepted == true ? dialog.FileName : null;
    }

    public string? PromptText(string title, string label)
    {
        var viewModel = new TextPromptViewModel(title, label);
        return ShowModal(new TextPromptDialog(), viewModel) ? viewModel.Text.Trim() : null;
    }

    public string? PromptPassword(string title)
    {
        var viewModel = new PasswordPromptViewModel(title, localization["Dialog_Password_Label"]);
        return ShowModal(new PasswordPromptDialog(), viewModel) ? viewModel.Password : null;
    }

    public PaymentMethod? ChoosePaymentMethod(decimal amount)
    {
        var viewModel = new PaymentDialogViewModel(amount);
        return ShowModal(new PaymentDialog(), viewModel) ? viewModel.SelectedMethod : null;
    }

    private static bool ShowModal(Window dialog, IDialogViewModel viewModel)
    {
        dialog.DataContext = viewModel;
        dialog.Owner = ActiveWindow();
        viewModel.CloseRequested += result => dialog.DialogResult = result;
        return dialog.ShowDialog() == true;
    }

    private static Window? ActiveWindow()
    {
        var app = System.Windows.Application.Current;
        return app?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? app?.MainWindow;
    }

    private static string SafeFileName(string name) =>
        string.Concat(name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
}
```

- [ ] **Step 11: Build and test**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests`
Expected: 0 warnings, `failed: 0`.

- [ ] **Step 12: Commit**

```bash
git add src/Transport.Presentation.Wpf tests/Transport.UnitTests
git commit -m "feat(ui): add Fluent theme, notifications, dialogs, navigation and page base"
```

---

### Task 10: Login window, main window shell and Generic Host startup

**Files:**
- Create: `src/Transport.Presentation.Wpf/appsettings.json`
- Create: `src/Transport.Presentation.Wpf/Services/IWindowService.cs`, `WindowService.cs`
- Create: `src/Transport.Presentation.Wpf/ViewModels/LoginViewModel.cs`, `MainViewModel.cs`
- Create: `src/Transport.Presentation.Wpf/Views/LoginWindow.xaml(.cs)`, `MainWindow.xaml(.cs)`, `PageTemplates.xaml`
- Create: `src/Transport.Presentation.Wpf/PresentationServices.cs`
- Modify: `src/Transport.Presentation.Wpf/App.xaml.cs`
- Test: `tests/Transport.UnitTests/Fakes/FakeAuthService.cs`, `FakeWindowService.cs`, `FakeNavigationService.cs`
- Test: `tests/Transport.UnitTests/Presentation/LoginViewModelTests.cs`, `MainViewModelTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 8–9, `IAuthService`, `IUserSession`.
- Produces:
  - `IWindowService { void ShowMain(); void ShowLogin(); }`
  - `MainViewModel(INavigationService, NotificationService, IUserSession, ILocalizationService, IAuthService, IDialogService, IWindowService, IEnumerable<PageDescriptor>)` with `Menu`, `SelectedMenu`, `PageTitle`, `UserFullName`, `RoleKey`, `Navigation`, `Notifications`, `ToggleLanguageCommand`, `LogoutCommand`
  - `PresentationServices.AddPresentation(this IServiceCollection)` and `PresentationServices.AddPage<TViewModel>(this IServiceCollection, string titleKey, string glyph, int order, bool operatorOnly = false)`; private `AddPages(this IServiceCollection)` (empty here – Plan 3 adds one `AddPage` line per screen)
  - `Views/PageTemplates.xaml` – resource dictionary where Plan 3 adds one `DataTemplate` per page

- [ ] **Step 1: Write the fakes**

`tests/Transport.UnitTests/Fakes/FakeAuthService.cs`:
```csharp
using Transport.Application.Abstractions;
using Transport.Domain.Errors;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeAuthService : IAuthService
{
    public BusinessRuleException? LoginError { get; set; }
    public (string Username, string Password)? LastLogin { get; private set; }
    public int LogoutCount { get; private set; }

    public Task LoginAsync(string username, string password, CancellationToken ct = default)
    {
        LastLogin = (username, password);
        return LoginError is null ? Task.CompletedTask : Task.FromException(LoginError);
    }

    public void Logout() => LogoutCount++;
}
```

`tests/Transport.UnitTests/Fakes/FakeWindowService.cs`:
```csharp
using Transport.Presentation.Wpf.Services;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeWindowService : IWindowService
{
    public List<string> Shown { get; } = [];

    public void ShowMain() => Shown.Add("main");

    public void ShowLogin() => Shown.Add("login");
}
```

`tests/Transport.UnitTests/Fakes/FakeNavigationService.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Transport.Presentation.Wpf.Services;
using Transport.Presentation.Wpf.ViewModels;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeNavigationService : ObservableObject, INavigationService
{
    public List<Type> Navigated { get; } = [];

    public PageViewModel? CurrentPage => null;

    public Task NavigateToAsync(Type viewModelType)
    {
        Navigated.Add(viewModelType);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Write the failing tests**

`tests/Transport.UnitTests/Presentation/LoginViewModelTests.cs`:
```csharp
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.ViewModels;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class LoginViewModelTests
{
    private readonly FakeAuthService _auth = new();
    private readonly FakeWindowService _windows = new();
    private readonly FakeLocalizationService _localization = new();

    private LoginViewModel CreateViewModel() => new(_auth, _localization, _windows);

    [Fact]
    public void Login_IsDisabledUntilUsernameAndPasswordAreEntered()
    {
        var login = CreateViewModel();
        Assert.False(login.LoginCommand.CanExecute(null));

        login.Username = "seller01";
        login.Password = "Seller#001";

        Assert.True(login.LoginCommand.CanExecute(null));
    }

    [Fact]
    public async Task Login_Success_OpensMainWindow()
    {
        var login = CreateViewModel();
        login.Username = "  seller01 ";
        login.Password = "Seller#001";

        await login.LoginCommand.ExecuteAsync(null);

        Assert.Equal(("seller01", "Seller#001"), _auth.LastLogin);
        Assert.Equal(new[] { "main" }, _windows.Shown);
        Assert.Null(login.ErrorMessage);
    }

    [Fact]
    public async Task Login_Failure_ShowsTranslatedError()
    {
        _auth.LoginError = new BusinessRuleException(BusinessErrorCode.InvalidCredentials, "invalid");
        var login = CreateViewModel();
        login.Username = "seller01";
        login.Password = "wrong";

        await login.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Err_1017", login.ErrorMessage);
        Assert.Empty(_windows.Shown);
    }

    [Fact]
    public void ChangingLanguage_UpdatesLocalization()
    {
        var login = CreateViewModel();

        login.Language = "en";

        Assert.Equal("en", _localization.CurrentLanguage);
    }
}
```

`tests/Transport.UnitTests/Presentation/MainViewModelTests.cs`:
```csharp
using Transport.Presentation.Wpf.Services;
using Transport.Presentation.Wpf.ViewModels;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class MainViewModelTests
{
    private sealed class PageA;
    private sealed class PageB;
    private sealed class OperatorPage;

    private static readonly PageDescriptor[] Pages =
    [
        new("Menu_B", "\uE80F", typeof(PageB), OperatorOnly: false, Order: 20),
        new("Menu_Operator", "\uE80F", typeof(OperatorPage), OperatorOnly: true, Order: 30),
        new("Menu_A", "\uE80F", typeof(PageA), OperatorOnly: false, Order: 10),
    ];

    private readonly FakeNavigationService _navigation = new();
    private readonly FakeLocalizationService _localization = new();
    private readonly FakeAuthService _auth = new();
    private readonly FakeDialogService _dialogs = new();
    private readonly FakeWindowService _windows = new();

    private MainViewModel Create(FakeUserSession session) =>
        new(_navigation, new NotificationService(), session, _localization, _auth, _dialogs, _windows, Pages);

    [Fact]
    public void Menu_IsOrderedAndHidesOperatorPagesFromSellers()
    {
        var seller = Create(FakeUserSession.Seller());
        var operatorVm = Create(new FakeUserSession());

        Assert.Equal(new[] { "Menu_A", "Menu_B" }, seller.Menu.Select(p => p.TitleKey));
        Assert.Equal(new[] { "Menu_A", "Menu_B", "Menu_Operator" }, operatorVm.Menu.Select(p => p.TitleKey));
        Assert.Equal("Role_TicketSeller", seller.RoleKey);
        Assert.Equal("Role_Operator", operatorVm.RoleKey);
    }

    [Fact]
    public void Constructor_OpensFirstPage()
    {
        var main = Create(new FakeUserSession());

        Assert.Equal(new[] { typeof(PageA) }, _navigation.Navigated);
        Assert.Equal("Menu_A", main.PageTitle);
    }

    [Fact]
    public void SelectingMenuItem_Navigates()
    {
        var main = Create(new FakeUserSession());

        main.SelectedMenu = main.Menu[1];

        Assert.Equal(typeof(PageB), _navigation.Navigated[^1]);
        Assert.Equal("Menu_B", main.PageTitle);
    }

    [Fact]
    public void ToggleLanguage_SwitchesLanguageAndReloadsCurrentPage()
    {
        var main = Create(new FakeUserSession());

        main.ToggleLanguageCommand.Execute(null);

        Assert.Equal("en", _localization.CurrentLanguage);
        Assert.Equal(new[] { typeof(PageA), typeof(PageA) }, _navigation.Navigated);
        Assert.Equal("Menu_A", main.SelectedMenu!.TitleKey);
    }

    [Fact]
    public void Logout_WhenConfirmed_SignsOutAndShowsLogin()
    {
        var main = Create(new FakeUserSession());

        main.LogoutCommand.Execute(null);

        Assert.Equal(1, _auth.LogoutCount);
        Assert.Equal(new[] { "login" }, _windows.Shown);
    }

    [Fact]
    public void Logout_WhenCancelled_StaysSignedIn()
    {
        _dialogs.ConfirmResult = false;
        var main = Create(new FakeUserSession());

        main.LogoutCommand.Execute(null);

        Assert.Equal(0, _auth.LogoutCount);
        Assert.Empty(_windows.Shown);
    }
}
```

- [ ] **Step 3: Run to verify they fail**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: build errors for `IWindowService`, `LoginViewModel`, `MainViewModel`.

- [ ] **Step 4: Write window service interface and the two view models**

`Services/IWindowService.cs`:
```csharp
namespace Transport.Presentation.Wpf.Services;

// Chuyển giữa cửa sổ đăng nhập và cửa sổ chính
public interface IWindowService
{
    void ShowMain();
    void ShowLogin();
}
```

`ViewModels/LoginViewModel.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Abstractions;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly ILocalizationService _localization;
    private readonly IWindowService _windows;

    public LoginViewModel(IAuthService auth, ILocalizationService localization, IWindowService windows)
    {
        _auth = auth;
        _localization = localization;
        _windows = windows;
        Username = string.Empty;
        Password = string.Empty;
        Language = localization.CurrentLanguage;
    }

    public IReadOnlyList<string> Languages { get; } = ["vi", "en"];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    public partial string Username { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    public partial string Password { get; set; }

    [ObservableProperty]
    public partial string Language { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    partial void OnLanguageChanged(string value) => _localization.SetLanguage(value);

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = null;
        try
        {
            await _auth.LoginAsync(Username.Trim(), Password);
            _windows.ShowMain();
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = _localization.Translate(ex);
        }
    }

    private bool CanLogin() => Username.Trim().Length > 0 && Password.Length > 0;
}
```

`ViewModels/MainViewModel.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transport.Application.Abstractions;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf.ViewModels;

// Cửa sổ chính: menu theo quyền, điều hướng trang, đổi ngôn ngữ, đăng xuất
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IUserSession _session;
    private readonly ILocalizationService _localization;
    private readonly IAuthService _auth;
    private readonly IDialogService _dialogs;
    private readonly IWindowService _windows;

    public MainViewModel(
        INavigationService navigation,
        NotificationService notifications,
        IUserSession session,
        ILocalizationService localization,
        IAuthService auth,
        IDialogService dialogs,
        IWindowService windows,
        IEnumerable<PageDescriptor> pages)
    {
        Navigation = navigation;
        Notifications = notifications;
        _session = session;
        _localization = localization;
        _auth = auth;
        _dialogs = dialogs;
        _windows = windows;
        Menu = [.. pages.Where(p => !p.OperatorOnly || session.IsOperator).OrderBy(p => p.Order)];
        SelectedMenu = Menu.FirstOrDefault();
    }

    public INavigationService Navigation { get; }

    public NotificationService Notifications { get; }

    public string UserFullName => _session.FullName;

    public string RoleKey => _session.IsOperator ? "Role_Operator" : "Role_TicketSeller";

    public string PageTitle => SelectedMenu is null ? string.Empty : _localization[SelectedMenu.TitleKey];

    [ObservableProperty]
    public partial IReadOnlyList<PageDescriptor> Menu { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    public partial PageDescriptor? SelectedMenu { get; set; }

    partial void OnSelectedMenuChanged(PageDescriptor? value)
    {
        if (value is not null)
        {
            _ = Navigation.NavigateToAsync(value.ViewModelType);
        }
    }

    // Đổi ngôn ngữ rồi dựng lại menu + trang hiện tại để mọi chữ được dịch lại
    [RelayCommand]
    private void ToggleLanguage()
    {
        _localization.SetLanguage(_localization.CurrentLanguage == "vi" ? "en" : "vi");
        var current = SelectedMenu;
        Menu = [.. Menu];
        SelectedMenu = null;
        SelectedMenu = current;
        OnPropertyChanged(nameof(RoleKey));
    }

    [RelayCommand]
    private void Logout()
    {
        if (!_dialogs.Confirm(_localization["Msg_ConfirmLogout"]))
        {
            return;
        }

        _auth.Logout();
        _windows.ShowLogin();
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test --project tests/Transport.UnitTests`
Expected: `failed: 0`.

- [ ] **Step 6: Write the windows**

`Views/LoginWindow.xaml`:
```xml
<Window x:Class="Transport.Presentation.Wpf.Views.LoginWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
        xmlns:c="clr-namespace:Transport.Presentation.Wpf.Controls"
        xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels"
        mc:Ignorable="d"
        d:DataContext="{d:DesignInstance Type=vm:LoginViewModel}"
        Title="{l:Loc App_Title}" Width="900" Height="540" ResizeMode="CanMinimize"
        WindowStartupLocation="CenterScreen"
        Background="{DynamicResource ApplicationBackgroundBrush}">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="380" />
            <ColumnDefinition />
        </Grid.ColumnDefinitions>

        <!-- Cột thương hiệu -->
        <Border Background="{StaticResource HaUiNavyBrush}">
            <StackPanel VerticalAlignment="Center" Margin="40">
                <TextBlock Text="HaUI" FontSize="44" FontWeight="Bold" Foreground="White" />
                <Rectangle Height="4" Width="64" Fill="{StaticResource HaUiGoldBrush}" HorizontalAlignment="Left" Margin="0,8,0,16" />
                <TextBlock Text="{l:Loc App_Title}" FontSize="22" Foreground="White" TextWrapping="Wrap" />
                <TextBlock Text="{l:Loc Login_Hint}" Foreground="#CCFFFFFF" Margin="0,12,0,0" TextWrapping="Wrap" />
            </StackPanel>
        </Border>

        <!-- Form đăng nhập -->
        <StackPanel Grid.Column="1" VerticalAlignment="Center" Margin="64,0"
                    FocusManager.FocusedElement="{Binding ElementName=UsernameBox}">
            <TextBlock Text="{l:Loc Login_Title}" Style="{StaticResource PageTitleStyle}" Margin="0,0,0,16" />

            <TextBlock Text="{l:Loc Login_Username}" Style="{StaticResource FieldLabelStyle}" />
            <TextBox x:Name="UsernameBox" Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}"
                     AutomationProperties.Name="{l:Loc Login_Username}" />

            <TextBlock Text="{l:Loc Login_Password}" Style="{StaticResource FieldLabelStyle}" />
            <PasswordBox c:PasswordBoxAssistant.BoundPassword="{Binding Password}"
                         AutomationProperties.Name="{l:Loc Login_Password}" />

            <TextBlock Text="{l:Loc Login_Language}" Style="{StaticResource FieldLabelStyle}" />
            <ComboBox ItemsSource="{Binding Languages}" SelectedItem="{Binding Language}"
                      AutomationProperties.Name="{l:Loc Login_Language}">
                <ComboBox.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Text="{Binding Converter={StaticResource Localize}, ConverterParameter=Lang_}" />
                    </DataTemplate>
                </ComboBox.ItemTemplate>
            </ComboBox>

            <TextBlock Text="{Binding ErrorMessage}" Foreground="{StaticResource ErrorBrush}" TextWrapping="Wrap"
                       Margin="0,12,0,0" Visibility="{Binding ErrorMessage, Converter={StaticResource NullToCollapsed}}" />

            <Button Content="{l:Loc Login_Button}" Command="{Binding LoginCommand}" IsDefault="True"
                    Style="{StaticResource PrimaryButtonStyle}" HorizontalAlignment="Stretch" Margin="0,24,0,0" />
            <ProgressBar IsIndeterminate="True" Height="3" Margin="0,8,0,0"
                         Visibility="{Binding LoginCommand.IsRunning, Converter={StaticResource BoolToVisibility}}" />
        </StackPanel>
    </Grid>
</Window>
```

`Views/LoginWindow.xaml.cs`:
```csharp
using System.Windows;
using Transport.Presentation.Wpf.ViewModels;

namespace Transport.Presentation.Wpf.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

`Views/PageTemplates.xaml`:
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- Mỗi trang một DataTemplate: ViewModel -> View. Plan 3 thêm vào đây. -->
</ResourceDictionary>
```

`Views/MainWindow.xaml`:
```xml
<Window x:Class="Transport.Presentation.Wpf.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:l="clr-namespace:Transport.Presentation.Wpf.Localization"
        xmlns:svc="clr-namespace:Transport.Presentation.Wpf.Services"
        xmlns:vm="clr-namespace:Transport.Presentation.Wpf.ViewModels"
        mc:Ignorable="d"
        d:DataContext="{d:DesignInstance Type=vm:MainViewModel}"
        Title="{l:Loc App_Title}" Width="1360" Height="820" MinWidth="1100" MinHeight="680"
        WindowStartupLocation="CenterScreen">
    <Window.Resources>
        <ResourceDictionary Source="PageTemplates.xaml" />
    </Window.Resources>

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="240" />
            <ColumnDefinition />
        </Grid.ColumnDefinitions>

        <!-- Sidebar -->
        <DockPanel Background="{StaticResource HaUiNavyBrush}">
            <StackPanel DockPanel.Dock="Top" Margin="24,24,24,20">
                <TextBlock Text="HaUI" FontSize="30" FontWeight="Bold" Foreground="White" />
                <Rectangle Height="3" Width="48" Fill="{StaticResource HaUiGoldBrush}" HorizontalAlignment="Left" Margin="0,6" />
                <TextBlock Text="{l:Loc App_Subtitle}" Foreground="#CCFFFFFF" TextWrapping="Wrap" />
            </StackPanel>

            <Button DockPanel.Dock="Bottom" Style="{StaticResource SidebarButtonStyle}" Command="{Binding LogoutCommand}"
                    AutomationProperties.Name="{l:Loc Btn_Logout}">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="&#xE7E8;" FontFamily="{StaticResource SymbolFont}" Foreground="White" Width="28" VerticalAlignment="Center" />
                    <TextBlock Text="{l:Loc Btn_Logout}" Foreground="White" />
                </StackPanel>
            </Button>

            <ListBox ItemsSource="{Binding Menu}" SelectedItem="{Binding SelectedMenu}"
                     Style="{StaticResource SidebarListBoxStyle}" ItemContainerStyle="{StaticResource SidebarItemStyle}">
                <ListBox.ItemTemplate>
                    <DataTemplate DataType="{x:Type svc:PageDescriptor}">
                        <StackPanel Orientation="Horizontal"
                                    AutomationProperties.Name="{Binding TitleKey, Converter={StaticResource Localize}}">
                            <TextBlock Text="{Binding Glyph}" FontFamily="{StaticResource SymbolFont}" Foreground="White"
                                       Width="28" VerticalAlignment="Center" />
                            <TextBlock Text="{Binding TitleKey, Converter={StaticResource Localize}}" Foreground="White" />
                        </StackPanel>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
        </DockPanel>

        <!-- Nội dung -->
        <Grid Grid.Column="1" Background="{DynamicResource ApplicationBackgroundBrush}">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
                <RowDefinition />
            </Grid.RowDefinitions>

            <DockPanel Margin="24,16,24,8">
                <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" VerticalAlignment="Center">
                    <StackPanel Margin="0,0,16,0">
                        <TextBlock Text="{Binding UserFullName}" FontWeight="SemiBold" HorizontalAlignment="Right" />
                        <TextBlock Text="{Binding RoleKey, Converter={StaticResource Localize}}"
                                   Foreground="{StaticResource HaUiNavyBrush}" HorizontalAlignment="Right" />
                    </StackPanel>
                    <Button Command="{Binding ToggleLanguageCommand}" AutomationProperties.Name="{l:Loc Btn_SwitchLanguage}">
                        <StackPanel Orientation="Horizontal">
                            <TextBlock Text="&#xE774;" FontFamily="{StaticResource SymbolFont}" Margin="0,0,8,0" VerticalAlignment="Center" />
                            <TextBlock Text="{l:Loc Btn_SwitchLanguage}" />
                        </StackPanel>
                    </Button>
                </StackPanel>
                <TextBlock Text="{Binding PageTitle}" Style="{StaticResource PageTitleStyle}" VerticalAlignment="Center" />
            </DockPanel>

            <ProgressBar Grid.Row="1" IsIndeterminate="True" Height="3" Margin="24,0"
                         Visibility="{Binding Navigation.CurrentPage.IsBusy, Converter={StaticResource BoolToVisibility}, FallbackValue=Collapsed}" />

            <Border Grid.Row="2" DataContext="{Binding Notifications}" Style="{StaticResource InfoBarStyle}">
                <DockPanel>
                    <Button DockPanel.Dock="Right" Command="{Binding CloseCommand}" Padding="8,2"
                            AutomationProperties.Name="{l:Loc Btn_Close}">
                        <TextBlock Text="&#xE711;" FontFamily="{StaticResource SymbolFont}" />
                    </Button>
                    <TextBlock Text="{Binding Message}" TextWrapping="Wrap" VerticalAlignment="Center" />
                </DockPanel>
            </Border>

            <ContentControl Grid.Row="3" Content="{Binding Navigation.CurrentPage}" Margin="24,8,24,24" Focusable="False" />
        </Grid>
    </Grid>
</Window>
```

`Views/MainWindow.xaml.cs`:
```csharp
using System.Windows;
using Transport.Presentation.Wpf.ViewModels;

namespace Transport.Presentation.Wpf.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

- [ ] **Step 7: Write `Services/WindowService.cs`**

```csharp
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Transport.Presentation.Wpf.Views;

namespace Transport.Presentation.Wpf.Services;

public sealed class WindowService(IServiceProvider services) : IWindowService
{
    public void ShowMain() => Swap(services.GetRequiredService<MainWindow>());

    public void ShowLogin() => Swap(services.GetRequiredService<LoginWindow>());

    // Mở cửa sổ mới trước rồi mới đóng cửa sổ cũ để app không tự thoát
    private static void Swap(Window next)
    {
        var app = System.Windows.Application.Current;
        var previous = app.Windows.OfType<Window>().Where(w => w != next).ToList();
        app.MainWindow = next;
        next.Show();
        foreach (var window in previous)
        {
            window.Close();
        }
    }
}
```

- [ ] **Step 8: Write `PresentationServices.cs`, `appsettings.json` and the host startup**

`src/Transport.Presentation.Wpf/PresentationServices.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.Services;
using Transport.Presentation.Wpf.ViewModels;
using Transport.Presentation.Wpf.Views;

namespace Transport.Presentation.Wpf;

public static class PresentationServices
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<ILocalizationService>(LocalizationService.Instance);
        services.AddSingleton<NotificationService>();
        services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<NotificationService>());
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<PageServices>();
        // Mỗi cửa sổ chính có bộ điều hướng riêng
        services.AddTransient<INavigationService, NavigationService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
        return services.AddPages();
    }

    // Đăng ký một trang: ViewModel (mới mỗi lần mở) + mục menu
    public static IServiceCollection AddPage<TViewModel>(this IServiceCollection services, string titleKey, string glyph,
        int order, bool operatorOnly = false)
        where TViewModel : PageViewModel
    {
        services.AddTransient<TViewModel>();
        services.AddSingleton(new PageDescriptor(titleKey, glyph, typeof(TViewModel), operatorOnly, order));
        return services;
    }

    // Danh sách trang của ứng dụng (Plan 3 thêm từng dòng services.AddPage<...>(...) trước return)
    private static IServiceCollection AddPages(this IServiceCollection services)
    {
        return services;
    }
}
```

`src/Transport.Presentation.Wpf/appsettings.json`:
```json
{
  "Database": {
    "DataSource": "localhost:1521/XEPDB1",
    "Schema": "TRANSPORT"
  },
  "Language": "vi"
}
```

`src/Transport.Presentation.Wpf/App.xaml.cs`:
```csharp
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Transport.Application;
using Transport.Infrastructure;
using Transport.Presentation.Wpf.Localization;
using Transport.Presentation.Wpf.Services;

namespace Transport.Presentation.Wpf;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Số tiền, ngày giờ luôn theo định dạng Việt Nam (1.250.000 ₫, 17/10/2026) dù giao diện tiếng Anh
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("vi-VN")));
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // Composition root: Generic Host đọc appsettings.json và dựng DI cho cả 3 tầng
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = e.Args,
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.Services.AddApplication().AddInfrastructure(builder.Configuration).AddPresentation();
        _host = builder.Build();
        _host.Start();

        LocalizationService.Instance.SetLanguage(builder.Configuration["Language"] ?? "vi");
        _host.Services.GetRequiredService<IWindowService>().ShowLogin();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }

    // Lỗi ngoài dự kiến: báo cho người dùng thay vì tắt ứng dụng
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, LocalizationService.Instance["App_Title"],
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
```

- [ ] **Step 9: Build and run all tests**

Run: `dotnet build TransportManagement.slnx && dotnet test --project tests/Transport.UnitTests`
Expected: 0 warnings, `failed: 0`.

- [ ] **Step 10: Smoke-run the app**

Run (PowerShell):
```powershell
$exe = "src/Transport.Presentation.Wpf/bin/Debug/net10.0-windows/Transport.Presentation.Wpf.exe"
$p = Start-Process -FilePath $exe -PassThru
Start-Sleep -Seconds 6
$p.Refresh()
"$($p.HasExited) | $($p.MainWindowTitle)"
Stop-Process -Id $p.Id
```
Expected: `False | Quản lý vận tải và đặt vé xe` (process alive, login window shown).

- [ ] **Step 11: Manual check (ask the user to do it, or do it with computer-use if available)**

1. Run `dotnet run --project src/Transport.Presentation.Wpf`.
2. Login window: navy brand column on the left, form on the right, Fluent-styled text boxes.
3. Switch the language combo to English → all texts change immediately.
4. Sign in with `seller01` / `wrong` → red message "Wrong username or password." (or Vietnamese equivalent).
5. Sign in with `seller01` / `Seller#001` → main window opens: navy sidebar (empty menu for now), user "Trần Thị Bán", role "Nhân viên bán vé", language button, sign-out button.
6. Click the language button → role label switches language. Click sign out → confirm → login window returns.

- [ ] **Step 12: Commit**

```bash
git add src/Transport.Presentation.Wpf tests/Transport.UnitTests
git commit -m "feat(ui): add login window, main shell and Generic Host startup"
```
