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
