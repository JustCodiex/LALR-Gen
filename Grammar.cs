using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ParserGen;

internal class Grammar {

    static readonly Regex __prec = new(@"%(?<type>left|right|none)\s+(?<tokens>(\w|\s)+)");
    static readonly Regex __rule = new(@"(?<lhs>\w+)\s*::=(?<rhs>(\w|\s|;|!|~|\$|\(|\)|,)+)\{(?<code>.*?)\}\s*(%prec\s+(?<prec>(\w)+))?");
    static readonly Regex __ruex = new(@"\|(?<rhs>(\w|\s|\$|!|~|;|\(|\)|,)+)\{(?<code>.*?)\}\s*(%prec\s+(?<prec>(\w)+))?");

    internal List<Priority> Priorities { get; }

    internal List<Production> Productions { get; }

    internal List<string> Using { get; }

    internal List<string> InClass { get; }

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
        this.LowestPriority = new(-1, Association.none, Array.Empty<Symbol>());
        this.Priorities = new() { this.LowestPriority };
        this.Productions = new();
        this.Symbols = new();
        this.Using = new();
        this.InClass = new();
        this.InClassStartIndex = 0;
        this.Symbols["$"] = EndOfInput;
        this.Symbols["~"] = EmptySymbol;
        this.ProductionHash = Array.Empty<byte>();
        this.SemanticHash = Array.Empty<byte>();
        this.UsingHash = Array.Empty<byte>();
        this.ClassHash = Array.Empty<byte>();
        this.Macros = (new MacroType[] { 
            new MacroType("optional", MacroOp.Optional),
            //new MacroType("separated_list"),
            //new MacroType("separated_list_nonempty"),
            //new MacroType("list"),
            //new MacroType("list_nonempty")
        }).ToDictionary(k => k.Name, v => v);
    }

    internal void CalculateFirsts() {

        // Add all terminal symbols to their of first set
        foreach (var (_, sym) in this.Symbols) {
            if (sym.IsTerminal) {
                sym.First.Add(sym);
            }
        }

        bool updated = true;
        while (updated) {
            updated = false;

            foreach (Production p in this.Productions) {

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
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        continue;
                    } else if (lines[i].StartsWith("@class")) {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        mode = ReadMode.InClassData;
                        grammar.InClassStartIndex = i;
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
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("The following productions were detected:");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        for (int i = 0; i < grammar.Productions.Count; i++) {
            Console.WriteLine($"[{(i+1):000}] {grammar.Productions[i]}");
        }
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;

        // Return grammar
        return grammar;

    }

    internal void ComputeHashes(HashAlgorithm hash) {

        // Compute hashes
        this.ClassHash = hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join(' ', this.InClass)));
        this.UsingHash = hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join(' ', this.Using)));
        this.SemanticHash = hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join(' ', this.Productions.Select(x => x.SemanticInput))));
        this.ProductionHash = hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join(' ', this.Productions)));

    }

    internal void InjectEOF() {

        if (this.Productions.Count > 0) {
            if (this.Productions[0].Rhs.Length > 0 && this.Productions[0].Rhs[^1] != EndOfInput) {
                this.Productions[0].Rhs = this.Productions[0].Rhs.Append(EndOfInput).ToArray();
            }
        }

    }

    private static void RegisterPresedence(Grammar G, Match m, ref int priocntr) {

        Symbol[] tokens = m.Groups["tokens"].Value.Split(' ').Where(x => !string.IsNullOrEmpty(x)).Select(x => NewSymbol(G, x, true)).ToArray();

        G.Priorities.Add(new(priocntr++, Enum.Parse<Association>(m.Groups["type"].Value), tokens));
        
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"Registered precedence rule [{G.Priorities[^1].Association} - {string.Join(", ", G.Priorities[^1].Tokens.Select(x => x.Sym))}]");

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
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"{(P is null ? "Creating" : "Extending")} grammar rule [{R}]");

        // Return created rule
        return R;

    }

    private static Symbol NewSymbol (Grammar G, string sym, bool terminal = false) {
        if (G.Symbols.ContainsKey(sym)) {
            return G.Symbols[sym];
        } else {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Adding symbol '{sym}' as {(terminal ? "terminal" : "non-terminal")}");
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
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"Adding symbol '{s}' as {(g.Symbols[s].IsTerminal ? "terminal" : "non-terminal")}");
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
        Set<Symbol> nonTerminals = new(this.Symbols.Where(x => !x.Value.IsTerminal).Select(x => x.Value));

        // Get current production
        Set<Production> touched = new();

        // Local recursive look
        void Follow(Production p) {
            nonTerminals.Remove(p.Lhs);
            touched.Add(p);
            foreach (Symbol s in p.Rhs) {
                Production[] gotos = this.Productions.Where(x => x.Lhs == s).ToArray();
                foreach (Production sp in gotos) {
                    if (!touched.Contains(sp)) {
                        Follow(sp);
                    }
                }
            }
        }

        // Call on grammar entry
        Follow(this.Productions[0]);

        // If any left in non terminals
        if (nonTerminals.Count > 0) {
            Production[] unreachable = this.Productions.Where(x => nonTerminals.Contains(x.Lhs)).ToArray();
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (Production u in unreachable) {
                Console.WriteLine($"Detected unreachable production [{u}]");
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            this.Productions.RemoveAll(x => unreachable.Contains(x));
        }

        // Loop over productions and assign an index
        for (int i = 0; i < this.Productions.Count; i++) {
            this.Productions[i].Index = i;
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
            return this.GetPriority(best);
        } else {
            return this.LowestPriority;
        }
    }

    internal Priority GetPriority(Symbol symbol)
        => this.Priorities.Find(x => x.Tokens.Contains(symbol)) ?? this.LowestPriority;

    internal void RunMacros(bool fullmacroset) {

        // Make sure user has allowed macros
        if (!fullmacroset) {
            foreach (var rule in this.Productions) {
                if (rule.Rhs.Any(x => x is MacroSymbol)) {
                    Console.WriteLine($"Error: Production {rule.Index} contains a macro. To enable macros use the -macro compile flag.");
                }
            }
            return;
        }

        // Init counter
        int i = 0;

        // Run over each new production
        while (i < this.Productions.Count) {

            // Contains a macro?
            if (this.Productions[i].Rhs.Any(x => x is MacroSymbol)) {

                // Loop over symbols until we find the first macro
                for (int j = 0; j < this.Productions[i].Rhs.Length; j++) {
                    if (this.Productions[i].Rhs[j] is MacroSymbol x) {

                        // Run macro
                        var subs = x.Macro.Creator(this, this.Productions[i], x.MacroArgs, j);

                        // Add new productions
                        this.Productions.AddRange(subs);

                        // break -> This way can process remaining macro symbols later without too many problems
                        break;
                    }
                }

                // Remove this production
                this.Productions.RemoveAt(i);

            } else { // go to next
                
                i++;

            }

        }

        // Loop over productions and assign a new ID
        for (i = 0; i < this.Productions.Count; i++) {
            this.Productions[i].Index = i;
        }

    }

}

