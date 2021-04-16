  
![Nuget](https://img.shields.io/nuget/v/Mars.Life.Simulations?label=mars)

![Nuget](https://img.shields.io/nuget/dt/Mars.Life.Simulations)

[![pipeline status](https://git.haw-hamburg.de/mars/life/badges/feature/soh-debugging/pipeline.svg)](https://git.haw-hamburg.de/mars/life/-/commits/feature/master)

[![coverage report](https://git.haw-hamburg.de/mars/life/badges/master/coverage.svg)](https://git.haw-hamburg.de/mars/life/-/commits/master)

<h1  align="center">MARS Runtime System | <a  href="https://mars-group.org">Website</a></h1>

The **Mars runtime system** provides agent-based simulation and geographical query processing methods with a set of common mathematical functions and data structures for .NET Core.

Therefore it contains the simulation core of the MARS framework whic executes the simulation with variable stepsize. It can be used on **Windows**, **MacOS**, **Linux** and in conjunction with other popular frameworks such as rendering engines **Unity3D** or **Veldrid** or just for geographic and temporal data processing. The framework offers a unified API for modelling and agent-based systems and process spatio-temporal data from heterogenous data sources.

## Documentation

All documentation about the framework can be found [here](https://mars.haw-hamburg.de/).
For domain-experts without knowledge in modelling and software development we provide the MARS DSL,  an easy to use modelling language. For more information, please use the extra [modelling handbook](https://mars-group.org/modeling-handbook/)
  

## Installing

To build the solution on your own, please download and install the newest [.NETCore SDK](https://dotnet.microsoft.com/download) for your operating system.

All modelling components are provided as`netstandard2.0`  packages from [nuget](https://www.nuget.org/packages/Mars.Life.Simulations/). To use them as dependencies in your application, please use the public `nuget` feed from Microsoft. Use the **dotnet cli** in your model project. Navigate to your project directory and execute the following command a command terminal:

```bash
dotnet add package Mars.Life.Simulations
```

This adds the `Mars.Life.Simulations` dependency and all other required ones to your .NETCore application.
  
## Sample Models
Visit the [model repo](https://git.haw-hamburg.de/mars/model-deployments) for all sample agent models and public scenarios. Multiple cases are also contained in this repo, showing the basic usage of multiple component.s Look into the _Mars.Test_ project for more.
More ready-to-use scenarios to show some MARS features are described and can  directly downloaded [here](https://mars.haw-hamburg.de/articles/soh/scenarios/index.html).  


## Building
To build the the **Mars runtime system** use the `dotnet` CLI ad `git` .

Clone the repository and naviate into the directory:
```bash 
git clone https://gitlab.informatik.haw-hamburg.de/mars/life.git
cd life
```
Build the framework solution using `dotnet cli`:
```bash 
dotnet build
```

Execute all tests by navigating into the directory and calling the test execution. This execute all *MARS* specific tests:
```bash 
cd Tests
dotnet build
dotnet test
```
 
 ## Development

When you want to contribute some features and further refine the system, open the `LIFE.sln` file in with an suiteable IDE. We recommend to use [Jetbrains Rider](https://www.jetbrains.com/de-de/rider/) or [Mircosoft Visual Studio](https://visualstudio.microsoft.com/de/).

### Local Development

When developing something for **Mars runtime system** according to a given model which not contained in this repository, it is required to pulblish the package first on your local system. 

New package releases are publishd by executing the `pack.sh` script in the `Build` directory. This creates all required Nuget packages and assings them with given version number and optional suffix (e.g., *beta*).
```bash
cd Build
./pack.sh X.Y.Z test
```

Be sure you've add an entry int your **~/.nuget/NuGet/NuGet.Config** to see the constructed `Mars.Life.Simulations-XXX` packages on your local system. The following configuration contains the default feed and your local one.
```bash
<?xml version="1.0" encoding="utf-8"?>
<configuration>
<packageSources>
	<add key="nuget.org" value="https://api.nuget.org/v3/index.json" 	protocolVersion="3" />
	<add key="MARS Local Feed" value="~/Projects/life/Release" />
</packageSources>
</configuration>
```
