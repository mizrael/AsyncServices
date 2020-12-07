using System;
using Scrutor;

namespace AsyncServices.Worker.Extensions
{
    public static class IImplementationTypeSelectorExtensions
    {
        public static IImplementationTypeSelector RegisterHandlers(this IImplementationTypeSelector selector, Type type)
        {
            return selector.AddClasses(c => c.AssignableTo(type))
                .UsingRegistrationStrategy(RegistrationStrategy.Append)
                .AsImplementedInterfaces()
                .WithScopedLifetime();
        }
    }
}
