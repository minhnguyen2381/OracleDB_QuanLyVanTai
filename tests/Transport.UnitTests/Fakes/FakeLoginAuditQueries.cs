using Transport.Application.Abstractions;
using Transport.Application.Models;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeLoginAuditQueries : ILoginAuditQueries
{
    public List<LoginAuditEntry> Entries { get; } = [];
    public (string? Username, DateTime From, DateTime To)? LastQuery { get; private set; }

    public Task<IReadOnlyList<LoginAuditEntry>> GetAsync(string? username, DateTime from, DateTime to, CancellationToken ct = default)
    {
        LastQuery = (username, from, to);
        return Task.FromResult<IReadOnlyList<LoginAuditEntry>>(Entries.ToList());
    }
}
