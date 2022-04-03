// Include parser gen
using ParserGen;

// Log output
Console.ForegroundColor = ConsoleColor.Magenta;
Console.WriteLine("@@ LALR-Parser generator");
Console.WriteLine("@@ Grammar file = grammar.g");
Console.WriteLine("@@ Target Language = F#");
Console.WriteLine("@@ Table output = table.dat");
Console.WriteLine("@@ Language output = parsersemantics.fs");

// Bail if grammar is not there
if (!File.Exists("grammar.g")) {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Grammar file not implemented.");
    return;
}

// Parse grammar
Grammar? G = Grammar.FromFile("grammar.g");
if (G is null) {
    Console.WriteLine("Failed to read grammar file.");
    return;
}

// Cleanup unused productions
G.RemoveUnusedProductions();

// Generate LR table
LR1 lr = new(G, true);
lr.Run("table.txt", "table.conflicts");

// Ensure table exists
if (lr.Table is null) {
    Console.WriteLine("Failed to generate LALR table.");
    return;
}

// Create emitter
FSEmit.Emit(lr.Table, G, "parsersemantics.fs");
