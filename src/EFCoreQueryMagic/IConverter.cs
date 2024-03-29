﻿using Microsoft.EntityFrameworkCore;

namespace EFCoreQueryMagic;

public interface IConverter<TFrom, TTo>: IConverter
{
    public TTo ConvertTo(TFrom from);

    public TFrom ConvertFrom(TTo to);
}

public interface IConverter
{
    public DbContext? Context { get; set; }
}