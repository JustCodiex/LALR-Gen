// Include parser gen
using ParserGen;

// Define target file
string targetfile = "grammar.g";
string prefix = string.Empty;
if (args.Length == 1) {
    targetfile = args[0];
    prefix = Path.GetFileNameWithoutExtension(targetfile) + "_";
}

// Define outs
string tableOut = prefix + "table.dat";
string semOut = prefix + "parsersemantics.g.fs";
string semOutBin = prefix + "parsetable.g.bin";
string tableTxt = prefix + "table.txt";
string tableConflict = prefix + "table.conflicts";

// Log output
Console.ForegroundColor = ConsoleColor.Magenta;
Console.WriteLine("@@ LALR-Parser generator");
Console.WriteLine("@@ Grammar file = " + targetfile);
Console.WriteLine("@@ Target Language = F#");
Console.WriteLine("@@ Table output = "+ tableOut);
Console.WriteLine("@@ Language output = "+ semOut);

// Bail if grammar is not there
if (!File.Exists(targetfile)) {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Grammar file not implemented.");
    return;
}

// Parse grammar
Grammar? G = Grammar.FromFile(targetfile);
if (G is null) {
    Console.WriteLine("Failed to read grammar file.");
    return;
}

// Cleanup unused productions
G.RemoveUnusedProductions();

// Generate LR table
LR1 lr = new(G, true);
lr.Run(tableTxt, tableConflict);

// Ensure table exists
if (lr.Table is null) {
    Console.WriteLine("Failed to generate LALR table.");
    return;
}

// Emit binary encoding of the table
BinaryTableEmit.Emit(lr.Table, G, semOutBin);

// Emit F# code
FSEmit.Emit(lr.Table, G, semOut);
