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

    sealed class ValueTupleTypeReader<T1> : ITypeReader<ValueTuple<T1>>
        where T1 : notnull
    {
        public ValueTuple<T1> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1>();
        }
    }

    sealed class TupleTypeReader<T1> : ITypeReader<Tuple<T1>>
        where T1 : notnull
    {
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

    sealed class ValueTupleTypeReader<T1, T2> : ITypeReader<ValueTuple<T1, T2>>
        where T1 : notnull
        where T2 : notnull
    {
        public ValueTuple<T1, T2> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1, T2>();
        }
    }

    sealed class TupleTypeReader<T1, T2> : ITypeReader<Tuple<T1, T2>>
        where T1 : notnull
        where T2 : notnull
    {
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

    sealed class ValueTupleTypeReader<T1, T2, T3> : ITypeReader<ValueTuple<T1, T2, T3>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        public ValueTuple<T1, T2, T3> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1, T2, T3>();
        }
    }

    sealed class TupleTypeReader<T1, T2, T3> : ITypeReader<Tuple<T1, T2, T3>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
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

    sealed class ValueTupleTypeReader<T1, T2, T3, T4> : ITypeReader<ValueTuple<T1, T2, T3, T4>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        public ValueTuple<T1, T2, T3, T4> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1, T2, T3, T4>();
        }
    }

    sealed class TupleTypeReader<T1, T2, T3, T4> : ITypeReader<Tuple<T1, T2, T3, T4>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
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

    sealed class ValueTupleTypeReader<T1, T2, T3, T4, T5> : ITypeReader<ValueTuple<T1, T2, T3, T4, T5>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        public ValueTuple<T1, T2, T3, T4, T5> Read(ref Reader reader)
        {
            return reader.ReadStruct<T1, T2, T3, T4, T5>();
        }
    }

    sealed class TupleTypeReader<T1, T2, T3, T4, T5> : ITypeReader<Tuple<T1, T2, T3, T4, T5>>
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
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
        Type readerType = typeof(ValueTupleTypeReader<,,,,>).MakeGenericType(new[] { type1, type2, type3, type4 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }

    private ITypeReader CreateTupleTypeReader(Type type1, Type type2, Type type3, Type type4, Type type5)
    {
        Type readerType = typeof(TupleTypeReader<,,,,>).MakeGenericType(new[] { type1, type2, type3, type4, type5 });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }
}
