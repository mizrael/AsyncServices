using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncServices.Worker.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddMediatR(this IServiceCollection services, params System.Type[] types)
        {
            services.AddScoped<ServiceFactory>(ctx => ctx.GetRequiredService);
            services.AddScoped<IMediator, Mediator>();
            services.Scan(scan =>
            {                
                scan.FromAssembliesOf(types)
                    .RegisterHandlers(typeof(IRequestHandler<>))
                    .RegisterHandlers(typeof(IRequestHandler<,>))
                    .RegisterHandlers(typeof(INotificationHandler<>));
            });

            return services;
        }
    }
}
