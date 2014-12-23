﻿using System;
using System.Linq;
using Mond.Binding;
using NUnit.Framework;

namespace Mond.Tests.Binding
{
    [TestFixture]
    public class FunctionTests
    {
        private MondState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new MondState();

            // TODO: need a better function binder
            _state["ArgumentTypes"] = MondFunctionBinder.Bind(null, "ArgumentTypes", typeof(FunctionTests).GetMethod("ArgumentTypes"));
            _state["Add"] = MondFunctionBinder.Bind(null, "Add", typeof(FunctionTests).GetMethod("Add"));
            _state["Concat"] = MondFunctionBinder.Bind(null, "Concat", typeof(FunctionTests).GetMethod("Concat"));
            _state["Greet"] = MondFunctionBinder.Bind(null, "Greet", typeof(FunctionTests).GetMethod("Greet"));
        }

        [Test]
        public void Arguments()
        {
            var result = _state.Run(@"
                return global.ArgumentTypes(1, 2, 3, 4, 5, 6, 7, 8, '9', true);
            ");

            Assert.True(result["a"] == 1);
            Assert.True(result["b"] == 2);
            Assert.True(result["c"] == 3);
            Assert.True(result["d"] == 4);
            Assert.True(result["e"] == 5);
            Assert.True(result["f"] == 6);
            Assert.True(result["g"] == 7);
            Assert.True(result["h"] == 8);
            Assert.True(result["i"] == "9");
            Assert.True(result["j"] == true);
        }

        [Test]
        public void ArgumentChecks()
        {
            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.ArgumentTypes(1, 2, 3);
            "), "too few");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.ArgumentTypes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
            "), "too many");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.ArgumentTypes('1', 2, 3, 4, 5, 6, 7, 8, '9', true);
            "), "check double");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.ArgumentTypes(1, '2', 3, 4, 5, 6, 7, 8, '9', true);
            "), "check float");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.ArgumentTypes(1, 2, '3', 4, 5, 6, 7, 8, '9', true);
            "), "check int");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.ArgumentTypes(1, 2, 3, '4', 5, 6, 7, 8, '9', true);
            "), "check uint");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.ArgumentTypes(1, 2, 3, 4, '5', 6, 7, 8, '9', true);
            "), "check short");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.ArgumentTypes(1, 2, 3, 4, 5, '6', 7, 8, '9', true);
            "), "check ushort");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.ArgumentTypes(1, 2, 3, 4, 5, 6, '7', 8, '9', true);
            "), "check sbyte");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.ArgumentTypes(1, 2, 3, 4, 5, 6, 7, '8', '9', true);
            "), "check byte");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.ArgumentTypes(1, 2, 3, 4, 5, 6, 7, 8, 9, true);
            "), "check string");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return global.ArgumentTypes(1, 2, 3, 4, 5, 6, 7, 8, '9', 10);
            "), "check bool");
        }

        [Test]
        public void StateArgument()
        {
            Assert.True(_state.Run(@"
                global.Add(1, 2);
                return global.result;
            ") == 3);

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                global.Add(1, 2, 3);
            "));
        }

        [Test]
        public void ParamsArgument()
        {
            Assert.True(_state.Run(@"
                return global.Concat('test');
            ") == "test");

            Assert.True(_state.Run(@"
                return global.Concat('hello', ' world', '!');
            ") == "hello world!");
        }

        [Test]
        public void ClassArgument()
        {
            var person = new ClassTests.Person(new MondValue(MondValueType.Object), "Rohan");

            var personValue = new MondValue(_state);
            personValue.UserData = person;

            _state["rohan"] = personValue;

            Assert.True(_state.Run(@"
                return global.Greet(global.rohan);
            ") == "hello Rohan!");

            personValue.UserData = "something";

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                global.Greet(global.rohan);
            "));

            personValue.UserData = null;

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                global.Greet(global.rohan);
            "));
        }

        // TODO: need to test return types

        public static MondValue ArgumentTypes(
            double  a,  float   b,
            int     c,  uint    d,
            short   e,  ushort  f,
            sbyte   g,  byte    h,
            string  i,  bool    j)
        {
            var result = new MondValue(MondValueType.Object);

            result["a"] = a;
            result["b"] = b;
            result["c"] = c;
            result["d"] = d;
            result["e"] = e;
            result["f"] = f;
            result["g"] = g;
            result["h"] = h;
            result["i"] = i;
            result["j"] = j;

            return result;
        }

        public static void Add(MondValue a, MondState state, MondValue b)
        {
            state["result"] = a + b;
        }

        public static string Concat(string first, params MondValue[] values)
        {
            return first + string.Concat(values.Select(v => (string)v));
        }

        public static string Greet(ClassTests.Person person)
        {
            return person.GenerateGreeting();
        }
    }
}
