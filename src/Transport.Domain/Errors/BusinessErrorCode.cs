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
    TooManyRegistrations = 20061,
    InvalidRegistrationData = 20062,

    // Lỗi hệ thống Oracle hay gặp (giá trị = số ORA)
    DuplicateValue = 1,
    InvalidCredentials = 1017,
    InsufficientPrivileges = 1031,
    UsernameAlreadyExists = 1920,
    ChildRecordExists = 2292,
    CheckConstraintViolated = 2290,
    AccountLocked = 28000,
    PasswordExpired = 28001,
    PasswordReused = 28007,
    DatabaseUnavailable = 12541,

    // Lỗi kiểm tra phía client, không đi tới Oracle
    RequiredField = 90001,
    InvalidPhone = 90002,
    InvalidSeat = 90003,
    InvalidTimeRange = 90004,
    InvalidPrice = 90005,
    OperatorOnly = 90006,
}
