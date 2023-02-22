using ELK.Play.Config;
using ELK.Play.Models;
using Nest;
namespace ELK.Play.Extensions;

public static class ElasticSearchExtensions
{
    public static async Task AddElasticSearch(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var elkConfiguration = configuration.GetSection(nameof(ElkConfiguration)).Get<ElkConfiguration>();

        var settings = new ConnectionSettings(elkConfiguration.Uri);

        var client = new ElasticClient(settings);

        serviceCollection.AddSingleton<IElasticClient>(client);

        await CreateIndexAsync(client, elkConfiguration.ProductIndex);
    }
    
    private static async Task CreateIndexAsync(IElasticClient client, string indexName)
    {
        if ((await client.Indices.ExistsAsync(indexName)).Exists is false)
        {
            // best practices for explicit mappings
            var newIndexResponse = await client.Indices.CreateAsync(indexName, i => i
                .Map(m => m.Dynamic(new Union<bool, DynamicMapping>(DynamicMapping.Strict)) // ELS throws exception, if not mapped fields are encountered 
                    .Properties<Product>(p => p
                        .Keyword(k => k.Name(n => n.Id)) // exact matches, for filtering, aggregations and sorting
                        .Keyword(k => k.Name(n => n.Producer).DocValues().Norms(false).Index().IgnoreAbove(256)) // exact matches, for filtering, aggregations and sorting, default configuration
                        .Text(t => t.Name(n => n.Title).Analyzer("standard")) // inverted index
                        .Text(t => t.Name(n => n.Description)) // inverted index
                        .Text(t => t.Name(n => n.Tags))// inverted index
                        .Number(n => n.Name(price => price.Price).Type(NumberType.ScaledFloat).ScalingFactor(100)) //BKD trees
                        .Number(n => n.Name(q => q.Quantity).Type(NumberType.Integer)))) //BKD trees
                .Settings(s => s.NumberOfShards(1).NumberOfReplicas(1)));

            if (!newIndexResponse.IsValid || newIndexResponse.Acknowledged is false)
            {
                throw new Exception("Index creation failed!");
            }
            
            var bulkAll = client.BulkAll(GetProducts(), r
                => r.Index(indexName)
                    .BackOffRetries(2)
                    .BackOffTime("2s")
                    .ContinueAfterDroppedDocuments()
                    .DroppedDocumentCallback((response, d) =>
                    {
                        Console.WriteLine($"Error indexing the document {d.Id}: {response.Error.Reason}");
                    })
                    .MaxDegreeOfParallelism(4) // optional, how many http requests in parallel
                    .Size(10));

            bulkAll.Wait(TimeSpan.FromMinutes(10), r => Console.WriteLine("Data indexed"));
        }
    }

    private static IList<Product> GetProducts()
    {
        return new List<Product>()
        {
            new Product()
            {
                Id = 1,
                Title = "Sweet cookies",
                Description = "Eat it!.",
                Price = 3.99M,
                Quantity = 10000,
                Producer = "Milka",
                Tags = new []{ "sweet", "chocolate" }
                
            },
            new Product()
            {
                Id = 2,
                Title = "Web cookies",
                Description = "Do not eat it!",
                Price = 0.99M,
                Quantity = 1,
                Producer = "Microsoft",
                Tags = new []{ "http", "web", "IT" }
            },
            new Product()
            {
                Id = 3,
                Title = "Полезное вкусное вещество.",
                Description = "Made in China",
                Price = 99.99M,
                Quantity = 290900000,
                Producer = "FengShui",
                Tags = new []{ "asia", "exotic", "extreme" }
            },
            new Product()
            {
                Id = 4,
                Title = "M16 assault rifle",
                Description =
                    "The M16 rifle is a family of military rifles adapted from the ArmaLite AR-15 rifle.",
                Price = 1800.99M,
                Quantity = 1,
                Producer = "Pentagon",
                Tags = new []{ "guns", "arms", "weapon" }
            },
            new Product()
            {
                Id = 5,
                Title = "Lobster - Live",
                Description =
                    "Live Lobster at your table every morning!",
                Price = 999.79M,
                Quantity = 3,
                Producer = "Global Seafood",
                Tags = new []{ "shellfish", "see", "seafood" }
            }

        };
    }
}