using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpPtr.System.Complier
{
    abstract class AstNode { }

    class MethodCall : AstNode
    {
        public string Obj, Method;
        public MethodCall(string o, string m)
        {
            Obj = o;
            Method = m;
        }
        public override string ToString() => $"{Obj}->{Method}()";
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

    class MethodDef : AstNode
    {
        public string Name;
        public List<AstNode> Body;

        public MethodDef(string name ,List<AstNode> body)
        {
            Name = name;
            Body = body;
        }
        public override string ToString() => $"method {{ {Body.Count} items }}";
    }
}
