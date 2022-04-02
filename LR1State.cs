namespace ParserGen;

internal class LR1State {

    public int Index { get; }

    public Dictionary<Symbol, List<LR1Action>> Actions { get; }

    public LR1State(int index) {
        this.Index = index;
        this.Actions = new();
    }

    public void Push(Symbol symbol, LR1Action action) {
        if (this.Actions.ContainsKey(symbol)) {
            this.Actions[symbol].Add(action);
        } else {
            this.Actions[symbol] = new() { action };
        }
    }

}

