using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpPtr.System.Complier
{
    abstract class AstNode { }

    // definition for methods
    class MethodParameter
    {
        public string Name;
        public string Type;

        public MethodParameter(string name, string type = null)
        {
            Name = name;
            Type = type;
        }

        public override string ToString() => Type != null ? $"{Type} {Name}" : Name;
    }

    class MethodCall : AstNode
    {
        public string Obj, Method;
        public List<string> Arguments; // argument values passed to method

        public MethodCall(string o, string m, List<string> args = null)
        {
            Obj = o;
            Method = m;
            Arguments = args ?? new List<string>();
        }

        public override string ToString()
        {
            if (Arguments.Count > 0)
                return $"{Obj}->{Method}({string.Join(", ", Arguments)})";
            return $"{Obj}->{Method}()";
        }
    }

    class SafeCast : AstNode
    {
        public string From, To;
        public SafeCast(string f, string t)
        {
            From = f;
            To = t;
        }
        public override string ToString() => $"{From}-?{To}";
    }

    class VarAssign : AstNode
    {
        public string VarName, Value, VarType;
        public VarAssign(string name, string val, string type = null)
        {
            VarName = name;
            Value = val;
            VarType = type;
        }
        public override string ToString() => $"{VarName} = {Value}";
    }

    class ClassDef : AstNode
    {
        public string Name;
        public List<AstNode> Body;
        public ClassDef(string name, List<AstNode> body)
        {
            Name = name;
            Body = body;
        }
        public override string ToString() => $"class {Name} {{ {Body.Count} items }}";
    }

    class MainDef : AstNode
    {
        public List<AstNode> Body;
        public MainDef(List<AstNode> body)
        {
            Body = body;
        }
        public override string ToString() => $"main {{ {Body.Count} items }}";
    }

    // Enhanced MethodDef with parameters
    class MethodDef : AstNode
    {
        public string Name;
        public List<MethodParameter> Parameters;
        public List<AstNode> Body;

        public MethodDef(string name, List<MethodParameter> parameters, List<AstNode> body)
        {
            Name = name;
            Parameters = parameters ?? new List<MethodParameter>();
            Body = body;
        }

        public override string ToString()
        {
            var paramStr = Parameters.Count > 0
                ? $"({string.Join(", ", Parameters)})"
                : "()";
            return $"method {Name}{paramStr} {{ {Body.Count} items }}";
        }
    }
}