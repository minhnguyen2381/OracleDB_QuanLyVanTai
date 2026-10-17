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
