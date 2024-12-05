[![NuGet Package](https://img.shields.io/nuget/v/Valobtify.EntityFrameworkCore)](https://www.nuget.org/packages/Valobtify.EntityFrameworkCore/)

### Overview

`Valobtify.EntityFrameworkCore` is an extension of the `Valobtify` library that simplifies the configuration and persistence of single-value objects in Entity Framework Core. It automates the application of data annotations like `MaxLength` and handles type conversions, making your value objects database-ready with minimal setup.

---

### Installation

To install the `Valobtify.EntityFrameworkCore` package, run the following command in your terminal:

```bash
dotnet add package Valobtify.EntityFrameworkCore
```

Ensure you have the required .NET SDK installed.

---

### Setup Single-Value Objects

To configure single-value objects in your Entity Framework Core `DbContext`, override the `OnModelCreating` method as shown below:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.SetupSingleValueObjects();

    base.OnModelCreating(modelBuilder);
}
```

**Explanation:**
- `SetupSingleValueObjects` automatically applies the necessary configurations for your single-value objects, including data annotations and type conversions.
- By calling this method, you ensure your value objects are properly mapped to the database schema.

