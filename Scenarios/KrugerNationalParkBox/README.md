# Kruger National Park Traffic Simulation Model

The Kruger National Park (KNP) traffic simulation model is an agent-based model (ABM) that simulates the daily traffic flow of commuters, visitors, and open safari vehicles (OSVs) in the KNP.

## Model components
The main components of the model can be categorized into agents, entities, layers, and resources.

### Agents

- `Commuter`: a worker who lives outside the KNP and works at a rest camp in the KNP.
  - Summary of daily behavior routine:
    1. Enter the KNP via a KNP gate
    2. Commute by `KnpCar` (see below) to a KNP rest camp
    3. Spend some amount of time at the KNP rest camp
    4. Exit the KNP via the same KNP gate
  - `CommuterState`: an enumeration that contains the possible states of `Commuter` agents based on their current activity in the behavior routine
- `Visitor`: a visitor who drives around the KNP in search for wildlife.
  - Summary of daily behavior routine:
    - Daily visitor:
      1. Enter the KNP via a KNP gate
      2. Drive around randomly for some time, taking breaks at KNP rest camps or other points of interst (POIs)
         - Stop driving for some amount of time when spotting wildlife
      3. Exit the KNP via a KNP gate
    - Overnight visitor:
      1. Enter the KNP via a KNP rest camp
      2. Drive around randomly for somet ime, taking breaks at KNP rest camps or other POIs
         - Stop driving for some amount of time when spotting wildlife
      3. Exit the KNP via the same KNP rest camp or another KNP rest camp
  - `VisitorState`: an enumeration that contains the possible states of `Visitor` agents based on their current activity in the behavior routine
- `OSV`: TBD
- `EventProducer`: an abstract component that generates `KnpEvent` instances (see below) on the KNP traffic network for `Visitor` agents to interact with

### Entities
- `KnpCar`: a car that can be used by `Commuter` agents and `Visitor` agents to move on the KNP traffic network. The first-come-first-served rule is applied to determine who has the right of way at intersections.

### Layers
- `CommuterSchedulingLayer`: spawns a given number of `Commuter` agents at a given frequency at a given location over a given time interval (see Configuration)
- `ProducerSchedulingLayer`: spawns one `EventProducer` agent at the beginning of a simulation
- `POILayer`: holds a set of `KnpPoi` instances (see below) for `Visitor` agents to interact with
- `VisitorSchedulingLayer`: spawns a given number of `Visitor` agents at a given frequency at a given location over a given time interval (see Configuration)
- `VisitorTravelerLayer`: calculates routes for `Visitor` agents, making sure that they can satisfy time constraints (e.g., closing hours of KNP)
- `SpatialGraphMediatorLayer`: holds the KNP travel network for `Commuter` agents and `Visitor` agents to drive on

### Resources
- `KnpPoi`: a POI in the KNP and a potential travel destination of `Visitor` agents
- `KnpEvent`: a marker on the KNP traffic network that represents a wildlife sighting. `Visitor` agents stop and observe for some amount of time when spotting an event

## Data integration
The following data are integrated into the KNP at runtime.
- `roads_all_2019_inferred.geojson`: a geospatial representation of the KNP road network, provided by SANParks.
- `pois_inferred.geojson`: a geospatial representation of the POIs (gates, rest camps, and other locations) within the KNP
- Additionally, there are gate entry data, gate quota data, OSV permit data, and rest camp capacity and occupancy data provided by SANParks (see the `sanparks_res` directory). These data can be used to spatially distribute the spawn locations of agents and the camp occupancy distribution.

## Configuration
The following files can be edited to configure the model:
- `car.csv`: used to configure `KnpCar` entities that are used by agent types
- Agent configuration files and scheduling files:
  - The following files serve to define spawn intervals and frequencies as well as spawn locations and attributes of agent types. For information on the required temporal and spatial parameters, please see the [scheduling layer documentation](https://mars.haw-hamburg.de/articles/soh/layers/scheduling_layer.html). In addition to the required scheduling parameters, the following agent-specific parameters can be set in the following files:
  - `CommScheduler.csv`: used to schedule `Commuter` spawns
    - `workDuration`: the amount of time (in minutes) that a `Commuter` agent will spend at its place of work
  - `VisitorScheduler.csv`: used to schedule `Visitor` spawns
  - `ProducerScheduler.csv`: used to schedule `Producer` spawns
  - To spawn agent types at a KNP gate or KNP rest camp, the following locations can be entered into the respective scheduler configuration file. Example: to spawn `Commuter` agents at the Crocodile Bridge Gate
    1. Open `CommScheduler.csv`
    2. Create a new row and specify the temporal attributes and spawning amount as desired
    3. Add `Crocodile Bridge Gate` in the `gateName` attribute
    4. Add `"POINT (31.893563,-25.358438)"` in the `source` attribute (in general, input geometries are expected in the [well-known text (WKT)](https://en.wikipedia.org/wiki/Well-known_text_representation_of_geometry) representation format)
    5. `destination`: add a WKT geometry (e.g., a MULTIPOINT containing a subset of the coordinates listed in the table below). The agent chooses a random destination from the options provided in the MULTIPOINT

<center>

| POI Type  | POI Name              | Longitude | Latitude   | WKT geometry                   |
|-----------|-----------------------|-----------|------------|--------------------------------|
| Gate      | Crocodile Bridge Gate | 31.893563 | -25.358438 | "POINT (31.893563,-25.358438)" |
| Gate      | Giriyondo Gate        | 31.659575 | -23.584323 | "POINT (31.659575,-23.584323)" |
| Gate      | Kruger Gate           | 31.484812 | -24.980938 | "POINT (31.484812,-24.980938)" |
| Gate      | Malelane Gate         | 31.532321 | -25.462187 | "POINT (31.532321,-25.462187)" |
| Gate      | Numbi Gate            | 31.198188 | -25.155313 | "POINT (31.198188,-25.155313)" |
| Gate      | Orpen Gate            | 31.390833 | -24.475833 | "POINT (31.390833,-24.475833)" |
| Gate      | Pafuri Gate           | 31.041389 | -22.400278 | "POINT (31.041389,-22.400278)" |
| Gate      | Phabeni Gate          | 31.240647 | -25.02469  | "POINT (31.240647,-25.02469)"  |
| Gate      | Phalaborwa Gate       | 31.165687 | -23.945687 | "POINT (31.165687,-23.945687)" |
| Gate      | Punda Maria Gate      | 31.010438 | -22.737313 | "POINT (31.010438,-22.737313)" |
| Rest camp | Berg-en-Dal           | 31.445044 | -25.427937 | "POINT (31.445044,-25.427937)" |
| Rest camp | Crocodile Bridge      | 31.893852 | -25.358176 | "POINT (31.893852,-25.358176)" |
| Rest camp | Letaba                | 31.574732 | -23.854036 | "POINT (31.574732,-23.854036)" |
| Rest camp | Lower Sabie           | 31.916231 | -25.119539 | "POINT (31.916231,-25.119539)" |
| Rest camp | Malelane              | 31.511609 | -25.476576 | "POINT (31.511609,-25.476576)" |
| Rest camp | Mopani                | 31.397381 | -23.521428 | "POINT (31.397381,-23.521428)" |
| Rest camp | Olifants              | 31.740904 | -24.005762 | "POINT (31.740904,-24.005762)" |
| Rest camp | Orpen                 | 31.390995 | -24.475490 | "POINT (31.390995,-24.475490)" |
| Rest camp | Skukuza               | 31.592347 | -24.996215 | "POINT (31.592347,-24.996215)" |
| Rest camp | Shingwedzi            | 31.436037 | -23.108628 | "POINT (31.436037,-23.108628)" |
| Rest camp | Balule                | 31.733793 | -24.053363 | "POINT (31.733793,-24.053363)" |
| Rest camp | Pretoriuskop          | 31.017207 | -22.691722 | "POINT (31.017207,-22.691722)" |
| Rest camp | Punda Maria           | 31.445044 | -25.427937 | "POINT (31.445044,-25.427937)" |
| Rest camp | Satara                | 31.779862 | -24.393159 | "POINT (31.779862,-24.393159)" |

</center>

The model configuration takes place in the `SimConfig()` method in `Program.cs`. Here, the simulation parameters and input files can be specified in a `SimulationConfig` object.
 
## Execution
The model can be executed by running the `Program.cs` file.

## Analysis
The model produces one CSV-file per agent type. In addition, one `trips.geojson` file is produced that contains the movement trajectories of each agent. The trajectories can be visualized on [kepler.gl](kepler.gl).
- TODO: list geospatial files that are suitable for visualization
