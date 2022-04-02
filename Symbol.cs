using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserGen;     
internal class Symbol {

    public string Sym { get; }

    public bool IsTerminal { get; }

    public bool IsNullable { get; set; }

    public int Index { get; set; }

    public Set<Symbol> First { get; } // Used by v1 grammar

    public Symbol(string sym, bool terminal) {
        this.Sym = sym;
        this.IsTerminal = terminal;
        this.First = new();
    }

    public override string ToString() => this.IsTerminal ? $"\"{this.Sym}\"" : $"<{this.Sym}>";

    public static bool IsTerminalStr(string str) => str.All(char.IsUpper) || str == "~";

    public override bool Equals(object? obj) => obj is Symbol s && s.Sym == this.Sym;

    public override int GetHashCode() => this.Sym.GetHashCode();

}

