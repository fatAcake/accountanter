namespace backend.Abstractions.Common
{
    public interface IDateTimePeriodService
    {
        DateTime ToUtcStart(DateTime date);
        DateTime ToUtcEnd(DateTime date);
        DateTime ToUtc(DateTime date);
    }
}
