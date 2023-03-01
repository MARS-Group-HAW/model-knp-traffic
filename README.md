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
    - To spawn agent types at a KNP Gate or Rest camp, the following locations can be entered into the respective scheduler configuration file. Example:
        1. Open the CSV scheduler file of an agent type
        2. Create a new row and specify the temporal attributes and spawning amount as desired
        3. Add the POI name (see table below) in the `gateName` attribute
        4. `source` (optional): Add the WKT geometry of the chosen POI (in general, input geometries are expected in the [well-known text (WKT)](https://en.wikipedia.org/wiki/Well-known_text_representation_of_geometry) representation format)
        5. `destination` (optional): Add a WKT geometry (e.g., a MULTIPOINT containing a subset of the coordinates listed in the table below). The agent chooses a random destination from the options provided in the MULTIPOINT

| POI Type  | POI Name         | Longitude          | Latitude   | WKT geometry                                     |
|-----------|------------------|--------------------|------------|--------------------------------------------------|
| KNP Gate  | Crocodile Bridge | 31.89258657143726  | -25.358480632913864 | "POINT (31.89258657143726 -25.358480632913864)"  |
| KNP Gate  | Malelane         | 31.5322480748238   | -25.462242036021298 | "POINT (31.5322480748238 -25.462242036021298)"   |
| KNP Gate  | Numbi            | 31.19779114628988  | -25.155235225028587 | "POINT (31.19779114628988 -25.155235225028587)"  |
| KNP Gate  | Orpen            | 31.390454263127054 | -24.481379204477278 | "POINT (31.390454263127054 -24.481379204477278)" |
| KNP Gate  | Pafuri           | 31.04128207709203  | -22.399849267439016 | "POINT (31.04128207709203 -22.399849267439016)"  |
| KNP Gate  | Paul Kruger      | 31.484825272079146 | -24.981054292519527  | "POINT (31.484825272079146 -24.981054292519527)" |
| KNP Gate  | Phabeni          | 31.24188273415545  | -25.02502054780226  | "POINT (31.24188273415545 -25.02502054780226)"   |
| KNP Gate  | Phalaborwa       | 31.166061973814458 | -23.94570262899793 | "POINT (31.166061973814458 -23.94570262899793)"  |
| KNP Gate  | Punda Maria      | 31.01048065058071  | -22.737288654024038 | "POINT (31.01048065058071 -22.737288654024038)"  |
| Rest camp | Berg-en-Dal      | 31.444319855824595 | -25.428126851153632 | "POINT (31.444319855824595 -25.428126851153632)" |
| Rest camp | Crocodile Bridge | 31.89330364010209  | -25.358233850884123 | "POINT (31.89330364010209 -25.358233850884123)"  |
| Rest camp | Letaba           | 31.57451374376673  | -23.85430033699618 | "POINT (31.57451374376673 -23.85430033699618)"   |
| Rest camp | Lower Sabie      | 31.91437694895015  | -25.119944502413436 | "POINT (31.91437694895015 -25.119944502413436)"  |
| Rest camp | Mopani           | 31.399132836948997 | -23.521639819788223 | "POINT (31.399132836948997 -23.521639819788223)" |
| Rest camp | Olifants         | 31.7386871449655   | -24.00454966132233 | "POINT (31.7386871449655 -24.00454966132233)"    |
| Rest camp | Orpen            | 31.390454263127054 | -24.481379204477278 | "POINT (31.390454263127054 -24.481379204477278)" |
| Rest camp | Skukuza          | 31.591989637766584 | -24.996507331891323 | "POINT (31.591989637766584 -24.996507331891323)" |
| Rest camp | Shingwedzi       | 31.434153793525685 | -23.107844185540074 | "POINT (31.434153793525685 -23.107844185540074)" |
| Rest camp | Pretoriuskop     | 31.268696006058565 | -25.168990139725594 | "POINT (31.268696006058565 -25.168990139725594)" |
| Rest camp | Punda Maria      | 31.018924936716804 | -22.692239597900695 | "POINT (31.018924936716804 -22.692239597900695)" |
| Rest camp | Satara           | 31.780473375065686 | -24.39220480951074 | "POINT (31.780473375065686 -24.39220480951074)"  |
| Satellite camp | Balule      | 31.733020373834496 | -24.053980346565005 | "POINT (31.733020373834496 -24.053980346565005)" |
| Satellite camp | Malelane    | 31.51273269058665  | -25.470369247759475 | "POINT (31.51273269058665 -25.470369247759475)"  |

The model configuration takes place in the `SimConfig()` method in `Program.cs`. Here, the simulation parameters and input files can be specified in a `SimulationConfig` object.

## Execution
The model can be executed by running the `Program.cs` file.

## Analysis
The model produces one CSV-file per agent type. In addition, one `trips.geojson` file is produced that contains the movement trajectories of each agent. The trajectories can be visualized on [kepler.gl](https://kepler.gl).
- TODO: list geospatial files that are suitable for visualization
