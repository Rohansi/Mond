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

        Assert.AreEqual((MondValue)123.456, result);
    }

    [Test]
    public void ParseFloat_InvalidNumber()
    {
        var result = Script.Run(@"
            return parseFloat('hello');
        ");

        Assert.AreEqual(MondValue.Undefined, result);
    }

    [Test]
    public void ParseInt_ValidNumber()
    {
        var result = Script.Run(@"
            return parseInt('123');
        ");

        Assert.AreEqual((MondValue)123, result);
    }

    [Test]
    public void ParseInt_InvalidNumber()
    {
        var result = Script.Run(@"
            return parseInt('hello');
        ");

        Assert.AreEqual(MondValue.Undefined, result);
    }

    [Test]
    public void ParseHex_ValidNumber()
    {
        var result = Script.Run(@"
            return parseHex('DEaDb33F');
        ");

        Assert.AreEqual((MondValue)0xDEaDb33F, result);
    }

    [Test]
    public void ParseHex_DigitsOnly()
    {
        var result = Script.Run(@"
            return parseHex('1000');
        ");

        Assert.AreEqual((MondValue)0x1000, result);
    }

    [Test]
    public void ParseHex_InvalidNumber()
    {
        var result = Script.Run(@"
            return parseFloat('hello');
        ");

        Assert.AreEqual(MondValue.Undefined, result);
    }
}
