using NUnit.Framework;

namespace Mond.Tests.Expressions
{
    [TestFixture]
    public class SwitchExpressionTests
    {
        [Test]
        public void JumpTableArms()
        {
            var result = Script.Run(@"
                fun describe(x) {
                    return x switch {
                        1 -> 'one',
                        2 -> 'two',
                        3 -> 'three',
                        4 -> 'four',
                        _ -> 'many'
                    };
                }

                return describe(1) + describe(3) + describe(9);
            ");

            Assert.That(result, Is.EqualTo((MondValue)"onethreemany"));
        }

        [Test]
        public void MultipleValuesPerArm()
        {
            var result = Script.Run(@"
                fun describe(x) {
                    return x switch {
                        'a', 'b' -> 1,
                        'c' -> 2,
                        _ -> 3
                    };
                }

                return [describe('a'), describe('b'), describe('c'), describe('d')];
            ");

            Assert.That(result[0], Is.EqualTo((MondValue)1));
            Assert.That(result[1], Is.EqualTo((MondValue)1));
            Assert.That(result[2], Is.EqualTo((MondValue)2));
            Assert.That(result[3], Is.EqualTo((MondValue)3));
        }

        [Test]
        public void NonConstantArms()
        {
            var result = Script.Run(@"
                const a = 10;
                var b = 20;

                fun describe(x) {
                    return x switch {
                        a -> 'a',
                        b -> 'b',
                        _ -> 'other'
                    };
                }

                return describe(10) + describe(20) + describe(30);
            ");

            Assert.That(result, Is.EqualTo((MondValue)"abother"));
        }

        [Test]
        public void TrailingComma()
        {
            var result = Script.Run(@"
                return 1 switch {
                    1 -> 'one',
                    _ -> 'other',
                };
            ");

            Assert.That(result, Is.EqualTo((MondValue)"one"));
        }

        [Test]
        public void BindingArmWithGuard()
        {
            var result = Script.Run(@"
                fun describe(x) {
                    return x switch {
                        var n when n > 100 -> 'big',
                        var n when n > 10 -> 'medium',
                        _ -> 'small'
                    };
                }

                return describe(500) + describe(50) + describe(5);
            ");

            Assert.That(result, Is.EqualTo((MondValue)"bigmediumsmall"));
        }

        [Test]
        public void ValueArmWithGuard()
        {
            var result = Script.Run(@"
                var flag = false;

                fun describe(x) {
                    return x switch {
                        1 when flag -> 'flagged',
                        1 -> 'one',
                        _ -> 'other'
                    };
                }

                var a = describe(1);
                flag = true;
                return a + describe(1);
            ");

            Assert.That(result, Is.EqualTo((MondValue)"oneflagged"));
        }

        [Test]
        public void ObjectDestructuringArm()
        {
            var result = Script.Run(@"
                fun describe(x) {
                    return x switch {
                        { name, age } -> name + ' is ' + age,
                        { name } -> name,
                        _ -> 'unknown'
                    };
                }

                return [
                    describe({ name: 'bob', age: 5 }),
                    describe({ name: 'joe' }),
                    describe(123)
                ];
            ");

            Assert.That(result[0], Is.EqualTo((MondValue)"bob is 5"));
            Assert.That(result[1], Is.EqualTo((MondValue)"joe"));
            Assert.That(result[2], Is.EqualTo((MondValue)"unknown"));
        }

        [Test]
        public void ObjectDestructuringAlias()
        {
            var result = Script.Run(@"
                return { name: 'bob' } switch {
                    { name: n } -> n,
                    _ -> 'unknown'
                };
            ");

            Assert.That(result, Is.EqualTo((MondValue)"bob"));
        }

        [Test]
        public void ArrayDestructuringArm()
        {
            var result = Script.Run(@"
                fun describe(x) {
                    return x switch {
                        [a, b] -> a + b,
                        [a, ...rest] -> a + rest.length(),
                        _ -> -1
                    };
                }

                return [describe([1, 2]), describe([1, 2, 3]), describe('nope')];
            ");

            Assert.That(result[0], Is.EqualTo((MondValue)3));
            Assert.That(result[1], Is.EqualTo((MondValue)3));
            Assert.That(result[2], Is.EqualTo((MondValue)(-1)));
        }

        [Test]
        public void ArmScopesAreIsolated()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                var x = { a: 1 } switch {
                    { a } -> a,
                    _ -> 0
                };

                return a;
            "));
        }

        [Test]
        public void NestedInExpressions()
        {
            var result = Script.Run(@"
                fun add(a, b) -> a + b;

                var x = 1;
                return add(x switch { 1 -> 10, _ -> 0 }, x == 1 ? x switch { 1 -> 5, _ -> 0 } : 0);
            ");

            Assert.That(result, Is.EqualTo((MondValue)15));
        }

        [Test]
        public void NestedSwitchExpression()
        {
            var result = Script.Run(@"
                return 1 switch {
                    1 -> 2 switch {
                        2 -> 'nested',
                        _ -> 'no'
                    },
                    _ -> 'no'
                };
            ");

            Assert.That(result, Is.EqualTo((MondValue)"nested"));
        }

        [Test]
        public void SubjectEvaluatedOnce()
        {
            var result = Script.Run(@"
                var calls = 0;
                fun next() {
                    calls++;
                    return 3;
                }

                var value = next() switch {
                    1 -> 'one',
                    3 -> 'three',
                    _ -> 'other'
                };

                return value + calls;
            ");

            Assert.That(result, Is.EqualTo((MondValue)"three1"));
        }

        [Test]
        public void SwitchStatementStillWorks()
        {
            var result = Script.Run(@"
                var x = 2;
                switch (x) {
                    case 1:
                        return 'one';
                    case 2:
                        return 'two';
                    default:
                        return 'other';
                }
            ");

            Assert.That(result, Is.EqualTo((MondValue)"two"));
        }

        [Test]
        public void MissingDiscardArm()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return 1 switch {
                    1 -> 'one'
                };
            "));
        }

        [Test]
        public void ArmAfterDiscardArm()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return 1 switch {
                    _ -> 'other',
                    1 -> 'one'
                };
            "));
        }

        [Test]
        public void DiscardArmCantHaveGuard()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return 1 switch {
                    _ when true -> 'other'
                };
            "));
        }

        [Test]
        public void DuplicateConstantArms()
        {
            Assert.Throws<MondCompilerException>(() => Script.Run(@"
                return 1 switch {
                    1 -> 'one',
                    1 -> 'uno',
                    _ -> 'other'
                };
            "));
        }
    }
}
