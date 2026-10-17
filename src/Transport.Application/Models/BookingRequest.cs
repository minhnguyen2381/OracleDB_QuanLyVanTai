namespace Transport.Application.Models;

public sealed record BookingRequest(int ScheduleId, string FullName, string Phone, string? IdCard, int SeatNo);
