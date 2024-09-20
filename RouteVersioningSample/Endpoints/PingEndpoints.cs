using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RouteVersioningSample.Endpoints;

public class PingEndpoints : IEndpointRouteHandlerBuilder
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("ping", () =>
        {
            return TypedResults.Ok();
        })
        .MapToApiVersion(1)
        .WithOpenApi();

        endpoints.MapGet("ping", () =>
        {
            return TypedResults.Ok();
        })
        .MapToApiVersion(2)
        .WithOpenApi();

        endpoints.MapGet("pong", Pong)
            .WithOpenApi();
    }

    //[MapToApiVersion(1)]
    [MapToApiVersion(2)]
    private static Ok Pong() => TypedResults.Ok();
}
