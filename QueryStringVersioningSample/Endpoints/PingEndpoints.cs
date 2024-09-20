
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;

namespace QueryStringVersioningSample.Endpoints;

public class PingEndpoints : IEndpointRouteHandlerBuilder
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("ping", () =>
        {
            return TypedResults.Ok();
        })
            .MapToApiVersion(new DateOnly(2024, 1, 1))
        .WithOpenApi();

        endpoints.MapGet("ping", () =>
        {
            return TypedResults.Ok();
        })
        .MapToApiVersion(new DateOnly(2024, 9, 20))
        .WithOpenApi();

        endpoints.MapGet("pong", Pong)
            .WithOpenApi();
    }

    //[MapToApiVersion("2024-01-01")]
    [MapToApiVersion("2024-09-20")]
    private static Ok Pong() => TypedResults.Ok();
}
