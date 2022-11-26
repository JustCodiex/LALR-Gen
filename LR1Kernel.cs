namespace ParserGen;

internal class LR1Kernel {

    public int Index { get; }

    public Set<LR1Item> Items { get; }

    public Set<LR1Item> Closure { get; }

    public Set<Symbol> Keys { get; }

    public Dictionary<Symbol, int> Gotos { get; }

    public LR1Kernel(int index, Set<LR1Item> items) { 
        this.Index = index; 
        this.Items = items;
        this.Closure = new(items);
        this.Gotos = new();
        this.Keys = new();
    }

    public override bool Equals(object? obj) {
        if (obj is LR1Kernel k) {
            return this.Items.ContainsEachother(k.Items);
        }
        return false;
    }

    public override int GetHashCode() => base.GetHashCode();

}
