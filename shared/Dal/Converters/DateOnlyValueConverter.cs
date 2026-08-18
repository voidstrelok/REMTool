using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RemTool.Shared
{
    public class DateOnlyValueConverter : ValueConverter<DateOnly, DateTime>
    {
        public DateOnlyValueConverter() : base(
            dateonly => dateonly.ToDateTime(TimeOnly.MinValue),
            datetime => DateOnly.FromDateTime(datetime))
        {
        }
    }
}
