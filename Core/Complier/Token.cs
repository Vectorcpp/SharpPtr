using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpPtr.System.Complier
{
    enum TokenType
    {
        Identifier,
        Number,
        Arrow, // ->
        CastSafe, // -?
        LParen, RParen,
        LBrace, RBrace, // { }
        Dot, // .
        Assign, // =
        Colon, DoubleColon, // :, ::
        Class, // class keyword
        Main, // main keyword
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
