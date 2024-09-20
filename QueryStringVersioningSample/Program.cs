using Asp.Versioning;
using Microsoft.OpenApi.Models;
using QueryStringVersioningSample.Swagger;
using TinyHelpers.AspNetCore.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApiVersioning(options =>
{
    options.ApiVersionReader = new QueryStringApiVersionReader("api-version");
    options.DefaultApiVersion = new ApiVersion(new DateOnly(2024, 1, 1));
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerOperationParameters(options =>
{
    options.Parameters.Add(new()
    {
        Name = "api-version",
        In = ParameterLocation.Query,
        Schema = OpenApiSchemaHelper.CreateStringSchema("2024-01-01"),
    });
});

builder.Services.ConfigureOptions<ConfigureSwaggerGenOptions>();
builder.Services.AddSwaggerGen(options =>
{
    options.AddOperationParameters();
    options.OperationFilter<SwaggerDefaultValues>();
});

var app = builder.Build();

var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(new DateOnly(2024, 1, 1)))
    .HasApiVersion(new ApiVersion(new DateOnly(2024, 9, 20)))
    .Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

var versionedApi = app.MapGroup("/api")
    .WithApiVersionSet(apiVersionSet);

versionedApi.MapEndpoints();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var descriptions = app.DescribeApiVersions();

    // Build a swagger endpoint for each discovered API version
    foreach (var description in descriptions)
    {
        var url = $"/swagger/{description.GroupName}/swagger.json";
        options.SwaggerEndpoint(url, description.GroupName);
    }
});

app.Run();
