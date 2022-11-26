namespace ParserGen; 

internal enum ActionType {
    Error,
    Accept,
    Shift,
    Reduce,
    Goto
}

internal interface IAction {

    uint Encode();

    string EncodedString();

}
