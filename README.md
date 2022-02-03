# [GlobalPollenProject](https://globalpollenproject.org)
*Key Words: Pollen; Palynology; Reference-Collection; Digitisation; Dissemination; Crowdsourcing*

The Global Pollen Project is a web-based tool to enable:

1. **crowdsourcing** of pollen identification using images; and

2. **digitisation** of existing pollen reference material.


The ideas behind the tool, and the development of the initial 1.0 release, are discussed in detail in the following publication:

[Martin A.C., and Harvey, W. J. (2017). The Global Pollen Project: A New Tool for Pollen Identification and the Dissemination of Physical Reference Collections. *Methods in Ecology and Evolution*. **Accepted**](http://dx.doi.org/10.1111/2041-210X.12752)

## Development Environment
The GPP can run in a docker container or independently.

1. **Create Read and Write Stores.** Spin up a local Redis and an EventStore for the read and write models. You can do this simply using docker, by running `docker-compose up -d` in the src/GlobalPollenProject.Web folder.

2. **Install Front-End Dependencies.** To install dependencies, navigate to src/GlobalPollenProject.Web and run `yarn install`.

3. **Compile TypeScript and SASS.** Run `yarn run:dev` in the src/GlobalPollenProject.Web directory to compile the SCSS styles. This will watch the directory for any changed source files.


## Components

GPP consists of the following components:

1. **GPP Core**. Wraps up the major functions into a single interface, which is exposed by a REST API.

2. **API**. A public-facing API for the GPP. Access requires a valid API token, which can be obtained via the web interface.

3. **Web App**. The public-facing website that exposes the library and basic identification functions.

4. **Lab UI**. Digitisation tools are split into a seperate interface (in progress). This is a SPA Borelo F# app.
