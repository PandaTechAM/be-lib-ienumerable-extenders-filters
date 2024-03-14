﻿using Microsoft.EntityFrameworkCore;

namespace EFCoreQueryMagic;

public interface IConverter<TFrom, TTo>
{
    public DbContext Context { get; set; }
    public TTo ConvertTo(TFrom from);

    public TFrom ConvertFrom(TTo to);
}