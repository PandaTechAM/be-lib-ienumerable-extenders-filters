﻿using EFCoreQueryMagic.Attributes;

namespace EFCoreQueryMagic.Demo.db;

[FilterModel(typeof(CompanyFilter))]
public class Company
{
    public long Id { get; set; }
    public long Age { get; set; }
    public string Name { get; set; }
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
    public long? NullableAge { get; set; }
    
    public string? NullableString { get; set; }
    public byte[]? NameEncrypted { get; set; }
    public bool IsEnabled { get; set; }
    public CType Type { get; set; }
    public CType[] Types { get; set; }
    public Info Info { get; set; } = null!;
    
    public long? SomeClassId { get; set; }
    public SomeClass? SomeClass { get; set; }
    public List<OneToMany> OneToManys { get; set; }
}