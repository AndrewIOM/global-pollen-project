module Events

type DomainEvent =
| Grain of GlobalPollenProject.Core.Aggregates.Grain.Event
| Taxonomy of GlobalPollenProject.Core.Aggregates.Taxonomy.Event