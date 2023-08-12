using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SourceGenerator.Grammar;

public class TopLevel
{
    public static void Match(TokenStream stream)
    {
        for (var token = stream.Next;
            token > 0;
            stream.Poll(), token = stream.Next)
        switch (stream.Next)
        {
            case 1:
                Console.WriteLine($"public object {stream.Text} {"{"} get; set; {"}"}");
                break;
        }
    }
}
