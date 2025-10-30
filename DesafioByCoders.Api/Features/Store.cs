namespace DesafioByCoders.Api.Features;

internal sealed class Store
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Owner { get; private set; } = null!;

    private Store()
    {
    }

    private Store(string name, string owner) : this()
    {
        Name = name.Trim();
        Owner = owner.Trim();
    }

    public static Store Create(string name, string owner)
    {
        return new Store(name, owner);
    }

    public override string ToString()
    {
        return Name.ToLowerInvariant() + " - " + Owner.ToLowerInvariant();
    }
}