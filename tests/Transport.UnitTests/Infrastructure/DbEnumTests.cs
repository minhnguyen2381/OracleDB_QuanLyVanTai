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
