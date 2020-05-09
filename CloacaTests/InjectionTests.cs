using System.Collections.Generic;

using NUnit.Framework;

using LanguageImplementation;
using CloacaInterpreter;
using System.Reflection;
using System;

namespace CloacaTests
{
    [TestFixture]
    public class InjectionTests
    {
        public void NoArgsFunction()
        {

        }

        public void WantsInterpreterFunction(IInterpreter interpreter)
        {

        }

        public void WantsBothFunction(IInterpreter interpreter, FrameContext context)
        {

        }

        public void WantsBothAndArgFunction(IInterpreter interpreter, FrameContext context, int i)
        {

        }

        public void WantsBothAndArgAndParamFunction(IInterpreter interpreter, FrameContext context, int i, params string[] extras)
        {

        }

        public void WantsOnlyParamFunction(params string[] extras)
        {

        }

        Interpreter interpreter;
        FrameContext context;
        Injector injector;

        [SetUp]
        public void Setup()
        {
            var scheduler = new Scheduler();
            interpreter = new Interpreter(scheduler);
            context = new FrameContext();
            injector = new Injector(interpreter, context, scheduler);
        }

        [Test]
        public void NoArgs()
        {
            var out_args = injector.Inject(typeof(InjectionTests).GetMethod(nameof(NoArgsFunction)), new object[0]);
            Assert.That(out_args.Length, Is.Zero);
        }

        [Test]
        public void WantsInterpreter()
        {
            var out_args = injector.Inject(typeof(InjectionTests).GetMethod(nameof(WantsInterpreterFunction)), new object[0]);
            Assert.That(out_args.Length, Is.EqualTo(1));
            Assert.That(out_args[0], Is.EqualTo(interpreter));
        }

        [Test]
        public void WantsOnlyParamSpecified()
        {
            var out_args = injector.Inject(typeof(InjectionTests).GetMethod(nameof(WantsOnlyParamFunction)), new object[1] { "Yay!" });
            Assert.That(out_args.Length, Is.EqualTo(1));
            var params_array = (string[])out_args[0];
            Assert.That(params_array[0], Is.EqualTo("Yay!"));
        }

        [Test]
        public void WantsOnlyParamUnspecified()
        {
            var out_args = injector.Inject(typeof(InjectionTests).GetMethod(nameof(WantsOnlyParamFunction)), new object[0]);
            Assert.That(out_args.Length, Is.EqualTo(1));
            Assert.That(out_args[0], Is.Null);
        }

        [Test]
        public void WantsBoth()
        {
            var out_args = injector.Inject(typeof(InjectionTests).GetMethod(nameof(WantsBothFunction)), new object[0]);
            Assert.That(out_args.Length, Is.EqualTo(2));
            Assert.That(out_args[0], Is.EqualTo(interpreter));
            Assert.That(out_args[1], Is.EqualTo(context));
        }

        [Test]
        public void WantsBothAndArg()
        {
            var out_args = injector.Inject(typeof(InjectionTests).GetMethod(nameof(WantsBothAndArgFunction)), new object[1] { 1 });
            Assert.That(out_args.Length, Is.EqualTo(3));
            Assert.That(out_args[0], Is.EqualTo(interpreter));
            Assert.That(out_args[1], Is.EqualTo(context));
            Assert.That(out_args[2], Is.EqualTo(1));
        }

        [Test]
        public void WantsBothAndArgAndParamSpecified()
        {
            var out_args = injector.Inject(typeof(InjectionTests).GetMethod(nameof(WantsBothAndArgAndParamFunction)), new object[2] { 1, "Yay!"});
            Assert.That(out_args.Length, Is.EqualTo(4));
            Assert.That(out_args[0], Is.EqualTo(interpreter));
            Assert.That(out_args[1], Is.EqualTo(context));
            Assert.That(out_args[2], Is.EqualTo(1));
            var params_array = (string[]) out_args[3];
            Assert.That(params_array[0], Is.EqualTo("Yay!"));
        }

        [Test]
        public void WantsBothAndArgAndParamUnspecified()
        {
            var out_args = injector.Inject(typeof(InjectionTests).GetMethod(nameof(WantsBothAndArgAndParamFunction)), new object[1] { 1 });
            Assert.That(out_args.Length, Is.EqualTo(4));
            Assert.That(out_args[0], Is.EqualTo(interpreter));
            Assert.That(out_args[1], Is.EqualTo(context));
            Assert.That(out_args[2], Is.EqualTo(1));
            Assert.That(out_args[3], Is.Null);
        }

    }
}
