using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace RemTool.Shared
{
    public class DateOnlyValueComparer : ValueComparer<DateOnly>
    {
        public DateOnlyValueComparer() : base(
            (t1, t2) => t1.Equals(t2),
            t => t.GetHashCode())
        {
        }
    }
}
