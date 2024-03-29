﻿using EFCoreQueryMagic.Enums;

namespace EFCoreQueryMagic.Dto;

public class FilterDto
{
    public string PropertyName { get; set; } = null!;
    public ComparisonType ComparisonType { get; set; }

    public List<object> Values { get; set; } = null!;
}