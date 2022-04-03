namespace ParserGen;

internal partial class LR1 {

    private readonly List<LR1Conflict> Conflicts;

    private void SolveSolveableConflicts() {

        // Loop over all states in table
        foreach (var state in this.States) {

            // Loop over all symbols in state
            foreach ((Symbol symbol, List<LR1Action> actions) in state.Actions) {
                if (actions.Count > 1) { // If more than one action possible

                    // Define the best possible action
                    LR1Action resolvedAction = actions[0];

                    // Loop over all actions
                    for (int i = 1; i < actions.Count; i++) {
                        this.Conflicts.Add(this.TryResolve(state.Index, symbol, resolvedAction, actions[i], out resolvedAction));
                    }

                    // Set action
                    actions.Clear();
                    actions.Add(resolvedAction);

                }
            }

        }

    }

    private LR1Conflict TryResolve(int state, Symbol S, LR1Action first, LR1Action second, out LR1Action pick) {

        // Set pick to first
        pick = first;

        // Flag setting if resolved
        bool resolved = false;

        // Determine conflict type
        // Is shift/shft?
        if (first.Action == ActionType.Shift && second.Action == ActionType.Shift) {

            // Return shift/shift conflict (no resolution approach here and should never happen)
            return new LR1ShiftShiftConflict(state, S, first.ActionArgument, second.ActionArgument) { Resolved = resolved };

        } else if (first.Action == ActionType.Reduce && second.Action == ActionType.Reduce) { // Is reduce/reduce?

            // Check priorities
            Priority firstPriority = this.G.Productions[first.ActionArgument].Priority;
            Priority secondPriority = this.G.Productions[second.ActionArgument].Priority;

            // Check if of different priorities
            if (firstPriority != secondPriority) {

                // Mark as resolved, one or both have explicit priorities defined
                resolved = true;

                // Determine which to use
                if (firstPriority.IsHigherThanMe(secondPriority)) {
                    pick = second;
                } else {
                    pick = first;
                }

            } else {

                // Not resolved, pick based on index
                pick = first.ActionArgument < second.ActionArgument ? first : second;

            }

            // Return reduce/reduce conflict
            return new LR1ReduceReduceConflict(state, S, first.ActionArgument, second.ActionArgument) { Resolved = resolved };

        } else { // Is shift/reduce

            // Get nice references to the shift/reduce actions
            LR1Action shift = first.Action == ActionType.Shift ? first : second;
            LR1Action reduce = first.Action == ActionType.Reduce ? first : second;

            // Get shift priority
            Priority shiftPriority = this.G.GetPriority(S);
            Priority reducePriority = this.G.Productions[reduce.ActionArgument].Priority;

            // Determine who gets to decide
            if (shiftPriority.IsEqualPriority(reducePriority)) {
                bool guided = true;
                switch (shiftPriority.Association) {
                    case Association.left:
                        pick = reduce;
                        break;
                    case Association.right:
                        pick = shift;
                        break;
                    case Association.none:
                        pick = new LR1Action(ActionType.Error); // Syntax error
                        guided = shiftPriority.Level != this.G.LowestPriority.Level;
                        break;
                }
                resolved = guided; // Mark as resolved, if guided was true
            } else if (shiftPriority.IsHigherThanMe(reducePriority)) { // resolve in favour of highest, in this case reduce
                pick = reduce;
                resolved = true;
            } else if (reducePriority.IsHigherThanMe(shiftPriority)) { // resolve in favour of highest, in this case shift
                pick = shift;
                resolved = true;
            }

            // Return shift/reduce conflict
            return new LR1ShiftReduceConflict(state, S, shift.ActionArgument, reduce.ActionArgument) { Resolved = resolved };

        }

    }

    private void ReportUnresolvedConflicts(string conflictOutput) {

        // Get unresolved conflicts
        var unresolved = this.Conflicts.Where(x => !x.Resolved);

        // Select specifics
        var src = unresolved.Where(x => x is LR1ShiftReduceConflict).Cast<LR1ShiftReduceConflict>().ToArray();
        var ssc = unresolved.Where(x => x is LR1ShiftShiftConflict).Cast<LR1ShiftShiftConflict>().ToArray();
        var rrc = unresolved.Where(x => x is LR1ReduceReduceConflict).Cast<LR1ReduceReduceConflict>().ToArray();
        int total = src.Length + ssc.Length + rrc.Length;

        // If any to debug
        if (total > 0) {

            // Set console colour
            Console.ForegroundColor = ConsoleColor.Yellow;

            // Open stream writer
            using StreamWriter sw = new(File.Open(conflictOutput, FileMode.Create));

            // Add some spacing
            Console.WriteLine();

            // Log shift/reduce conflicts
            if (src.Length > 0) {
                Console.WriteLine($"Detected {src.Length} shift/reduce conflicts");
                sw.WriteLine("Shift/Reduce conflicts:");
                foreach (var sr in src) {
                    sw.Write($"\tIn state {sr.ConflictState}, there's a conflict in ");
                    sw.WriteLine($"shifting to state {sr.Shift} or reducing by production {sr.Reduce} on symbol {sr.Symbol.Sym}");
                }
                sw.WriteLine();
            }

            // Log shift/shift conflicts
            if (ssc.Length > 0) {
                Console.WriteLine($"Detected {ssc.Length} shift/shift conflicts"); sw.WriteLine("Shift/Reduce conflicts:");
                foreach (var ss in ssc) {
                    sw.Write($"\tIn state {ss.ConflictState}, there's a conflict in ");
                    sw.WriteLine($"shifting to state {ss.First} or shifting to state {ss.Second} on symbol {ss.Symbol.Sym}.");
                }
                sw.WriteLine();
            }

            // Log reduce/reduce conflicts
            if (rrc.Length > 0) {
                Console.WriteLine($"Detected {rrc.Length} reduce/reduce conflicts"); sw.WriteLine("Reduce/Reduce conflicts:");
                foreach (var rr in rrc) {
                    sw.Write($"\tIn state {rr.ConflictState}, there's a conflict in ");
                    sw.WriteLine($"reducing by production {rr.First} or reducing by production {rr.Second} on symbol {rr.Symbol.Sym}.");
                }
                sw.WriteLine();
            }

            // Spacing
            Console.WriteLine();

        } else {

            // Set console colour
            Console.ForegroundColor = ConsoleColor.Green;

            // Log that no fatal conflicts were detected
            Console.WriteLine(this.Conflicts.Count == 0 ? "Parse table contains no conflicts." : "Parse table had conflicts but all were resolved.");

        }

        // Set console colour back
        Console.ForegroundColor = ConsoleColor.White;

    }

}

