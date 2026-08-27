using WCountLib.Counters;
using WCountLib.Testing.TestData;

namespace WCountLib.Testing.Counters;

public class WordCounterTest
{
    private readonly WordCounter _counter = new();

    [Test]
    [MethodDataSource<RealWordsTestData>(nameof(RealWordsTestData.GetAllData))]
    public async Task CountWords(string words, int expected)
    {
        int actual = _counter.CountWords(words);
        
        await Assert.That(actual).IsEqualTo(expected);
    }
}