using System;
using System.Collections.Generic;

namespace DeepAbyssHive.Core.Services
{
    // 與現有 ServiceLocator 並存的相容層；只提供缺失 API，不干擾原本邏輯。
    public static partial class ServiceLocator
    {
        // 狀態視覺化（舊呼叫點會用到）；維持無副作用的安全預設。
        private static bool _compatInitialized;
        public static bool IsInitialized => _compatInitialized;

        public static void MarkAsInitialized() => _compatInitialized = true;

        public static string GetStatusInfo()
        {
            try
            {
                // 盡量從主 ServiceLocator 取資訊；拿不到就提供降級字串
                var total = 0;
                var type = typeof(ServiceLocator);
                var mapField = type.GetField("_services", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (mapField?.GetValue(null) is System.Collections.IDictionary d) total = d.Count;
                return $"ServiceLocator: initialized={_compatInitialized}, registered={total}";
            }
            catch
            {
                return $"ServiceLocator: initialized={_compatInitialized}";
            }
        }

        // 舊版呼叫點：Register(實例, 第二參數) – 第二參數實際無用，只為相容
        public static void Register<TService>(TService instance, string _)
            where TService : class
        {
            Register(instance);
        }

        public static void Register<TService>(TService instance, bool _)
            where TService : class
        {
            Register(instance);
        }

        public static void Register(Type serviceType, object instance)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var m = typeof(ServiceLocator).GetMethod("Register", new[] { serviceType });
            if (m != null)
            {
                // 呼叫泛型 Register<T>(T)
                var gm = m;
                gm.Invoke(null, new[] { instance });
            }
            else
            {
                // 若主體沒有泛型版本，就丟例外（理論上不會進來）
                throw new ServiceNotFoundException(serviceType);
            }
        }

        // 舊版呼叫點：IsRegistered(某個參數) – 實際只看型別
        public static bool IsRegistered(object any)
        {
            if (any == null) return false;
            if (any is Type t) return IsRegistered(t);
            return IsRegistered(any.GetType());
        }

        public static bool IsRegistered(Type t)
        {
            if (t == null) return false;
            try
            {
                var m = typeof(ServiceLocator).GetMethod("Get", new[] { typeof(Type) });
                if (m != null)
                {
                    m.Invoke(null, new object[] { t });
                    return true;
                }
                // 後備：用泛型 IsRegistered<T>() 若存在
                var gm = typeof(ServiceLocator).GetMethod("IsRegistered", Type.EmptyTypes);
                if (gm != null)
                {
                    var closed = gm.MakeGenericMethod(t);
                    return (bool)closed.Invoke(null, null);
                }
            }
            catch { /* 取不到表示未註冊 */ }
            return false;
        }

        // 方便被上面反射呼叫（若主體沒有 Get(Type)）
        public static object Get(Type t)
        {
            var gm = typeof(ServiceLocator).GetMethod("Get", Type.EmptyTypes);
            if (gm != null)
            {
                var closed = gm.MakeGenericMethod(t);
                return closed.Invoke(null, null);
            }
            throw new ServiceNotFoundException(t);
        }
    }
}