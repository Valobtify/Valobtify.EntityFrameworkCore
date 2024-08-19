using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Valobtify.EntityFrameworkCore;

public static class ValueObjectConfigExtensions
{
    public static ModelBuilder SetupSingleValueObjects(this ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model
            .GetEntityTypes().ToList();

        foreach (var entityType in entityTypes)
        {
            var properties = entityType.ClrType
                .GetProperties().ToList()
                .GetValueObjectTypeProperties();

            modelBuilder.MapValueObjectDataAnnotations(entityType, properties);

            modelBuilder.SetupSingleValueObjectConversions(entityType, properties);
        }

        return modelBuilder;
    }

    static ModelBuilder MapValueObjectDataAnnotations(
        this ModelBuilder modelBuilder,
        IMutableEntityType entityType,
        List<PropertyInfo> properties)
    {
        foreach (PropertyInfo property in properties)
        {
            var maxLength = property.PropertyType
                        .GetProperty(nameof(SingleValueObject<string>.Value))!
                        .GetCustomAttribute<MaxLengthAttribute>()?.Length;

            if (maxLength is not null)
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.PropertyType, property.Name)
                    .HasMaxLength(maxLength.Value);
            }
        }

        return modelBuilder;
    }

    static ModelBuilder SetupSingleValueObjectConversions(
        this ModelBuilder modelBuilder,
        IMutableEntityType entityType,
        List<PropertyInfo> properties)
    {
        foreach (var property in properties)
        {
            var entityMethod = modelBuilder
                .GetType()
                .GetMethods()
                .Single(m =>
                m.IsGenericMethod &&
                m.IsPublic &&
                !m.IsStatic &&
                m.Name is nameof(modelBuilder.Entity) &&
                m.GetParameters().Length == 0)
                .MakeGenericMethod(entityType.ClrType);
            var entityConfig = entityMethod.Invoke(modelBuilder, null);

            var propertyMethod = entityConfig?
                .GetType()
                .GetMethods()
                .Single(m =>
                m.Name is nameof(EntityTypeBuilder.Property) &&
                m.IsGenericMethod &&
                m.GetParameters().Length is 1 &&
                m.GetParameters().First().ParameterType == typeof(string))
                .MakeGenericMethod(property.PropertyType);
            var propertyConfig = propertyMethod?.Invoke(entityConfig, [property.Name]);

            var valueType = property.PropertyType.GetProperty(nameof(SingleValueObject<string>.Value))!.PropertyType;

            var hasConversionMethod = propertyConfig?
                .GetType()
                .GetMethods()
                .Single(m =>
                m.Name is nameof(PropertyBuilder.HasConversion) &&
                m.IsPublic &&
                m.GetParameters().Length is 2 &&
                m.GetParameters().First().Name is "convertToProviderExpression")
                .MakeGenericMethod(valueType);

            var convertToProviderType = typeof(ValueObjectConfigExtensions)
                .GetMethod(nameof(ConvertToProvider))!
                .MakeGenericMethod(valueType, property.PropertyType);

            var convertFromProviderType = typeof(ValueObjectConfigExtensions)
                .GetMethod(nameof(ConvertFromProvider))!
                .MakeGenericMethod(property.PropertyType, valueType);

            hasConversionMethod!.Invoke(propertyConfig,
                [
                    convertToProviderType.Invoke(null, null),
                    convertFromProviderType.Invoke(null, null)
                ]);
        }

        return modelBuilder;
    }

    static List<PropertyInfo> GetValueObjectTypeProperties(this List<PropertyInfo> properties)
    {
        var valueObjectTypeProperties = new List<PropertyInfo>();

        foreach (var property in properties.ToList())
        {
            var propertyBaseType = property.PropertyType.BaseType;

            if (propertyBaseType?.IsGenericType is not true) continue;

            if (propertyBaseType.GetGenericTypeDefinition() != typeof(SingleValueObject<>)) continue;

            valueObjectTypeProperties.Add(property);
        }

        return valueObjectTypeProperties;
    }

    public static Expression<Func<TValueObject, TValue?>> ConvertToProvider<TValue, TValueObject>()
          where TValueObject : SingleValueObject<TValue>, new()
    {
        return c => c.Value;
    }

    public static Expression<Func<TValue?, TValueObject>> ConvertFromProvider<TValueObject, TValue>()
           where TValueObject : SingleValueObject<TValue>, new()
    {
        return c => new TValueObject() { Value = c! };
    }
}
