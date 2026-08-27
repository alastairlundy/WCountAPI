using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WCountLib.Abstractions.Models;
using WCountLib.Counters;
using WCountLib.Logic;

namespace WCountLib.Testing.Logic;

public class TextReaderLogicTests
{
    private const int ChunkSize = 8192;

    private readonly TextReaderLogic _logic;

    public TextReaderLogicTests()
    {
        _logic = new TextReaderLogic(new WordCounter(), new ByteCounter(), new CharacterCounter());
    }

    private static CancellationToken Ct => CancellationToken.None;

    [Test]
    public async Task EmptyInput_ReturnsZeros()
    {
        using StringReader reader = new("");
        WCountInfo info = await _logic.ReadTextReaderAsync(reader, true, true, true, true, null, Ct);

        await Assert.That(info.WordCount).IsEqualTo(0L);
        await Assert.That(info.LineCount).IsEqualTo(0L);
        await Assert.That(info.CharCount).IsEqualTo(0L);
        await Assert.That(info.ByteCount).IsEqualTo(0L);
    }

    [Test]
    public async Task LfOnly_LineCount()
    {
        using StringReader reader = new("line1\nline2\nline3\n");
        WCountInfo info = await _logic.ReadTextReaderAsync(reader, false, true, false, false, null, Ct);

        await Assert.That(info.LineCount).IsEqualTo(3L);
    }

    [Test]
    public async Task CrlfOnly_LineCount()
    {
        using StringReader reader = new("line1\r\nline2\r\nline3\r\n");
        WCountInfo info = await _logic.ReadTextReaderAsync(reader, false, true, false, false, null, Ct);

        await Assert.That(info.LineCount).IsEqualTo(3L);
    }

    [Test]
    public async Task CrlfStraddlingChunkBoundary_CountsAsOneLine()
    {
        // The '\r' lands on the last char of chunk one and the '\n' on the first char of chunk two,
        // so the CR/LF pair must be carried across the boundary rather than counted twice.
        string input = new string('a', ChunkSize - 1) + "\r\n";
        using StringReader reader = new(input);
        WCountInfo info = await _logic.ReadTextReaderAsync(reader, false, true, false, false, null, Ct);

        await Assert.That(info.LineCount).IsEqualTo(1L);
    }

    [Test]
    public async Task WordStraddlingChunkBoundary_CountsAsOneWord()
    {
        // A single unbroken word longer than one chunk. The word counter sees a word in each chunk,
        // so the in-word continuation adjustment must collapse them back into one.
        string input = new string('a', ChunkSize + 100);
        using StringReader reader = new(input);
        WCountInfo info = await _logic.ReadTextReaderAsync(reader, true, false, false, false, null, Ct);

        await Assert.That(info.WordCount).IsEqualTo(1L);
    }

    [Test]
    public async Task WordsAroundChunkBoundary_CountedIndependently()
    {
        // "alpha" ends before the boundary, the padding word straddles it, "omega" starts after.
        string input = "alpha " + new string('x', ChunkSize) + " omega";
        using StringReader reader = new(input);
        WCountInfo info = await _logic.ReadTextReaderAsync(reader, true, false, false, false, null, Ct);

        await Assert.That(info.WordCount).IsEqualTo(3L);
    }

    [Test]
    public async Task NoTrailingNewline_CountsFinalLine()
    {
        using StringReader reader = new("hello world");
        WCountInfo info = await _logic.ReadTextReaderAsync(reader, false, true, false, false, null, Ct);

        await Assert.That(info.LineCount).IsEqualTo(1L);
    }

    [Test]
    public async Task SelectiveFlags_WordOnly_LeavesOtherCountsNull()
    {
        using StringReader reader = new("hello world");
        WCountInfo info = await _logic.ReadTextReaderAsync(reader, true, false, false, false, null, Ct);

        await Assert.That(info.WordCount).IsEqualTo(2L);
        await Assert.That(info.LineCount).IsNull();
        await Assert.That(info.CharCount).IsNull();
        await Assert.That(info.ByteCount).IsNull();
    }

    [Test]
    public async Task ExplicitNonUtf8Encoding_ChangesByteCount()
    {
        const string input = "hello";

        using StringReader utf8Reader = new(input);
        WCountInfo utf8Info = await _logic.ReadTextReaderAsync(utf8Reader, false, false, false, true, null, Ct);

        using StringReader unicodeReader = new(input);
        WCountInfo unicodeInfo = await _logic.ReadTextReaderAsync(unicodeReader, false, false, false, true,
            Encoding.Unicode, Ct);

        await Assert.That(utf8Info.ByteCount).IsEqualTo(5L);
        await Assert.That(unicodeInfo.ByteCount).IsEqualTo(10L);
    }

    [Test]
    public async Task SameInstance_ReusedSequentially_DoesNotLeakStateBetweenReads()
    {
        // TextReaderLogic is registered as a DI singleton, so per-read state must not persist.
        // A first read ending mid-word must not leave the instance flagged as "in a word",
        // which would silently swallow the first word of the next read.
        using StringReader first = new("trailing");
        WCountInfo firstInfo = await _logic.ReadTextReaderAsync(first, true, true, false, false, null, Ct);

        using StringReader second = new("trailing");
        WCountInfo secondInfo = await _logic.ReadTextReaderAsync(second, true, true, false, false, null, Ct);

        await Assert.That(secondInfo.WordCount).IsEqualTo(firstInfo.WordCount);
        await Assert.That(secondInfo.LineCount).IsEqualTo(firstInfo.LineCount);
        await Assert.That(secondInfo.WordCount).IsEqualTo(1L);
        await Assert.That(secondInfo.LineCount).IsEqualTo(1L);
    }

    [Test]
    public async Task SameInstance_ConcurrentReads_ProduceIndependentResults()
    {
        const string text = "one two three\nfour five six\n";
        CancellationToken ct = Ct;

        Task<WCountInfo>[] reads = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(async () =>
            {
                using StringReader reader = new(text);
                return await _logic.ReadTextReaderAsync(reader, true, true, false, false, null, ct);
            }, ct))
            .ToArray();

        WCountInfo[] results = await Task.WhenAll(reads);

        foreach (WCountInfo result in results)
        {
            await Assert.That(result.WordCount).IsEqualTo(6L);
            await Assert.That(result.LineCount).IsEqualTo(2L);
        }
    }
}
