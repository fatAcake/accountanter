using backend.Abstractions.Common;

namespace backend.Services.Common
{
    public sealed class DateTimePeriodService : IDateTimePeriodService
    {
        public DateTime ToUtcStart(DateTime date)
        {
            var d = date.Kind switch
            {
                DateTimeKind.Utc => date.Date,
                DateTimeKind.Local => date.ToUniversalTime().Date,
                _ => date.Date,
            };
            return DateTime.SpecifyKind(d, DateTimeKind.Utc);
        }

        public DateTime ToUtcEnd(DateTime date) =>
            ToUtcStart(date).AddDays(1).AddTicks(-1);

        public DateTime ToUtc(DateTime date) =>
            date.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
                : date.ToUniversalTime();
    }
}
