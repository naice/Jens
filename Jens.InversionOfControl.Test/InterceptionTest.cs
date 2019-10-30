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

    public interface ITestInterface
    {
        string TestProp { get; }
        bool ExecuteStuff();
    }

    [Intercept(typeof(ITestInterface), typeof(AttributeTestInterceptor))]
    public class InterceptedTestImplementation : ITestInterface
    {
        public string TestProp => "test";

        [AttributeInterceptorTest]
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
            var intf = ProxyGenerator.Create<ITestInterface>(new InterceptedTestImplementation(), new AttributeTestInterceptor().WithThrowOnIntercept());
            Assert.AreEqual(intf.TestProp, "test");
            Assert.ThrowsException<AttributeTestInterceptorException>(() => intf.ExecuteStuff());
        }
        [TestMethod]
        public void BasicInterceptionShouldNotThrow()
        {
            var intf = ProxyGenerator.Create<ITestInterface>(new InterceptedTestImplementation(), new AttributeTestInterceptor());
            Assert.AreEqual(intf.TestProp, "test");
            intf.ExecuteStuff();
        }
        [TestMethod]
        public void AutoWiredInterceptionShouldThrow()
        {
            var container = new Container()
                .WithSingleton<AttributeTestInterceptor>()
                .WithSingleton<ITestInterface, InterceptedTestImplementation>();
            container.GetDependency<AttributeTestInterceptor>().WithThrowOnIntercept();
            var intf = container.GetDependency<ITestInterface>();
            Assert.AreEqual(intf.TestProp, "test");
            Assert.ThrowsException<AttributeTestInterceptorException>(() => intf.ExecuteStuff());
        }
        [TestMethod]
        public void AutoWiredInterceptionShouldNotThrow()
        {
            var container = new Container()
                .WithSingleton<AttributeTestInterceptor>()
                .WithSingleton<ITestInterface, InterceptedTestImplementation>();

            var intf = container.GetDependency<ITestInterface>();
            Assert.AreEqual(intf.TestProp, "test");
            intf.ExecuteStuff();
        }
    }
}
