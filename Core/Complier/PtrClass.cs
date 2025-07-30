using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace SharpPtr.System.Complier
{
    enum PtrKind
    {
        Variable,
        Method,
        Type,
        Keyword,
        Operator,
        Builtin,
        Custom
    }

    class PtrEntry
    {
        public PtrKind Kind;
        public AstNode Definition;
        public string Description;
        public bool IsBuiltIn;

        public PtrEntry(PtrKind kind, AstNode def, string desc = "", bool builtin = false)
        {
            Kind = kind;
            Definition = def;
            Description = desc;
            IsBuiltIn = builtin;
        }

        public override string ToString() => $"[{Kind}] {Definition} - {Description}";
    }

    static class PtrNode
    {
        static Dictionary<string, PtrEntry> stuff = new Dictionary<string, PtrEntry>();
        static Dictionary<string, List<string>> tags = new Dictionary<string, List<string>>();

        // just add whatever
        public static void Register(string name, PtrKind kind, AstNode definition, string description = "")
        {
            if (stuff.ContainsKey(name))
            {
                // eh just overwrite
            }

            stuff[name] = new PtrEntry(kind, definition, description);
        }

        // quick adds for common stuff
        public static void AddMethod(string objName, string methodName, string desc = "")
        {
            var method = new MethodCall(objName, methodName);
            Register($"{objName}.{methodName}", PtrKind.Method, method, desc);
        }

        public static void AddCast(string from, string to, string desc = "")
        {
            var cast = new SafeCast(from, to);
            Register($"{from}_to_{to}", PtrKind.Type, cast, desc);
        }

        public static void AddVariable(string name, string value, string desc = "")
        {
            var variable = new VarAssign(name, value);
            Register(name, PtrKind.Variable, variable, desc);
        }

        // get my stuff back
        public static PtrEntry Get(string name)
        {
            return stuff.TryGetValue(name, out var entry) ? entry : null;
        }

        public static AstNode GetDefinition(string name)
        {
            var entry = Get(name);
            return entry?.Definition;
        }

        public static bool Exists(string name) => stuff.ContainsKey(name);

        // remove stuff when i mess up
        public static bool Remove(string name)
        {
            if (stuff.ContainsKey(name))
            {
                var entry = stuff[name];
                if (entry.IsBuiltIn)
                {
                    return false; // nah keep builtins
                }
                stuff.Remove(name);
                return true;
            }
            return false;
        }

        // tag stuff so i can find it later
        public static void AddTag(string name, string tag)
        {
            if (!tags.ContainsKey(tag))
                tags[tag] = new List<string>();

            if (!tags[tag].Contains(name))
                tags[tag].Add(name);
        }

        public static List<string> GetByTag(string tag)
        {
            return tags.TryGetValue(tag, out var items) ? items : new List<string>();
        }

        // get by type
        public static Dictionary<string, PtrEntry> GetByKind(PtrKind kind)
        {
            var result = new Dictionary<string, PtrEntry>();
            foreach (var kvp in stuff)
            {
                if (kvp.Value.Kind == kind)
                    result[kvp.Key] = kvp.Value;
            }
            return result;
        }

        // nuke everything (except builtins obv)
        public static void Clear()
        {
            var toRemove = new List<string>();
            foreach (var kvp in stuff)
            {
                if (!kvp.Value.IsBuiltIn)
                    toRemove.Add(kvp.Key);
            }

            foreach (var name in toRemove)
                stuff.Remove(name);

            tags.Clear();
        }

        // debug dump
        public static void ListAll()
        {
            Console.WriteLine("\n=== my stuff ===");
            foreach (var kvp in stuff)
            {
                var marker = kvp.Value.IsBuiltIn ? "[builtin]" : "[mine]";
                Console.WriteLine($"{marker} {kvp.Key}: {kvp.Value}");
            }

            if (tags.Count > 0)
            {
                Console.WriteLine("\n=== tags ===");
                foreach (var tag in tags)
                {
                    Console.WriteLine($"#{tag.Key}: {string.Join(", ", tag.Value)}");
                }
            }
        }

        // initialize with basic types i always need
        static PtrNode()
        {
            stuff["null"] = new PtrEntry(PtrKind.Keyword, null, "null value", true);
            stuff["void"] = new PtrEntry(PtrKind.Type, null, "void type", true);
            stuff["int"] = new PtrEntry(PtrKind.Type, null, "integer type", true);
            stuff["string"] = new PtrEntry(PtrKind.Type, null, "string type", true);
            stuff["bool"] = new PtrEntry(PtrKind.Type, null, "boolean type", true);
        }
    }

    class SymbolRegistry
    {
        public Dictionary<string, MethodCall> Calls = new Dictionary<string, MethodCall>();
        public Dictionary<string, SafeCast> Casts = new Dictionary<string, SafeCast>();
        public Dictionary<string, VarAssign> Variables = new Dictionary<string, VarAssign>();

        public void Add(MethodCall mc)
        {
            string key = $"{mc.Obj}.{mc.Method}";

            if (Calls.ContainsKey(key))
                throw new Exception($"'{key}' already exists");

            Calls[key] = mc;
            PtrNode.AddMethod(mc.Obj, mc.Method, "parsed method call");
        }

        public void Add(SafeCast sc)
        {
            var key = $"{sc.From}->{sc.To}";
            if (Casts.ContainsKey(key))
                throw new Exception($"'{key}' already exists");

            Casts[key] = sc;
            PtrNode.AddCast(sc.From, sc.To, "parsed safe cast");
        }

        public void Add(VarAssign va)
        {
            Variables[va.VarName] = va;
            PtrNode.AddVariable(va.VarName, va.Value, "parsed variable");
        }

        public void Add(ClassDef cd)
        {
            PtrNode.Register(cd.Name, PtrKind.Type, cd, "parsed class definition");
        }

        public void Add(MainDef md)
        {
            PtrNode.Register("main", PtrKind.Method, md, "main function");
        }
    }

    class Parser
    {
        List<Token> toks;
        int i = 0;
        public SymbolRegistry Registry = new SymbolRegistry();

        private bool inFunction = false;
        private HashSet<string> localVars = new HashSet<string>();

        public Parser(List<Token> tokens)
        {
            toks = tokens;
        }

        public AstNode Parse()
        {
            // type name = value (supports global + inside main/method)
            if (look(TokenType.Identifier) && look(TokenType.Identifier, 1) && look(TokenType.Assign, 2))
            {
                string typeName = grab(TokenType.Identifier).Lexeme;
                string varName = grab(TokenType.Identifier).Lexeme;
                grab(TokenType.Assign);

                string value = look(TokenType.Number) ? grab(TokenType.Number).Lexeme : grab(TokenType.Identifier).Lexeme;

                var v = new VarAssign(varName, value, typeName);
                Registry.Add(v);

                if (inFunction) localVars.Add(varName); // track local declared vars
                return v;
            }

            // class Name:
            if (look(TokenType.Class))
            {
                grab(TokenType.Class);
                string className = grab(TokenType.Identifier).Lexeme;
                grab(TokenType.Colon);
                grab(TokenType.Newline);
                grab(TokenType.Indent);

                var body = new List<AstNode>();
                while (!look(TokenType.Dedent) && !look(TokenType.DoubleColon) && !look(TokenType.EOF))
                {
                    body.Add(Parse());
                    if (look(TokenType.Newline)) grab(TokenType.Newline);
                }

                if (look(TokenType.DoubleColon)) grab(TokenType.DoubleColon);
                else if (look(TokenType.Newline) && look(TokenType.Dedent, 1)) grab(TokenType.Dedent);

                var c = new ClassDef(className, body);
                Registry.Add(c);
                return c;
            }

            // main:
            if (look(TokenType.Main))
            {
                grab(TokenType.Main);
                grab(TokenType.Colon);

                if (look(TokenType.Newline)) grab(TokenType.Newline);
                if (look(TokenType.Indent))
                {
                    grab(TokenType.Indent);
                    inFunction = true;
                    localVars.Clear();

                    var body = new List<AstNode>();
                    while (!look(TokenType.Dedent) && !look(TokenType.DoubleColon) && !look(TokenType.EOF))
                    {
                        body.Add(Parse());
                        if (look(TokenType.Newline)) grab(TokenType.Newline);
                    }

                    if (look(TokenType.DoubleColon)) grab(TokenType.DoubleColon);
                    else if (look(TokenType.Dedent)) grab(TokenType.Dedent);

                    inFunction = false;

                    var m = new MainDef(body);
                    Registry.Add(m);
                    return m;
                }
                else if (look(TokenType.DoubleColon))
                {
                    grab(TokenType.DoubleColon);
                    var m = new MainDef(new List<AstNode>());
                    Registry.Add(m);
                    return m;
                }

                throw new Exception("Expected indentation or '::' after main:");
            }

            // method name: or method name(params):
            if (look(TokenType.Funcs))
            {
                grab(TokenType.Funcs);
                string funcName = grab(TokenType.Identifier).Lexeme;

                // Parse parameters if present
                List<MethodParameter> parameters = new List<MethodParameter>();
                if (look(TokenType.LParen))
                {
                    parameters = ParseParameters();
                }

                grab(TokenType.Colon);

                if (look(TokenType.Newline)) grab(TokenType.Newline);

                var body = new List<AstNode>();

                if (look(TokenType.Indent))
                {
                    grab(TokenType.Indent);
                    inFunction = true;
                    localVars.Clear();

                    // Add parameters to local scope
                    foreach (var param in parameters)
                    {
                        localVars.Add(param.Name);
                    }

                    while (!look(TokenType.DoubleColon) && !look(TokenType.EOF))
                    {
                        body.Add(Parse());
                        if (look(TokenType.Newline)) grab(TokenType.Newline);
                    }

                    if (!look(TokenType.DoubleColon))
                        throw new Exception("Method blocks must be closed with '::'");

                    grab(TokenType.DoubleColon);
                    if (look(TokenType.Dedent)) grab(TokenType.Dedent);

                    inFunction = false;
                }
                else if (look(TokenType.DoubleColon))
                {
                    grab(TokenType.DoubleColon);
                }
                else throw new Exception("Expected indentation or '::'");

                var method = new MethodDef(funcName, parameters, body);
                PtrNode.Register(funcName, PtrKind.Method, method, "user-defined function");
                return method;
            }

            // obj->method() or obj->method(args)
            if (look(TokenType.Identifier) && look(TokenType.Arrow, 1))
            {
                string obj = grab(TokenType.Identifier).Lexeme;
                grab(TokenType.Arrow);
                string method = grab(TokenType.Identifier).Lexeme;

                List<string> arguments = new List<string>();
                if (look(TokenType.LParen))
                {
                    arguments = ParseArguments();
                }
                else
                {
                    throw new Exception("Expected '(' after method name");
                }

                var m = new MethodCall(obj, method, arguments);
                Registry.Add(m);
                return m;
            }

            // from-?to
            if (look(TokenType.Identifier) && look(TokenType.CastSafe, 1))
            {
                string from = grab(TokenType.Identifier).Lexeme;
                grab(TokenType.CastSafe);
                string to = grab(TokenType.Identifier).Lexeme;

                var c = new SafeCast(from, to);
                Registry.Add(c);
                return c;
            }

            // untyped name = value (only allowed if declared)
            if (look(TokenType.Identifier) && look(TokenType.Assign, 1))
            {
                string varName = grab(TokenType.Identifier).Lexeme;

                bool declared =
                    (!inFunction && Registry.Variables.ContainsKey(varName)) ||
                    (inFunction && (Registry.Variables.ContainsKey(varName) || localVars.Contains(varName)));

                if (!declared)
                    throw new Exception($"Variable '{varName}' is not declared");

                grab(TokenType.Assign);

                string value = look(TokenType.Number) ? grab(TokenType.Number).Lexeme : grab(TokenType.Identifier).Lexeme;

                var v = new VarAssign(varName, value);
                Registry.Add(v);
                return v;
            }

            throw new Exception("this syntax is broken");
        }

        // Parse method parameters: (type name, type name, ...) or (name, name, ...)
        private List<MethodParameter> ParseParameters()
        {
            var parameters = new List<MethodParameter>();

            grab(TokenType.LParen);

            while (!look(TokenType.RParen) && !look(TokenType.EOF))
            {
                if (look(TokenType.Identifier) && look(TokenType.Identifier, 1))
                {
                    // Typed parameter: type name
                    string type = grab(TokenType.Identifier).Lexeme;
                    string name = grab(TokenType.Identifier).Lexeme;
                    parameters.Add(new MethodParameter(name, type));
                }
                else if (look(TokenType.Identifier))
                {
                    // Untyped parameter: name
                    string name = grab(TokenType.Identifier).Lexeme;
                    parameters.Add(new MethodParameter(name));
                }

                if (look(TokenType.Comma))
                {
                    grab(TokenType.Comma);
                }
                else if (!look(TokenType.RParen))
                {
                    throw new Exception("Expected ',' or ')' in parameter list");
                }
            }

            grab(TokenType.RParen);
            return parameters;
        }

        // parse method call arguments: (value, value, ...)
        private List<string> ParseArguments()
        {
            var arguments = new List<string>();

            grab(TokenType.LParen);

            while (!look(TokenType.RParen) && !look(TokenType.EOF))
            {
                if (look(TokenType.Identifier))
                {
                    arguments.Add(grab(TokenType.Identifier).Lexeme);
                }
                else if (look(TokenType.Number))
                {
                    arguments.Add(grab(TokenType.Number).Lexeme);
                }
                else if (look(TokenType.String))  // ADD THIS CASE
                {
                    arguments.Add(grab(TokenType.String).Lexeme);
                }
                else
                {
                    throw new Exception("Expected argument value");
                }

                if (look(TokenType.Comma))
                {
                    grab(TokenType.Comma);
                }
                else if (!look(TokenType.RParen))
                {
                    throw new Exception("Expected ',' or ')' in argument list");
                }
            }

            grab(TokenType.RParen);
            return arguments;
        }


        public List<AstNode> ParseAll()
        {
            var nodes = new List<AstNode>();

            while (!look(TokenType.EOF))
            {
                while (look(TokenType.Newline)) grab(TokenType.Newline);

                if (look(TokenType.DoubleColon))
                {
                    grab(TokenType.DoubleColon);
                    continue;
                }

                if (look(TokenType.EOF)) break;

                nodes.Add(Parse());
            }
            return nodes;
        }

        bool look(TokenType t, int ahead = 0) => i + ahead < toks.Count && toks[i + ahead].Type == t;

        Token grab(TokenType t)
        {
            if (!look(t))
                throw new Exception($"expected {t}, got {toks[i].Type} at line {toks[i].Line}");
            return toks[i++];
        }
    }

}
