namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    internal void WriteStruct<T1>(ValueTuple<T1> value)
        where T1 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
    }

    internal void WriteStruct<T1, T2>((T1, T2) value)
        where T1 : notnull
        where T2 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
    }

    internal void WriteStruct<T1, T2, T3>((T1, T2, T3) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
    }

    internal void WriteStruct<T1, T2, T3, T4>((T1, T2, T3, T4) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
    }

    internal void WriteStruct<T1, T2, T3, T4, T5>((T1, T2, T3, T4, T5) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
    }

    internal void WriteStruct<T1, T2, T3, T4, T5, T6>((T1, T2, T3, T4, T5, T6) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
    }

    internal void WriteStruct<T1, T2, T3, T4, T5, T6, T7>((T1, T2, T3, T4, T5, T6, T7) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
        Write<T7>(value.Item7);
    }

    internal void WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8>((T1, T2, T3, T4, T5, T6, T7, T8) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
        Write<T7>(value.Item7);
        Write<T8>(value.Rest.Item1);
    }

    internal void WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>((T1, T2, T3, T4, T5, T6, T7, T8, T9) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
        Write<T7>(value.Item7);
        Write<T8>(value.Rest.Item1);
        Write<T9>(value.Rest.Item2);
    }

    internal void WriteStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>((T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) value)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
        where T10 : notnull
    {
        WriteStructureStart();
        Write<T1>(value.Item1);
        Write<T2>(value.Item2);
        Write<T3>(value.Item3);
        Write<T4>(value.Item4);
        Write<T5>(value.Item5);
        Write<T6>(value.Item6);
        Write<T7>(value.Item7);
        Write<T8>(value.Rest.Item1);
        Write<T9>(value.Rest.Item2);
        Write<T10>(value.Rest.Item3);
    }
}
