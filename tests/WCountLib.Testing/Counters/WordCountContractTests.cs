using WCountLib.Counters;

namespace WCountLib.Testing.Counters;

public class WordCountContractTests
{
    private readonly WordCounter _counter = new();

    [Test]
    public async Task WhitespaceSeparatedTokens_CountedAsWords()
    {
        await Assert.That(_counter.CountWords("hello world")).IsEqualTo(2);
    }

    [Test]
    public async Task PunctuationOnlyToken_CountsAsOneWord()
    {
        // wc counts any non-whitespace run as a word; "---" is one token.
        await Assert.That(_counter.CountWords("---")).IsEqualTo(1);
    }

    [Test]
    public async Task ApostropheToken_CountsAsOneWord()
    {
        await Assert.That(_counter.CountWords("don't!")).IsEqualTo(1);
    }

    [Test]
    public async Task CommaSeparated_CountsAsOneWord()
    {
        await Assert.That(_counter.CountWords("a,b,c")).IsEqualTo(1);
    }

    [Test]
    public async Task MultipleSpaces_Collapsed()
    {
        await Assert.That(_counter.CountWords("multiple   spaces   here")).IsEqualTo(3);
    }

    [Test]
    public async Task EmptyString_IsZero()
    {
        await Assert.That(_counter.CountWords("")).IsEqualTo(0);
    }

    [Test]
    public async Task WhitespaceOnly_IsZero()
    {
        await Assert.That(_counter.CountWords("   ")).IsEqualTo(0);
    }

    [Test]
    public async Task TabSeparated_CountedAsWords()
    {
        await Assert.That(_counter.CountWords("tab\tseparated")).IsEqualTo(2);
    }
}
