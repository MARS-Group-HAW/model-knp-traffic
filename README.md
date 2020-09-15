
![Nuget](https://img.shields.io/nuget/v/Mars.Life.Simulations?label=mars)
![Nuget](https://img.shields.io/nuget/dt/Mars.Life.Simulations)
[![pipeline status](https://git.haw-hamburg.de/mars/life/badges/feature/soh-debugging/pipeline.svg)](https://git.haw-hamburg.de/mars/life/-/commits/feature/master)
[![coverage report](https://git.haw-hamburg.de/mars/life/badges/master/coverage.svg)](https://git.haw-hamburg.de/mars/life/-/commits/master)
    
<h1 align="center">MARS Runtime System | <a href="https://mars-group.org">Website</a></h1>    
    
The **Mars runtime system** provides agent-based simulation and geographical query processing methods with a set of common mathematical functions and data structures for .NET Core.
Therefore it contains the simulation core of the MARS framework whic executes the simulation with variable stepsize. It can be used on **Microsoft Windows**, **MacOS**, **Xamarin**, **Unity3D**, **Windows Store applications**, **Linux** or **mobile**.

The framework offers a unified API for buliding agent-based models and let them execute, that is both easy to use *and* extensible.

For more information, please see the [modelling handbook](https://mars-group.org/modeling-handbook/). *Please do not hesitate to edit the handbook if you would like!*


## Installing

To install the framework in your application, please use the public **nuget** feed from Microsoft. Use the **dotnet cli** in your model project:

`dotnet add package Mars.Life.Simulations`


If you are on Visual Studio, right-click on the "References" item in your solution folder, and select "Manage NuGet Packages." Click on the a new nuget settings functions and add the source "https://nexus.informatik.haw-hamburg.de/repository/nuget-group/" to the active sets. 
If you are using Jetbrains Rider, click on NuGet package management below.

Search for **Mars.Life.Simulations** ([or equivalently Mars](https://nexus.informatik.haw-hamburg.de/#browse/search=keyword%3Dmars.life.simulations)) and select "Install."
   
## Sample applications

Visit the [model repo](https://git.haw-hamburg.de/mars/model-deployments) for all sample agent models and public scenarios. Multiple cases are also contained in this repo, showing the basic usage of multiple component.s Look into the _Mars.Test_ project for more. 
  
## Building

To build the solution on your own, please download and install the dotnet SDK dependency:

- [NetCore. 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)

Using the **dotnet** cli execute the following commands: 

### From Command Line Interface

```bash

# Clone the repository
git clone https://gitlab.informatik.haw-hamburg.de/mars/life.git

# Enter the directory
cd life

# Build the framework solution using NETCore in Debug mode
dotnet build -c Debug

# Build the framework solution using NETCore in Release mode
dotnet build -c Release

# Test execution
cd Tests

# Run all tests including long running performance tests and wohle simulation executions
dotnet test 

# Run only the unit test and some basic integration tests
dotnet test --filter Category!=Performance Mars.Tests
```

#### With Visual Studio 2015+ and Jetbrains Rider

Then navigate to the Sources directory, and open the *LIFE.sln* solution file. After your solution is initialized, right on the solution file and click on "Build solution".
To execute the test, select the test function in the menu bar and click on "Run Unit Tests".


## Release

The release build will be handled by the owner of this repository. A framework release is created in form of a NuGet package through the ***Mars.Life.Simulations.nusepc*** file.

To create a local feed and the respective ***Mars.Life.Simulations*** package, run the following command from the root directory of LIFE:

```
# Change the current directory to Setup
cd Build

# Build and pack all component
./pack.sh X.Y.Z test
```

Be sure you've add an entry int your **~/.nuget/NuGet/NuGet.Config** to make your local feed visible. The following configuration contains the default feed and your local one.
```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="MARS Local Feed" value="~/Projects/life/Release" />
  </packageSources>
</configuration>
```

Please be sure that you have enough access to the **pack.sh** script. Otherwise perform the following command:

```
chmod +x pack.sh
```

New dependencies and components which is used by the framework have to be listed in the file tags, including their XML documentation.