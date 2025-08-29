using System;
using System.Collections.Generic;

namespace DeepAbyssHive.Core.Services
{
    /// <summary>
    /// Minimal, canonical service locator used by managers & bootstrapper.
    /// API: Register<T>, Get<T>, IsRegistered<T>, TryGet<T>, Clear()
    /// </summary>
    public static partial class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T service) where T : class
        {
            _services[typeof(T)] = service ?? throw new ArgumentNullException(nameof(service));
        }

        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s))
                return (T)s;
            throw new InvalidOperationException($"Service '{typeof(T).Name}' is not registered.");
        }

        public static bool IsRegistered<T>() where T : class => _services.ContainsKey(typeof(T));

        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s))
            {
                service = (T)s;
                return true;
            }
            service = null;
            return false;
        }

        public static void Clear() => _services.Clear();
    }
}