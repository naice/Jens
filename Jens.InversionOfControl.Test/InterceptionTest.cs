using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jens.InversionOfControl.Test
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class AttributeInterceptorTestAttribute : Attribute
    {
        // This is a positional argument
        public AttributeInterceptorTestAttribute()
        { }
    }

    public class AttributeTestInterceptorException : Exception
    { }

    public class AttributeTestInterceptor : AttributeInterceptor<AttributeInterceptorTestAttribute>
    {
        public bool ThrowOnIntercept { get; set; } = false;

        public AttributeTestInterceptor WithThrowOnIntercept(bool value = true)
        {
            ThrowOnIntercept = value;
            return this;
        }

        public override void Intercept(IInvocation invocation, AttributeInterceptorTestAttribute attribute)
        {
            if (ThrowOnIntercept)
                throw new AttributeTestInterceptorException();
        }
    }

    public interface IProxyTestClass
    {
        string TestProp { get; }
        [AttributeInterceptorTest]
        bool ExecuteStuff();
    }

    [Intercept(typeof(IProxyTestClass), typeof(AttributeTestInterceptor))]
    public class ProxyTestClass : IProxyTestClass
    {
        public string TestProp => "test";

        public bool ExecuteStuff()
        {
            return true;
        }
    }



    [TestClass]
    public class BasicInterceptionTest
    {
        [TestMethod]
        public void BasicInterceptionShouldThrow()
        {
            var intf = ProxyGenerator.Create<IProxyTestClass>(new ProxyTestClass(), new AttributeTestInterceptor().WithThrowOnIntercept());
            Assert.AreEqual(intf.TestProp, "test");
            Assert.ThrowsException<AttributeTestInterceptorException>(() => intf.ExecuteStuff());
        }
        [TestMethod]
        public void BasicInterceptionShouldNotThrow()
        {
            var intf = ProxyGenerator.Create<IProxyTestClass>(new ProxyTestClass(), new AttributeTestInterceptor());
            Assert.AreEqual(intf.TestProp, "test");
            intf.ExecuteStuff();
        }
        [TestMethod]
        public void AutoWiredInterceptionShouldThrow()
        {
            var container = new Container()
                .WithSingleton<AttributeTestInterceptor>()
                .WithSingleton<IProxyTestClass, ProxyTestClass>();
            container.GetDependency<AttributeTestInterceptor>().WithThrowOnIntercept();
            var intf = container.GetDependency<IProxyTestClass>();
            Assert.AreEqual(intf.TestProp, "test");
            Assert.ThrowsException<AttributeTestInterceptorException>(() => intf.ExecuteStuff());
        }
        [TestMethod]
        public void AutoWiredInterceptionShouldNotThrow()
        {
            var container = new Container()
                .WithSingleton<AttributeTestInterceptor>()
                .WithSingleton<IProxyTestClass, ProxyTestClass>();

            var intf = container.GetDependency<IProxyTestClass>();
            Assert.AreEqual(intf.TestProp, "test");
            intf.ExecuteStuff();
        }
    }
}
