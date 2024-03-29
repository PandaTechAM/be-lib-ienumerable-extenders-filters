namespace EFCoreQueryMagic.Dto;

public class DistinctColumnValues
{
    public List<object> Values { get; set; } = [];
    public long TotalCount { get; set; }

    public override string ToString()
    {
        return $"{nameof(Values)}: {string.Join(';', Values)}, {nameof(TotalCount)}: {TotalCount}";
    }
}