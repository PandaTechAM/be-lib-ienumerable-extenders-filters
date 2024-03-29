﻿using System.Linq.Dynamic.Core;
using System.Reflection;
using EFCoreQueryMagic.Attributes;
using EFCoreQueryMagic.Converters;
using EFCoreQueryMagic.Dto;
using EFCoreQueryMagic.Exceptions;
using EFCoreQueryMagic.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EFCoreQueryMagic.Extensions;

public static class DistinctColumnValuesExtensions
{
    private static IQueryable<object> GenerateBaseQueryable<TModel>(this IQueryable<TModel> dbSet,
        List<FilterDto> filters, DbContext? context) where TModel : class
    {
        var query = dbSet.AsNoTracking().ApplyFilters(filters, context);


        return query;
    }

    static List<T> Paginate<T>(this List<T> list, int pageSize, int page)
    {
        return list.Skip(pageSize * (page - 1)).Take(pageSize).ToList();
    }


    static Type GetEnumerableType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return type.GetGenericArguments()[0];

        if (type.IsArray)
            return type.GetElementType()!;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];

        if (type.IsEnum)
            return type;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return type.GetGenericArguments()[0];

        return type;
    }

    public static DistinctColumnValues DistinctColumnValues<TModel>(this IQueryable<TModel> dbSet,
        List<FilterDto> filters, string columnName, int pageSize, int page, DbContext? context = null) where TModel : class
    {
        var result = new DistinctColumnValues();

        var targetProperty = typeof(TModel)
            .GetTargetType()
            .GetProperties()
            .Where(x => x.GetCustomAttribute<MappedToPropertyAttribute>() != null)
            .FirstOrDefault(x => x.Name == columnName);

        if (targetProperty is null)
            throw new PropertyNotFoundException($"Property {columnName} not found in {typeof(TModel).Name}");

        var mappedToPropertyAttribute = targetProperty.GetCustomAttribute<MappedToPropertyAttribute>()!;

        var propertyType = PropertyHelper.GetPropertyType(typeof(TModel), mappedToPropertyAttribute);

        if (propertyType.EnumCheck())
        {
            var values = Enum.GetValues(GetEnumerableType(propertyType)).Cast<object>()
                .Where(x => !(x as Enum)!.HasAttributeOfType<HideEnumValueAttribute>());
            var stringValues = values.Select(x => x.ToString() as object).ToList();

            var list = stringValues.ToList();
            result.Values = list.Paginate(pageSize, page);
            result.TotalCount = list.Count;
            return result;
        }

        var query = GenerateBaseQueryable(dbSet, filters, context);
        IQueryable<object> query2;

        // check for ICollection<>

        var property = PropertyHelper.GetPropertyLambda(mappedToPropertyAttribute);

        if (propertyType.IsIEnumerable() && !mappedToPropertyAttribute.Encrypted)
        {
            query2 = (IQueryable<object>)query.AsNoTracking().Select(property).SelectMany("x => x");
        }
        else
        {
            query2 = query.Select<object>(property);
        }

        var converter = (mappedToPropertyAttribute.Encrypted
            ? Activator.CreateInstance(mappedToPropertyAttribute.ConverterType ?? typeof(EncryptedConverter))
            : Activator.CreateInstance(mappedToPropertyAttribute.ConverterType ?? typeof(DirectConverter))) as IConverter;

        converter.Context = context;
        
        var method = converter!.GetType().GetMethods().First(x => x.Name == "ConvertFrom");

        IQueryable<object> query3;
        try
        {
            query3 = query2.Distinct().OrderBy(x => x);
            result.TotalCount = mappedToPropertyAttribute.Encrypted ? 1 : query3.LongCount();
            result.Values = query3.Skip(pageSize * (page - 1)).Take(pageSize)
                .ToList()
                .Select(x => method.Invoke(converter, [x])!).Distinct().ToList();
            return result;
        }
        catch
        {
            query3 = query2.Distinct().OrderBy(x => x);
            result.TotalCount = mappedToPropertyAttribute.Encrypted ? 1 : long.MaxValue;
            result.Values = query3.Skip(pageSize * (page - 1)).Take(pageSize)
                .ToList()
                .Select(x => method.Invoke(converter, [x])!).Distinct().ToList();
            return result;
        }
    }

    public static async Task<DistinctColumnValues> DistinctColumnValuesAsync<TModel>(
        this IQueryable<TModel> dbSet,
        List<FilterDto> filters,
        string columnName, int pageSize, int page, DbContext? context = null,
        CancellationToken cancellationToken = default) where TModel : class
    {
        var result = new DistinctColumnValues();

        var targetProperty = typeof(TModel)
            .GetTargetType()
            .GetProperties()
            .Where(x => x.GetCustomAttribute<MappedToPropertyAttribute>() != null)
            .FirstOrDefault(x => x.Name == columnName);

        if (targetProperty is null)
            throw new PropertyNotFoundException($"Property {columnName} not found in {typeof(TModel).Name}");

        var mappedToPropertyAttribute = targetProperty.GetCustomAttribute<MappedToPropertyAttribute>()!;

        var propertyType = PropertyHelper.GetPropertyType(typeof(TModel), mappedToPropertyAttribute);

        if (propertyType.EnumCheck())
        {
            var values = Enum.GetValues(GetEnumerableType(propertyType)).Cast<object>()
                .Where(x => !(x as Enum)!.HasAttributeOfType<HideEnumValueAttribute>());
            var stringValues = values.Select(x => x.ToString() as object).ToList();

            var list = stringValues.ToList();
            result.Values = list.Paginate(pageSize, page);
            result.TotalCount = list.Count;
            return result;
        }

        var query = GenerateBaseQueryable(dbSet, filters, context);
        IQueryable<object> query2;

        // check for ICollection<>

        var property = PropertyHelper.GetPropertyLambda(mappedToPropertyAttribute);

        if (propertyType.IsIEnumerable() && !mappedToPropertyAttribute.Encrypted)
        {
            query2 = (IQueryable<object>)query.Select(property).SelectMany("x => x");
        }
        else
        {
            query2 = query.Select<object>(property);
        }

        var converter = (mappedToPropertyAttribute.Encrypted
            ? Activator.CreateInstance(mappedToPropertyAttribute.ConverterType ?? typeof(EncryptedConverter))
            : Activator.CreateInstance(mappedToPropertyAttribute.ConverterType ?? typeof(DirectConverter))) as IConverter;

        converter.Context = context;
        
        var method = converter!.GetType().GetMethods().First(x => x.Name == "ConvertFrom");
      
        IQueryable<object> query3;
        try
        {
            query3 = query2.Distinct().OrderBy(x => x);
            result.TotalCount = mappedToPropertyAttribute.Encrypted ? 1 : query3.LongCount();
            result.Values = (await query3.Skip(pageSize * (page - 1)).Take(pageSize)
                    .ToListAsync(cancellationToken: cancellationToken))
                .Select(x => method.Invoke(converter, [x])!).Distinct().ToList();
            return result;
        }
        catch
        {
            query3 = query2.Distinct().OrderBy(x => x);
            result.TotalCount = mappedToPropertyAttribute.Encrypted ? 1 : long.MaxValue;
            result.Values = (await query3.Skip(pageSize * (page - 1)).Take(pageSize)
                    .ToListAsync(cancellationToken: cancellationToken))
                .Select(x => method.Invoke(converter, [x])!).Distinct().ToList();
            return result;
        }
    }
}