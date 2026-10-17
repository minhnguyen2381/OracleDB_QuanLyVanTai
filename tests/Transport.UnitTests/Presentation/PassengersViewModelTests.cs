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
