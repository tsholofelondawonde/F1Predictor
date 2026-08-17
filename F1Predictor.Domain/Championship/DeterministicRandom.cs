namespace F1Predictor.Domain.Championship;

/// <summary>
/// A small, fast, fully specified pseudo-random generator (xoshiro256++ seeded by SplitMix64).
/// </summary>
/// <remarks>
/// Deliberately not <see cref="System.Random"/>: the framework makes no promise that a given
/// seed produces the same sequence across .NET releases, so a shared seed alone would not keep
/// the published championship odds stable. This is stable by construction, which is what lets
/// the dashboard poll on a timer without the headline number drifting under the reader.
/// <para>
/// Not for anything security-sensitive — it is a simulation input, not a source of secrets.
/// </para>
/// </remarks>
public sealed class DeterministicRandom
{
    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;

    public DeterministicRandom(ulong seed)
    {
        // SplitMix64 spreads a small seed across the whole state; seeding xoshiro directly from
        // a value like 42 would leave it mostly zeros and correlated for the first draws.
        _s0 = SplitMix64(ref seed);
        _s1 = SplitMix64(ref seed);
        _s2 = SplitMix64(ref seed);
        _s3 = SplitMix64(ref seed);
    }

    /// <summary>A uniform value in [0, 1).</summary>
    public double NextDouble() =>
        // 53 significant bits is the most a double can carry exactly.
        (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    /// <summary>
    /// A standard Gumbel(0, 1) draw.
    /// </summary>
    /// <remarks>
    /// Adding one of these to each log-strength and taking the order of the results samples
    /// exactly from a Plackett–Luce distribution — the Gumbel-max trick. It replaces a chain of
    /// sequential draws with a single sort, which is what keeps ten thousand simulated seasons
    /// inside a fraction of a second.
    /// </remarks>
    public double NextGumbel()
    {
        // log(0) is not a useful finishing position.
        double uniform;

        do
        {
            uniform = NextDouble();
        }
        while (uniform <= 0);

        return -Math.Log(-Math.Log(uniform));
    }

    private ulong NextUInt64()
    {
        var result = ulong.RotateLeft(_s0 + _s3, 23) + _s0;
        var t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = ulong.RotateLeft(_s3, 45);

        return result;
    }

    private static ulong SplitMix64(ref ulong state)
    {
        state += 0x9E3779B97F4A7C15UL;

        var z = state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;

        return z ^ (z >> 31);
    }
}
