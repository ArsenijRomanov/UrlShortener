using SharpJuice.Essentials;

namespace Generator;

public class SnowFlakeGenerator: IGenerator
{
    private readonly DateTimeOffset _epoch;
    private readonly IClock _clock;
    private readonly byte _millisBits;
    private readonly byte _instanceBits;
    private readonly byte _sequenceBits;
    private readonly ushort _sequenceCountMaxValue;
    private readonly ushort _instanceId;
    
    private ushort _sequenceCount;
    private ulong _lastTick;
    private readonly Lock _lock = new();

    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    
    public SnowFlakeGenerator(IClock clock, SnowFlakeGeneratorConfig config)
    {
        _epoch = config.Epoch;
        _millisBits = SnowFlakeGeneratorConfig.MillisBits;
        _instanceBits = SnowFlakeGeneratorConfig.InstanceBits;
        _sequenceBits = SnowFlakeGeneratorConfig.SequenceBits;
        _sequenceCountMaxValue = (ushort)((1 << _sequenceBits) - 1);
        _instanceId = config.InstanceId;
        _clock = clock;
    }
    
    public string GenerateShortCode()
    {
        lock (_lock)
        {
            var now = _clock.Now;
            if (now <= _epoch) throw new Exception("TODO");
            ulong millis = (ulong)(now - _epoch).TotalMilliseconds;
            if (millis >> _millisBits > 0) throw new Exception("TODO");

            if (millis == _lastTick)
            {
                ++_sequenceCount;
                if (_sequenceCount >= _sequenceCountMaxValue)
                {
                    do
                    {
                        now = _clock.Now;
                        if (millis < _lastTick)
                            throw new Exception("Clock moved backwards");
                        millis = (ulong)(now - _epoch).TotalMilliseconds;
                    } while (millis <= _lastTick);

                    _sequenceCount = 0;
                    _lastTick = millis;
                }
            }
            else
            {
                _sequenceCount = 0;
                _lastTick = millis;
            }

            ulong shortCodeNumber =
                (millis << (_instanceBits + _sequenceBits))
                | ((ulong)_instanceId << _sequenceBits)
                | _sequenceCount;

            return ConvertNumberToShortCode(shortCodeNumber);
        }
    }

    private string ConvertNumberToShortCode(ulong number)
    {
        if (number == 0)
            return Alphabet[0].ToString();

        Span<char> buffer = stackalloc char[11];
        int pos = buffer.Length;

        while (number > 0)
        {
            var rem = (int)(number % 62);
            number /= 62;
            buffer[--pos] = Alphabet[rem];
        }

        return new string(buffer[pos..]);
    }
}
