using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Valobtify.EntityFrameworkCore;

public static class ValueObjectConfigExtensions
{
    static readonly Template _template = Template.Create("").Content!;
    static readonly PropertyConvertor convertor = new();

    public static ModelBuilder SetupSingleValueObjects(this ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model
            .GetEntityTypes().ToList();

        foreach (var entityType in entityTypes)
        {
            var properties = entityType.ClrType
                .GetProperties().ToList()
                .GetValueObjectTypeProperties();

            modelBuilder.ApplyMaxLength(entityType, properties);

            modelBuilder.SetupSingleValueObjectConversions(entityType, properties);
        }

        return modelBuilder;
    }

    static ModelBuilder ApplyMaxLength(
         this ModelBuilder modelBuilder,
         IMutableEntityType entityType,
         List<PropertyInfo> properties)
    {
        foreach (PropertyInfo property in properties)
        {
            var maxLength = property.PropertyType
                .GetProperty(nameof(_template.Value))!
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
            // generate modelBuilder.Entity<Entity>()
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

            // generate modelBuilder.Entity<Entity>().Property()
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

            var applyConversionMethod = typeof(ValueObjectConfigExtensions)
                .GetMethod(nameof(ApplyConversion))!
                .MakeGenericMethod(
                    property.PropertyType,
                    property.PropertyType.GetProperty(nameof(_template.Value))!.PropertyType);

            applyConversionMethod.Invoke(null, [propertyConfig]);
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

            if (propertyBaseType.GetGenericTypeDefinition() != typeof(SingleValueObject<,>)) continue;

            valueObjectTypeProperties.Add(property);
        }

        return valueObjectTypeProperties;
    }

    public static void ApplyConversion<TSingleValueObject, TValue>(this PropertyBuilder<TSingleValueObject> builder)
       where TSingleValueObject : SingleValueObject<TSingleValueObject, TValue>, ICreatableValueObject<TSingleValueObject, TValue>
       where TValue : notnull
    {
        builder.HasConversion(
            valueObject => valueObject.Value,
            content => convertor.Convert<TSingleValueObject, TValue>(content));
    }
}