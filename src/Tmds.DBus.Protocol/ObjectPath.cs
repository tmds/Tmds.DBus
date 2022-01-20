// Strongly-typed signature string for writing a variant with an object path.
struct ObjectPath
{
    private string _value;

    public ObjectPath(string value) => _value = value;

    public override string ToString() => _value ?? "";
}