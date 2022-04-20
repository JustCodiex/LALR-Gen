namespace ParserGen;

internal class LR1Item {

    public Production Rule { get; }

    public int Pos { get; }

    public Set<Symbol> Lookahead { get; }

    public LR1Item(Production rule, int position) {

        // Define base LR(1) item
        this.Rule = rule;
        this.Pos = position;

        // Create lookahead set
        this.Lookahead = new Set<Symbol>();

        // If rule 0, add EOF to lookahead
        if (this.Rule.Index is 0) {
            this.Lookahead.Add(Grammar.EndOfInput);
        }

    }

    public bool AddUniqueItemsTo(Set<LR1Item> items) {

        bool result = false;

        for (int i = 0; i < items.Count; i++) {

            if (items[i].Rule == this.Rule && items[i].Pos == this.Pos) {
                for (int j = 0; j < this.Lookahead.Count; j++) {
                    result |= items[i].Lookahead.Add(this.Lookahead[j]);
                }
                return result;
            }


        }

        items.Add(this);

        return true;

    }

    public LR1Item? GetAfterShift(LR1 lr) {

        LR1Item? next = this.Pos < this.Rule.Rhs.Length && !this.Rule.Rhs[this.Pos].IsNullable ? lr.Item(this.Rule, this.Pos + 1) : null;
        if (next is not null) {
            next.Lookahead.Clear();
            next.Lookahead.Union(this.Lookahead);
        }

        return next;

    }

    public Set<LR1Item> GetAfterDot(LR1 lr) {

        Set<LR1Item> result = new();

        // Sanity check => Make sure we don't get index out of range exceptionss here
        if (this.Pos >= this.Rule.Rhs.Length) {
            return result;
        }

        var prods = GetRulesForNonterminal(lr.G, this.Rule.Rhs[this.Pos]);
        for (int i = 0; i < prods.Count; i++) {

            LR1Item itm = lr.Item(prods[i], 0);
            result.Add(itm, (a, b) => a.Equals(b));

        }

        if (result.Count == 0) {
            return result;
        }

        Set<Symbol> newLookahead = new();
        bool eps = false;
        var firsts = lr.GetFirstSeq(this.Rule.Rhs[(this.Pos + 1)..]);

        for (int i = 0; i < firsts.Count; i++) {

            if (firsts[i].IsNullable) {
                eps = true;
            } else {
                newLookahead.Add(firsts[i]);
            }

        }

        if (eps) {
            for (int i = 0; i < this.Lookahead.Count; i++) {
                newLookahead.Add(this.Lookahead[i]);
            }
        }

        for (int i = 0; i < result.Count; i++) {
            result[i].Lookahead.Clear();
            for (int j = 0; j < newLookahead.Count; j++) {
                result[i].Lookahead.Add(newLookahead[j]);
            }
        }

        return result;

    }

    private static List<Production> GetRulesForNonterminal(Grammar G, Symbol symbol) {

        List<Production> rules = new List<Production>();

        for (int i = 0; i < G.Productions.Count; i++) {
            if (G.Productions[i].Lhs == symbol) {
                rules.Add(G.Productions[i]);
            }
        }

        return rules;

    }

    public override bool Equals(object? obj) {
        if (obj is LR1Item other) {
            return this.Rule == other.Rule && this.Pos == other.Pos && this.Lookahead.ContainsEachother(other.Lookahead);
        }
        return false;
    }

    public override int GetHashCode() => base.GetHashCode();

}

internal class LALR1Item : LR1Item {

    public LALR1Item(Production rule, int pos) : base(rule, pos) {}

    public override bool Equals(object? obj) {
        if (obj is LALR1Item other) {
            return this.Rule == other.Rule && this.Pos == other.Pos;
        }
        return false;
    }

    public override int GetHashCode() => base.GetHashCode();

}

