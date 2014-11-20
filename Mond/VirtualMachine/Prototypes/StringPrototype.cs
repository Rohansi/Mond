﻿using System;
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
            Value.Prototype = ValuePrototype.Value;

            Value["charAt"] = new MondInstanceFunction(CharAt);
            Value["charCodeAt"] = new MondInstanceFunction(CharCodeAt);
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
            Value["format"] = new MondInstanceFunction(Format);

            Value["length"] = new MondInstanceFunction(Length);
            Value["getEnumerator"] = new MondInstanceFunction(GetEnumerator);

            Value.Freeze();
        }

        private const string IndexOutOfBounds = "String.{0}: index out of bounds";

        /// <summary>
        /// charAt(index: number): string
        /// </summary>
        private static MondValue CharAt(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("charAt", instance.Type, arguments, MondValueType.Number);

            var instStr = (string)instance;
            var index = (int)arguments[0];

            if (index < 0 || index >= instStr.Length)
                throw new MondRuntimeException(IndexOutOfBounds, "charAt");

            return new string(instStr[index], 1);
        }

        /// <summary>
        /// charCodeAt(index: number): number
        /// </summary>
        private static MondValue CharCodeAt(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("charCodeAt", instance.Type, arguments, MondValueType.Number);

            var instStr = (string)instance;
            var index = (int)arguments[0];

            if (index < 0 || index >= instStr.Length)
                throw new MondRuntimeException(IndexOutOfBounds, "charAt");

            return (double)instStr[index];
        }

        /// <summary>
        /// contains(value: string): bool
        /// </summary>
        private static MondValue Contains(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("contains", instance.Type, arguments, MondValueType.String);
            return ((string)instance).Contains(arguments[0]);
        }

        /// <summary>
        /// endsWith(value: string): bool
        /// </summary>
        private static MondValue EndsWith(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("endsWith", instance.Type, arguments, MondValueType.String);
            return ((string)instance).EndsWith(arguments[0]);
        }

        /// <summary>
        /// indexOf(value: string): number
        /// </summary>
        private static MondValue IndexOf(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("indexOf", instance.Type, arguments, MondValueType.String);
            return ((string)instance).IndexOf(arguments[0]);
        }

        /// <summary>
        /// insert(index: number, value: string): string
        /// </summary>
        private static MondValue Insert(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("insert", instance.Type, arguments, MondValueType.Number, MondValueType.String);

            var instStr = (string)instance;
            var index = (int)arguments[0];

            if (index < 0 || index > instStr.Length)
                throw new MondRuntimeException(IndexOutOfBounds, "insert");

            return instStr.Insert(index, arguments[1]);
        }

        /// <summary>
        /// lastIndexOf(value: string): number
        /// </summary>
        private static MondValue LastIndexOf(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("lastIndexOf", instance.Type, arguments, MondValueType.String);
            return ((string)instance).LastIndexOf(arguments[0]);
        }

        /// <summary>
        /// replace(oldValue: string, newValue: string): string
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
        /// split(separator: string): array
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
        /// startsWith(value: string): bool
        /// </summary>
        private static MondValue StartsWith(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("startsWith", instance.Type, arguments, MondValueType.String);
            return ((string)instance).StartsWith(arguments[0]);
        }

        /// <summary>
        /// substring(startIndex: number): string
        /// substring(startIndex: number, length: number): string
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
                throw new MondRuntimeException("Argument 2 in String.substring must be of type number");

            var length = (int)arguments[1];
            if (startIndex + length >= instStr.Length)
                length = Math.Max(instStr.Length - startIndex, 0);

            return instStr.Substring(startIndex, length);
        }

        /// <summary>
        /// toUpper(): string
        /// </summary>
        private static MondValue ToUpper(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("toUpper", instance.Type, arguments);
            return ((string)instance).ToUpper();
        }

        /// <summary>
        /// toLower(): string
        /// </summary>
        private static MondValue ToLower(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("toLower", instance.Type, arguments);
            return ((string)instance).ToLower();
        }

        /// <summary>
        /// trim(): string
        /// </summary>
        private static MondValue Trim(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("trim", instance.Type, arguments);
            return ((string)instance).Trim();
        }

        /// <summary>
        /// format(): string
        /// </summary>
        private static MondValue Format(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("format", instance.Type, arguments);

            var values = arguments.Select<MondValue, object>(x =>
            {
                //System.String.Format has certain format specifiers
                //that are valid for integers but not floats
                //(ex. String.Format( "{0:x2}", 1.23f ); throws FormatException
                //So we treat all whole numbers as integers, everything else
                //remains unchanged.
                if (x.Type == MondValueType.Number)
                {
                    if (x % 1.0 == 0.0)
                        return (int)x;

                    return (double)x;
                }

                return x.ToString();
            }).ToArray();

            return string.Format(instance.ToString(), values);
        }

        /// <summary>
        /// length(): number
        /// </summary>
        private static MondValue Length(MondState state, MondValue instance, params MondValue[] arguments)
        {
            Check("length", instance.Type, arguments);
            return ((string)instance).Length;
        }

        /// <summary>
        /// getEnumerator(): object
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

                enumerator["current"] = new string(str[i++], 1);
                return true;
            });

            enumerator["dispose"] = new MondFunction((_, args) => MondValue.Undefined);

            return enumerator;
        }

        private static void Check(string method, MondValueType type, IList<MondValue> arguments, params MondValueType[] requiredTypes)
        {
            if (type != MondValueType.String)
                throw new MondRuntimeException("String.{0}: must be called on a string", method);

            if (arguments.Count < requiredTypes.Length)
                throw new MondRuntimeException("String.{0}: must be called with {1} argument{2}", method, requiredTypes.Length, requiredTypes.Length == 1 ? "" : "s");

            for (var i = 0; i < requiredTypes.Length; i++)
            {
                if (requiredTypes[i] == MondValueType.Undefined)
                    continue;

                if (arguments[i].Type != requiredTypes[i])
                    throw new MondRuntimeException("String.{0}: argument {1} must be of type {2}", method, i + 1, requiredTypes[i]);
            }
        }
    }
}
