using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpPtr.System.Complier
{
    // Updated TokenType enum - add to Token.cs
    enum TokenType
    {
        Identifier,
        Number,
        String,        // For string literals like "Alice"
        Arrow,         // ->
        CastSafe,      // -?
        LParen, RParen,
        LBrace, RBrace, // { }
        Dot,           // .
        Assign,        // =
        Comma,         // ,
        Colon, DoubleColon, // :, ::
        Class,         // class keyword
        Main,          // main keyword
        Indent, Dedent, // indentation tokens
        Newline,
        Funcs,
        EOF
    }

    class Token
    {
        public TokenType Type;
        public string Lexeme;
        public int Line;

        public Token(TokenType type, string lexeme, int line)
        {
            Type = type;
            Lexeme = lexeme;
            Line = line;
        }

        public override string ToString()
        {
            return $"{Type} \"{Lexeme}\"";
        }
    }
}
