﻿using EFCoreQueryMagic.Dto;
using EFCoreQueryMagic.Enums;
using EFCoreQueryMagic.Exceptions;
using Microsoft.Extensions.Logging;

namespace EFCoreQueryMagic;

[Obsolete]
public class FilterProvider
{
    private readonly List<Filter> _filters = new();
    private ILogger<FilterProvider> Logger { get; }

    private readonly Dictionary<FilterKey, string> _expressions = new();

    public class Filter
    {
        public Type SourceType = null!;
        public Type TargetType = null!;
        public string SourcePropertyName = null!;
        public Type SourcePropertyType = null!;
        public string TargetPropertyName = null!;
        public Type TargetPropertyType = null!;
        public List<ComparisonType> ComparisonTypes = null!;
        public Func<object, object> Converter = null!;

        public Func<object, object> DtoConverter = null!;
        // TODO add order key for classes 
        // TODO add proper constructor
    }

    public void Add<TSource, TTarget>()
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        var sourceProperties = sourceType.GetProperties();
        var targetProperties = targetType.GetProperties();

        foreach (var sourceProperty in sourceProperties)
        {
            var targetProperty = targetProperties.FirstOrDefault(p => p.Name == sourceProperty.Name);
            if (targetProperty == null)
            {
                Logger.LogDebug("No matching property found for {SourceProperty}", sourceProperty.Name);
                continue;
            }

            var comparisonTypes = Enum.GetValues<ComparisonType>().ToList();
            if (comparisonTypes.Count == 0)
            {
                Logger.LogDebug("No comparison types found");
                continue;
            }

            var converter = new Func<object, object>(x => x);

            var filter = new Filter
            {
                SourcePropertyName = sourceProperty.Name,
                SourcePropertyType = sourceProperty.PropertyType.IsGenericType
                    ? sourceProperty.PropertyType.GenericTypeArguments[0]
                    : sourceProperty.PropertyType,
                TargetPropertyName = targetProperty.Name,
                TargetPropertyType = targetProperty.PropertyType,
                ComparisonTypes = comparisonTypes,
                Converter = converter,
                DtoConverter = converter,
                SourceType = typeof(TSource),
                TargetType = typeof(TTarget)
            };

            _filters.Add(filter);

            foreach (var comparisonType in Enum.GetValues<ComparisonType>())
            {
                var key = new FilterKey
                {
                    SourceType = sourceType,
                    TargetType = targetType,
                    SourcePropertyName = sourceProperty.Name,
                    TargetPropertyName = targetProperty.Name,
                    ComparisonType = comparisonType,
                    SourcePropertyType = sourceProperty.PropertyType.IsGenericType
                        ? sourceProperty.PropertyType.GenericTypeArguments[0]
                        : sourceProperty.PropertyType,
                    TargetPropertyType = targetProperty.PropertyType
                };

                try
                {
                    AddLambdaString(key);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    public void Add(Filter filter)
    {
        _filters.RemoveAll(x =>
            x.SourcePropertyName == filter.SourcePropertyName
            && x.TargetType == filter.TargetType);

        _filters.Add(filter);

        foreach (var filterComparisonType in filter.ComparisonTypes)
        {
            AddLambdaString(new FilterKey
            {
                SourceType = filter.SourceType,
                TargetType = filter.TargetType,
                SourcePropertyName = filter.SourcePropertyName,
                TargetPropertyName = filter.TargetPropertyName,
                ComparisonType = filterComparisonType,
                SourcePropertyType = filter.SourcePropertyType,
                TargetPropertyType = filter.TargetPropertyType
            });
        }
    }

    void AddLambdaString(FilterKey key)
    {
        _expressions[key] = FilterLambdaBuilder.BuildLambdaString(key);
    }

    public FilterProvider(ILogger<FilterProvider> logger)
    {
        Logger = logger;
    }

    public Filter GetFilter(string sourcePropertyName, ComparisonType comparisonType, Type targetType)
    {
        var filter = _filters.FirstOrDefault(x =>
            x.SourcePropertyName == sourcePropertyName && x.ComparisonTypes.Contains(comparisonType) &&
            x.TargetType == targetType);

        if (filter == null)
        {
            throw new PropertyNotFoundException(sourcePropertyName);
        }

        return filter;
    }

    public List<FilterInfo> GetFilterDtos<T>()
    {
        return _filters.Where(x => x.SourceType == typeof(T)).Select(
            x => new FilterInfo()
            {
                ComparisonTypes = x.ComparisonTypes,
                PropertyName = x.SourcePropertyName,
                Table = x.SourceType.Name
            }
        ).ToList();
    }

    public string GetFilterLambda(string filterDtoPropertyName, ComparisonType filterDtoComparisonType,
        Type targetTable)
    {
        var key =
            _expressions.Keys.FirstOrDefault(x =>
                x.SourcePropertyName == filterDtoPropertyName && x.ComparisonType == filterDtoComparisonType &&
                x.TargetType == targetTable) ?? throw new PropertyNotFoundException(filterDtoPropertyName);
        return _expressions.TryGetValue(key, out var expression)
            ? expression
            : throw new PropertyNotFoundException(filterDtoPropertyName);
    }

    public Filter GetFilter(string sourcePropertyName, Type tableType)
    {
        return _filters.FirstOrDefault(x => x.SourcePropertyName == sourcePropertyName && x.TargetType == tableType) ??
               throw new PropertyNotFoundException(sourcePropertyName);
    }
    
    public List<string> GetTables()
    {
        return _filters.Select(f => f.SourceType.Name).Distinct().ToList();
    }

    public Type? GetTable<T>()
    {
        return _filters.FirstOrDefault(x => x.SourceType == typeof(T))?.TargetType;
    }

    public Type? GetTableByName(string name)
    {
        return _filters.FirstOrDefault(x => x.SourceType.Name == name)?.SourceType ??
               _filters.FirstOrDefault(x => x.TargetType.Name == name)?.TargetType;
    }

    public List<FilterInfo> GetFilterDtos(Type T)
    {
        return _filters.Where(x => x.SourceType == T).Select(
            x => new FilterInfo()
            {
                ComparisonTypes = x.ComparisonTypes,
                PropertyName = x.SourcePropertyName,
                Table = x.SourceType.Name
            }
        ).ToList();
    }

    public Type? GetDbTableType(string name)
    {
        return _filters.FirstOrDefault(x => x.SourceType.Name == name)?.TargetType;
    }
}