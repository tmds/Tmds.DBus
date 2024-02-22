namespace Tmds.DBus.Protocol;

public static class Struct
{
    public static Struct<T1> Create
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1
        >
        (T1 item1)
        where T1 : notnull
            => new Struct<T1>(item1);

    public static Struct<T1, T2> Create
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2
        >
        (T1 item1, T2 item2)
        where T1 : notnull
        where T2 : notnull
            => new Struct<T1, T2>(item1, item2);

    public static Struct<T1, T2, T3> Create
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3
        >
        (T1 item1, T2 item2, T3 item3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
            => new Struct<T1, T2, T3>(item1, item2, item3);

    public static Struct<T1, T2, T3, T4> Create
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4
        >
        (T1 item1, T2 item2, T3 item3, T4 item4)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
            => new Struct<T1, T2, T3, T4>(item1, item2, item3, item4);

    public static Struct<T1, T2, T3, T4, T5> Create
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5
        >
        (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
            => new Struct<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);

    public static Struct<T1, T2, T3, T4, T5, T6> Create
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6
        >
        (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
            => new Struct<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);

    public static Struct<T1, T2, T3, T4, T5, T6, T7> Create
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7
        >
        (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
            => new Struct<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);

    public static Struct<T1, T2, T3, T4, T5, T6, T7, T8> Create
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T8
        >
        (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
            => new Struct<T1, T2, T3, T4, T5, T6, T7, T8>(item1, item2, item3, item4, item5, item6, item7, item8);

    public static Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9> Create
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T8,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T9
        >
        (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
        where T9 : notnull
            => new Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9>(item1, item2, item3, item4, item5, item6, item7, item8, item9);

    public static Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Create
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T8,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T9,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T10
        >
        (T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10)
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
            => new Struct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10);
}

public sealed class Struct
    <
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1
    >
    : IDBusReadable, IDBusWritable
    where T1  : notnull
{
    public T1 Item1;

    public Struct(T1 item1)
        => Item1 = item1;

    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    void IDBusReadable.ReadFrom(ref Reader reader)
        => Item1 = reader.ReadStruct<T1>().Item1;

    public ValueTuple<T1> ToValueTuple()
        => new ValueTuple<T1>(Item1);
}

public sealed class Struct
    <
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2
    >
    : IDBusReadable, IDBusWritable
    where T1  : notnull
    where T2  : notnull
{
    public T1 Item1;
    public T2 Item2;

    public Struct(T1 item1, T2 item2)
        => (Item1, Item2) = (item1, item2);

    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    void IDBusReadable.ReadFrom(ref Reader reader)
        => (Item1, Item2) = reader.ReadStruct<T1, T2>();

    public (T1, T2) ToValueTuple()
        => (Item1, Item2);
}
public sealed class Struct
    <
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3
    >
    : IDBusReadable, IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;

    public Struct(T1 item1, T2 item2, T3 item3)
        => (Item1, Item2, Item3) = (item1, item2, item3);

    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    void IDBusReadable.ReadFrom(ref Reader reader)
        => (Item1, Item2, Item3) = reader.ReadStruct<T1, T2, T3>();

    public (T1, T2, T3) ToValueTuple()
        => (Item1, Item2, Item3);
}
public sealed class Struct
    <
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4
    >
    : IDBusReadable, IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
    where T4  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;

    public Struct(T1 item1, T2 item2, T3 item3, T4 item4)
        => (Item1, Item2, Item3, Item4) = (item1, item2, item3, item4);

    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    void IDBusReadable.ReadFrom(ref Reader reader)
        => (Item1, Item2, Item3, Item4) = reader.ReadStruct<T1, T2, T3, T4>();

    public (T1, T2, T3, T4) ToValueTuple()
        => (Item1, Item2, Item3, Item4);
}
public sealed class Struct
    <
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5
    >
    : IDBusReadable, IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
    where T4  : notnull
    where T5  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;
    public T5 Item5;

    public Struct(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
        => (Item1, Item2, Item3, Item4, Item5) = (item1, item2, item3, item4, item5);

    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    void IDBusReadable.ReadFrom(ref Reader reader)
        => (Item1, Item2, Item3, Item4, Item5) = reader.ReadStruct<T1, T2, T3, T4, T5>();

    public (T1, T2, T3, T4, T5) ToValueTuple()
        => (Item1, Item2, Item3, Item4, Item5);
}
public sealed class Struct
    <
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6
    >
    : IDBusReadable, IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
    where T4  : notnull
    where T5  : notnull
    where T6  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;
    public T5 Item5;
    public T6 Item6;

    public Struct(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
        => (Item1, Item2, Item3, Item4, Item5, Item6) = (item1, item2, item3, item4, item5, item6);

    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    void IDBusReadable.ReadFrom(ref Reader reader)
        => (Item1, Item2, Item3, Item4, Item5, Item6) = reader.ReadStruct<T1, T2, T3, T4, T5, T6>();

    public (T1, T2, T3, T4, T5, T6) ToValueTuple()
        => (Item1, Item2, Item3, Item4, Item5, Item6);
}
public sealed class Struct
    <
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7
    >
    : IDBusReadable, IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
    where T4  : notnull
    where T5  : notnull
    where T6  : notnull
    where T7  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;
    public T5 Item5;
    public T6 Item6;
    public T7 Item7;

    public Struct(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7) = (item1, item2, item3, item4, item5, item6, item7);

    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    void IDBusReadable.ReadFrom(ref Reader reader)
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7) = reader.ReadStruct<T1, T2, T3, T4, T5, T6, T7>();

    public (T1, T2, T3, T4, T5, T6, T7) ToValueTuple()
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7);
}
public sealed class Struct
    <
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T8
    >
    : IDBusReadable, IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
    where T4  : notnull
    where T5  : notnull
    where T6  : notnull
    where T7  : notnull
    where T8  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;
    public T5 Item5;
    public T6 Item6;
    public T7 Item7;
    public T8 Item8;

    public Struct(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8) = (item1, item2, item3, item4, item5, item6, item7, item8);

    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    void IDBusReadable.ReadFrom(ref Reader reader)
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8) = reader.ReadStruct<T1, T2, T3, T4, T5, T6, T7, T8>();

    public (T1, T2, T3, T4, T5, T6, T7, T8) ToValueTuple()
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8);
}
public sealed class Struct
    <
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T8,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T9
    >
    : IDBusReadable, IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
    where T4  : notnull
    where T5  : notnull
    where T6  : notnull
    where T7  : notnull
    where T8  : notnull
    where T9  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;
    public T5 Item5;
    public T6 Item6;
    public T7 Item7;
    public T8 Item8;
    public T9 Item9;

    public Struct(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9)
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9) = (item1, item2, item3, item4, item5, item6, item7, item8, item9);

    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    void IDBusReadable.ReadFrom(ref Reader reader)
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9) = reader.ReadStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>();

    public (T1, T2, T3, T4, T5, T6, T7, T8, T9) ToValueTuple()
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9);
}
public sealed class Struct
    <
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T1,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T2,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T3,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T4,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T5,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T6,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T7,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T8,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T9,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T10
    >
    : IDBusReadable, IDBusWritable
    where T1  : notnull
    where T2  : notnull
    where T3  : notnull
    where T4  : notnull
    where T5  : notnull
    where T6  : notnull
    where T7  : notnull
    where T8  : notnull
    where T9  : notnull
    where T10  : notnull
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
    public T4 Item4;
    public T5 Item5;
    public T6 Item6;
    public T7 Item7;
    public T8 Item8;
    public T9 Item9;
    public T10 Item10;

    public Struct(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10)
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9, Item10) = (item1, item2, item3, item4, item5, item6, item7, item8, item9, item10);

    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteStruct(ToValueTuple());

    void IDBusReadable.ReadFrom(ref Reader reader)
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9, Item10) = reader.ReadStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();

    public (T1, T2, T3, T4, T5, T6, T7, T8, T9, T10) ToValueTuple()
        => (Item1, Item2, Item3, Item4, Item5, Item6, Item7, Item8, Item9, Item10);
}
