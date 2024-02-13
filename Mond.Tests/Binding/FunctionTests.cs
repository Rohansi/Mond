using System.Collections.Generic;
using System.Linq;
using Mond.Binding;
using NUnit.Framework;

namespace Mond.Tests.Binding
{
    [TestFixture]
    [MondModule(bareMethods: true)]
    public partial class FunctionTests
    {
        private MondState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new MondState
            {
                Libraries =
                {
                    new ClassTests.Person.Library(),
                    new Library(),
                },
            };
        }

        [Test]
        public void Arguments()
        {
            var result = _state.Run(@"
                return argumentTypes(1, 2, 3, 4, 5, 6, 7, 8, '9', true);
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
        public void ReturnTypes()
        {
            var types = new List<string>
            {
                "Double", "Float",
                "Int", "UInt",
                "Short", "UShort",
                "SByte", "Byte",
                "String", "Bool",
                "Void",
                "NullString"
            };

            var results = new List<MondValue>
            {
                double.MaxValue, float.MaxValue,
                int.MaxValue, uint.MaxValue,
                short.MaxValue, ushort.MaxValue,
                sbyte.MaxValue, byte.MaxValue,
                "a string", true,
                MondValue.Undefined,
                MondValue.Null, MondValue.Null
            };

            for (var i = 0; i < types.Count; i++)
            {
                var result = _state.Run(string.Format(@"
                    return return{0}();
                ", types[i]));

                Assert.True(result == results[i], types[i]);
            }

            {
                var result = _state.Run(@"
                    return returnClass();
                ");

                Assert.True(result.Type == MondValueType.Object);

                var person = result.UserData as ClassTests.Person;

                Assert.True(person != null);

                Assert.True(person.Name == "Test");
            }
        }

        [Test]
        public void ArgumentChecks()
        {
            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return argumentTypes(1, 2, 3);
            "), "too few");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return argumentTypes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
            "), "too many");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return argumentTypes('1', 2, 3, 4, 5, 6, 7, 8, '9', true);
            "), "check double");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return argumentTypes(1, '2', 3, 4, 5, 6, 7, 8, '9', true);
            "), "check float");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return argumentTypes(1, 2, '3', 4, 5, 6, 7, 8, '9', true);
            "), "check int");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return argumentTypes(1, 2, 3, '4', 5, 6, 7, 8, '9', true);
            "), "check uint");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return argumentTypes(1, 2, 3, 4, '5', 6, 7, 8, '9', true);
            "), "check short");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return argumentTypes(1, 2, 3, 4, 5, '6', 7, 8, '9', true);
            "), "check ushort");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return argumentTypes(1, 2, 3, 4, 5, 6, '7', 8, '9', true);
            "), "check sbyte");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return argumentTypes(1, 2, 3, 4, 5, 6, 7, '8', '9', true);
            "), "check byte");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return argumentTypes(1, 2, 3, 4, 5, 6, 7, 8, 9, true);
            "), "check string");

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                return argumentTypes(1, 2, 3, 4, 5, 6, 7, 8, '9', 10);
            "), "check bool");
        }

        [Test]
        public void StateArgument()
        {
            Assert.True(_state.Run(@"
                add(1, 2);
                return global.result;
            ") == 3);

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                add(1, 2, 3);
            "));
        }

        [Test]
        public void ParamsArgument()
        {
            Assert.True(_state.Run(@"
                return concat('test');
            ") == "test");

            Assert.True(_state.Run(@"
                return concat('hello', ' world', '!');
            ") == "hello world!");
        }

        [Test]
        public void ClassArgument()
        {
            var person = new ClassTests.Person("Rohan");

            var personValue = MondValue.Object(_state);
            personValue.UserData = person;

            _state["rohan"] = personValue;

            Assert.True(_state.Run(@"
                return greet(global.rohan);
            ") == "hello Rohan!");

            personValue.UserData = "something";

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                greet(global.rohan);
            "));

            personValue.UserData = null;

            Assert.Throws<MondRuntimeException>(() => _state.Run(@"
                greet(global.rohan);
            "));
        }

        [MondFunction]
        public static MondValue ArgumentTypes(
            double  a,  float   b,
            int     c,  uint    d,
            short   e,  ushort  f,
            sbyte   g,  byte    h,
            string  i,  bool    j)
        {
            var result = MondValue.Object();

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

        #region Return Types

        [MondFunction]
        public static double ReturnDouble()
        {
            return double.MaxValue;
        }

        [MondFunction]
        public static float ReturnFloat()
        {
            return float.MaxValue;
        }

        [MondFunction]
        public static int ReturnInt()
        {
            return int.MaxValue;
        }

        [MondFunction]
        public static uint ReturnUInt()
        {
            return uint.MaxValue;
        }

        [MondFunction]
        public static short ReturnShort()
        {
            return short.MaxValue;
        }

        [MondFunction]
        public static ushort ReturnUShort()
        {
            return ushort.MaxValue;
        }

        [MondFunction]
        public static sbyte ReturnSByte()
        {
            return sbyte.MaxValue;
        }

        [MondFunction]
        public static byte ReturnByte()
        {
            return byte.MaxValue;
        }

        [MondFunction]
        public static string ReturnString()
        {
            return "a string";
        }

        [MondFunction]
        public static bool ReturnBool()
        {
            return true;
        }

        [MondFunction]
        public static void ReturnVoid()
        {

        }

        [MondFunction]
        public static string ReturnNullString()
        {
            return null;
        }

        [MondFunction]
        public static ClassTests.Person ReturnClass()
        {
            return new ClassTests.Person("Test");
        }

        #endregion

        [MondFunction]
        public static void Add(MondValue a, MondState state, MondValue b)
        {
            state["result"] = a + b;
        }

        [MondFunction]
        public static string Concat(string first, params MondValue[] values)
        {
            return first + string.Concat(values.Select(v => (string)v));
        }

        [MondFunction]
        public static string Greet(ClassTests.Person person)
        {
            return person.GenerateGreeting();
        }

#if false
        [MondFunction]
        public static decimal UnsupportedReturn()
        {
            return default;
        }

        [MondFunction]
        public static void UnsupportedParameter(decimal x)
        {
        }

        [MondFunction]
        public decimal UnsupportedProperty { get; set; }
#endif
    }
}
