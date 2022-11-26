using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ParserGen;

internal partial class Grammar {

    [GeneratedRegex("%(?<type>left|right|none)\\s+(?<tokens>(\\w|\\s)+)")]
    private static partial Regex __precGenerator();

    [GeneratedRegex("(?<lhs>\\w+)\\s*::=(?<rhs>(\\w|\\s|;|!|~|\\$|\\(|\\)|,)+)\\{(?<code>.*?)\\}\\s*(%prec\\s+(?<prec>(\\w)+))?")]
    private static partial Regex __ruleGenerator();
    [GeneratedRegex("\\|(?<rhs>(\\w|\\s|\\$|!|~|;|\\(|\\)|,)+)\\{(?<code>.*?)\\}\\s*(%prec\\s+(?<prec>(\\w)+))?")]
    private static partial Regex __ruexGenerator();

    static readonly Regex __prec = __precGenerator();
    static readonly Regex __rule = __ruleGenerator();
    static readonly Regex __ruex = __ruexGenerator();

    internal List<Priority> Priorities { get; }

    internal List<Production> Productions { get; }

    internal List<string> Using { get; }

    internal List<string> InClass { get; }

    internal string Namespace { get; set; }

    internal int InClassStartIndex { get; set; }

    internal Dictionary<string, Symbol> Symbols { get; }

    internal Dictionary<string, MacroType> Macros { get; }

    public static Symbol EndOfInput { get; } = new("$", true);

    public Symbol EmptySymbol { get; } = new("~", true) { IsNullable = true };

    public Priority LowestPriority { get; }

    public byte[] ProductionHash { get; set; }

    public byte[] SemanticHash { get; set; }

    public byte[] UsingHash { get; set; }

    public byte[] ClassHash { get; set; }

    internal Grammar() {
        LowestPriority = new(-1, Association.none, Array.Empty<Symbol>());
        Priorities = new() { LowestPriority };
        Productions = new();
        Symbols = new();
        Using = new();
        InClass = new();
        Namespace = string.Empty;
        InClassStartIndex = 0;
        Symbols["$"] = EndOfInput;
        Symbols["~"] = EmptySymbol;
        ProductionHash = Array.Empty<byte>();
        SemanticHash = Array.Empty<byte>();
        UsingHash = Array.Empty<byte>();
        ClassHash = Array.Empty<byte>();
        Macros = (new MacroType[] { 
            new MacroType("optional", MacroOp.Optional),
            //new MacroType("separated_list"),
            //new MacroType("separated_list_nonempty"),
            //new MacroType("list"),
            //new MacroType("list_nonempty")
        }).ToDictionary(k => k.Name, v => v);
    }

    internal void CalculateFirsts() {

        // Add all terminal symbols to their of first set
        foreach (var (_, sym) in Symbols) {
            if (sym.IsTerminal) {
                sym.First.Add(sym);
            }
        }

        bool updated = true;
        while (updated) {
            updated = false;

            foreach (Production p in Productions) {

                int i;
                for (i = 0; i < p.Rhs.Length; i++) {

                    updated |= p.Lhs.First.Union(p.Rhs[i].First) > 0;
                    if (!p.Rhs[i].IsNullable) {
                        break;
                    }

                }
                
                if (i == p.Rhs.Length) {
                    updated |= !p.Lhs.IsNullable;
                    p.Lhs.IsNullable = true;
                }

            }

        }

    }

    private enum ReadMode {
        Grammar,
        UsingsData,
        InClassData,
    }

    internal static bool Verbose { get; set; } = false;

    internal static Grammar? FromFile(string file) {

        // Read in data
        string[] lines = File.ReadAllLines(file);

        // Set read mode
        var mode = ReadMode.Grammar;

        // Create grammar
        Grammar grammar = new Grammar();

        // Define vars to use while reading
        Production? lastProd = null;
        int priocntr = 0;

        // Set console colour
        Console.ForegroundColor = ConsoleColor.DarkGray;

        for (int i = 0; i < lines.Length; i++) {

            switch (mode) {
                case ReadMode.InClassData:
                    if (lines[i].StartsWith("@end")) {
                        mode = ReadMode.Grammar;
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine();
                    } else {
                        grammar.InClass.Add(lines[i]);
                        if (!string.IsNullOrEmpty(lines[i])) {
                            Console.WriteLine(lines[i]);
                        }
                    }
                    break;
                case ReadMode.UsingsData:
                    if (lines[i].StartsWith("@end")) {
                        mode = ReadMode.Grammar;
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Gray;
                    } else {
                        grammar.Using.Add(lines[i]);
                        if (!string.IsNullOrEmpty(lines[i])) {
                            Console.WriteLine(lines[i]);
                        }
                    }
                    break;
                case ReadMode.Grammar:

                    if (lines[i].StartsWith("@using")) {
                        mode = ReadMode.UsingsData;
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        continue;
                    } else if (lines[i].StartsWith("@class")) {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        mode = ReadMode.InClassData;
                        grammar.InClassStartIndex = i;
                        continue;
                    } else if (lines[i].StartsWith("@namespace")) {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        grammar.Namespace = lines[i][10..].Trim();
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine();
                        continue;
                    }

                    int commentCut = lines[i].IndexOf('#');
                    string line = commentCut >= 0 ? lines[i][0..commentCut] : lines[i];

                    if (__prec.Match(line) is Match m && m.Success) {

                        // Register presedence
                        RegisterPresedence(grammar, m, ref priocntr);

                    } else if (__rule.Match(line) is Match mr && mr.Success) {

                        // Register new production
                        lastProd = RegisterProduction(i + 1, grammar, mr, null);

                    } else if (__ruex.Match(line) is Match er && er.Success) {

                        if (lastProd is null) {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error adding grammar rule [{line}] on line #{i + 1}");
                        } else {

                            _ = RegisterProduction(i + 1, grammar, er, lastProd);

                        }
                    }
                    break;

            }

        }

        // Return null
        if (grammar.Productions.Count is 0) {
            return null;
        }

        // Log detected productions
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("The following productions were detected:");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        for (int i = 0; i < grammar.Productions.Count; i++) {
            Console.WriteLine($"[{i+1:000}] {grammar.Productions[i]}");
        }
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;

        // Return grammar
        return grammar;

    }

    internal void ComputeHashes(HashAlgorithm hash) {

        // Compute hashes
        ClassHash = hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join(' ', InClass)));
        UsingHash = hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join(' ', Using)));
        SemanticHash = hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join(' ', Productions.Select(x => x.SemanticInput))));
        ProductionHash = hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join(' ', Productions)));

    }

    internal void InjectEOF() {

        if (Productions.Count > 0) {
            if (Productions[0].Rhs.Length > 0 && Productions[0].Rhs[^1] != EndOfInput) {
                Productions[0].Rhs = Productions[0].Rhs.Append(EndOfInput).ToArray();
            }
        }

    }

    private static void RegisterPresedence(Grammar G, Match m, ref int priocntr) {

        Symbol[] tokens = m.Groups["tokens"].Value.Split(' ').Where(x => !string.IsNullOrEmpty(x)).Select(x => NewSymbol(G, x, true)).ToArray();

        G.Priorities.Add(new(priocntr++, Enum.Parse<Association>(m.Groups["type"].Value), tokens));

        if (Verbose) {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"Registered precedence rule [{G.Priorities[^1].Association} - {string.Join(", ", G.Priorities[^1].Tokens.Select(x => x.Sym))}]");
        }

    }

    private static Production RegisterProduction(int currLn, Grammar G, Match m, Production? P) {

        // Get LHS
        Symbol lhs = P is null ? NewSymbol(G, m.Groups["lhs"].Value) : P.Lhs;

        // Get RHS
        string[] rhst = m.Groups["rhs"].Value.Split(';').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
        Symbol[] rhs = RegisterTerminals(rhst, G);

        // Define priority
        Priority p = G.GetLastValidPriority(rhs);

        // Get PREC
        string prec = m.Groups["prec"].Value;
        if (!string.IsNullOrEmpty(prec)) {
            if (G.Symbols.TryGetValue(prec, out Symbol? token) && token is not null) {
                p = G.GetPriority(token);
            } else {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Invalid precedence '{prec}' assigned to production [{lhs} ::= {m.Groups["rhs"].Value}].");
            }
        }

        // Create
        Production R = new(currLn, lhs, rhs, m.Groups["code"].Value.Trim(), p);

        // Register
        G.Productions.Add(R);

        if (Verbose) {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"{(P is null ? "Creating" : "Extending")} grammar rule [{R}]");
        }

        // Return created rule
        return R;

    }

    private static Symbol NewSymbol (Grammar G, string sym, bool terminal = false) {
        if (G.Symbols.TryGetValue(sym, out Symbol? value)) {
            return value;
        } else {
            if (Verbose) {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"Adding symbol '{sym}' as {(terminal ? "terminal" : "non-terminal")}");
            }
            return G.Symbols[sym] = new Symbol(sym, terminal);
        }
    }

    private static Symbol[] RegisterTerminals(string[] rhs, Grammar g) {
        List<Symbol> symbols = new();
        foreach (string s in rhs) {
            if (s == "$") {
                symbols.Add(EndOfInput);
                continue;
            } else if (s == "~") {
                symbols.Add(g.EmptySymbol);
                continue;
            }
            int open = s.IndexOf('(');
            int close = s.LastIndexOf(')');
            if (open != -1 && close != -1) {
                if (HandleMacroSymbol(s, open, close, g) is MacroSymbol ms) {
                    symbols.Add(ms);
                }
            } else if (open != -1 || close != -1) {
                Console.WriteLine($"Error: The production entry '{s}' has one or more unmatched macro parenthesis sets.");
            } else if (!g.Symbols.ContainsKey(s)) {
                symbols.Add(g.Symbols[s] = new(s, Symbol.IsTerminalStr(s)));
                if (Verbose) {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"Adding symbol '{s}' as {(g.Symbols[s].IsTerminal ? "terminal" : "non-terminal")}");
                }
            } else {
                symbols.Add(g.Symbols[s]);
            }
        }
        return symbols.ToArray();
    }

    private static MacroSymbol? HandleMacroSymbol(string macro, int open, int close, Grammar g) {

        // Split raw data into more workable stuff
        string macroSymbol = macro[..open];
        string[] args = macro[(open + 1)..close].Split(',').Select(x => x.Trim()).ToArray();

        // Verify macro exists
        if (!g.Macros.ContainsKey(macroSymbol)) {
            Console.WriteLine($"Unknown macro token '{macroSymbol}'");
            return null;
        }

        // Get macro
        MacroType type = g.Macros[macroSymbol];

        // Get arguments as symbols
        Symbol[] syms = RegisterTerminals(args, g);

        // Return macro as symbol
        return new MacroSymbol(type, syms);

    }

    internal void RemoveUnusedProductions() {

        // Get set of unreached non-terminals
        Set<Symbol> nonTerminals = new(Symbols.Where(x => !x.Value.IsTerminal).Select(x => x.Value));

        // Get current production
        Set<Production> touched = new();

        // Local recursive look
        void Follow(Production p) {
            nonTerminals.Remove(p.Lhs);
            touched.Add(p);
            foreach (Symbol s in p.Rhs) {
                Production[] gotos = Productions.Where(x => x.Lhs == s).ToArray();
                foreach (Production sp in gotos) {
                    if (!touched.Contains(sp)) {
                        Follow(sp);
                    }
                }
            }
        }

        // Call on grammar entry
        Follow(Productions[0]);

        // If any left in non terminals
        if (nonTerminals.Count > 0) {
            Production[] unreachable = Productions.Where(x => nonTerminals.Contains(x.Lhs)).ToArray();
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (Production u in unreachable) {
                Console.WriteLine($"Detected unreachable production [{u}]");
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Productions.RemoveAll(x => unreachable.Contains(x));
        }

        // Loop over productions and assign an index
        for (int i = 0; i < Productions.Count; i++) {
            Productions[i].Index = i;
        }

    }

    internal Priority GetLastValidPriority(Symbol[] ls) {
        if (ls.Length > 0) {
            Symbol best = ls[^1];
            for (int i = 0; i < ls.Length; i++) {
                if (ls[i].IsTerminal) {
                    best = ls[i];
                }
            }
            return GetPriority(best);
        } else {
            return LowestPriority;
        }
    }

    internal Priority GetPriority(Symbol symbol)
        => Priorities.Find(x => x.Tokens.Contains(symbol)) ?? LowestPriority;

    internal void RunMacros(bool fullmacroset) {

        // Make sure user has allowed macros
        if (!fullmacroset) {
            foreach (var rule in Productions) {
                if (rule.Rhs.Any(x => x is MacroSymbol)) {
                    Console.WriteLine($"Error: Production {rule.Index} contains a macro. To enable macros use the -macro compile flag.");
                }
            }
            return;
        }

        // Init counter
        int i = 0;

        // Run over each new production
        while (i < Productions.Count) {

            // Contains a macro?
            if (Productions[i].Rhs.Any(x => x is MacroSymbol)) {

                // Loop over symbols until we find the first macro
                for (int j = 0; j < Productions[i].Rhs.Length; j++) {
                    if (Productions[i].Rhs[j] is MacroSymbol x) {

                        // Run macro
                        var subs = x.Macro.Creator(this, Productions[i], x.MacroArgs, j);

                        // Add new productions
                        Productions.AddRange(subs);

                        // break -> This way can process remaining macro symbols later without too many problems
                        break;
                    }
                }

                // Remove this production
                Productions.RemoveAt(i);

            } else { // go to next
                
                i++;

            }

        }

        // Loop over productions and assign a new ID
        for (i = 0; i < Productions.Count; i++) {
            Productions[i].Index = i;
        }

    }

}

