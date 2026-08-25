# Running Tests

The model's tests live in the [`KrugerNationalParkTests`](../Models/KrugerNationalParkTests) project.

To run them, use the IDE-integrated test execution or run the following command:
```bash
dotnet test Models/KrugerNationalParkTests
```

To run a single test, filter by its fully qualified name:
```bash
dotnet test Models/KrugerNationalParkTests --filter "FullyQualifiedName=KrugerNationalParkTests.Travel.FindRoute.TestFindRouteWithAccessAttribute"
```

> **Note**
> `Travel/FindRoute.cs::TestCreateMultipleRandomRoutesOnKNPGraph` runs an effectively unbounded random walk over the real KNP graph and will hang a full test run. Exclude it explicitly when running the whole suite:
> ```bash
> dotnet test Models/KrugerNationalParkTests --filter "FullyQualifiedName!~TestCreateMultipleRandomRoutesOnKNPGraph"
> ```