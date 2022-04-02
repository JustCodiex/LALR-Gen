namespace ParserGen;     
internal class Production {

    public Symbol Lhs { get; }

    public Symbol[] Rhs { get; set; }

    public string SemanticInput { get; }

    public int Index { get; set; }

    public Priority Priority { get; set; }

    public int Line { get; }

    public string Original { get; init; }

    public SemanticAction Action { get; set; }

    public Production(int ln, Symbol lhs, Symbol[] rhs, string cscode, Priority priority) {
        this.Lhs = lhs;
        this.Rhs = rhs;
        this.Line = ln;
        this.SemanticInput = cscode;
        this.Action = new(cscode, string.Empty);
        this.Priority = priority;
        this.Original = string.Empty;
    }

    public override string ToString() => $"{this.Lhs} ::= {string.Join("; ", this.Rhs.Select(x => x.ToString()))}";

    public string ToComment() => $"[{this.Lhs.Sym}] ::= {string.Join(' ', this.Rhs.Select(x => x.IsTerminal ? $"{x.Sym}" : $"[{x.Sym}]"))}";

}

