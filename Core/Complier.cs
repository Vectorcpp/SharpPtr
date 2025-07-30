using System;
using System.Collections.Generic;
using System.Linq;
using SharpPtr.System.Complier;

namespace SharpPtr.Core
{
    // hi
    class Lexer
    {
        string src;
        int i = 0;
        int line = 1;
        Stack<int> indentStack = new Stack<int>();

        public Lexer(string source)
        {
            src = source;
            indentStack.Push(0); // base indentation
        }

        public List<Token> Tokenize()
        {
            var toks = new List<Token>();
            bool atLineStart = true;

            

            while (!done())
            {
                char c = peek();

                // handle indentation at start of line
                if (atLineStart && (c == ' ' || c == '\t'))
                {
                    int indentLevel = 0;
                    while (!done() && (peek() == ' ' || peek() == '\t'))
                    {
                        if (peek() == ' ') indentLevel++;
                        else indentLevel += 4; // tab = 4 spaces
                        next();
                    }

                    if (!done() && peek() != '\n') // ignore blank lines
                    {
                        int currentIndent = indentStack.Peek();

                        if (indentLevel > currentIndent)
                        {
                            indentStack.Push(indentLevel);
                            toks.Add(new Token(TokenType.Indent, "INDENT", line));
                        }
                        else if (indentLevel < currentIndent)
                        {
                            while (indentStack.Count > 1 && indentStack.Peek() > indentLevel)
                            {
                                indentStack.Pop();
                                toks.Add(new Token(TokenType.Dedent, "DEDENT", line));
                            }
                        }
                    }
                    atLineStart = false;
                    continue;
                }

                if (c == '\n')
                {
                    toks.Add(new Token(TokenType.Newline, "\\n", line));
                    line++;
                    next();
                    atLineStart = true;
                    continue;
                }


                if (char.IsWhiteSpace(c))
                {
                    next();
                    continue;
                }

                atLineStart = false;

                if (char.IsLetter(c) || c == '_')
                {
                    string id = read(() => char.IsLetterOrDigit(peek()) || peek() == '_');

                    // check for keywords
                    TokenType type = id switch
                    {
                        "class" => TokenType.Class,
                        "main" => TokenType.Main,
                        "method" => TokenType.Funcs,

                        "int" => TokenType.Identifier,
                        "string" => TokenType.Identifier,
                        "bool" => TokenType.Identifier,
                        "float"  => TokenType.Identifier,
                        "double" => TokenType.Identifier,

                        _ => TokenType.Identifier
                    };

                    toks.Add(new Token(type, id, line));
                }
                else if (char.IsDigit(c))
                {
                    string num = read(() => char.IsDigit(peek()) || peek() == '.');
                    toks.Add(new Token(TokenType.Number, num, line));
                }
                else if (match('-', '>')) toks.Add(new Token(TokenType.Arrow, "->", line));
                else if (match('-', '?')) toks.Add(new Token(TokenType.CastSafe, "-?", line));
                else if (c == '(') { toks.Add(new Token(TokenType.LParen, "(", line)); next(); }
                else if (c == ')') { toks.Add(new Token(TokenType.RParen, ")", line)); next(); }
                else if (c == '{') { toks.Add(new Token(TokenType.LBrace, "{", line)); next(); }
                else if (c == '}') { toks.Add(new Token(TokenType.RBrace, "}", line)); next(); }
                else if (c == '.') { toks.Add(new Token(TokenType.Dot, ".", line)); next(); }
                else if (c == '=') { toks.Add(new Token(TokenType.Assign, "=", line)); next(); }
                else if (match(':', ':'))
                {
                    int currentIndent = indentStack.Peek();
                    if (currentIndent > 0)
                    {
                        while (indentStack.Count > 1)
                        {
                            indentStack.Pop();
                            toks.Add(new Token(TokenType.Dedent, "DEDENT", line));
                        }
                    }
                    toks.Add(new Token(TokenType.DoubleColon, "::", line));
                }
                else if (peek() == ':')
                {
                    toks.Add(new Token(TokenType.Colon, ":", line));
                    next();
                }
                else if (c == '"')
                {
                    next(); // consume opening quote
                    string str = "";

                    while (!done() && peek() != '"')
                    {
                        if (peek() == '\\' && i + 1 < src.Length) // handle escape sequences
                        {
                            next(); // consume backslash
                            char escaped = peek();
                            switch (escaped)
                            {
                                case 'n': str += '\n'; break;
                                case 't': str += '\t'; break;
                                case 'r': str += '\r'; break;
                                case '\\': str += '\\'; break;
                                case '"': str += '"'; break;
                                default: str += escaped; break;
                            }
                            next();
                        }
                        else
                        {
                            str += peek();
                            next();
                        }
                    }

                    if (done())
                        throw new Exception($"Unterminated string literal on line {line}");

                    next(); // consume closing quote
                    toks.Add(new Token(TokenType.String, str, line));
                }
                else if (c == ',') { toks.Add(new Token(TokenType.Comma, ",", line)); next(); }


                else throw new Exception($"wtf is '{c}' on line {line}");
            }

            // close any remaining indents
            while (indentStack.Count > 1)
            {
                indentStack.Pop();
                toks.Add(new Token(TokenType.Dedent, "DEDENT", line));
            }

            toks.Add(new Token(TokenType.EOF, "", line));
            return toks;
        }

        bool match(char a, char b)
        {
            if (i + 1 >= src.Length) return false;
            if (src[i] == a && src[i + 1] == b)
            {
                i += 2;
                return true;
            }
            return false;
        }

        string read(Func<bool> cond)
        {
            int start = i;
            while (!done() && cond()) i++;
            return src.Substring(start, i - start);
        }

        char peek() => i >= src.Length ? '\0' : src[i];
        char next() => src[i++];
        bool done() => i >= src.Length;
    }


    class Compiler
    {
        public void Run(string src)
        {
            var lex = new Lexer(src);
            var toks = lex.Tokenize();


            foreach (var t in toks)
                Console.WriteLine($"[{t.Type}] \"{t.Lexeme}\" on line {t.Line}");

            var parser = new Parser(toks);
            var nodes = parser.ParseAll();

            Console.WriteLine($"\nparsed nodes:");
            foreach (var node in nodes)
            {
                if (node is SafeCast cast)
                {
                    string result = SafeCast(cast, parser.Registry);
                    Console.WriteLine($"{cast.From}-?{cast.To} = {result}");
                }
                
            }

            Console.WriteLine("\nlocal stuff:");
            foreach (var c in parser.Registry.Calls.Values)
                Console.WriteLine($"  method: {c}");
            foreach (var c in parser.Registry.Casts.Values)
                Console.WriteLine($"  cast: {c}");
            foreach (var v in parser.Registry.Variables.Values)
                Console.WriteLine($"  var: {v}");

            PtrNode.ListAll();
        }

        private string SafeCast(SafeCast cast, SymbolRegistry registry)
        {
            if (!registry.Variables.TryGetValue(cast.From, out var varAssign))
                throw new Exception($"null variable '{cast.From}'");

            string value = varAssign.Value;
            string fromType = varAssign.VarType ?? "unknow";


            switch (cast.To)
            {
                case "string":
                    return value;

                case "int":
                    if (fromType == "string" && int.TryParse(value, out int i)) return i.ToString();

                    throw new Exception($"Cannot cast {fromType} to int");
                default:
                    throw new Exception($"Unsupported safe cast to {cast.To}");
            }
        }
    }

    // just a lil tester
    class Program
    {
        static void Main()
        {
            var compiler = new Compiler();

            Console.WriteLine("SharpPtr REPL - press F5 to compile, Esc to quit");
            Console.WriteLine("Use Python-style indentation for blocks");
            Console.WriteLine("Example:");
            Console.WriteLine("main:");
            Console.WriteLine("    health = 100");
            Console.WriteLine("    player->jump()");
            Console.WriteLine();

            var lines = new List<string>();
            string currentLine = "";
            bool needPrompt = true;

            while (true)
            {
                if (needPrompt)
                {
                    Console.Write("> ");
                    Console.Write(currentLine);
                    needPrompt = false;
                }

                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Escape)
                    break;

                /*

                if (key.Key == ConsoleKey.F5)
                {
                    if (!string.IsNullOrWhiteSpace(currentLine))
                        lines.Add(currentLine);

                    if (lines.Count > 0)
                    {
                        string input = string.Join("\n", lines);
                        Console.WriteLine("\n\n========== COMPILING ==========\n");

                        try
                        {
                            Console.WriteLine("Source Input (raw):\n" +
                                input.Replace(" ", "·").Replace("\t", "→") + "\n");

                            compiler.Run(input);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"error: {e.Message}");
                        }

                        Console.WriteLine("\n========== DONE ==========\n");

                        lines.Clear();
                        currentLine = "";
                        needPrompt = true;
                    }

                    continue;
                }

                */

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();

                    // If current line is empty and we have accumulated lines, finish input
                    if (string.IsNullOrWhiteSpace(currentLine) && lines.Count > 0)
                    {
                        // Empty line signals end of multi-line input - compile it
                        string input = string.Join("\n", lines);
                        Console.WriteLine("\n========== COMPILING ==========\n");

                        try
                        {
                            Console.WriteLine("Source Input (raw):\n" +
                                input.Replace(" ", "·").Replace("\t", "→") + "\n");

                            compiler.Run(input);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"error: {e.Message}");
                        }

                        Console.WriteLine("\n========== DONE ==========\n");

                        lines.Clear();
                        currentLine = "";
                        needPrompt = true;
                        continue;
                    }

                    if (currentLine.TrimEnd().EndsWith(":"))
                    {
                        lines.Add(currentLine);
                        currentLine = "    "; // auto-indent
                    }
                    else
                    {
                        lines.Add(currentLine);
                        currentLine = "";
                    }

                    needPrompt = true;
                    continue;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (currentLine.Length > 0)
                    {
                        currentLine = currentLine[..^1];
                        Console.Write("\b \b");
                    }
                    continue;
                }

                if (key.Key == ConsoleKey.Tab)
                {
                    currentLine += "    "; // insert spaces for tab
                    Console.Write("    ");
                    continue;
                }

                if (key.Key == ConsoleKey.Spacebar)
                {
                    currentLine += " ";
                    Console.Write(" ");
                    continue;
                }

                if (!char.IsControl(key.KeyChar))
                {
                    currentLine += key.KeyChar;
                    Console.Write(key.KeyChar);
                }
            }

            Console.WriteLine("\nbye!");
        }
    }
}