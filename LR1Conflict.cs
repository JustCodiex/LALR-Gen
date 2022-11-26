namespace ParserGen; 

internal abstract record LR1Conflict(int ConflictState, Symbol Symbol, bool Resolved);

internal record LR1ShiftReduceConflict(int ConflictState, Symbol Symbol, int Shift, int Reduce)
    : LR1Conflict(ConflictState, Symbol, false);

internal record LR1ReduceReduceConflict(int ConflictState, Symbol Symbol, int First, int Second)
    : LR1Conflict(ConflictState, Symbol, false);

internal record LR1ShiftShiftConflict(int ConflictState, Symbol Symbol, int First, int Second)
    : LR1Conflict(ConflictState, Symbol, false);
