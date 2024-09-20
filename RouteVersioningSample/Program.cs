using Asp.Versioning;
using RouteVersioningSample.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApiVersioning(options =>
{
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;

    options.Policies.Sunset(1)
        .Effective(2024, 12, 31)
        .Link("https://github.com/dotnet/aspnet-api-versioning/wiki/Version-Policies")
            .Title("Version Policies")
            .Type("text/html");
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.ConfigureOptions<ConfigureSwaggerGenOptions>();
builder.Services.AddSwaggerGen(options =>
{
    //options.AddOperationParameters();
    options.OperationFilter<SwaggerDefaultValues>();
});

var app = builder.Build();

var apiVersionSet = app.NewApiVersionSet()
    .HasDeprecatedApiVersion(new ApiVersion(1))
    .HasApiVersion(new ApiVersion(2))
    .Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

var versionedApi = app.MapGroup("/api/v{version:apiVersion}")
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
