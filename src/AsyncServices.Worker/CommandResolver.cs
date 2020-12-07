using System;
using System.Collections.Generic;
using AsyncServices.Common.Queues;
using AsyncServices.Common.Services;

namespace AsyncServices.Worker
{
    public class CommandResolver : ICommandResolver
    {
        private readonly IDecoder _encoder;
        private readonly IEnumerable<System.Reflection.Assembly> _assemblies;

        public CommandResolver(IDecoder encoder, IEnumerable<System.Reflection.Assembly> assemblies)
        {
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            _assemblies = assemblies ?? throw new ArgumentNullException(nameof(encoder));
        }

        public MediatR.INotification Resolve(QueueMessage message)
        {
            Type dataType = null;
            foreach (var assembly in _assemblies)
                dataType = assembly.GetType(message.MessageType, throwOnError: false, ignoreCase: true);
            if (dataType is null)
                throw new TypeLoadException($"unable to resolve type '{message.MessageType}' ");

            var data = _encoder.Decode(message.Payload, dataType);
            return data as MediatR.INotification;
        }
    }
}
