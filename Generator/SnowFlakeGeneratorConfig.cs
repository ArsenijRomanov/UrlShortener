namespace Generator;

public class SnowFlakeGeneratorConfig
{
    public readonly DateTimeOffset Epoch  = 
        new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    public const byte MillisBits = 41;
    public const byte InstanceBits  = 10;
    public const byte SequenceBits  = 12;
    public ushort InstanceId { get; init; }

    public SnowFlakeGeneratorConfig(ushort instanceId)
    {
        if (instanceId > (1 << InstanceBits) - 1 || instanceId < 0) throw new ArgumentOutOfRangeException(nameof(instanceId));
        InstanceId = instanceId;
    }
}
