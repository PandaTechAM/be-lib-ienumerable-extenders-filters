﻿namespace EFCoreQueryMagic.Dto;

public class FilteredDataResult<T>
{
    public List<T> Data { get; set; } = null!;
    public long TotalCount { get; set; }
}