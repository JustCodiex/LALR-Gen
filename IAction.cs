using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserGen; 
internal enum ActionType {
    Error,
    Accept,
    Shift,
    Reduce,
    Goto
}

internal interface IAction {

    uint Encode();

    string EncodedString();

}

