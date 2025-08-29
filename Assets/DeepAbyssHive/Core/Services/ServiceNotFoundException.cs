using System;

namespace DeepAbyssHive.Core.Services
{
    public class ServiceNotFoundException : Exception
    {
        public Type ServiceType { get; }
        public ServiceNotFoundException(Type type)
            : base($"Service not found: {type?.FullName}")
        {
            ServiceType = type;
        }
    }
}