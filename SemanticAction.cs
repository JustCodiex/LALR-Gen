namespace ParserGen;     

internal class SemanticAction {

    public string Code { get; }

    public string Comment { get; }

    internal SemanticAction(string actionCode, string inlineComment) {
        this.Code = actionCode;
        this.Comment = inlineComment;
    }

}
