  
![Nuget](https://img.shields.io/nuget/v/Mars.Life.Simulations?label=mars) 

![Nuget](https://img.shields.io/nuget/dt/Mars.Life.Simulations) 

[![pipeline status](https://git.haw-hamburg.de/mars/life/badges/master/pipeline.svg)](https://git.haw-hamburg.de/mars/life/-/commits/master)

<h1  align="center">MARS Runtime System | <a  href="https://mars-group.org">Website</a></h1>

The **MARS runtime system** can be used to design and build agent-based models (ABM) and spatio-temporal processing systems. The framework provides a set of database, indexing and computational components, targeting `netstandard2.0`.

The framework executes time-discrete simulations with variable stepsize and can be integrated in `.NET/.NET Core`, `.NET Framework` and `Xamarin` application. 

## Documentation

Documentation can be found [online here](https://mars.haw-hamburg.de/) and contains the API descriptions and tutorials for constructing and running models.

## Usage

To build the framework, please download and install the newest [.NETCore SDK](https://dotnet.microsoft.com/download) for your operating system.

All modelling components are provided libraries targeting `netstandard2.0` and available via public [nuget](https://www.nuget.org/packages/Mars.Life.Simulations/). 

Create new project (here with the name `MyModelProject`):

```bash
dotnet new console -n MyModelProject
cd MyModelProject
```
This creates a new `.NET/.NETCore` application in a new directory called `MyModelProject`. Within the directory, execute the following command:

```bash
dotnet add MyModelProject package Mars.Life.Simulations
```
This adds the `Mars.Life.Simulations` dependency and all transient required dependencies. Now you can start to build your model.
  
## Examples

Visit the [model repo](https://git.haw-hamburg.de/mars/model-deployments) for sample agent models and public scenarios. Multiple cases are also contained in this repo, showing the basic usage of multiple component.s Look into the _Mars.Test_ project for more.
More ready-to-use scenarios to show some MARS features are described and can  directly downloaded [here](https://mars.haw-hamburg.de/articles/soh/scenarios/index.html).  


## Building

To build the the framework, use the `dotnet` CLI ad `git`.  Clone the repository and naviate into the directory:

```bash 
git clone https://gitlab.informatik.haw-hamburg.de/mars/life.git && cd life
```
Build the framework by calling:
```bash 
dotnet build
```

## Development

When you want to contribute some features or bugfixes, first issue a ticket and design the test case for change.

Prerequisites:
* [.NETCore SDK](https://dotnet.microsoft.com/download)
* [Docker](https://www.docker.com/products/docker-desktop) *for integration tests*

### Development Environment 

Open the `LIFE.sln` solution with preferred IDE such as [Jetbrains Rider](https://www.jetbrains.com/de-de/rider/) or [Mircosoft Visual Studio](https://visualstudio.microsoft.com/de/).
To making all services for testing purposes available, start required **external** services as docker containers:
```bash
docker compose -f Deployments/docker-compose.yaml up -d
```
This will start all required containers for testing all data wrappers.

### Quality Assurance
When you think you are finished, execute your **newly created** and **all existing** tests cases:
```bash 
cd Tests
dotnet test Mars.Tests
```

> Changes are accepted only when new created test are provided and all executed test cases are valid. 

### Development Model and MARS Framework

When developing booth a model and making changes to the framework, you can make the following:
* Build your model as porject part of `LIFE.sln` and use the framework as `project-depdendency`.
* Make your changes in the framework and create a local ``NuGet`` package. Reference the updated version from a local feed.

New package releases are published by executing the `pack.sh` script in the `Build` directory, creating all `NuGet` packages locally for a given version `X.Y.Z` and suffix `my-suffix-name`.
```bash
cd Build
./pack.sh X.Y.Z my-suffix-name
```

To making the created package available, be sure you have changed `~/.nuget/NuGet/NuGet.Config` to see the constructed `Mars.Life.Simulations-XXX` packages. The following configuration contains the default feed and your local one:
```bash
<?xml version="1.0" encoding="utf-8"?>
<configuration>
<packageSources>
	<add key="nuget.org" value="https://api.nuget.org/v3/index.json" 	protocolVersion="3" />
	<add key="MARS Local Feed" value="~/Projects/life/Release" />
</packageSources>
</configuration>
```

# Publish NuGet Packages

To create a new version of the framework, use the `pack.sh` script in the `Build` directory.

```bash
cd Build
./pack.sh X.Y.Z
```

## Create Pre-releases
When only a prerelease version shall be deployed, add the version suffix `beta` to your command:
```bash
./pack.sh X.Y.Z beta
```

Pre-releases are globally available but are not be shown directly to client, only when asking explicitly.
