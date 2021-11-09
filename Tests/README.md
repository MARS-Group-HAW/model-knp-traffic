# Running Tests

The test for the MARS runtime system are separated into multiple projects.

To run a specific test project use the IDE integrated test execution or run the following command:
```bash
dotnet test Mars.Algebra.Tests
```


## Code Coverage

In order to create a code coverage report, we recommend to use the integrated code coverage functionality, provided by Jetbrains Rider (see here for [help](https://blog.jetbrains.com/dotnet/2018/07/20/unit-test-coverage-continuous-testing-now-rider/))

Another option is to use the already configured [coverlet](https://github.com/coverlet-coverage/coverlet) code coverage tool. Use the following command to create a code coverage report:

```bash
dotnet test Mars.Algebra.Tests -s ./tests.runsettings
```

The `tests.runsettings` file contains all configuration for the test projects, e.g., the logger configuration used to create the report or the target test result directory, which is currently `TestsResult` in the current folder.  

