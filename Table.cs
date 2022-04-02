using System.Text;

namespace ParserGen;

internal class Table<T> {

    public int Rows { get; }

    public int Columns { get; }

    public string[] Headers { get; }

    private readonly T[,] m_cells;

    public T this[int row, int column] {
        get => this.GetCell(row, column);
        set => this.SetCell(row, column, value);
    }

    public Table(int rows, IEnumerable<string> headers) {
        this.Headers = headers.ToArray();
        this.Columns = this.Headers.Length;
        this.Rows = rows;
        this.m_cells = new T[rows, this.Columns];
    }

    public void SetDefault(T defaultValue) {
        for (int r = 0; r < this.Rows; r++) {
            for (int c = 0; c < this.Columns; c++) {
                this.m_cells[r, c] = defaultValue;
            }
        }
    }

    public void SetCell(int row, int column, T cellValue) {
        
        // Assert row index
        if (row < 0 || row >= this.Rows) {
            throw new IndexOutOfRangeException();
        }
        
        // Assert column index
        if (column < 0 || column >= this.Columns) {
            throw new IndexOutOfRangeException();
        }

        // Set cell value
        this.m_cells[row, column] = cellValue;

    }

    public T GetCell(int row, int column) {

        // Assert row index
        if (row < 0 || row >= this.Rows) {
            throw new IndexOutOfRangeException();
        }

        // Assert column index
        if (column < 0 || column >= this.Columns) {
            throw new IndexOutOfRangeException();
        }

        // Return cell
        return this.m_cells[row, column];

    }

    public void SaveToFile(string outputFile) {

        // Make sure there's stuff to write out
        if (this.Columns > 0 && this.Rows > 0) {

            // Open table file
            using FileStream fs = File.Open(outputFile, FileMode.Create);
            using StreamWriter sw = new(fs);

            // Get the length of the longest symbol
            int longestSymbol = this.Headers.Max(x => x.Length) + 5;

            sw.Write("State   ");
            for (int i = 0; i < this.Headers.Length; i++) {
                sw.Write($"| {this.Headers[i]}");
                int pad = longestSymbol + 1 - this.Headers[i].Length;
                sw.Write(new string(' ', pad));
            }
            sw.WriteLine();

            // Sum padding
            int ln = (longestSymbol + 3) * this.Headers.Length;
            string lnstr = new('-', ln + 8);

            // Write table
            for (int r = 0; r < this.Rows; r++) {
                sw.WriteLine(lnstr);
                string rs = r.ToString();
                sw.Write(rs);
                sw.Write(new string(' ', 8 - rs.Length));
                StringBuilder contents = new StringBuilder();
                for (int c = 0; c < this.Columns; c++) {
                    contents.Append("| ");
                    string content = this.m_cells[r, c]?.ToString() ?? "**NULL**";
                    contents.Append(content);
                    contents.Append(new string(' ', longestSymbol + 1 - content.Length));
                }
                sw.WriteLine(contents.ToString());
            }

        }

    }

    public Table<TMapped> Map<TMapped>(Func<T, TMapped> mapFunction) {
        Table<TMapped> mapped = new(this.Rows, this.Headers);
        for (int r = 0; r < this.Rows; r++) {
            for (int c = 0; c < this.Columns; c++) {
                mapped[r, c] = mapFunction(this[r,c]);
            }
        }
        return mapped;
    }

}
