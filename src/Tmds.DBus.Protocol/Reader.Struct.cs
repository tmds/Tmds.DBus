namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public ValueTuple<T1> ReadStruct<T1>()
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>());
    }

    private Tuple<T1> ReadStructAsTuple<T1>()
    {
        AlignStruct();
        return Tuple.Create(Read<T1>());
    }

    public ValueTuple<T1, T2> ReadStruct<T1, T2>()
        where T1 : notnull
        where T2 : notnull
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>(), Read<T2>());
    }

    private Tuple<T1, T2> ReadStructAsTuple<T1, T2>()
        where T1 : notnull
        where T2 : notnull
    {
        AlignStruct();
        return Tuple.Create(Read<T1>(), Read<T2>());
    }

    public ValueTuple<T1, T2, T3> ReadStruct<T1, T2, T3>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>(), Read<T2>(), Read<T3>());
    }

    private Tuple<T1, T2, T3> ReadStructAsTuple<T1, T2, T3>()
    {
        AlignStruct();
        return Tuple.Create(Read<T1>(), Read<T2>(), Read<T3>());
    }

    public ValueTuple<T1, T2, T3, T4> ReadStruct<T1, T2, T3, T4>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>());
    }

    private Tuple<T1, T2, T3, T4> ReadStructAsTuple<T1, T2, T3, T4>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        AlignStruct();
        return Tuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>());
    }

    public ValueTuple<T1, T2, T3, T4, T5> ReadStruct<T1, T2, T3, T4, T5>()
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>());
    }

    private Tuple<T1, T2, T3, T4, T5> ReadStructAsTuple<T1, T2, T3, T4, T5>()
    {
        AlignStruct();
        return Tuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>());
    }

    public ValueTuple<T1, T2, T3, T4, T5, T6> ReadStruct<T1, T2, T3, T4, T5, T6>()
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>());
    }

    private Tuple<T1, T2, T3, T4, T5, T6> ReadStructAsTuple<T1, T2, T3, T4, T5, T6>()
    {
        AlignStruct();
        return Tuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>());
    }

    public ValueTuple<T1, T2, T3, T4, T5, T6, T7> ReadStruct<T1, T2, T3, T4, T5, T6, T7>()
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>());
    }

    private Tuple<T1, T2, T3, T4, T5, T6, T7> ReadStructAsTuple<T1, T2, T3, T4, T5, T6, T7>()
    {
        AlignStruct();
        return Tuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>());
    }

    public ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>> ReadStruct<T1, T2, T3, T4, T5, T6, T7, T8>()
    {
        AlignStruct();
        return ValueTuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>(), Read<T8>());
    }

    private Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> ReadStructAsTuple<T1, T2, T3, T4, T5, T6, T7, T8>()
    {
        AlignStruct();
        return Tuple.Create(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>(), Read<T8>());
    }

    public ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>> ReadStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
    {
        AlignStruct();
        return (Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>(), Read<T8>(), Read<T9>());
    }

    private Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>> ReadStructAsTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
    {
        AlignStruct();
        return new Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>>(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>(), Tuple.Create(Read<T8>(), Read<T9>()));
    }

    public ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>> ReadStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
    {
        AlignStruct();
        return (Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>(), Read<T8>(), Read<T9>(), Read<T10>());
    }

    private Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>> ReadStructAsTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
    {
        AlignStruct();
        return new Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>>(Read<T1>(), Read<T2>(), Read<T3>(), Read<T4>(), Read<T5>(), Read<T6>(), Read<T7>(), Tuple.Create(Read<T8>(), Read<T9>(), Read<T10>()));
    }
}
