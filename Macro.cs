using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserGen;

internal delegate Production[] MacroDelegate(Grammar G, Production P, Symbol[] args, int macroId);

internal class MacroType {

    public string Name { get; }

    public MacroDelegate Creator { get; }

    public MacroType(string name, MacroDelegate del) {
        this.Name = name;
        this.Creator = del;
    }

}

internal class MacroSymbol : Symbol {

    public MacroType Macro { get; }

    public Symbol[] MacroArgs { get; }

    public MacroSymbol(MacroType macro, Symbol[] Args) : base(macro.Name, false) {
        this.Macro = macro;
        this.MacroArgs = Args;
    }

}

