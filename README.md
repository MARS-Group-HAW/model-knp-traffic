  
![Nuget](https://img.shields.io/nuget/v/Mars.Life.Simulations?label=mars) 
![Nuget](https://img.shields.io/nuget/dt/Mars.Life.Simulations) 
[![coverage report](https://git.haw-hamburg.de/mars/life/badges/master/coverage.svg)](https://git.haw-hamburg.de/mars/life/-/commits/master)
[![pipeline status](https://git.haw-hamburg.de/mars/life/badges/master/pipeline.svg)](https://git.haw-hamburg.de/mars/life/-/commits/master)

<h1  align="center">MARS Runtime System | <a  href="https://mars-group.org">Website</a></h1>

The **Mars runtime system** can be used to design and build agent-based simulation and spatio-temporal processing systems, providing a set of common mathematical functions and data structures for .NET Core.

The framework executes time-discrete simulations with variable stepsize and is designed for cross-plattform execution on **Windows**, **MacOS** and **Linux**. 

## Documentation

Documentation can be found [online here](https://mars.haw-hamburg.de/) and contains the API descriptions and tutorials for constructing and running models. Without knowledge in modelling and software development, domain experts can use the MARS DSL as a simplified modelling language. The documentation of the MARS DSL can be found in an extra [modelling handbook](https://mars-group.org/modeling-handbook/).
  

## Installing

To build the framework, please download and install the newest [.NETCore SDK](https://dotnet.microsoft.com/download) for your operating system.

All modelling components are provided libraries targeting `netstandard2.0` so each version (>= .NETCore 2.0) will work. We use the public [nuget](https://www.nuget.org/packages/Mars.Life.Simulations/) to distribute the framework. 

Create new project (here with the name `MyModelProject`):

```bash
dotnet new console -n MyModelProject
cd MyModelProject
```
This creates .NETCore project in a new directory called `MyModelProject` and navigates into this 
Use the `dotnet cli` in your model project. Navigate to your project directory and execute the following command in a terminal:

```bash
dotnet add MyModelProject package Mars.Life.Simulations
```
This adds the `Mars.Life.Simulations` dependency and all other required ones to the new created `Mars.Life.Simulations` model project.
  
## Examples
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
