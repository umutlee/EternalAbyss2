using System;

namespace DeepAbyssHive.Core.Exceptions
{
    public class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException(string message) : base(message) {}
    }
}