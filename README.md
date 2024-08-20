[![NuGet Package](https://img.shields.io/nuget/v/Valobtify.EntityFrameworkCore)](https://www.nuget.org/packages/Valobtify.EntityFrameworkCore/)

With this package, you can automatically configure your single-value objects for the database, including applying data annotations like MaxLength and performing conversions.
### 1. Install package
  ```bash
  dotnet add package Valobtify.EntityFrameworkCore
  ```


### 2. Setup single value objects 
In your DbContext, override the OnModelCreating method as shown in the following code.
 ```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.SetupSingleValueObjects();

    base.OnModelCreating(modelBuilder);
}
 ```
