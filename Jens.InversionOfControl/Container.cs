using System;
using System.Collections.Generic;
using System.Linq;

namespace Jens.InversionOfControl
{
    public class Container : IDependencyResolver, IDisposable
    {
        private class LazyPacked
        {
            public object Lazy { get; }

            public object Value => GetValueTypeLess();

            public LazyPacked(object lazy)
            {
                Lazy = lazy ?? throw new ArgumentNullException(nameof(lazy));
            }

            private object GetValueTypeLess()
            {
                var genType = Lazy.GetType().GetGenericArguments()[0];
                var valueProperty = typeof(Lazy<>).MakeGenericType(genType).GetProperty("Value");
                
                return valueProperty.GetValue(Lazy);
            }
        }
        private readonly Dictionary<Type, LazyPacked> _dependencySingletonContainer;
        private readonly List<Type> _dependencyTypeContainer;

        public Container()
        {
            _dependencyTypeContainer = new List<Type>();
            _dependencySingletonContainer = new Dictionary<Type, LazyPacked>();
        }

        /// <summary>
        /// [Lazy] Register Singleton of Type T. 
        /// </summary>
        public Container WithSingleton<T>() where T : class
        {
            WithSingleton(new Lazy<T>(()=>SimpleActivator.Activate(typeof(T), this) as T));
            return this;
        }
        /// <summary>
        /// [STRICT] Register Singleton of Type T using the given instance.
        /// </summary>
        public Container WithSingleton<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var singletonObjectType = instance.GetType();
            var lazyPacked = new LazyPacked(new Lazy<T>(() => instance));
            _dependencySingletonContainer.Add(singletonObjectType, lazyPacked);
            return this;
        }
        /// <summary>
        /// [LAZY] Register Singleton of Type T using the given instance.
        /// </summary>
        public Container WithSingleton<T>(Lazy<T> lazyLoadedSinglton) where T : class
        {
            var singletonObjectType = typeof(T);

            if (_dependencySingletonContainer.ContainsKey(singletonObjectType))
            {
                throw new InvalidOperationException($"The type \"{singletonObjectType.FullName}\" is already registered.");
            }

            _dependencySingletonContainer.Add(singletonObjectType, new LazyPacked(lazyLoadedSinglton));
            return this;
        }
        /// <summary>
        /// [LAZY] Register Type as Dependency.
        /// </summary>
        public Container WithType<T>() where T : class
        {
            var type = typeof(T);

            if (_dependencyTypeContainer.Any(listType => listType == type))
            {
                throw new InvalidOperationException($"The type \"{type.FullName}\" is already registered.");
            }

            _dependencyTypeContainer.Add(type);
            return this;
        }

        public Type GetDependency<Type>() where Type : class
        {
            return GetDependency(typeof(Type)) as Type;
        }

        public object[] GetDependencies(Type[] dependencyTypes)
        {
            return dependencyTypes.Select(type => GetDependency(type)).ToArray();
        }

        public object GetDependency(Type dependencyType)
        {
            // return this container as it resolves.
            if (dependencyType == typeof(IDependencyResolver))
            {
                return this;
            }

            var resolvedType = ResolveDependencyType(dependencyType);

            if (resolvedType != null)
            {
                // check for singleton existing.
                if (_dependencySingletonContainer.ContainsKey(resolvedType))
                {
                    return _dependencySingletonContainer[resolvedType].Value;
                }
                
                // return activated instance.
                return SimpleActivator.Activate(resolvedType, this);
            }

            throw new InvalidOperationException($"Unknown dependency, please register type {dependencyType.FullName}");
        }

        private Type ResolveDependencyType(Type dependencyType)
        {
            if (dependencyType == null)
            {
                throw new ArgumentNullException(nameof(dependencyType));
            }
            if (dependencyType == typeof(IDependencyResolver))
            {
                return typeof(IDependencyResolver);
            }

            var allTypes = _dependencySingletonContainer.Keys.Union(_dependencyTypeContainer);

            var detected = allTypes.FirstOrDefault(type => type == dependencyType);
            if (detected == null && dependencyType.IsInterface)
            {
                detected = allTypes.FirstOrDefault(type => dependencyType.IsAssignableFrom(type));
            }

            return detected;
        }

        public bool AreTypesKnown(Type[] types)
        {
            return !types.Select(t => ResolveDependencyType(t)).Any((rt) => rt == null);
        }

        public Type Activate<Type>()
            where Type : class
        {
            return Activate(typeof(Type)) as Type;
        }

        public object Activate(Type type)
        {
            return SimpleActivator.Activate(type, this);
        }

        public void Dispose()
        {
            foreach (var item in _dependencySingletonContainer)
            {
                var disposable = item.Value as IDisposable;
                disposable?.Dispose();
            }
        }

        public object[] GetDependecys(Type[] dependencyTypes)
        {
            return GetDependencies(dependencyTypes);
        }
    }
}