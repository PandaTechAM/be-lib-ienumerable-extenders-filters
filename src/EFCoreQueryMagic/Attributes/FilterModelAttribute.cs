﻿namespace EFCoreQueryMagic.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class FilterModelAttribute(Type targetType) : Attribute
{
    public readonly Type TargetType = targetType;
}