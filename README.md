#LIFE
This project contains the simulation core of the MARS framework. Currently there are two major versions available: 2.x and 3.x

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
