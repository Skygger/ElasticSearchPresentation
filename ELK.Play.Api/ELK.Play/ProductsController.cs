using System.Text.Json;
using ELK.Play.Config;
using ELK.Play.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;

namespace ELK.Play;

[ApiController]
[Route("[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IElasticClient _elasticClient;
    private readonly ILogger<ProductsController> _logger;
    private readonly IOptions<ElkConfiguration> _elkOptions;

    public ProductsController(
        IElasticClient elasticClient,
        ILogger<ProductsController> logger,
        IOptions<ElkConfiguration> elkOptions)
    {
        _elasticClient = elasticClient;
        _logger = logger;
        _elkOptions = elkOptions;
    }


    /// <summary>
    /// Versatile full text search using 'query_string' query.
    /// This kind of query is used to create a complex search that includes wildcard characters, searches across multiple fields, and more
    /// </summary>
    /// GET /_search
    /// {
    ///     "query": {
    ///         "query_string": {
    ///             "query": "*query*"             
    ///         }
    ///     }
    /// }
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] string query)
    {
        var result = await _elasticClient.SearchAsync<Product>(selector =>
            selector
                .Index(_elkOptions.Value.ProductIndex)
                .Query(q => q.QueryString(d => d.Query('*' + query + '*')))
                .From(0)
                .Size(100)
                .Sort(srt => srt.Descending(f => f.Id)));

        return Ok(result.Documents.ToArray());
    }

    /// <summary>
    /// Fulltext search by match query. The query is analyzed by the analyzer specified in the field mapping.
    /// Match query is a boolean query with 'OR' as default operator
    /// GET /products/product/_search
    /// {
    ///    "query": {
    ///        "match": {
    ///            "description": "query"
    ///        }
    ///    }
    /// } 
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet("match")]
    public async Task<IActionResult> GetByMatchAsync([FromQuery] string query)
    {
        var result = await _elasticClient.SearchAsync<Product>(selector =>
            selector
                .Index(_elkOptions.Value.ProductIndex)
                .Query(q =>
                    q.Match(d => d.Field(f => f.Description).Query(query)))
                .Size(100));

        return Ok(JsonConvert.SerializeObject(result.Hits));
    }
    /// <summary>
    /// Fulltext search by match query. The query is analyzed by the analyzer specified in the field mapping.
    /// Match query is a boolean query with 'OR' as default operator
    /// GET /products/product/_search
    /// {
    ///    "query": {
    ///        "match": {
    ///            "description": "query"
    ///        }
    ///    }
    /// } 
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet("match/fuzz")]
    public async Task<IActionResult> GetByMatchFuzzinessAsync([FromQuery] string query)
    {
        var fuzziness = Fuzziness.Auto;

        var result = await _elasticClient.SearchAsync<Product>(selector =>
            selector
                .Index(_elkOptions.Value.ProductIndex)
                .Query(q =>
                    q.Match(d 
                        => d.Field(f => f.Description)
                            .Query(query)
                            .Fuzziness(fuzziness)
                            .FuzzyTranspositions() // by default is true in all queries of type 'match'
                        ))
                .Size(100));

        return Ok(result.Documents);
    }

    /// <summary>
    /// Fulltext search by match_phrase query. This kind of query matches exact phrases.
    /// GET /products/product/_search
    /// {
    ///    "query": {
    ///        "match_phrase": {
    ///            "title": "query"
    ///        }
    ///    }
    /// } 
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet("match-phrase")]
    public async Task<IActionResult> GetMatchPhraseAsync([FromQuery] string query)
    {
        var result = await _elasticClient.SearchAsync<Product>(selector =>
            selector
                .Index(_elkOptions.Value.ProductIndex)
                .Query(q =>
                    q.MatchPhrase(d => d.Field(f => f.Description).Query(query)))
                .Size(100));

        return Ok(JsonConvert.SerializeObject(result.Hits));
    }

    /// <summary>
    /// Fulltext search by multi_match query for multiple fields searching.
    /// GET /products/product/_search
    /// {
    ///    "query": {
    ///        "multi_match": {
    ///            "query": "query",
    ///            "fields" : [ ... ]
    ///        }
    ///    }
    /// } 
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet("multi-match")]
    public async Task<IActionResult> GetMultiMatchAsync([FromQuery] string query)
    {
        var result = await _elasticClient.SearchAsync<Product>(selector =>
            selector
                .Index(_elkOptions.Value.ProductIndex)
                .Query(q =>
                    q.MultiMatch(d =>
                        d.Fields(f => f.Field(product => product.Description).Field(product => product.Tags)).Query(query)))
                .Size(100));

        return Ok(result.Documents.ToArray());
    }

    /// <summary>
    /// Term level queries are for enums, numbers, dates etc. This example shows the important difference of 
    /// </summary>
    /// GET /products/_search
    ///{
    ///    "query": {
    ///        "term": {
    ///            "description": "query"
    ///        }
    ///    }
    ///}
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet("term")]
    public async Task<IActionResult> GetByTermAsync([FromQuery] string query)
    {
        var result = await _elasticClient.SearchAsync<Product>(selector =>
            selector
                .Index(_elkOptions.Value.ProductIndex)
                .Query(q =>
                    q.Term(d => d.Field(f => f.Description).Value(query)))
                .Size(100));

        return Ok(result.Documents.ToArray());
    }

    /// <summary>
    /// Term level queries are for enums, numbers, dates etc. This example defines price range
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet("term/range")]
    public async Task<IActionResult> GetByTermRangeAsync([FromQuery] string query)
    {
        var result = await _elasticClient.SearchAsync<Product>(selector =>
            selector
                .Index(_elkOptions.Value.ProductIndex)
                .Query(q =>
                    q.TermRange(d => d.Field(f => f.Price).LessThanOrEquals(query)))
                .Size(100));

        return Ok(result.Documents.ToArray());
    }

    /// <summary>
    /// Get a product by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] int id)
    {
        var result = await _elasticClient.GetAsync<Product>(id, s => s.Index(_elkOptions.Value.ProductIndex));

        return Ok(result.Source);
    }

    /// <summary>
    /// Term level query by ids
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("ids")]
    public async Task<IActionResult> GetByIdsAsync([FromBody] long[] ids)
    {
        var result = await _elasticClient.SearchAsync<Product>(selector =>
            selector
                .Index(_elkOptions.Value.ProductIndex)
                .Query(q => q.Ids(d => d.Values(ids)))
                .Size(100));

        return Ok(result.Hits.Select(t => t.Source).ToArray());
    }

    /// <summary>
    /// Create new product document in the index
    /// </summary>
    /// <param name="product"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] Product product)
    {

        // Here can by a write operation to your DB
        //_dbContext.Products.Add(product)

        //POST /products/product/_doc - full replace of the document
        var response = await _elasticClient.IndexAsync<Product>(product, s => s.Index(_elkOptions.Value.ProductIndex));

        if (response.Result == Result.Created)
        {
            return Accepted();
        }

        return BadRequest();
    }

    [HttpPut]
    public async Task<IActionResult> PutAsync([FromBody] Product product)
    {
        // Here can by a write operation to your DB
        //_dbContext.Products.Add(product)

        // get documents for optimistic concurrency
        var productFromEls = await _elasticClient.GetAsync<Product>(product.Id, s => s.Index(_elkOptions.Value.ProductIndex));

        if (!productFromEls.Found)
        {
            return NotFound();
        }

        //POST /products/product/_update/id?if_primary_term=X&if_seq_no=X
        var response = await _elasticClient.UpdateAsync<Product>(product.Id, s
            => s.Index(_elkOptions.Value.ProductIndex)
                .Doc(product)
                .DocAsUpsert(true)
                .IfPrimaryTerm(productFromEls.PrimaryTerm) // optimistic concurrency
                .IfSequenceNumber(productFromEls.SequenceNumber)); // optimistic concurrency

        if (response.Result == Result.Updated)
        {
            return Accepted();
        }

        return BadRequest();
    }

    [HttpPut("bulk")]
    public async Task<IActionResult> BulkUpdateAsync([FromBody] IList<Product> products)
    {
        var bulkUpdateResult = await _elasticClient.BulkAsync(s
            => s.Index(_elkOptions.Value.ProductIndex)
                .UpdateMany(products, (descriptor, product) => descriptor.Doc(product).Id(product.Id)));

        if (bulkUpdateResult.IsValid)
        {
            return Accepted();
        }

        if (bulkUpdateResult.ItemsWithErrors != null)
        {
            foreach (var resultItemsWithError in bulkUpdateResult.ItemsWithErrors)
            {
                _logger.LogError($"Bulk update failed for item {resultItemsWithError.Id}. {resultItemsWithError.Error}.");
            }
        }

        return BadRequest();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync([FromRoute] int id)
    {
        //DELETE /products/product/_doc/id
        var response = await _elasticClient.DeleteAsync<Product>(id, s
            => s.Index(_elkOptions.Value.ProductIndex));

        if (response.Result == Result.Deleted)
        {
            return Accepted();
        }

        return BadRequest();
    }

    [HttpPut("{id}/quantity/{amount}")]
    public async Task<IActionResult> UpdateQuantityAsync([FromRoute] int id, [FromRoute] int amount)
    {
        //POST /products/product/_update/100
        //{
        //    "script": {
        //        "source": "ctx._source.quantity=amount"
        //    }
        //}

        var response = await _elasticClient.UpdateAsync<Product>(id, s =>
            s.Index(_elkOptions.Value.ProductIndex)
                .Script(script => script.Source($"ctx._source.quantity={amount}")));

        if (response.Result == Result.Updated)
        {
            return Accepted();
        }

        return BadRequest();
    }

    /// <summary>
    /// Example of metrics aggregations
    /// </summary>
    /// <returns></returns>
    [HttpGet("aggr/metrics")]
    public async Task<IActionResult> GetMetricsAggregationsAsync()
    {
        var result = await _elasticClient.SearchAsync<Product>(selector =>
            selector
                .Index(_elkOptions.Value.ProductIndex)
                .Aggregations(aggr => aggr
                    .Average("AveragePrice", aggregationDescriptor => aggregationDescriptor.Field(f => f.Price))
                    .Max("MaxPrice", aggregationDescriptor => aggregationDescriptor.Field(f => f.Price))
                    .Min("MinPrice", aggregationDescriptor => aggregationDescriptor.Field(f => f.Price))
                    .Stats("PriceStats", st => st.Field(f => f.Price)))
                .Size(0));

        return Ok(new
        {
            AvgPrice = result.Aggregations.Average("AveragePrice"),
            Max = result.Aggregations.Max("MaxPrice"),
            Min = result.Aggregations.Min("MinPrice"),
            Stats = result.Aggregations.Stats("PriceStats")
        });
    }

    /// <summary>
    /// Example of metrics aggregations
    /// </summary>
    /// <returns></returns>
    [HttpGet("aggr/buckets")]
    public async Task<IActionResult> GetBucketsAggregationsAsync()
    {
        var result = await _elasticClient.SearchAsync<Product>(selector =>
            selector
                .Index(_elkOptions.Value.ProductIndex)
                .Aggregations(aggr => aggr
                    .Terms("Producer", t => t.Field(f => f.Producer)
                        .Aggregations(a => a.Stats("PriceStats", st => st.Field(f => f.Price)))))
                .Size(100));

        var buckets = result.Aggregations.Terms("Producer").Buckets.Select(b => new
        {
            Producer = b.Key,
            b.DocCount,
            PriceStats = b.Stats("PriceStats")
        });

        return Ok(buckets);
    }
}