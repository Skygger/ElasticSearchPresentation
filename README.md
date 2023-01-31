# ElasticSearchPresentation
Elasticsearch for .NET Developers Presentation held for Exadel

Minimal API web application with official Elasticsearch .NET Client (NEST) usage examples.

# Prerequesites:
Windows Subsystem for Linux 2 istalled, 
Docker Desktop installed

Run `docker compose up -d(detached)` in the folder with docker-compose.yml for Elasticsearch and Kibana instances installation on your machine

# Kibana DSL Queries examples

## Cluster Health, Nodes, Indices, Shards

```
GET /_cluster/health

GET _cat/nodes?v
GET _cat/indices?v
GET _cat/indices?v&expand_wildcards=all

GET _cat/shards?v=true&h=index,shard,prirep,state,node,unassigned.reason&s=state
GET _cat/shards?v
```

## Index CRD operations

```
PUT products
DELETE products
GET products
```

## Get Products Index mapping

```
GET products/_mapping
```

## Analyze API example

```
POST /_analyze
{
  "text": "2 guys walk into   a bar, but the third... DUCKS! :-) вкусное",
  "analyzer": "standard"
}
```

## Basic Search API examples

```
GET products/_search
GET products/_doc/1080
GET products/_search
{
  "query": {
    "match_all": {
    }
  }
}
```
