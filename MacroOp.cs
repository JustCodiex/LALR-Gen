using System.Text.RegularExpressions;

namespace ParserGen;

internal static partial class MacroOp {

    internal static readonly Regex __Type = __TypeRegexGenerator();
    internal static readonly Regex __lookupType = new(@"\$(?<i>\d+)\.<(?<type>\w+)>(?<j>\d+)");

    internal static Production[] Optional(Grammar G, Production P, Symbol[] args, int i) {

        // For optional, we have a production A, where we do not include it and a B, where we do include it.

        // Split production
        Symbol[] first = P.Rhs[..i];
        Symbol[] follow = P.Rhs[(i + 1)..];

        // A's sequence
        Symbol[] _as = first.Concat(follow).ToArray();
        Symbol[] _bs = first.Concat(args).Concat(follow).ToArray();

        // Update semantic actions
        string Asem = P.SemanticInput;
        string Bsem = P.SemanticInput;

        // Subtract 1 from all semantiics in A following optional
        for (int j = i + 1; j < P.Rhs.Length; j++) {
            Asem = Asem.Replace($"${j + 1}", $"${j}");
        }

        string TypedARepl(Match m) {
            int ii = int.Parse(m.Groups["i"].Value);
            if (ii == i + 1) {
                string t = m.Groups["type"].Value;
                if (!string.IsNullOrEmpty(t)) {
                    return $"new None<{t}>()";
                }
            }
            return m.Value;
        }

        Asem = __lookupType.Replace(__Type.Replace(Asem, TypedARepl), TypedARepl);

        // Make B's follow references refer to their correct target
        int offset = args.Length;
        for (int j = i + 1; j < P.Rhs.Length; j++) {
           Bsem = Bsem.Replace($"${j + 1}", $"${j + offset}");
        }

        string TypedBRepl(Match m) {
            int ii = int.Parse(m.Groups["i"].Value);
            if (ii == i + 1) {
                int j = m.Groups.ContainsKey("j") ? int.Parse(m.Groups["j"].Value) : offset;
                string t = m.Groups["type"].Value;
                if (!string.IsNullOrEmpty(t)) {
                    return $"new Some<{t}>(${i + j})";
                }
            }
            return m.Value;
        }

        Bsem = __lookupType.Replace(__Type.Replace(Bsem, TypedBRepl), TypedBRepl);

        // Create production A
        string og = string.IsNullOrEmpty(P.Original) ? P.ToComment() : P.Original;
        Production A = new(P.Line, P.Lhs, _as, Asem, G.GetLastValidPriority(_as)) { Original = og };
        Production B = new(P.Line, P.Lhs, _bs, Bsem, G.GetLastValidPriority(_bs)) { Original = og };

        // Return A and B
        return new Production[] { A, B };

    }

    [GeneratedRegex("\\$(<(?<type>\\w+)>)?(?<i>\\d+)")]
    private static partial Regex __TypeRegexGenerator();
}

