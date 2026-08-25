# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

An agent-based model (ABM) of visitor and traffic management in Kruger National Park (KNP), built on the [MARS Framework](https://www.mars-group.org/docs/tutorial/intro) (Mars.Life.Simulations / Mars.Life.SOH NuGet packages). Developed with SANParks. The [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) is the authoritative spec for model behavior and configuration — consult its section numbers (referenced in code comments and README) before changing agent/layer logic.

## Solution layout

`KNPTrafficModel.sln` has three projects:

- `Models/KrugerNationalPark` — the model library (netstandard2.0). All agent, layer, and domain logic lives here.
- `Models/KrugerNationalParkTests` — xUnit tests (net10.0) for the model library.
- `Scenarios/KrugerNationalParkBox` — the runnable executable (net10.0) that wires up `Program.cs`, ships `config.json` and the `resources/` data files, and can be packaged into a standalone "box".

## Common commands

```bash
# Build everything
dotnet build

# Run the simulation (from Scenarios/KrugerNationalParkBox — needs config.json and resources/ alongside the binary)
dotnet run --project Scenarios/KrugerNationalParkBox

# Run all tests
dotnet test Models/KrugerNationalParkTests

# Run a single test (by fully qualified name or filter)
dotnet test Models/KrugerNationalParkTests --filter "FullyQualifiedName=KrugerNationalParkTests.Travel.FindRoute.TestFindRouteWithAccessAttribute"

# Build the distributable "box" archives (macOS/Windows/Linux self-contained publishes) for KrugerNationalParkBox
./Scenarios/build.sh
# or build.sh <modelname> to target one project only
```

`Program.cs` also supports two utility CLI flags instead of running a full simulation:
- `-poi` — runs `GetRouteTimings.Timings()` (POI-to-POI route/timing report).
- `-infergraph` — re-infers road network intersections from `resources/roads_all_2019.geojson` and writes `resources/roads_all_2019_inferred.geojson`.
- `-l` — turns on console logging during a normal run.

`global.json` pins the .NET SDK to `10.0.100` with `rollForward: latestMajor`.

## Architecture

The model follows MARS's layer/agent/entity pattern. Everything is registered in `Scenarios/KrugerNationalParkBox/Program.cs::Main`, which is the best starting point for understanding how pieces connect.

**Environment layers** (`Models/KrugerNationalPark/Layers/`):
- `KnpRoadNetwork` (extends `SpatialGraphMediatorLayer`) — the road graph. Exposes `FindVisitorRoute`/`FindOsvRoute` (wrap the generic `FindRoute` with access-permission filters — `Public` for visitors, `Public`+`Staff` for OSVs) and enforce a time-limit-constrained A* search over edge `ACCESS` attributes.
- `PointsOfInterest` (a `VectorLayer<KnpPoi>`) — holds `KnpPoi` destinations (gates, rest camps, etc., typed via `Misc/PoiType.cs`); queried by type and/or geometry for agent spawning/routing.
- `TrafficGrid`, `TrafficJamGrid`, `SightingsGrid` — raster output layers accumulating movement density, jam locations/duration, and wildlife-sighting locations/duration respectively over the simulation.
- `*Scheduler` layers (`VisitorScheduler`, `CommuterScheduler`, `OsvTourGuideScheduler`, `EventProducerScheduler`) — thin `AgentSchedulerLayer<TAgent, KnpRoadNetwork>` subclasses; spawn timing/origin/destination per agent instance is driven entirely by the paired CSV file in `resources/` (see Scheduler Configuration in README), not by code in these classes.

**Agents** (`Models/KrugerNationalPark/Agents/`):
- `Visitor` — drives around searching for wildlife; picks source/target POIs, acquires a `KnpCar`, and calls `KnpRoadNetwork.FindRoute` under a time budget.
- `Commuter` — lives outside the park, commutes to work at a rest camp, stays `workDuration` minutes.
- `OsvTourGuide` — drives an Open Safari Vehicle; has broader road access (`Staff` + `Public`).
- `EventProducer` — creates and manages `KnpEvent` wildlife-sighting events along the road network (consumed by `Misc/Events/*`, e.g. `SightingEvent`, `VisitorEventComponent`, `OsvTourGuideEventComponent`).
- `KnpCar` — the vehicle entity agents drive (`IAgent`/entity via `EntityManager.Create<KnpCar>`, configured by `resources/car.csv`).

Each driving agent implements `IAgent<KnpRoadNetwork>` + `ICarSteeringCapable`, follows an `Init(layer)` → per-tick `Tick()` lifecycle typical of MARS agents, and tracks its own state enum (`VisitorState`, `CommuterState`, `OsvTourGuideState` in `Misc/`).

**Misc** (`Models/KrugerNationalPark/Misc/`): domain value types (`Poi`, `PoiType`, `RoadAccess`, `RoadSurface`, `TripOrigin`/`TripDestination`), per-agent state enums, and two event subsystems:
- `Misc/Events/` — KNP domain events (wildlife sightings, social media events) and the components agents use to react to them.
- `Misc/EventsMars/` — a generic pub/sub event framework (`MarsEvent`, `IEventHandler`, subscription exceptions) that `Misc/Events` builds on.

**Configuration**: everything runtime-tunable is driven by `Scenarios/KrugerNationalParkBox/config.json` plus the CSV/GeoJSON/ASC files it references in `resources/` — not by code changes. Preconfigured scenario/configuration file sets live under `resources/scenario_configs/`. See the README's Configuration section and final report §3.3 for the full schema (layer, entity, and scheduler config blocks).

**Data prep**: Jupyter notebooks in `Scenarios/KrugerNationalParkBox/` (e.g. `SchedulerCSVPrep.ipynb`, `Commuter.ipynb`, `Visitor.ipynb`, `VisitorEventBraking.ipynb`) and `Scenarios/KrugerNationalParkBox/sanparks_res/` process raw SANParks datasets into the GeoJSON/CSV inputs the model consumes. `POI Layer Timings.ipynb` and `Prepare POI Layer.ipynb`/`Scheduler.ipynb` are marked deprecated in the README — don't extend them.

**Outputs**: per-agent-type CSV/trip-GeoJSON, the three heatmap grids as CSV, and (if `WriteRouteAsGeoJSON` is set for `Visitor`/`OsvTourGuide`) per-agent route JSON. Intended for visualization in [kepler.gl](https://kepler.gl) — see README's Output Analysis section.

## Notes when editing

- `Models/KrugerNationalParkTests/ResourcesConstants.cs` references resource paths (ferry/bicycle/traffic-light data) from an unrelated MARS example project and is largely dead code for this repo — don't treat it as a guide to this project's actual test resources, which live under `Models/KrugerNationalParkTests/resources/`.
- Several test files (`Travel/FindRoute.cs` and others) contain commented-out and TODO-marked blocks left over from a refactor of `KnpRoadNetwork` from `AbstractLayer` to `SpatialGraphMediatorLayer`; treat surrounding live code, not the comments, as ground truth.
- That refactor left `Models/KrugerNationalParkTests/Travel/FindRoute.cs` with several pre-existing, currently-failing tests: tests that construct `new KnpRoadNetwork()` directly without going through `InitLayer` hit a `NullReferenceException` inside `FindRoute` (e.g. `TestTimeLimitAB`), and `TestFindRouteWithAccessAttributeOnVisitorLayer` throws `KeyNotFoundException` because it sets edge attribute key `"access"` while `KnpRoadNetwork.FindVisitorRoute`/`FindOsvRoute` read `"ACCESS"`. Separately, `TestCreateMultipleRandomRoutesOnKNPGraph` runs an effectively unbounded random walk over the real KNP graph (its own comment: "loop, never stops") and will hang `dotnet test` — exclude it explicitly, e.g. `--filter "FullyQualifiedName!~TestCreateMultipleRandomRoutesOnKNPGraph"`, when running the full suite.
