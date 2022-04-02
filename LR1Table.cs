namespace ParserGen;

internal partial class LR1 {

    private readonly List<LR1State> States;

    public Table<LR1Action>? Table { get; set; }

    private void GetStates() {

        for (int i = 0; i < this.Kernels.Count; i++) { 
        
            var kernel = this.Kernels[i];
            LR1State state = new (i);

            this.States.Add(state);

            foreach (var key in kernel.Keys) { 
                
                var next = kernel.Gotos[key];

                if (key.IsTerminal) {
                    state.Push(key, new(ActionType.Shift, next));
                } else {
                    state.Push(key, new(ActionType.Goto, next));
                }

            }

            foreach (var item in kernel.Closure) { 
                
                if (item.Pos == item.Rule.Rhs.Length || item.Rule.Rhs[0].IsNullable) {

                    foreach (var k in item.Lookahead) {

                        if (item.Rule.Index is 0) {
                            state.Push(k, new(ActionType.Accept));
                        } else {
                            state.Push(k, new(ActionType.Reduce, item.Rule.Index));
                        }

                    }

                }

            }

        }

    }

    private void CreateTable() {

        // Setup table
        this.Table = new(this.States.Count, this.G.Symbols.OrderBy(x => x.Value.IsTerminal).Select(x => x.Key));
        this.Table.SetDefault(new(ActionType.Error));

        // Loop over all states
        foreach (var state in this.States) {

            // Loop over all symbols in state
            foreach (var actions in state.Actions) {

                // Get column
                int c = actions.Key.Index;

                // Set to first element
                this.Table.SetCell(state.Index, c, actions.Value[0]);

            }

        }

    }

}

