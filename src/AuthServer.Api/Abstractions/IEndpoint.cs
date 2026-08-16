namespace AuthServer.Api.Abstractions;

public interface IEndpoint
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
