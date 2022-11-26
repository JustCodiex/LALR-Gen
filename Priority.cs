namespace ParserGen;     

public enum Association {
    
    left,

    right,

    none

}

internal record Priority(int Level, Association Association, Symbol[] Tokens) {

    internal static Priority HighestPriority(Priority a, Priority b)
        => a.Level > b.Level ? a : b;

    internal bool IsEqualPriority(Priority other) => this.Level == other.Level;

    internal bool IsHigherThanMe(Priority other) => this.Level < other.Level;

}
