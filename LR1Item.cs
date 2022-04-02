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

    public bool AddUniqueItemsTo(ICollection<LR1Item> items) {

        bool result = false;

        foreach (var item in items) {

            if (item.Rule == this.Rule && item.Pos == this.Pos) {
                foreach (var s in this.Lookahead) {
                    result |= item.Lookahead.Add(s);
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

        foreach (var p in GetRulesForNonterminal(lr.G, this.Rule.Rhs[this.Pos])) {

            LR1Item itm = lr.Item(p, 0);
            result.Add(itm, (a,b) => a.Equals(b));

        }

        if (result.Count == 0) {
            return result;
        }

        Set<Symbol> newLookahead = new();
        bool eps = false;
        var firsts = lr.GetFirstSeq(this.Rule.Rhs[(this.Pos + 1)..]);

        foreach (Symbol s in firsts) {
            if (s.IsNullable) {
                eps = true;
            } else {
                newLookahead.Add(s);
            }
        }

        if (eps) {
            foreach (Symbol s in this.Lookahead) {
                newLookahead.Add(s);
            }
        }

        for (int i = 0; i < result.Count; i++) {
            result[i].Lookahead.Clear();
            foreach (Symbol s in newLookahead) {
                result[i].Lookahead.Add(s);
            }
        }

        return result;

    }

    private static List<Production> GetRulesForNonterminal(Grammar G, Symbol symbol) {

        List<Production> rules = new List<Production>();

        foreach (Production rule in G.Productions) {
            if (rule.Lhs == symbol) {
                rules.Add(rule);
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

