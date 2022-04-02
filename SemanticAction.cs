using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserGen;     
internal class SemanticAction {

    public string Code { get; }

    public string Comment { get; }

    internal SemanticAction(string actionCode, string inlineComment) {
        this.Code = actionCode;
        this.Comment = inlineComment;
    }

}

