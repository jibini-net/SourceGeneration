using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceGenerator;

public enum Token
{
    Schema,
    Partial,
    Repo,
    Ident,
    LCurly,
    RCurly,
    LParen,
    RParen
}
