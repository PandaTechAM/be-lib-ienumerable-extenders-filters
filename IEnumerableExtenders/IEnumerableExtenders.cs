﻿using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text.Json;
using PandaTech.IEnumerableFilters.Dto;
using static System.Linq.Expressions.Expression;
using Convert = System.Convert;

namespace PandaTech.IEnumerableFilters;

public static class EnumerableExtenders
{
    public static IQueryable<T> ApplyFilters<T>(this IEnumerable<T> dbSet, List<FilterDto> filters,
        FilterProvider filterProvider)
        where T : class
    {
        var q = dbSet.AsQueryable();

        try
        {
            foreach (var filterDto in filters)
            {
                var filter = filterProvider.GetFilter(filterDto.PropertyName, filterDto.ComparisonType, typeof(T));

                var filterType = filter.SourcePropertyType;
                var filterTypeName = filterType.Name;

                for (var index = 0; index < filterDto.Values.Count; index++)
                {
                    var val = (JsonElement)filterDto.Values[index];

                    if (filterType.IsEnum)
                    {
                        var enumType = filterType;
                        var getExpression = Call(typeof(Enum), "Parse", null,
                            Constant(enumType), Constant(val.GetString()!));

                        var lambda = Lambda<Func<object>>(getExpression).Compile();

                        filterDto.Values[index] = lambda();
                    }
                    // is list
                    else if (filterType.Name == "List`1")
                    {
                        filterDto.Values[index] =
                            val.ValueKind == JsonValueKind.String ? val.GetString()! : val.GetInt64();
                    }
                    else
                    {
                        filterDto.Values[index] = filterTypeName switch
                        {
                            "String" => val.GetString()!,
                            "Int32" => val.GetInt32(),
                            "Int64" => val.GetInt64(),
                            "Boolean" => val.GetBoolean(),
                            "DateTime" => val.GetDateTime(),
                            "Decimal" => val.GetDecimal(),
                            "Double" => val.GetDouble(),
                            "Single" => val.GetSingle(),
                            "Guid" => val.GetGuid(),
                            _ => Activator.CreateInstance(filterType)!
                        };
                    }
                }

                for (var index = 0; index < filterDto.Values.Count; index++)
                {
                    filterDto.Values[index] = filter.Converter(filterDto.Values[index]) ?? filterDto.Values[index];
                }

                object typedList;
                if (filter.TargetPropertyType.IsGenericType &&
                    filter.TargetPropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var genericType = filter.TargetPropertyType.GetGenericArguments()[0];
                    typedList = Activator.CreateInstance(typeof(List<>).MakeGenericType(genericType));
                }
                else
                {
                    typedList = Activator.CreateInstance(typeof(List<>).MakeGenericType(filter.TargetPropertyType));
                }

                var addMethod = typedList.GetType().GetMethod("Add")!;


                foreach (var Value in filterDto.Values)
                {
                    addMethod.Invoke(typedList, new[] { Value });
                }


                var filterString =
                    filterProvider.GetFilterLambda(filterDto.PropertyName, filterDto.ComparisonType, typeof(T));

                q = filterDto.ComparisonType switch
                {
                    ComparisonType.Between => q.Where(filterString, filterDto.Values[0], filterDto.Values[1]),
                    // @0.Contains
                    ComparisonType.In => q.Where(filterString, typedList),
                    ComparisonType.Contains when filter.SourcePropertyType != typeof(string) => q.Where(filterString,
                        filterDto.Values[0]),
                    // TODO: List contains 
                    _ => q.Where(filterString, filterDto.Values[0])
                };
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return q;
    }


    public static IQueryable<T> ApplyOrdering<T>(this IEnumerable<T> dbSet, Ordering ordering)
    {
        if (ordering.PropertyName == string.Empty)
            return dbSet.AsQueryable();

        var property = typeof(T).GetProperty(ordering.PropertyName);
        if (property is null)
            throw new Exception("Column not found");

        var parameter = Parameter(typeof(T));
        var propertyAccess = Property(parameter, property);
        var lambda = Lambda<Func<T, object>>(Convert(propertyAccess, typeof(object)),
            parameter);

        return !ordering.Descending
            ? dbSet.AsQueryable().OrderBy(lambda)
            : dbSet.AsQueryable().OrderByDescending(lambda);
    }


    public static Dictionary<string, object?> GetAggregates<T>(this IEnumerable<T> dbSet,
        List<AggregateDto> aggregates)
        where T : class
    {
        var query = dbSet.AsQueryable();

        var result = new Dictionary<string, object?>();
        foreach (var aggregate in aggregates)
        {
            var property = typeof(T).GetProperty(aggregate.PropertyName);

            if (property is null)
                throw new Exception("Column not found");
            var parameter = Parameter(typeof(T));
            var propertyAccess = Property(parameter, property);


            if (property.PropertyType == typeof(string))
            {
                var lambda = Lambda<Func<T, string>>(propertyAccess, parameter);

                decimal? res = aggregate.AggregateType switch
                {
                    AggregateType.UniqueCount => query.Select(lambda).Distinct().ToList().Count,
                    _ => null
                };

                result.Add($"{aggregate.PropertyName}_{aggregate.AggregateType.ToString()}", res);
                continue;
            }

            if (property.PropertyType == typeof(int))
            {
                var lambda = Lambda<Func<T, int>>(propertyAccess, parameter);

                double? res = aggregate.AggregateType switch
                {
                    AggregateType.UniqueCount => query.Select(lambda).Distinct().ToList().Count,
                    AggregateType.Sum => query.Select(lambda).Sum(),
                    AggregateType.Average => query.Select(lambda).Average(),
                    AggregateType.Min => query.Select(lambda).Min(),
                    AggregateType.Max => query.Select(lambda).Max(),
                    _ => null
                };

                result.Add($"{aggregate.PropertyName}_{aggregate.AggregateType.ToString()}", res);
                continue;
            }

            if (property.PropertyType == typeof(long))
            {
                var lambda = Lambda<Func<T, long>>(propertyAccess, parameter);

                double? res = aggregate.AggregateType switch
                {
                    AggregateType.UniqueCount => query.Select(lambda).Distinct().ToList().Count,
                    AggregateType.Sum => query.Select(lambda).Sum(),
                    AggregateType.Average => query.Select(lambda).Average(),
                    AggregateType.Min => query.Select(lambda).Min(),
                    AggregateType.Max => query.Select(lambda).Max(),
                    _ => null
                };

                result.Add($"{aggregate.PropertyName}_{aggregate.AggregateType.ToString()}", res);
                continue;
            }

            if (property.PropertyType == typeof(DateTime))
            {
                var lambda = Lambda<Func<T, DateTime>>(propertyAccess, parameter);
                DateTime? res = aggregate.AggregateType switch
                {
                    AggregateType.Min => query.Select(lambda).Min(),
                    AggregateType.Max => query.Select(lambda).Max(),
                    AggregateType.Average => new DateTime(
                        Convert.ToInt64(query.Select(lambda).ToList().Average(x => x.Ticks / 10_000_000)) *
                        10_000_000),
                    _ => null
                };
                result.Add($"{aggregate.PropertyName}_{aggregate.AggregateType.ToString()}", res);
                continue;
            }

            if (property.PropertyType == typeof(decimal))
            {
                var lambda = Lambda<Func<T, decimal>>(propertyAccess, parameter);

                decimal? res = aggregate.AggregateType switch
                {
                    AggregateType.UniqueCount => query.Select(lambda).Distinct().ToList().Count,
                    AggregateType.Sum => query.Select(lambda).Sum(),
                    AggregateType.Average => query.Select(lambda).Average(),
                    AggregateType.Min => query.Select(lambda).Min(),
                    AggregateType.Max => query.Select(lambda).Max(),
                    _ => null
                };

                result.Add($"{aggregate.PropertyName}_{aggregate.AggregateType.ToString()}", res);
                continue;
            }

            if (property.PropertyType == typeof(double))
            {
                var lambda = Lambda<Func<T, double>>(propertyAccess, parameter);

                double? res = aggregate.AggregateType switch
                {
                    AggregateType.UniqueCount => query.Select(lambda).Distinct().ToList().Count,
                    AggregateType.Sum => query.Select(lambda).Sum(),
                    AggregateType.Average => query.Select(lambda).Average(),
                    AggregateType.Min => query.Select(lambda).Min(),
                    AggregateType.Max => query.Select(lambda).Max(),
                    _ => null
                };

                result.Add($"{aggregate.PropertyName}_{aggregate.AggregateType.ToString()}", res);
                continue;
            }

            if (property.PropertyType == typeof(Guid))
            {
                throw new NotImplementedException();
            }

            if (property.PropertyType.IsClass)
            {
                throw new NotImplementedException();
            }

            result.Add($"{aggregate.PropertyName}_{aggregate.AggregateType.ToString()}", null);
        }

        return result;
    }

    public static List<object> DistinctColumnValues<T>(this IEnumerable<T> dbSet, List<FilterDto> filters,
        string columnName, FilterProvider filterProvider,
        int pageSize, int page) where T : class
    {
        var filter = filterProvider.GetFilter(columnName, typeof(T));

        var propertyType = filter.TargetPropertyType;

        var query = dbSet.ApplyFilters(filters, filterProvider);
        IQueryable<object> query2;

        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
        {
            query2 = (IQueryable<object>)query.Select(filter.TargetPropertyName).SelectMany("x => x");
        }
        else
        {
            query2 = query.Select<object>(filter.TargetPropertyName);
        }

        IQueryable<object> query3;
        try
        {
            query3 = query2.Distinct().OrderBy(x => x);
            return query3.Skip(pageSize * (page - 1)).Take(pageSize).ToList().Select(filter.DtoConverter).ToList();
        }
        catch
        {
            query3 = query2;
            return query3.Skip(pageSize * (page - 1)).Take(pageSize).Distinct().AsEnumerable().Select(filter.DtoConverter).ToList();
        }
    }
}

public class ComparisonNotSupportedException : Exception
{
    public ComparisonNotSupportedException(string message) : base(message)
    {
    }

    public ComparisonNotSupportedException(FilterProvider.FilterKey key) : base(
        $"Comparison {key.ComparisonType} not supported for type {key.TargetType}")
    {
    }
}

public class PropertyNotFoundException : Exception
{
    public PropertyNotFoundException(string message) : base(message)
    {
    }
}