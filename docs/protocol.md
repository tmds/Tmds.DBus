# Tmds.DBus.Protocol

The `Tmds.DBus.Protocol` packages provides a D-Bus protocol API.

## Example application

The following example shows how you can use it to expose and consume a D-Bus service that adds two integers.

Create a console application:

```
dotnet new console -o example
cd example
```

Update `Program.cs`:
```cs
using Tmds.DBus.Protocol;

class Program
{
    static async Task Main()
    {
        string peerName = await StartAddServiceAsync();

        var connection = Connection.Session;

        var addProxy = new AddProxy(connection, peerName);

        int i = 10;
        int j = 20;
        int sum = await addProxy.AddAsync(i, j);

        Console.WriteLine($"The sum of {i} and {j} is {sum}.");
    }

    private async static Task<string> StartAddServiceAsync()
    {
        var connection = new Connection(Address.Session!);

        await connection.ConnectAsync();

        connection.AddMethodHandler(new AddImplementation());

        return connection.UniqueName ?? "";
    }
}

class AddProxy
{
    private const string Interface = "org.example.Adder";
    private const string Path = "/org/example/Adder";

    private readonly Connection _connection;
    private readonly string _peer;

    public AddProxy(Connection connection, string peer)
    {
        _connection = connection;
        _peer = peer;
    }

    public Task<int> AddAsync(int i, int j)
    {
        return _connection.CallMethodAsync(
            CreateAddMessage(),
            (Message message, object? state) =>
            {
                return message.GetBodyReader().ReadInt32();
            });

        MessageBuffer CreateAddMessage()
        {
            using var writer = _connection.GetMessageWriter();

            writer.WriteMethodCallHeader(
                destination: _peer,
                path: Path,
                @interface: Interface,
                signature: "ii",
                member: "Add");

            writer.WriteInt32(i);
            writer.WriteInt32(j);

            return writer.CreateMessage();
        }
    }
}

class AddImplementation : IMethodHandler
{
    private const string Interface = "org.example.Adder";
    public string Path => "/org/example/Adder";

    private static ReadOnlyMemory<byte> InterfaceXml { get; } =
        """
        <interface name="org.example.Adder">
          <method name="Add">
            <arg direction="in" type="i"/>
            <arg direction="in" type="i"/>
            <arg direction="out" type="i"/>
          </method>
        </interface>

        """u8.ToArray();

    public bool RunMethodHandlerSynchronously(Message message) => true;

    public ValueTask HandleMethodAsync(MethodContext context)
    {
        if (context.IsDBusIntrospectRequest)
        {
            context.ReplyIntrospectXml([ InterfaceXml ]);
            return default;
        }

        var request = context.Request;
        switch (request.InterfaceAsString)
        {
            case Interface:
                switch ((request.MemberAsString, request.SignatureAsString))
                {
                    case ("Add", "ii"):
                        var reader = request.GetBodyReader();
                        int i = reader.ReadInt32();
                        int j = reader.ReadInt32();
                        return Add(context, i, j);
                }
                break;
        }
        return default;
    }

    private ValueTask Add(MethodContext context, int i, int j)
    {
        int sum = i + j;

        ReplyToAdd(context, sum);

        return default;
    }

    private void ReplyToAdd(MethodContext context, int sum)
    {
        using var writer = context.CreateReplyWriter("i");
        writer.WriteInt32(sum);
        context.Reply(writer.CreateMessage());
    }
}
```

Now run the example:
```
$ dotnet run
The sum of 10 and 20 is 30.
```

## NativeAOT/Trimming

`Tmds.DBus.Protocol` is compatible with NativeAOT and trimming.

Methods that are not compatible have been annotated with both the `Obsolete` and `RequiresUnreferencedCode` attributes. These methods may be removed in a future version of the library.

If you are currently using these methods, the following section shows how you can change your code to make it compatible with NativeAOT/trimming.

## Reading writing composite types

The following sections show examples of writing composite types (structs/arrays/dictionaries and variants).

### Reading an array

Some types can be read directly using the `Reader`'s `ReadArrayOf` methods.

```cs
byte[] array = reader.ReadArrayOfByte();
```

When there is no such method, the item type can be read using a `while` loop as shown by the following example.

```cs
List<byte[]> arrayOfByteArrays = new();
ArrayEnd arrayEnd = reader.ReadArrayStart();
while (reader.HasNext(arrayEnd))
{
    arrayOfByteArrays.Add(reader.ReadArrayOfByte());
}
```

### Writing an array

The `MessageWriter`'s `WriteArray` overloads has overloads that enable directly writing some types.

```cs
writer.WriteArray(new int[] { 1, 2, 3 });
```

If no overload is available for the item type, the array can be written by writing each element separately.

```cs
ArrayStart arrayStart = writer.WriteArrayStart(DBusType.String);
foreach (var item in value)
{
    writer.WriteString(item);
}
writer.WriteArrayEnd(arrayStart);
```

Note: this example shows how to writing an array of strings. There is a `WriteArray` overload that allows to do this directly.

### Reading a dictionary

```cs
ArrayStart arrayStart = writer.WriteDictionaryStart();
foreach (var item in value)
{
    writer.WriteDictionaryEntryStart();
    writer.Write... // write the key
    writer.Write... // write the value
}
writer.WriteDictionaryEnd(arrayStart);
```

### Reading a dictionary

```cs
Dictionary<byte, string> dictionary = new();
ArrayEnd dictEnd = reader.ReadDictionaryStart();
while (reader.HasNext(dictEnd))
{
    var key = reader.ReadByte();
    var value = reader.ReadString();
    dictionary[key] = value;
}
```

### Reading a struct

To read a struct call `AlignStruct` and then read the struct fields.

```cs
// Read a struct of (int32, string).
reader.AlignStruct();
var value = (reader.ReadInt32(), reader.ReadString());
```

### Writing a struct

To write a struct call `WriteStructureStart` and write the struct fields.

```cs
// Write a struct of (int32, string).
writer.WriteStructureStart();
writer.WriteInt32(i);
writer.WriteString(s);
```

### Reading a variant

A variant can be read using the `Reader`'s `ReadVariantValue` method.

```cs
VariantValue vv = reader.ReadVariantValue();

switch (vv)
{
    case { Type: VariantValueType.Int32 }:
        int i = vv.GetInt32();
        break;
    case { ItemType: VariantValueType.Int32 }:
        int[] array = vv.GetArray<int>();
        break;
    case { KeyType: VariantValueType.String, ValueType: VariantValueType.VariantValue }:
        Dictionary<string, VariantValue> dict = vv.GetDictionary<string, VariantValue>();
        break;
    ...
}
```

Note that `VariantValue` is a small struct, there is no need to pass it by reference.

### Writing a variant

For writing variants, the value must be stored in a `Variant` struct and passed to `Writer.WriteVariant(Variant)`.

Basic types have implicit conversion to `Variant`.

```cs
Variant v1 = (byte)1;
Variant v2 = "string";
Variant v3 = new ObjectPath("/path");
```

For composite variant values, the libraries `Struct`/`Array`/`Dict` classes must be used.
```cs
Variant v4 = Struct.Create((byte)1, Struct.Create("string", "string"));
Variant v5 = new Dict<byte, Variant>()
{
    { 1, Struct.Create(1, 2) },
    { 2, "string" },
};
Variant v6 = new Array<int>() { 1, 2 };
```

As shown in the previous examples, the composite types support nesting.

Note that the `Variant` struct is a small struct, there is no need to pass it by reference.

