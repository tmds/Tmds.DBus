using System.Reflection;

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

    sealed class ValueTupleTypeReader<T1> : ITypeReader<ValueTuple<T1>>, ITypeReader<object>
        where T1 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public ValueTuple<T1> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1>();
        }
    }

    sealed class TupleTypeReader<T1> : ITypeReader<Tuple<T1>>, ITypeReader<object>
        where T1 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public Tuple<T1> Read(ref Reader reader)
        {
            return reader.ReadStructAsTuple<T1>();
        }
    }

    public static void AddValueTupleTypeReader<T1>()
        where T1 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(ValueTuple<T1>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new ValueTupleTypeReader<T1>());
            }
        }
    }

    public static void AddTupleTypeReader<T1>()
        where T1 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(Tuple<T1>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new TupleTypeReader<T1>());
            }
        }
    }

    private ITypeReader CreateValueTupleTypeReader(Type type1)
    {
        Type readerType = typeof(ValueTupleTypeReader<>).MakeGenericType(new[] { type1 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }

    private ITypeReader CreateTupleTypeReader(Type type1)
    {
        Type readerType = typeof(TupleTypeReader<>).MakeGenericType(new[] { type1 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
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

    sealed class ValueTupleTypeReader<T1, T2> : ITypeReader<ValueTuple<T1, T2>>, ITypeReader<object>
        where T1 : notnull
        where T2 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public ValueTuple<T1, T2> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1, T2>();
        }
    }

    sealed class TupleTypeReader<T1, T2> : ITypeReader<Tuple<T1, T2>>, ITypeReader<object>
        where T1 : notnull
        where T2 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public Tuple<T1, T2> Read(ref Reader reader)
        {
            return reader.ReadStructAsTuple<T1, T2>();
        }
    }

    public static void AddValueTupleTypeReader<T1, T2>()
        where T1 : notnull
        where T2 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(ValueTuple<T1, T2>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new ValueTupleTypeReader<T1, T2>());
            }
        }
    }

    public static void AddTupleTypeReader<T1, T2>()
        where T1 : notnull
        where T2 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(Tuple<T1, T2>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new TupleTypeReader<T1, T2>());
            }
        }
    }

    private ITypeReader CreateValueTupleTypeReader(Type type1, Type type2)
    {
        Type readerType = typeof(ValueTupleTypeReader<,>).MakeGenericType(new[] { type1, type2 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }

    private ITypeReader CreateTupleTypeReader(Type type1, Type type2)
    {
        Type readerType = typeof(TupleTypeReader<,>).MakeGenericType(new[] { type1, type2 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
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

    sealed class ValueTupleTypeReader<T1, T2, T3> : ITypeReader<ValueTuple<T1, T2, T3>>, ITypeReader<object>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public ValueTuple<T1, T2, T3> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1, T2, T3>();
        }
    }

    sealed class TupleTypeReader<T1, T2, T3> : ITypeReader<Tuple<T1, T2, T3>>, ITypeReader<object>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public Tuple<T1, T2, T3> Read(ref Reader reader)
        {
            return reader.ReadStructAsTuple<T1, T2, T3>();
        }
    }

    public static void AddValueTupleTypeReader<T1, T2, T3>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new ValueTupleTypeReader<T1, T2, T3>());
            }
        }
    }

    public static void AddTupleTypeReader<T1, T2, T3>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(Tuple<T1, T2, T3>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new TupleTypeReader<T1, T2, T3>());
            }
        }
    }

    private ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3)
    {
        Type readerType = typeof(ValueTupleTypeReader<,,>).MakeGenericType(new[] { type1, type2, type3 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }

    private ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3)
    {
        Type readerType = typeof(TupleTypeReader<,,>).MakeGenericType(new[] { type1, type2, type3 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
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

    sealed class ValueTupleTypeReader<T1, T2, T3, T4> : ITypeReader<ValueTuple<T1, T2, T3, T4>>, ITypeReader<object>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public ValueTuple<T1, T2, T3, T4> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1, T2, T3, T4>();
        }
    }

    sealed class TupleTypeReader<T1, T2, T3, T4> : ITypeReader<Tuple<T1, T2, T3, T4>>, ITypeReader<object>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public Tuple<T1, T2, T3, T4> Read(ref Reader reader)
        {
            return reader.ReadStructAsTuple<T1, T2, T3, T4>();
        }
    }

    public static void AddValueTupleTypeReader<T1, T2, T3, T4>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3, T4>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new ValueTupleTypeReader<T1, T2, T3, T4>());
            }
        }
    }

    public static void AddTupleTypeReader<T1, T2, T3, T4>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(Tuple<T1, T2, T3, T4>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new TupleTypeReader<T1, T2, T3, T4>());
            }
        }
    }

    private ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3, Type type4)
    {
        Type readerType = typeof(ValueTupleTypeReader<,,,>).MakeGenericType(new[] { type1, type2, type3, type4 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }

    private ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4)
    {
        Type readerType = typeof(TupleTypeReader<,,,>).MakeGenericType(new[] { type1, type2, type3, type4 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
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

    sealed class ValueTupleTypeReader<T1, T2, T3, T4, T5> : ITypeReader<ValueTuple<T1, T2, T3, T4, T5>>, ITypeReader<object>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public ValueTuple<T1, T2, T3, T4, T5> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1, T2, T3, T4, T5>();
        }
    }

    sealed class TupleTypeReader<T1, T2, T3, T4, T5> : ITypeReader<Tuple<T1, T2, T3, T4, T5>>, ITypeReader<object>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public Tuple<T1, T2, T3, T4, T5> Read(ref Reader reader)
        {
            return reader.ReadStructAsTuple<T1, T2, T3, T4, T5>();
        }
    }

    public static void AddValueTupleTypeReader<T1, T2, T3, T4, T5>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3, T4, T5>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new ValueTupleTypeReader<T1, T2, T3, T4, T5>());
            }
        }
    }

    public static void AddTupleTypeReader<T1, T2, T3, T4, T5>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(Tuple<T1, T2, T3, T4, T5>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new TupleTypeReader<T1, T2, T3, T4, T5>());
            }
        }
    }

    private ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5)
    {
        Type readerType = typeof(ValueTupleTypeReader<,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }

    private ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5)
    {
        Type readerType = typeof(TupleTypeReader<,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type5 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
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

    sealed class ValueTupleTypeReader<T1, T2, T3, T4, T5, T6> : ITypeReader<ValueTuple<T1, T2, T3, T4, T5, T6>>, ITypeReader<object>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public ValueTuple<T1, T2, T3, T4, T5, T6> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1, T2, T3, T4, T5, T6>();
        }
    }

    sealed class TupleTypeReader<T1, T2, T3, T4, T5, T6> : ITypeReader<Tuple<T1, T2, T3, T4, T5, T6>>, ITypeReader<object>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public Tuple<T1, T2, T3, T4, T5, T6> Read(ref Reader reader)
        {
            return reader.ReadStructAsTuple<T1, T2, T3, T4, T5, T6>();
        }
    }

    public static void AddValueTupleTypeReader<T1, T2, T3, T4, T5, T6>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3, T4, T5, T6>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new ValueTupleTypeReader<T1, T2, T3, T4, T5, T6>());
            }
        }
    }

    public static void AddTupleTypeReader<T1, T2, T3, T4, T5, T6>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(Tuple<T1, T2, T3, T4, T5, T6>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new TupleTypeReader<T1, T2, T3, T4, T5, T6>());
            }
        }
    }

    private ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6)
    {
        Type readerType = typeof(ValueTupleTypeReader<,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }

    private ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6)
    {
        Type readerType = typeof(TupleTypeReader<,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
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

    sealed class ValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7> : ITypeReader<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>, ITypeReader<object>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public ValueTuple<T1, T2, T3, T4, T5, T6, T7> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1, T2, T3, T4, T5, T6, T7>();
        }
    }

    sealed class TupleTypeReader<T1, T2, T3, T4, T5, T6, T7> : ITypeReader<Tuple<T1, T2, T3, T4, T5, T6, T7>>, ITypeReader<object>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public Tuple<T1, T2, T3, T4, T5, T6, T7> Read(ref Reader reader)
        {
            return reader.ReadStructAsTuple<T1, T2, T3, T4, T5, T6, T7>();
        }
    }

    public static void AddValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3, T4, T5, T6, T7>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new ValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7>());
            }
        }
    }

    public static void AddTupleTypeReader<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(Tuple<T1, T2, T3, T4, T5, T6, T7>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new TupleTypeReader<T1, T2, T3, T4, T5, T6, T7>());
            }
        }
    }

    private ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7)
    {
        Type readerType = typeof(ValueTupleTypeReader<,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }

    private ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7)
    {
        Type readerType = typeof(TupleTypeReader<,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
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

    sealed class ValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8> : ITypeReader<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>>, ITypeReader<object>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1, T2, T3, T4, T5, T6, T7, T8>();
        }
    }

    sealed class TupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8> : ITypeReader<Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>>, ITypeReader<object>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> Read(ref Reader reader)
        {
            return reader.ReadStructAsTuple<T1, T2, T3, T4, T5, T6, T7, T8>();
        }
    }

    public static void AddValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new ValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8>());
            }
        }
    }

    public static void AddTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        where T6 : notnull
        where T7 : notnull
        where T8 : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new TupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8>());
            }
        }
    }

    private ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8)
    {
        Type readerType = typeof(ValueTupleTypeReader<,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }

    private ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8)
    {
        Type readerType = typeof(TupleTypeReader<,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
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

    sealed class ValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ITypeReader<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>>, ITypeReader<object>
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
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>();
        }
    }

    sealed class TupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ITypeReader<Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>>>, ITypeReader<object>
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
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>> Read(ref Reader reader)
        {
            return reader.ReadStructAsTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>();
        }
    }

    public static void AddValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
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
        lock (_typeReaders)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new ValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9>());
            }
        }
    }

    public static void AddTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
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
        lock (_typeReaders)
        {
            Type keyType = typeof(Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new TupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9>());
            }
        }
    }

    private ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9)
    {
        Type readerType = typeof(ValueTupleTypeReader<,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8, type9 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }

    private ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9)
    {
        Type readerType = typeof(TupleTypeReader<,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8, type9 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
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

    sealed class ValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ITypeReader<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>>>, ITypeReader<object>
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
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();
        }
    }

    sealed class TupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ITypeReader<Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>>>, ITypeReader<object>
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
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>> Read(ref Reader reader)
        {
            return reader.ReadStructAsTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();
        }
    }

    public static void AddValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
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
        lock (_typeReaders)
        {
            Type keyType = typeof(ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new ValueTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>());
            }
        }
    }

    public static void AddTupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
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
        lock (_typeReaders)
        {
            Type keyType = typeof(Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new TupleTypeReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>());
            }
        }
    }

    private ITypeReader CreateValueTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9, Type type10)
    {
        Type readerType = typeof(ValueTupleTypeReader<,,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8, type9, type10 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }

    private ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5, Type type6, Type type7, Type type8, Type type9, Type type10)
    {
        Type readerType = typeof(TupleTypeReader<,,,,,,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5, type6, type7, type8, type9, type10 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }
}
