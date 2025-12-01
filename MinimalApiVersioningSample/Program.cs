using System.Text;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
    //options.ApiVersionReader = new QueryStringApiVersionReader("api-version");
    //options.DefaultApiVersion = new ApiVersion(new DateOnly(2024, 1, 1));
    //options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;

    options.Policies.Sunset(1)
        .Effective(2024, 12, 31)
        .Link("https://github.com/dotnet/aspnet-api-versioning/wiki/Version-Policies")
            .Title("Version Policies").Type("text/html");
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

var versions = new string[] { "v1", "v2" };
foreach (var description in versions)
{
    builder.Services.AddOpenApi(description, options =>
    {
        options.AddDocumentTransformer<DocumentInfoDocumentTransformer>();
        options.AddOperationTransformer<ApiVersionDeprecatedTransformer>();
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json");
}

var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))   // HasDeprecatedApiVersion to deprecate all the endpoints of this version.
    .HasApiVersion(new ApiVersion(2))
    .ReportApiVersions()
    .Build();

var versionedApi = app.MapGroup("/api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);

versionedApi.MapGet("for-all-versions", () =>
{
    return TypedResults.Ok();
});

versionedApi.MapGet("ping", () =>
{
    return TypedResults.Ok();
})
.MapToApiVersion(1)
.AddOpenApiOperationTransformer((operation, context, cancellationToken) =>
{
    // Mark this specific endpoint as deprecated.
    operation.Deprecated = true;
    return Task.CompletedTask;
});

versionedApi.MapGet("new-ping", () =>
{
    return TypedResults.Ok();
})
.MapToApiVersion(2);

app.UseSwaggerUI(options =>
{
    var descriptions = app.DescribeApiVersions();

    // Build a swagger endpoint for each discovered API version
    foreach (var description in descriptions)
    {
        var url = $"/openapi/{description.GroupName}.json";
        options.SwaggerEndpoint(url, description.GroupName);
    }
});

app.Run();

internal class ApiVersionDeprecatedTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Deprecated |= context.Description.IsDeprecated();
        return Task.CompletedTask;
    }
}

internal class DocumentInfoDocumentTransformer(IApiVersionDescriptionProvider apiVersionDescriptionProvider) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var apiDescription = apiVersionDescriptionProvider.ApiVersionDescriptions
            .FirstOrDefault(d => d.GroupName == context.DocumentName);

        if (apiDescription is not null)
        {
            document.Info = CreateInfoForApiVersion(apiDescription);
        }

        return Task.CompletedTask;
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var text = new StringBuilder("An example about how to use route versioning in a Minimal API project.");

        var info = new OpenApiInfo
        {
            Title = "Route Versioning Web API",
            Version = description.ApiVersion.ToString(),
        };

        if (description.IsDeprecated)
        {
            text.Append(" This API version has been deprecated.");
        }

        if (description.SunsetPolicy is { } policy)
        {
            if (policy.Date is { } when)
            {
                text.Append(" The API will be sunset on ")
                    .Append(when.Date.ToShortDateString())
                    .Append('.');
            }

            if (policy.HasLinks)
            {
                text.AppendLine();
                var rendered = false;

                for (var i = 0; i < policy.Links.Count; i++)
                {
                    var link = policy.Links[i];

                    if (link.Type == "text/html")
                    {
                        if (!rendered)
                        {
                            text.Append("<h4>Links</h4><ul>");
                            rendered = true;
                        }

                        text.Append("<li><a href=\"");
                        text.Append(link.LinkTarget.OriginalString);
                        text.Append("\">");
                        text.Append(StringSegment.IsNullOrEmpty(link.Title) ? link.LinkTarget.OriginalString : link.Title.ToString());
                        text.Append("</a></li>");
                    }
                }

                if (rendered)
                {
                    text.Append("</ul>");
                }
            }
        }

        info.Description = text.ToString();
        return info;
    }
}