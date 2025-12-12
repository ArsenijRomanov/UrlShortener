using Generator;
using NSubstitute;
using SharpJuice.Essentials;

namespace SnowFlakeGeneratorTests;

public class SnowFlakeGeneratorTests
{
    private readonly IClock _clock;
    private readonly SnowFlakeGeneratorConfig _config;
    private readonly SnowFlakeGenerator _sut;
    private readonly DateTimeOffset _baseTime;

    public SnowFlakeGeneratorTests()
    {
        _clock = Substitute.For<IClock>();
        _config = new SnowFlakeGeneratorConfig(1);
        _baseTime = _config.Epoch.AddDays(1);
        _sut = new SnowFlakeGenerator(_clock, _config);
    }

    [Fact]
    public void GenerateShortCode_WhenCalled_ReturnsNonEmptyString()
    {
        _clock.Now.Returns(_baseTime);

        var result = _sut.GenerateShortCode();

        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Matches("^[a-zA-Z0-9]+$", result);
    }

    [Fact]
    public void GenerateShortCode_WhenCalledTwiceAtSameTime_ReturnsDifferentCodes()
    {
        _clock.Now.Returns(_baseTime);

        var result1 = _sut.GenerateShortCode();
        var result2 = _sut.GenerateShortCode();

        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void GenerateShortCode_WhenCalledAtDifferentTimes_ReturnsDifferentCodes()
    {
        _clock.Now.Returns(_baseTime);
        var result1 = _sut.GenerateShortCode();

        _clock.Now.Returns(_baseTime.AddMilliseconds(1));
        var result2 = _sut.GenerateShortCode();

        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void GenerateShortCode_WithDifferentInstanceIds_ReturnsDifferentCodes()
    {
        _clock.Now.Returns(_baseTime);
        
        var config1 = new SnowFlakeGeneratorConfig(1);
        var generator1 = new SnowFlakeGenerator(_clock, config1);
        
        var config2 = new SnowFlakeGeneratorConfig(2);
        var generator2 = new SnowFlakeGenerator(_clock, config2);

        var result1 = generator1.GenerateShortCode();
        var result2 = generator2.GenerateShortCode();

        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void GenerateShortCode_WhenTimeIsBeforeEpoch_ThrowsException()
    {
        var beforeEpoch = _config.Epoch.AddMilliseconds(-1);
        _clock.Now.Returns(beforeEpoch);

        var exception = Assert.Throws<Exception>(() => _sut.GenerateShortCode());
        Assert.Equal("TODO", exception.Message);
    }

    [Fact]
    public void GenerateShortCode_WhenTimeIsAtEpoch_ThrowsException()
    {
        _clock.Now.Returns(_config.Epoch);

        var exception = Assert.Throws<Exception>(() => _sut.GenerateShortCode());
        Assert.Equal("TODO", exception.Message);
    }

    [Fact]
    public void GenerateShortCode_WhenTimeBitsOverflow_ThrowsException()
    {
        const long maxMillis = (1L << SnowFlakeGeneratorConfig.MillisBits) + 1;
        var overflowTime = _config.Epoch.AddMilliseconds(maxMillis);
        
        _clock.Now.Returns(overflowTime);

        var exception = Assert.Throws<Exception>(() => _sut.GenerateShortCode());
        Assert.Equal("TODO", exception.Message);
    }

    [Fact]
    public void GenerateShortCode_WhenSequenceOverflows_WaitsForNextTick()
    {
        const int maxSequence = (1 << SnowFlakeGeneratorConfig.SequenceBits) - 1;
        var responses = new List<DateTimeOffset>();
        
        for (var i = 0; i <= maxSequence; i++)
        {
            responses.Add(_baseTime);
        }
        
        responses.Add(_baseTime.AddMilliseconds(1));

        _clock.Now.Returns(responses[0], responses.Skip(1).ToArray());

        string? lastCode = null;
        for (var i = 0; i <= maxSequence + 1; i++)
        {
            lastCode = _sut.GenerateShortCode();
        }

        Assert.NotNull(lastCode);
    }
}