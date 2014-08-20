using System;
using System.Collections.Generic;
using System.Linq;

namespace Mond.VirtualMachine.Prototypes
{
    static class StringPrototype
    {
        public static readonly MondValue Value;

        static StringPrototype()
        {
            Value = new MondValue(MondValueType.Object);
            Value["prototype"] = ValuePrototype.Value;

            Value["charAt"] = new MondInstanceFunction(CharAt);
            Value["contains"] = new MondInstanceFunction(Contains);
            Value["endsWith"] = new MondInstanceFunction(EndsWith);
            Value["indexOf"] = new MondInstanceFunction(IndexOf);
            Value["insert"] = new MondInstanceFunction(Insert);
            Value["lastIndexOf"] = new MondInstanceFunction(LastIndexOf);
            Value["replace"] = new MondInstanceFunction(Replace);
            Value["split"] = new MondInstanceFunction(Split);
            Value["startsWith"] = new MondInstanceFunction(StartsWith);
            Value["substring"] = new MondInstanceFunction(Substring);
            Value["toUpper"] = new MondInstanceFunction(ToUpper);
            Value["toLower"] = new MondInstanceFunction(ToLower);
            Value["trim"] = new MondInstanceFunction(Trim);

            Value["length"] = new MondInstanceFunction(Length);
            Value["getEnumerator"] = new MondInstanceFunction(GetEnumerator);

            Value.Lock();
        }

        /// <summary>
        /// String charAt(Number index)
        /// </summary>
        private static MondValue CharAt(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("charAt", instance.Type, arguments, MondValueType.Number);

            var instStr = (string)instance;
            var index = (int)arguments[0];

            if (index < 0 || index >= instStr.Length)
                throw new MondRuntimeException("String.charAt: index out of bounds");

            return new string(instStr[index], 1);
        }

        /// <summary>
        /// Bool contains(String value)
        /// </summary>
        private static MondValue Contains(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("contains", instance.Type, arguments, MondValueType.String);
            return ((string)instance).Contains(arguments[0]);
        }

        /// <summary>
        /// Bool endsWith(String value)
        /// </summary>
        private static MondValue EndsWith(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("endsWith", instance.Type, arguments, MondValueType.String);
            return ((string)instance).EndsWith(arguments[0]);
        }

        /// <summary>
        /// Number indexOf(String value)
        /// </summary>
        private static MondValue IndexOf(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("indexOf", instance.Type, arguments, MondValueType.String);
            return ((string)instance).IndexOf(arguments[0]);
        }

        /// <summary>
        /// String insert(Number index, String value)
        /// </summary>
        private static MondValue Insert(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("insert", instance.Type, arguments, MondValueType.Number, MondValueType.String);

            var instStr = (string)instance;
            var index = (int)arguments[0];

            if (index < 0 || index > instStr.Length)
                throw new MondRuntimeException("String.insert: index out of bounds");

            return instStr.Insert(index, arguments[1]);
        }

        /// <summary>
        /// Number lastIndexOf(String value)
        /// </summary>
        private static MondValue LastIndexOf(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("lastIndexOf", instance.Type, arguments, MondValueType.String);
            return ((string)instance).LastIndexOf(arguments[0]);
        }

        /// <summary>
        /// String replace(String oldValue, String newValue)
        /// </summary>
        private static MondValue Replace(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("replace", instance.Type, arguments, MondValueType.String, MondValueType.String);

            var oldValue = (string)arguments[0];
            if (string.IsNullOrEmpty(oldValue))
                return instance;

            return ((string)instance).Replace(oldValue, arguments[1]);
        }

        /// <summary>
        /// Array split(String separator)
        /// </summary>
        private static MondValue Split(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("split", instance.Type, arguments, MondValueType.String);

            var values = ((string)instance).Split(new string[] { arguments[0] }, StringSplitOptions.None);
            var result = new MondValue(MondValueType.Array);

            result.ArrayValue.AddRange(values.Select(v => (MondValue)v));

            return result;
        }

        /// <summary>
        /// Bool startsWith(String value)
        /// </summary>
        private static MondValue StartsWith(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("startsWith", instance.Type, arguments, MondValueType.String);
            return ((string)instance).StartsWith(arguments[0]);
        }

        /// <summary>
        /// String substring(Number startIndex)
        /// String substring(Number startIndex, Number length)
        /// </summary>
        private static MondValue Substring(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("substring", instance.Type, arguments, MondValueType.Number);

            var instStr = (string)instance;
            var startIndex = (int)arguments[0];

            if (startIndex < 0 || startIndex >= instStr.Length)
                return "";

            if (arguments.Length <= 1)
                return instStr.Substring((int)arguments[0]);

            if (arguments[1].Type != MondValueType.Number)
                throw new MondRuntimeException("Argument 2 in String.substring must be of type Number");

            var length = (int)arguments[1];
            if (startIndex + length >= instStr.Length)
                length = Math.Max(instStr.Length - startIndex, 0);

            return instStr.Substring(startIndex, length);
        }

        /// <summary>
        /// String toUpper()
        /// </summary>
        private static MondValue ToUpper(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("toUpper", instance.Type, arguments);
            return ((string)instance).ToUpper();
        }

        /// <summary>
        /// String toLower()
        /// </summary>
        private static MondValue ToLower(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("toLower", instance.Type, arguments);
            return ((string)instance).ToLower();
        }

        /// <summary>
        /// String trim()
        /// </summary>
        private static MondValue Trim(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("trim", instance.Type, arguments);
            return ((string)instance).Trim();
        }

        /// <summary>
        /// Number length()
        /// </summary>
        private static MondValue Length(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("length", instance.Type, arguments);
            return ((string)instance).Length;
        }

        /// <summary>
        /// Object getEnumerator()
        /// </summary>
        private static MondValue GetEnumerator(MondState state, MondValue instance, MondValue[] arguments)
        {
            Check("getEnumerator", instance.Type, arguments);

            var enumerator = new MondValue(MondValueType.Object);
            var str = (string)instance;
            var i = 0;

            enumerator["current"] = MondValue.Null;
            enumerator["moveNext"] = new MondValue((_, args) =>
            {
                if (i >= str.Length)
                    return false;

                enumerator["current"] = str[i++];
                return true;
            });

            return enumerator;
        }

        private static void Check(string method, MondValueType type, IList<MondValue> arguments, params MondValueType[] requiredTypes)
        {
            if (type != MondValueType.String)
                throw new MondRuntimeException("String.{0} must be called on a String", method);

            if (arguments.Count < requiredTypes.Length)
                throw new MondRuntimeException("String.{0} must be called with {1} argument{2}", method, requiredTypes.Length, requiredTypes.Length == 1 ? "" : "s");

            for (var i = 0; i < requiredTypes.Length; i++)
            {
                if (requiredTypes[i] == MondValueType.Undefined)
                    continue;

                if (arguments[i].Type != requiredTypes[i])
                    throw new MondRuntimeException("Argument {1} in String.{0} must be of type {2}", method, i + 1, requiredTypes[i]);
            }
        }
    }
}
