using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Jens.InversionOfControl;
using System.Collections.Generic;
using System.Collections;

namespace Jens.InversionOfControl.Test
{
    [TestClass]
    public class ContainerTest
    {
        private class TypeA
        { }
        private class TypeB
        { }

        private class TypeC
        {
            private readonly TypeA _typeA;
            private readonly TypeB _typeB;

            public TypeC(TypeB typeB, TypeA typeA)
            {
                _typeA = typeA;
                _typeB = typeB;
            }
            public TypeC(TypeA typeA)
            {
                _typeA = typeA;
                _typeB = new TypeB();
            }
        }

        private class TypeD
        {
            private readonly TypeA _typeA;
            private readonly TypeB _typeB;
            private readonly TypeD _typeD;

            public TypeD(TypeB typeB, TypeD typeD)
            {
                _typeA = new TypeA();
                _typeB = typeB;
                _typeD = typeD;
            }
            public TypeD(TypeA typeA)
            {
                _typeA = typeA;
                _typeB = new TypeB();
            }
        }

        [TestMethod]
        public void SimpleContainerTest()
        {
            var container = new Container();

            container.WithSingleton<TypeA>();
            var result = container.GetDependencies(new Type[] { typeof(TypeA) });
            Assert.IsTrue(container.AreTypesKnown(new Type[] { typeof(IDependencyResolver) }));
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length == 1);
            Assert.IsTrue(result[0].GetType() == typeof(TypeA));
            Assert.IsTrue(container.GetDependency<TypeA>().GetType() == typeof(TypeA));
            Assert.IsTrue(container.GetDependency(typeof(TypeA)).GetType() == typeof(TypeA));
            Assert.IsTrue(container.GetDependency<TypeA>().GetHashCode() == container.GetDependency<TypeA>().GetHashCode());
            Assert.ThrowsException<InvalidOperationException>(() => container.WithSingleton<TypeA>());
            var typeB = new TypeB();
            container.WithSingleton(new Lazy<TypeB>(() => typeB));
            result = container.GetDependencies(new Type[] { typeof(TypeB) });
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length == 1);
            Assert.IsTrue(result[0].GetType() == typeof(TypeB));
            Assert.IsTrue(container.GetDependency<TypeB>().GetType() == typeof(TypeB));
            Assert.IsTrue(container.GetDependency(typeof(TypeB)).GetType() == typeof(TypeB));
            Assert.IsTrue(container.GetDependency<TypeB>().GetHashCode() == container.GetDependency<TypeB>().GetHashCode());
            Assert.ThrowsException<InvalidOperationException>(() => container.WithSingleton<TypeB>());
            Assert.ThrowsException<InvalidOperationException>(() => container.WithSingleton(new Lazy<TypeB>(() => typeB)));

            container = new Container();
            container.WithType<TypeA>();
            result = container.GetDependencies(new Type[] { typeof(TypeA) });
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length == 1);
            Assert.IsTrue(result[0].GetType() == typeof(TypeA));
            Assert.IsTrue(container.GetDependency<TypeA>().GetType() == typeof(TypeA));
            Assert.IsTrue(container.GetDependency(typeof(TypeA)).GetType() == typeof(TypeA));
            Assert.IsFalse(container.GetDependency<TypeA>().GetHashCode() == container.GetDependency<TypeA>().GetHashCode());
            Assert.ThrowsException<InvalidOperationException>(() => container.WithType<TypeA>());

            container = new Container();
            Assert.ThrowsException<InvalidOperationException>(() => container.GetDependency<TypeA>());
            Assert.ThrowsException<ArgumentNullException>(() => container.GetDependency(null));
            Assert.IsNotNull(container.GetDependency<IDependencyResolver>());

            container.WithSingleton(new List<string>());
            Assert.IsNotNull(container.GetDependency<IList>());

            container = new Container();
            container.WithType<TypeC>();
            Assert.ThrowsException<InvalidOperationException>(() => container.GetDependency<TypeC>());
            container.WithType<TypeA>();
            Assert.IsNotNull(container.GetDependency<TypeC>());
            container.WithType<TypeB>();
            Assert.IsNotNull(container.GetDependency<TypeC>());

            container = new Container();
            container.WithType<TypeD>();
            Assert.ThrowsException<InvalidOperationException>(() => container.GetDependency<TypeD>());
        }
    }
}
