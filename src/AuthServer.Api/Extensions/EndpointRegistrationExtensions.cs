using System.Reflection;
using AuthServer.Api.Abstractions;

public static class EndpointRegistrationExtensions
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var endpointTypes = Assembly.GetExecutingAssembly()
                                    .GetTypes()
                                    .Where(type =>
                                    typeof(IEndpoint).IsAssignableFrom(type)
                                    && type is
                                    {
                                        IsAbstract: false,
                                        IsInterface: false
                                    });

        foreach (var endpointType in endpointTypes)
        {
            var endpoint = (IEndpoint)ActivatorUtilities.CreateInstance(app.Services, endpointType);

            endpoint.MapEndpoints(app);
        }

        return app;
    }
}