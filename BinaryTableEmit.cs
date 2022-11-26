namespace ParserGen;

internal static class BinaryTableEmit {

    internal static void Emit(Table<LR1Action> table, Grammar G, string filename) {

        // Delete existing
        if (File.Exists(filename)) { File.Delete(filename); }

        // Open writer in binary mode
        using var fs = File.OpenWrite(filename);
        using var writer = new BinaryWriter(fs);

        for (int row = 0; row < table.Rows; row++) {
            for (int col = 0; col < table.Columns; col++) {

                // Get cell
                var cell = table[row, col];

                // Action type
                var arg = cell.Encode();

                // Check
                writer.Write(arg);

            }
        }

    }

}
