namespace ParserGen; 

internal class LR1Action : IAction {

    internal ActionType Action { get; set; }

    internal int ActionArgument { get; set; }

    internal LR1Action(ActionType action) {
        this.Action = action;
        this.ActionArgument = 0;
    }

    internal LR1Action(ActionType action, int index) {
        this.Action = action;
        this.ActionArgument = index;
    }

    public override string ToString() => this.Action switch {
        ActionType.Goto => $"-> {this.ActionArgument}",
        ActionType.Shift => $"S({this.ActionArgument})",
        ActionType.Reduce => $"R({this.ActionArgument})",
        ActionType.Accept => "Accept",
        _ => string.Empty
    };

    public uint Encode() => this.Action switch {
        ActionType.Accept => 0x1u,
        ActionType.Shift => (uint)(0x10000000u | (this.ActionArgument)),
        ActionType.Reduce => (uint)(0x20000000u | (this.ActionArgument)),
        ActionType.Goto => (uint)(0x30000000u | (this.ActionArgument)),
        _ => 0x0u
    };

    public string EncodedString() => "0x" + this.Encode().ToString("X") + "u";

}

