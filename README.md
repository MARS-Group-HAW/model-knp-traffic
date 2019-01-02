

[![status](https://gitlab.informatik.haw-hamburg.de/mars/life/badges/LIFE-v3.x-dev/pipeline.svg)](https://gitlab.informatik.haw-hamburg.de/mars/life/commits/LIFE-v3.x-dev)

# LIFE

The Mars.LIFE project provides agent-based simulation and geographical query processing methods with a set common mathematical functions and data structures for .NET.
Therefore it contains the simulation core of the MARS framework whic executes the simulation with variable stepsize. Currently there are two major versions available: 2.x and 3.x 
It can be used on Microsoft Windows, Xamarin, Unity3D, Windows Store applications, Linux or mobile.

The framework offers a unified API for buliding agent-based models and let them execute, that is both easy to use *and* extensible.

For more information, please see the [modelling handbook](https://mars-group.org/modeling-handbook/). *Please do not hesitate to edit the handbook if you would like!*

# Installing

To install the framework in your application, please use NuGet with our own [MARS Package Source](https://nexus.informatik.haw-hamburg.de/repository/nuget-group/).
- If you are on Visual Studio, right-click on the "References" item in your solution folder, and select "Manage NuGet Packages." Click on the a new nuget settings functions and add the source "https://nexus.informatik.haw-hamburg.de/repository/nuget-group/" to the active sets. 
- If you are using Jetbrains Rider, click on NuGet package management below.

Search for **Mars.Life.Simulations** ([or equivalently Mars](https://nexus.informatik.haw-hamburg.de/#browse/search=keyword%3Dmars.life.simulations)) and select "Install."
   
## Sample applications

The framework does provide some basic sample models for own development. Therefore look into the Test/Mars.Test/SimulationTests/ folder for more information.
  
  
# Building

To build the solution on your own, please download and install the following dependency:

- [NetCore. 2.1](https://dotnet.microsoft.com/download/dotnet-core/2.1)

#### With Visual Studio 2015+ and Jetbrains Rider

Then navigate to the Sources directory, and open the *LIFE.sln* solution file. After your solution is initialized, right on the solution file and click on "Build solution".

To execute the test, select the test function in the menu bar and click on "Run Unit Tests".

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

# Release

The release build will be handled by the owner of this repository. A framework release is created in form of a NuGet package through the ***Mars.Life.Simulations.nusepc*** file.

To create a local feed and the respective ***Mars.Life.Simulations*** package, run the following command from the root directory of LIFE:

```
# Change the current directory to Setup
cd Setup

# Enable execution right to the scripts if not already applied
chmod +x clean-all.sh 
chmod +x build-projects.sh 
chmod +x create-packages.sh 

# Clean all packages including temporary files and old binaries
./clean-all.sh

# Build the whole solution as an release build and copy the release targets to the folder <project_root>/Release
./build-projects.sh

# Create the NuGet package from the release folder and mark the package with the version from the version.txt and as a preview
./create-packages.sh

``` 

New dependencies and components which is used by the framework have to be listed in the file tags, including their XML documentation.

 
## LIFE v2.x

This project has been developed over the past years and is in a stable state. This project will receive bug fixes in the future but will not be developed further since all new functionality will be added to version 3.x

Reasons for moving on from this version were feature wishes that couldn't be integrated without undergoing major changes that would have jeopardised having a stable system. Therefore it was decided to do this in version 3.x and to leave version 2.x in its current state.

## LIFE v3.x

New functionality will flow into this version of LIFE. The Smart Open Hamburg project as well as the EMSAfrica project will be based on this version. The planned core innovations for this evolution of LIFE include:

* Local execution: Simulations based on LIFE 3.x will be executable in the cloud as well as on your local machine
* Decision support systems: run simulations as basis for decision support system and interact with the running simulation
* Distribution: Execute simulations in parallel on multiple nodes to gain performance

### Development for LIFE v3.x

Some ground rules for developing the project. This mostly concerns the dealings with Git and branching:

* life-v3.x-master is the master branch of version 3. No commits can happen to this branch without merge requests. The branch is protected and should only be used to do releases (3.0, 3.1, 3.2 etc.). Everything concerning a release must be discussed with Thomas, Daniel or Julius first
* life-v3.x-dev is the development branch which should always be kept in a state where whatever is on there works. Every developer can commit to this branch but should only do so if the developed features/ fixes actually work
* feature/ fix branches: whenever you develop new functionality please do that on a separate branch with a self-explaining name. Same goes for fix/ hotfix branches. 
* If the branching rules are being ignored, the development branch will be protected as well so that you cannot push anymore and everything has to go through merge requests. This is painful and nobody wants this to happen so please comply to the rules
