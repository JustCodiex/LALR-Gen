namespace ParserGen;     

internal partial class LR1 {

    public Grammar G { get; }

    /// <summary>
    /// Get if the algorithm should generate an LALR(1) parse table instead of a canonical LR(1)
    /// </summary>
    public bool LALR { get; }

    public LR1(Grammar G, bool lalr) {
        this.G = G;
        this.LALR = lalr;
        this.FirstSets = new();
        this.FollowSets = new();
        this.Kernels = new();
        this.States = new();
        this.Conflicts = new();
    }

    public void Run(string tableOutput, string conflictOutput) {

        // Delete table file if already exist
        if (File.Exists(tableOutput)) {
            File.Delete(tableOutput);
        }

        // Delete conflict file if already exist
        if (File.Exists(conflictOutput)) {
            File.Delete(conflictOutput);
        }

        // Assign indices to all symbols
        if (this.G.Symbols.All(x => x.Value.Index is 0)) {
            int index = 0;
            foreach ((string _, Symbol s) in this.G.Symbols.OrderBy(x => x.Value.IsTerminal)) {
                s.Index = index++;
            }
        }

        // Collect first sets
        this.GetFirst();

        // Collect follow sets
        this.GetFollow();

        // Collect closures
        this.GetClosures();

        // Log
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("Generated FIRST-set, FOLLOW-set and closure sets.");

        // Get states from closures
        this.GetStates();

        // Log
        Console.WriteLine("Generated automaton states.");

        // Solve conflicts
        this.SolveSolveableConflicts();

        // Report unresolved
        this.ReportUnresolvedConflicts(conflictOutput);

        // Create table
        this.CreateTable();

        // Make sure table is defined
        if (this.Table is null) {
            return;
        }

        // Save table to text output file
        if (!string.IsNullOrEmpty(tableOutput)) {
            this.Table.SaveToFile(tableOutput);
        }

        // Log
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Successfully generated table.");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;

    }

    internal LR1Item Item(Production r, int p) => this.LALR ? new LALR1Item(r, p) : new LR1Item(r, p);

}

