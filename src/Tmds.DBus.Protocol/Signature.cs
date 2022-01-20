// Strongly-typed signature string for writing a variant with a signature.
struct Signature
{
    private string _value;

    public Signature(string value) => _value = value;

    public override string ToString() => _value ?? "";
}