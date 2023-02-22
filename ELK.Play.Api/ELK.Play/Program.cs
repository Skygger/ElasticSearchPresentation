using ELK.Play.Config;
using ELK.Play.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<ElkConfiguration>(builder.Configuration.GetSection(nameof(ElkConfiguration)));

await builder.Services.AddElasticSearch(builder.Configuration);

var app = builder.Build();

app.MapControllers();

app.Run();
