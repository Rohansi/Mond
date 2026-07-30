using NUnit.Framework;

namespace Mond.Tests.Libraries;

[TestFixture]
internal class ParseModuleTests
{
    [Test]
    public void ParseFloat_ValidNumber()
    {
        var result = Script.Run(@"
            return parseFloat('123.456');
        ");

        Assert.That(result, Is.EqualTo((MondValue)123.456));
    }

    [Test]
    public void ParseFloat_InvalidNumber()
    {
        var result = Script.Run(@"
            return parseFloat('hello');
        ");

        Assert.That(result, Is.EqualTo(MondValue.Undefined));
    }

    [Test]
    public void ParseInt_ValidNumber()
    {
        var result = Script.Run(@"
            return parseInt('123');
        ");

        Assert.That(result, Is.EqualTo((MondValue)123));
    }

    [Test]
    public void ParseInt_InvalidNumber()
    {
        var result = Script.Run(@"
            return parseInt('hello');
        ");

        Assert.That(result, Is.EqualTo(MondValue.Undefined));
    }

    [Test]
    public void ParseHex_ValidNumber()
    {
        var result = Script.Run(@"
            return parseHex('DEaDb33F');
        ");

        Assert.That(result, Is.EqualTo((MondValue)0xDEaDb33F));
    }

    [Test]
    public void ParseHex_DigitsOnly()
    {
        var result = Script.Run(@"
            return parseHex('1000');
        ");

        Assert.That(result, Is.EqualTo((MondValue)0x1000));
    }

    [Test]
    public void ParseHex_InvalidNumber()
    {
        var result = Script.Run(@"
            return parseFloat('hello');
        ");

        Assert.That(result, Is.EqualTo(MondValue.Undefined));
    }
}
