# Kruger National Park Visitor and Traffic Management Model

The Kruger National Park (KNP) visitor and traffic management model is an agent-based model (ABM) developed with the [MARS Framework](https://www.mars-group.org/docs/tutorial/intro). It enables the configuration and simulations of traffic flow scenarios in the KNP. The current prototype was developed as part of a joint project with [South African National Parks (SANParks)](https://sanparks.org).

## Model Documentation

At the conclusion of the joint project with SANParks, a [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) was prepared that describes the functionality and configurability of the current prototype in great detail. Throughout this README, sections of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) are referred to as a source of further information.

## Model Usage

The model can be used in two ways, which are described below.

### Simulation Box

To run simulation scenarios without having to access the source code of the model, use one of the provided simulation boxes.

1. Navigate to the [latest release](https://github.com/MARS-Group-HAW/model-knp-traffic/releases/tag/v0.1.0) of the model (in the right side bar of the GitHub repository homepage under the **Release** section).
2. Download the ZIP archive that applies to your operating system (Linux, macOS, or Windows).
3. After the download has finished, unzip the ZIP archive.
4. In the unzipped directory, double-click the executable `KrugerNationalParkBox` to run a simulation scenario of the model with the default configuration.

> **Note**  
> See [Configuration](#configuration) in this README and Section 3.3 of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for an overview of the configuration options of the model.

> **Note**  
> See [Output Analysis](#output-analysis) in this README for an overview of the output formats of the model and how to use and view them.

> **Note**  
> To build a simulation box locally, use the provided Shell script [`build.sh`](./Scenarios/build.sh). This will produce the ZIP archives that are available for download via the GitHub repository directly on your local machine.

### Development

To develop the model, you can set it up locally and access its source code. For this, the following technologies are required:

- [.Net SDK](https://dotnet.microsoft.com/en-us/download)
- Recommended: a .NET IDE ([JetBrains Rider](https://www.jetbrains.com/rider/) or [Visual Studio](https://visualstudio.microsoft.com/))

To set up the project locally, follow these steps:

1. Clone or download the GitHub repository
   - Clone: `git clone https://github.com/MARS-Group-HAW/model-knp-traffic.git`
   - Download: Click the "Code" button on the GitHub page of the repository and select "Download ZIP"
2. Open the [Solution file](./KNPTrafficModel.sln) of the project in your preferred IDE

## Model Components

The main components of the model can be categorized into the following categories: environment, wildlife, entities, agents, model output, and schedulers.

> **Note**  
> See the [MARS Framework documentation](https://www.mars-group.org/docs/tutorial/intro) for general information on modelling and simulation with MARS.

### Environment

The georeferenced environment of the mdoel consists of the following two layer types.

- `KnpRoadNetwork`: This layer type holds the road network of the KNP and provides route finding services to agents.
- `PointsOfInterest`: This layer type holds a set of `KnpPoi` instances for agents to interact with.
  - `KnpPoi`: This is a POI in the KNP and a potential travel destination of agents.

See Section 3.1.1 of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for more details.

### Wildlife

Wildlife occurrences are modelled as temporary events of the type `KnpEvent` along the `KnpRoadNetwork`. These events are created and managed by the `EventProducer`.

See Section 3.1.2 of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for more details.

### Entities

The model features an entity type `KnpCar` that can be used by agents move on the `KnpRoadNetwork`.

See Section 3.1.3 of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for more details.

### Agents

The model features the following agent types:

- `Commuter`: a worker who lives outside the KNP and works at a rest camp in the KNP.
- `Visitor`: a visitor who drives around the KNP in search for wildlife.
- `OSV`: a driver of an Open Safari Vehicle (OSV).

See Section 3.1.4 of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for more details.

### Heatmap Output

The model produces three heatmaps over the `KnpRoadNetwork` as part of its output, containing the following information:

- `TrafficGrid`: movement density on the `KnpRoadNetwork`
- `TrafficJamGrid`: location and duration of traffic jams
- `SightingsGrid`: location and duration of wildlife sightings

See Section 3.1.5 of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for more details.

### Schedulers

The model features a set of schedulers that can be configured to spawn agents at specific locations and times. Each agent type has its own scheduler:

- `CommuterScheduler`: spawns `Commuter` agents.
- `VisitorScheduler`: spawns `Visitor` agents.
- `OsvTourGuideScheduler`: spawns `OsvTourGuide` agents.

See [Configuration](#configuration) in this README and Section 3.1.6 of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for more details.

## Data Integration

Various georeferenced datasets provided by SANParks are integrated into the model to populate and inform the environment. The raw versions of these datasets are stored in the directory [`sanparks_res`](./Scenarios/KrugerNationalParkBox/sanparks_res/).

See Section 3.2 of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for more details.

### Data Preprocessing and Verification

Some Jupyter notebooks are available in the project in the directory [`KrugerNationalParkBox`](./Scenarios/KrugerNationalParkBox/). Below is a brief description of each notebook:

- [SchedulerCSVPrep.ipynb](./Scenarios/KrugerNationalParkBox/SchedulerCSVPrep.ipynb): Programmatic Preparation of scheduler CSV files
- [Commuter.ipynb](./Scenarios/KrugerNationalParkBox/Commuter.ipynb): Visualisation of driving speed of `Commuter` agents
- [POI Layer Timings.ipynb](./Scenarios/KrugerNationalParkBox/POI%20Layer%20Timings.ipynb): Tabular representation of route distance and travel duration between pairwise KNP Gate and Rest camp POIs
- [Prepare POI Layer.ipynb](./Scenarios/KrugerNationalParkBox/Prepare%20POI%20Layer.ipynb): deprecated
- [Scheduler.ipynb](./Scenarios/KrugerNationalParkBox/Scheduler.ipynb): deprecated calculations of route distances and travel durations
- [Visitor.ipynb](./Scenarios/KrugerNationalParkBox/Visitor.ipynb): Visualisation of driving speed of `Visitor` agents
- [VisitorEventBraking.ipynb](./Scenarios/KrugerNationalParkBox/VisitorEventBraking.ipynb): verification of agent behaviour upon wildlife sighting (brake and stop driving for some time)
- Jupyter Notebooks in directory [`sanparks_res`](./Scenarios/KrugerNationalParkBox/sanparks_res/): exploration, processing, and analysis of datasets provided by SANParks

## Configuration

The main configuration file of the model is the JSON file [`config.json`](./Scenarios/KrugerNationalParkBox/config.json). This JSON file contains blocks (JSON keys) for configuring layers, entities, and agents. In these blocks, auxiliary configuration files are referenced which are located in the directory [`resources`](./Scenarios/KrugerNationalParkBox/resources/). Below, the configuration options are described and the default auxiliary files are named.

See Section 3.3 of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for an overview of the configuration options of the model.

### Preconfigured Scenarios

The directory [`scenario_configs`](./Scenarios/KrugerNationalParkBox/resources/scenario_configs/) contains configuration files for two scenarios: [scenario1](./Scenarios/KrugerNationalParkBox/resources/scenario_configs/scenario1/) and [scenario2](./Scenarios/KrugerNationalParkBox/resources/scenario_configs/scenario2/). Each scenario is ready to be run in two configurations: `configuration1` and `configuration2`.

> **Note**  
> See Section 4 of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for an overview of the two scenarios and details on their configuration.

To run a configuration of one of these scenarios, follow these steps:

1. Navigate to the directory of the desired scenario.
2. Move the `config.json` file of the scenario to the directory [`KrugerNationalParkBox`](./Scenarios/KrugerNationalParkBox/). Be sure not to accidentally overwrite any files that have the same name.
3. Move the files contained in the configuration directory to the directory [`resources`](./Scenarios/KrugerNationalParkBox/resources/). Be sure not to accidentally overwrite any files that have the same name.

### Layer Configuration

The layer configuration section of the `config.json` file contains configuration options for the environment (see [Environment](#environment) in this README), the raster data model output (see [Heatmap Output](#heatmap-output) in this README), and the schedulers (see [Schedulers](#schedulers) in this README).

See Section 3.3.2 of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for more details.

#### Environment Configuration

The environment configuration enables the specification of georeferenced datasets for the `KnpRoadNetwork` layer and the `PointsOfInterest` layer. Each layer type requires a GeoJSON file. The default GeoJSON file is located [here](./Scenarios/KrugerNationalParkBox/resources/roads_all_2019_inferred.geojson) and [here](./Scenarios/KrugerNationalParkBox/resources/pois_inferred.geojson), respectively.

#### Model Output Configuration

The model output configuration enables the specification of raster layer files that are used by the `TrafficGrid`, `TrafficJamGrid`, and `SightingsGrid` (see [Heatmap Output](#heatmap-output) in this README) to track information about the traffic on the `KnpRoadNetwork` during a simulation. Each layer requires an ASC file. The default ASC file for each layer is located [here](./Scenarios/KrugerNationalParkBox/resources/knp_raster_1111m.asc).

#### Scheduler Configuration

The scheduler configuration (see [Schedulers](#schedulers) in this README) enables the specification of spawn periods for each agent type (see [Agents](#agents) in this README). The `CommuterScheduler`, `VisitorScheduler`, and `OsvTourGuideScheduler` can be used to specify spawn periods for `Commuter`, `Visitor`, and `OsvTourGuide` agents, respectively.

> **Note**  
> See Section 3.1.6 of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for an overview of spawn periods and spawn events.

Each scheduler requires a CSV file. The default CSV files are located [here](./Scenarios/KrugerNationalParkBox/resources/CommuterScheduler.csv), [here](./Scenarios/KrugerNationalParkBox/resources/VisitorScheduler.csv), and [here](./Scenarios/KrugerNationalParkBox/resources/OsvTourGuideScheduler.csv), respectively. The parameters listed in the following table can be set via the scheduler CSV file of each agent type. Parameters marked with &check; are required, whereas parameters marked with &#10005; are optional.

| **Name**         | **Type**   | **Description**                                        | **Required?** |
|------------------|------------|--------------------------------------------------------|:-------------:|
| `sourceName`     | String     | Name of the POI at which agents spawn                  | &check;       |
| `sourceType`     | String     | Type of the POI specified in `sourceName`               | &check;       |
| `sourceGeometry` | WKT String | Geometry that contains POIs at which agents can spawn  | &#10005;      |
| `targetName`     | String     | Name of the POI to which agents travel                 | &#10005;      |
| `targetType`     | String     | Type of the POI specified in `targetName`               | &#10005;      |
| `targetGeometry` | WKT String | Geometry that contains POIs to which agents can travel | &#10005;      |

To specify a new spawn period for an agent type, follow these steps:

1. Open the CSV file of the scheduler of the agent type
2. Create a new row in the CSV file
3. Populate the row with values corresponding to the attributes in the column header

See Appendix A and Appendix B of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for aggregated information on the POIs that are available in the model environment. In addition, see the supplementary documentation file [KNP POI document](./Documentation/knp_pois.pdf) for a comprehensive listing of all individual POIs that are available in the model and that can be used as values for the parameters `sourceName`, `sourceType`, `targetName`, and `targetType`.

> **Note**  
> See [this](https://en.wikipedia.org/wiki/Well-known_text_representation_of_geometry) Wikipedia entry for an overview of WKT geometries and WKT-formatted strings.

### Entity Configuration

The `KnpCar` (see [Entities](#entities) in this README) requires a CSV file. The default CSV file is [`car.csv`](./Scenarios/KrugerNationalParkBox/resources/car.csv).

See Section 3.3.3 of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for more details.

### Agent Configuration

The parameters of a spawn period that apply to all agent types are listed in the above table (see [Scheduler Configuration](#scheduler-configuration) in this README). In addition, parameters specific to each agent type can also be set via the respective scheduler.

#### `Commuter`

The parameters listed in the following table are specific to `Commuter` agents.

| **Name**       | **Type** | **Description**                                                        | **Required?** |
|----------------|----------|------------------------------------------------------------------------|:-------------:|
| `workDuration` | String   | Amount of time, in minutes, that the agent spends at its place of work | &check;       |

#### `Visitor`

In `config.json`, the flag `WriteRouteAsGeoJSON` can be set to `true` to produce one GeoJSON file per agent containing the movement trajectory of that agent.

#### `OsvTourGuide`

In `config.json`, the flag `WriteRouteAsGeoJSON` can be set to `true` to produce one GeoJSON file per agent containing the movement trajectory of that agent.

## Model Execution

The model can be run via the [`Program.cs`](./Scenarios/KrugerNationalParkBox/Program.cs) file. To run the model with an IDE (e.g., JetBrains Rider or Microsoft Visual Studio), open the project and execute the Program.cs file with the Run button of the IDE.

Alternatively, run the model via the `dotnet` CLI:

1. Build the project: `dotnet build`
2. Run the project: `dotnet run`

## Model Outputs

Depending on the simulation configuration (see [Configuration](#configuration) in this README), the model produces the following outputs:

- One CSV file per agent type (named `<AgentType>.csv`), which contains each agent's state per simulation step.
- One GeoJSON file per agent type (named `<AgentType>_trips.geojson`), which contains each agent's travel trajectory.
- Three GeoJSON files named `TrafficGrid.csv`, `TrafficJamGrid.csv`, and `SightingsGrid.csv` (see [Heatmap Output](#heatmap-output) in this README).
- One JSON file per `Visitor` agent and/or `OsvTourGuide` agent (named `route_<AgentID>.json`) containing the agent's movement trajectory (see [Visitor](#visitor) and [OsvTourGuide](#osvtourguide) in this README).

## Output Analysis

The travel trajectories and the heatmaps can be visualised with [kepler.gl](https://kepler.gl). To do so, follow these steps:

1. In a web browser, open [kepler.gl](https://kepler.gl).
2. In a file explorer, navigate to the directory containing the model execution and output files.
3. Load the desired files (e.g., heatmap files, movement trajectories, or trips files (see [Model Output](#model-outputs) in this README)) into [kepler.gl](https://kepler.gl).
4. Adjust the visualisation via the options provided in the left sidebar.

See Section 5 of the [final report](./Documentation/KNP_Traffic_Model_Final_Report.pdf) for exemplary result visualisations.
