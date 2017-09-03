# [GlobalPollenProject](https://globalpollenproject.org)
*Key Words: Pollen; Palynology; Reference-Collection; Digitisation; Dissemination; Crowdsourcing*

[![Build status](https://ci.appveyor.com/api/projects/status/oy51ibjqbo8y7ltq?svg=true)](https://ci.appveyor.com/project/AndrewIOM/gpp-cqrs)

The Global Pollen Project is a web-based tool to enable:

1. **crowdsourcing** of pollen identification using images; and

2. **digitisation** of existing pollen reference material.


The ideas behind the tool, and the development of the initial 1.0 release, are discussed in detail in the following publication:

[Martin A.C., and Harvey, W. J. (2017). The Global Pollen Project: A New Tool for Pollen Identification and the Dissemination of Physical Reference Collections. *Methods in Ecology and Evolution*. **Accepted**](http://dx.doi.org/10.1111/2041-210X.12752)

## Setup
There are a couple of dependencies to setup once you have cloned the repository.

1. To setup styles, navigate to src/GlobalPollenProject.Web and run `npm install`. You can then call `node_modules/.bin/webpack` to compile the SCSS styles.
2. Run `bower install` in the src/GlobalPollenProject.Web folder to install front-end dependencies.
3. Spin up a local Redis and an EventStore for the read and write models. You can do this simply using docker, by running `docker-compose up -d` in the src/GlobalPollenProject.Web folder.