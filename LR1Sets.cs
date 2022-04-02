﻿namespace ParserGen;

/// <summary>
/// Part of the partial class that defines the set operations required for the LR1 table generation algorithm
/// </summary>
internal partial class LR1 {

    private readonly Dictionary<Symbol, Set<Symbol>> FirstSets;
    private readonly Dictionary<Symbol, Set<Symbol>> FollowSets;

    private void GetFirst() {

        // Flag setting if we've made and changes
        bool updated;

        do {

            // Set flag to false
            updated = false;

            // Loop over all productions
            for (int i = 0; i < this.G.Productions.Count; i++) {

                // Get rule at i'th position
                var rule = this.G.Productions[i];

                // Get set of LHS
                if (!this.FirstSets.TryGetValue(rule.Lhs, out Set<Symbol>? firsts)) {
                    firsts = new();
                    this.FirstSets.Add(rule.Lhs, firsts);
                }

                // If empty, add directly
                if (rule.Rhs.Length == 1 && rule.Rhs[0].IsNullable) {
                    updated |= firsts.Add(this.G.EmptySymbol);
                } else {

                    // eps
                    bool eps = true;

                    // Loop over each element in RHS
                    for (int j = 0; j < rule.Rhs.Length; j++) {

                        // Set epsilon symbol to false
                        eps = false;

                        // Is terminal
                        if (rule.Rhs[j].IsTerminal) {
                            updated |= firsts.Add(rule.Rhs[j]);
                            break;
                        }

                        // Loop over all firsts and collect those
                        if (this.FirstSets.ContainsKey(rule.Rhs[j])) {
                            foreach (Symbol s in this.FirstSets[rule.Rhs[j]]) {

                                // Update eps
                                eps |= s.IsNullable;

                                // Add symbol to current first set
                                updated |= firsts.Add(s);

                            }
                        }

                        // If epsilon detected, break
                        if (!eps) {
                            break;
                        }

                    }

                    // If eps
                    if (eps) {
                        updated |= firsts.Add(this.G.EmptySymbol);
                    }

                }

            }

        } while (updated); // continue while we make changes

    }

    private void GetFollow() {

        // Flag setting if we've made and changes
        bool updated;

        do {

            // Set flag to false
            updated = false;

            // For each prduction in grammar
            for (int i = 0; i < this.G.Productions.Count; i++) {

                // Get the rule
                var rule = this.G.Productions[i];

                // Get follow
                if (!this.FollowSets.TryGetValue(rule.Lhs, out Set<Symbol>? followslhs)) {
                    followslhs = new();
                    this.FollowSets.Add(rule.Lhs, followslhs);
                }

                // If first rule
                if (i == 0) {
                    updated |= followslhs.Add(Grammar.EndOfInput);
                }

                // Loop over each symbol in rule
                for (int j = 0; j < rule.Rhs.Length; j++) {

                    // Get symbol
                    Symbol sym = rule.Rhs[j];

                    // If not terminal
                    if (!sym.IsTerminal) {

                        // Get follow
                        if (!this.FollowSets.TryGetValue(sym, out Set<Symbol>? follows)) {
                            follows = new();
                            this.FollowSets.Add(sym, follows);
                        }

                        // Get follow seq
                        var after = this.GetFirstSeq(rule.Rhs[(j + 1)..]);

                        // Loop over each symbol
                        foreach (var s in after) {

                            // If empty symbol
                            if (s.IsNullable) {

                                foreach (var nt in followslhs) {
                                    updated |= follows.Add(nt);
                                }

                            } else {

                                updated |= follows.Add(s);

                            }

                        }

                    }

                }

            }

        } while (updated); // continue while we make changes

    }

    internal Set<Symbol> GetFirstSeq(Symbol[] seq) {

        // Define result set
        Set<Symbol> result = new();
        bool eps = true;

        // Foreach symbol in sequence
        for (int i = 0; i < seq.Length; i++) {

            // Set eps to false
            eps = false;

            // Add if terminal
            if (seq[i].IsTerminal) {
                result.Add(seq[i]);
                break;
            }

            // Foreach k in first of seq[i]
            foreach (Symbol k in this.FirstSets[seq[i]]) {

                // Update eps
                eps |= k.IsNullable;

                // Add symbol to result
                result.Add(k);

            }

            // Update eps properly
            eps |= !this.FirstSets.ContainsKey(seq[i]) || this.FirstSets[seq[i]].Count == 0;

            // If not eps break
            if (!eps)
                break;

        }

        // If eps detected
        if (eps) {
            result.Add(this.G.EmptySymbol);
        }

        // Return result
        return result;

    }

}

