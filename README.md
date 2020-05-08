![Nuget](https://img.shields.io/nuget/dt/Mars.Life.Simulations?style=flat-square)
[![pipeline status](https://git.haw-hamburg.de/mars/life/badges/feature/soh-debugging/pipeline.svg)](https://git.haw-hamburg.de/mars/life/-/commits/feature/master)
[![coverage report](https://git.haw-hamburg.de/mars/life/badges/master/coverage.svg)](https://git.haw-hamburg.de/mars/life/-/commits/master)


# MARS Runtime System

The Mars.LIFE project provides agent-based simulation and geographical query processing methods with a set of common mathematical functions and data structures for .NET Core.
Therefore it contains the simulation core of the MARS framework whic executes the simulation with variable stepsize. Currently there are two major versions available: 2.x and 3.x 
It can be used on **Microsoft Windows**, **MacOS**, **Xamarin**, **Unity3D**, **Windows Store applications**, **Linux** or **mobile**.

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


# Documentation Workflow

TODO: how do we decide what is a well formulated question for the documentation and what is not?
Possible considerations:
- Is the abstraction level appropriate for documentation purposes?
- Has a similar/related question already answered in the documentation?
- Others...

Workflow for adding a specific question&answer about C# MARS to documentation:
- A GitLab issue in the issue board for the current LIFE version is created and:
  - given the tag "documentation"
  - assigned to Daniel O & Nima A
- If Daniel O & Nima A can address the issue, they do so
- if Daniel O & Nima A cannot address the issue, the issue is assigned to Daniel G and/or Florian O
- When the issue has been addressed, its status is changed to Resolved
- When the issue has been validated, its status is changed to Closed

Ticket statuses for issues with the tag "documentation":
- Open: issue has been created
- Ready: issue is ready to be worked on
- Active: issue is being worked on
- Resolved: question in issue has been answered and answer needs to be validated and added to documentation (.md file)
- Closed: issue is completed

General discussion about documentation:
The mars-group Slack channel #documentation is used to discuss topics and questions regarding documentation. NOTE: when a question is formulated over the course of a discussion in the channel, it must be transferred into a GitLab issue to initiate the above workflow.