using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Language;

public enum NameScope
{
    Undefined,
    Local,
    EnclosedRead,
    EnclosedReadWrite,
    Global,
    Builtin,
}

// Added when I started to run into issues with classes in particular. Stuff like:
// a = 4
// class Foo:
//    nonlocal a <-- foul!
//
// So I needed to know what a given CodeNamesNode type was representing.
//
// I've included the types I could think of, with help from:
// https://realpython.com/python-scope-legb-rule/#discovering-unusual-python-scopes
//
// I currently only use Class and NotClass
//
// TODO: Use the unused types!
public enum ScopeType
{
    ModuleUnused,
    Function,
    Class,
    ComprehensionUnused,
    ExceptionUnused,
    NotClass
}

// TODO: Plumb this out to regular full-flow parsing so the line number and printout gets conveyed to the user.
public class VariableScanSyntaxException : Exception
{
    public int Line { get; private set; }
    public VariableScanSyntaxException(string msg, int line) : base(msg)
    {
        Line = line;
    }
}

public class UnboundNonlocalException : VariableScanSyntaxException
{
    public UnboundNonlocalException(string name, ParserRuleContext context) : 
        base("line " + context.Start.Line + ": no binding for nonlocal '" + name + "' found", context.Start.Line)
    {

    }
}

/// <summary>
/// Data structure for managing node names across layers of code. This is used to tell in the code generation pass
/// whether or not to treat a variable as global, enclosed, or local.
/// </summary>
public class CodeNamesNode
{
    public CodeNamesNode Parent;
    public Dictionary<string, NameScope> NamedScopes;
    public Dictionary<string, CodeNamesNode> Children;
    
    public ScopeType ScopeType { get; protected set; }
    public void SetScopeType(ScopeType newType)
    {
        this.ScopeType = newType;
    }

    // GlobalsSet: Globals that really came from the outside. Think functions and reserved named stuff.
    public HashSet<string> GlobalsSet;

    public CodeNamesNode()
    {
        GlobalsSet = new HashSet<string>();
        NamedScopes = new Dictionary<string, NameScope>();
        Children = new Dictionary<string, CodeNamesNode>();
        SetScopeType(ScopeType.NotClass);
    }

    // Alternate version that takes a list of variables from the outside to treat like globals.
    public CodeNamesNode(IEnumerable<string> externalGlobals) : this()
    {
        foreach(var global in externalGlobals)
        {
            GlobalsSet.Add(global);
        }
    }

    public CodeNamesNode(HashSet<string> globalsSet)
    {
        GlobalsSet = globalsSet;
        NamedScopes = new Dictionary<string, NameScope>();
        Children = new Dictionary<string, CodeNamesNode>();
        SetScopeType(ScopeType.NotClass);
    }

    // TODO: I think this actually has to keep going up and up and up.
    private void updateScope(string name, NameScope newScope)
    {
        if(NamedScopes.ContainsKey(name))
        {
            NamedScopes[name] = newScope;
        }

        foreach(var child in Children.Values)
        { 
            child.updateScope(name, newScope);
        }
    }

    private NameScope selectBroadestScope(NameScope a, NameScope b)
    {
        if(a > b)
        {
            return a;
        }
        else
        {
            return b;
        }
    }

    private bool foundUpstream(string name)
    {
        var parentCursor = Parent;
        while(parentCursor != null && parentCursor.ScopeType != ScopeType.Class)
        {
            if (parentCursor.NamedScopes.ContainsKey(name))
            {
                return true;
            }
            parentCursor = Parent.Parent;
        }
        return false;
    }

    public void AddName(string name, ParserRuleContext context)
    {
        AddName(name, NameScope.Local, context);
    }

    public void AddName(string name, NameScope scope, ParserRuleContext context)
    {
        // I think this can be optimized to stop when a local scope becomes enclosing or global, but I
        // need to see how things proceed with them before I go all-in on the optimization. I'm running on
        // a notion that we don't have tons and tons and tons of functions inside each other but who knows.
        var selectedScope = scope;

        // Root is global and overrides whatever we think it currently is.
        if(Parent == null)
        {
            selectedScope = NameScope.Global;
        }

        // Look upstream for locals when:
        // 1. We're not in a function. For functions, we use the FAST optimizations and it's assumed 
        //    local.
        // 2. There is a global matching the name.
        if(scope == NameScope.Local && this.ScopeType != ScopeType.Function && GlobalsSet.Contains(name))
        {
            selectedScope = NameScope.Global;
        }

        // Check that a nonlocal declaration actually resolves upstream.
        // Because this is illegal:
        // class A:
        //     v = 1
        //     class B :
        //         nonlocal v           // <--- dead right here.
        if (scope == NameScope.EnclosedRead || scope == NameScope.EnclosedReadWrite &&
            !foundUpstream(name))
        {
            throw new UnboundNonlocalException(name, context);
        }

        if (!NamedScopes.ContainsKey(name))
        {
            NamedScopes.Add(name, selectedScope);
        }
        else
        {
            selectedScope = selectBroadestScope(scope, NamedScopes[name]);
            NamedScopes[name] = selectedScope;
        }

        if(selectedScope == NameScope.Global)
        {
            GlobalsSet.Add(name);
        }

        CodeNamesNode lastFoundAbove = this;
        NameScope aboveScope = NameScope.Undefined;
        for (CodeNamesNode itr = Parent; itr != null; itr = itr.Parent)
        {
            if (itr.NamedScopes.ContainsKey(name))
            {
                lastFoundAbove = itr;
                aboveScope = itr.NamedScopes[name];
            }
        }

        if (lastFoundAbove != this)
        {
            if(selectedScope == NameScope.Local && aboveScope != NameScope.Global)
            {
                selectedScope = NameScope.EnclosedRead;
            }

            // Global doesn't propagate "up" so much as it propagates globally.
            // A higher level usage of the same name will just be a local unless
            // either at root scope or marked as global with the global keyword
            // explicitly.
            if(selectedScope != NameScope.Global && aboveScope != NameScope.Global)
            {
                lastFoundAbove.updateScope(name, selectedScope);
            }
        }
    }

    public string ToReportString(int indent=0)
    {
        var b = new StringBuilder();
        var keys = NamedScopes.Keys.ToList();
        keys.Sort();

        // We're not using AppendLine because it produces a classic Windows
        // \r\n and I don't want to have to put both in my assertions because
        // it looks like garbage.
        foreach(var key in keys)
        {
            b.Append(new string(' ', indent));
            b.Append(key);
            b.Append(": ");
            b.Append(NamedScopes[key].ToString());
            b.Append("\n");
        }

        keys = Children.Keys.ToList();
        keys.Sort();
        foreach(var key in keys)
        {
            b.Append(new string(' ', indent));
            b.Append(key);
            b.Append(":\n");
            b.Append(Children[key].ToReportString(indent + 2));
        }

        return b.ToString();
    }
}

/// <summary>
/// Data structure for managing node names across layers of code. This is used to tell in the code generation pass
/// whether or not to treat a variable as global, enclosed, or local.
/// </summary>
public class NewCodeNamesNode
{
    public NewCodeNamesNode Parent;
    public Dictionary<string, NameScope> NamedScopes;
    public Dictionary<string, NewCodeNamesNode> Children;

    public ScopeType ScopeType { get; protected set; }
    public void SetScopeType(ScopeType newType)
    {
        this.ScopeType = newType;
    }

    // GlobalsSet: Specifically for globals
    public HashSet<string> GlobalsSet;
    public HashSet<string> BuiltinsSet;

    public NewCodeNamesNode()
    {
        GlobalsSet = new HashSet<string>();
        BuiltinsSet = new HashSet<string>();
        NamedScopes = new Dictionary<string, NameScope>();
        Children = new Dictionary<string, NewCodeNamesNode>();
        SetScopeType(ScopeType.NotClass);
    }

    // Alternate version that takes a list of variables from the outside to treat like globals.
    public NewCodeNamesNode(IEnumerable<string> externalGlobals, IEnumerable<string> externalBuiltins) : this()
    {
        foreach (var global in externalGlobals)
        {
            GlobalsSet.Add(global);
        }

        foreach(var builtin in externalBuiltins)
        {
            BuiltinsSet.Add(builtin);
        }
    }

    public NewCodeNamesNode(HashSet<string> globalsSet, HashSet<string> builtinsSet)
    {
        GlobalsSet = globalsSet;
        BuiltinsSet = builtinsSet;
        NamedScopes = new Dictionary<string, NameScope>();
        Children = new Dictionary<string, NewCodeNamesNode>();
        SetScopeType(ScopeType.NotClass);
    }

    // TODO: I think this actually has to keep going up and up and up.
    private void updateScope(string name, NameScope newScope)
    {
        if (NamedScopes.ContainsKey(name))
        {
            NamedScopes[name] = newScope;
        }

        foreach (var child in Children.Values)
        {
            child.updateScope(name, newScope);
        }
    }

    private NameScope selectBroadestScope(NameScope a, NameScope b)
    {
        if (a > b)
        {
            return a;
        }
        else
        {
            return b;
        }
    }

    private bool foundUpstream(string name)
    {
        var parentCursor = Parent;
        while (parentCursor != null && parentCursor.ScopeType != ScopeType.Class)
        {
            if (parentCursor.NamedScopes.ContainsKey(name))
            {
                return true;
            }
            parentCursor = Parent.Parent;
        }
        return false;
    }

    public void AddName(string name, ParserRuleContext context)
    {
        AddName(name, NameScope.Local, context);
    }

    public void AddName(string name, NameScope scope, ParserRuleContext context)
    {
        // I think this can be optimized to stop when a local scope becomes enclosing or global, but I
        // need to see how things proceed with them before I go all-in on the optimization. I'm running on
        // a notion that we don't have tons and tons and tons of functions inside each other but who knows.
        var selectedScope = scope;

        // Root is global and overrides whatever we think it currently is.
        if (Parent == null)
        {
            selectedScope = NameScope.Global;
        }

        // Look upstream for locals when:
        // 1. We're not in a function. For functions, we use the FAST optimizations and it's assumed 
        //    local.
        // 2. There is a global matching the name.
        if (scope == NameScope.Local && this.ScopeType != ScopeType.Function && GlobalsSet.Contains(name))
        {
            selectedScope = NameScope.Global;
        }

        // Check that a nonlocal declaration actually resolves upstream.
        // Because this is illegal:
        // class A:
        //     v = 1
        //     class B :
        //         nonlocal v           // <--- dead right here.
        if (scope == NameScope.EnclosedRead || scope == NameScope.EnclosedReadWrite &&
            !foundUpstream(name))
        {
            throw new UnboundNonlocalException(name, context);
        }

        if (!NamedScopes.ContainsKey(name))
        {
            NamedScopes.Add(name, selectedScope);
        }
        else
        {
            selectedScope = selectBroadestScope(scope, NamedScopes[name]);
            NamedScopes[name] = selectedScope;
        }

        if (selectedScope == NameScope.Global)
        {
            GlobalsSet.Add(name);
        }

        NewCodeNamesNode lastFoundAbove = this;
        NameScope aboveScope = NameScope.Undefined;
        for (NewCodeNamesNode itr = Parent; itr != null; itr = itr.Parent)
        {
            if (itr.NamedScopes.ContainsKey(name))
            {
                lastFoundAbove = itr;
                aboveScope = itr.NamedScopes[name];
            }
        }

        if (lastFoundAbove != this)
        {
            if (selectedScope == NameScope.Local && aboveScope != NameScope.Global)
            {
                selectedScope = NameScope.EnclosedRead;
            }

            // Global doesn't propagate "up" so much as it propagates globally.
            // A higher level usage of the same name will just be a local unless
            // either at root scope or marked as global with the global keyword
            // explicitly.
            if (selectedScope != NameScope.Global && aboveScope != NameScope.Global)
            {
                lastFoundAbove.updateScope(name, selectedScope);
            }
        }
    }

    private void assign_LEGB(string name, NameScope scope)
    {

    }

    private NameScope resolve_LEGB(string name)
    {
        // Not found, start lookup upstairs in order: enclosing, global, built-in.
        for (var cursor = this; Parent != null; cursor = cursor.Parent)
        {
            if (cursor.NamedScopes.ContainsKey(name))
            {
                return NamedScopes[name];
            }
        }
        return NameScope.Local;
    }

    public string ToReportString(int indent = 0)
    {
        var b = new StringBuilder();
        var keys = NamedScopes.Keys.ToList();
        keys.Sort();

        // We're not using AppendLine because it produces a classic Windows
        // \r\n and I don't want to have to put both in my assertions because
        // it looks like garbage.
        foreach (var key in keys)
        {
            b.Append(new string(' ', indent));
            b.Append(key);
            b.Append(": ");
            b.Append(NamedScopes[key].ToString());
            b.Append("\n");
        }

        keys = Children.Keys.ToList();
        keys.Sort();
        foreach (var key in keys)
        {
            b.Append(new string(' ', indent));
            b.Append(key);
            b.Append(":\n");
            b.Append(Children[key].ToReportString(indent + 2));
        }

        return b.ToString();
    }
}



public class VariableScanVisitor : CloacaBaseVisitor<object>
{
    private CodeNamesNode rootNode;
    private CodeNamesNode currentNode;
    public string failureMessage;

    public CodeNamesNode RootNode
    {
        get
        {
            return rootNode;
        }
    }

    private CodeNamesNode descendFromName(string new_name)
    {
        var newNode = new CodeNamesNode(rootNode.GlobalsSet);

        // The same function can be defined more than once.
        // TODO: What happens if the involved function had created a nonlocal or something?
        if(currentNode.Children.ContainsKey(new_name))
        {
            currentNode.Children.Remove(new_name);
        }
        currentNode.Children.Add(new_name, newNode);
        newNode.Parent = currentNode;
        currentNode = newNode;
        return currentNode;
    }

    private CodeNamesNode ascendNameNode()
    {
        currentNode = currentNode.Parent;
        return currentNode;
    }

    public VariableScanVisitor(IEnumerable<string> names)
    {
        rootNode = new CodeNamesNode(names);
        currentNode = rootNode;
    }

    // Will visit the variable scan visitor, but has a guard around the
    // main visit call for unbound variables and other syntax errors. These will
    // be caught and modified.
    public void TryVisit([NotNull] IParseTree tree)
    {
        try
        {
            failureMessage = null;
            this.Visit(tree);
        } catch(VariableScanSyntaxException error)
        {
            failureMessage = error.Message;
        }
    }

    public override object VisitExpr_stmt([NotNull] CloacaParser.Expr_stmtContext context)
    {
        if (context.testlist_star_expr().Length == 1)
        {
            if(context.testlist() != null)
            {
                Visit(context.testlist());
            }
            Visit(context.testlist_star_expr(0));
        }
        else
        {
            Visit(context.testlist_star_expr(context.testlist_star_expr().Length - 1));
            for (int lvalue_i = context.testlist_star_expr().Length - 2; lvalue_i >= 0; --lvalue_i)
            {
                for (int test_i = 0; test_i < context.testlist_star_expr(lvalue_i).test().Length; ++test_i)
                {
                    VisitLValueTestlist_star_expr(context.testlist_star_expr(lvalue_i).test()[test_i]);
                }
            }
        }
        return null;
    }

    public object VisitLValueTestlist_star_expr([NotNull] CloacaParser.TestContext context)
    {
        var expr = context.or_test()[0].and_test()[0].not_test()[0].comparison().expr()[0];

        var variableName = expr.GetText();

        // Kind of hamfisted, but anything that's being subscripted (self.a.b.something_else)
        // only needs the first part. That's the variable name. We can blow off anything else.
        int firstDot = variableName.IndexOf('.');
        if (firstDot >= 0) {
            variableName = variableName.Substring(0, firstDot);
        } 

        currentNode.AddName(variableName, context);
        return null;
    }

    public override object VisitAtom_expr([NotNull] CloacaParser.Atom_exprContext context)
    {
        var trailers = context.trailer();
        if(trailers.Length > 0)
        {
            Visit(context.atom());
            for (int trailer_i = 0; trailer_i < context.trailer().Length; ++trailer_i)
            {
                var trailer = context.trailer(trailer_i);
                if (trailer.NAME() != null)
                {
                    var attrName = trailer.NAME().GetText();
                    currentNode.AddName(attrName, context);
                }

                else if (trailer.arglist() != null || trailer.GetText() == "()")
                {
                    int argIdx = 0;
                    for (argIdx = 0; trailer.arglist() != null &&
                        trailer.arglist().argument(argIdx) != null; ++argIdx)
                    {
                        if (trailer.arglist().argument(argIdx).test().Length > 1)
                        {
                            base.Visit(trailer.arglist().argument(argIdx).test(1));
                        }
                        else
                        {
                            base.Visit(trailer.arglist().argument(argIdx));
                        }
                    }
                }
                else
                {
                    base.Visit(trailer);
                }
            }
            return null;
        }
        else
        {
            return base.VisitAtom_expr(context);
        }
    }

    public override object VisitAtomSquareBrackets([NotNull] CloacaParser.AtomSquareBracketsContext context)
    {
        return base.VisitAtomSquareBrackets(context);
    }

    public override object VisitTrailer([NotNull] CloacaParser.TrailerContext context)
    {
        return base.VisitTrailer(context);
    }

    public override object VisitAtomName([NotNull] CloacaParser.AtomNameContext context)
    {
        var variableName = context.GetText();
        currentNode.AddName(variableName, context);
        return null;
    }

    public override object VisitTfpdef([NotNull] CloacaParser.TfpdefContext context)
    {
        var variableName = context.GetText();
        currentNode.AddName(variableName, context);
        return null;
    }

    public override object VisitFuncdef([NotNull] CloacaParser.FuncdefContext context)
    {
        var newNode = descendFromName(context.NAME().GetText());
        newNode.SetScopeType(ScopeType.Function);
        base.Visit(context.parameters());
        base.VisitSuite(context.suite());
        ascendNameNode();
        return null;
    }

    public override object VisitClassdef([NotNull] CloacaParser.ClassdefContext context)
    {
        var newNode = descendFromName(context.NAME().GetText());
        newNode.SetScopeType(ScopeType.Class);
        base.VisitSuite(context.suite());
        ascendNameNode();
        return null;
    }

    public override object VisitNonlocal_stmt([NotNull] CloacaParser.Nonlocal_stmtContext context)
    {
        foreach(var variableName in context.NAME())
        {
            currentNode.AddName(variableName.GetText(), NameScope.EnclosedReadWrite, context);
        }
        return null;
    }

    public override object VisitGlobal_stmt([NotNull] CloacaParser.Global_stmtContext context)
    {
        foreach (var variableName in context.NAME())
        {
            currentNode.AddName(variableName.GetText(), NameScope.Global, context);
        }
        return null;
    }

    public override object VisitExcept_clause([NotNull] CloacaParser.Except_clauseContext context)
    {
        base.VisitExcept_clause(context);
        if(context.NAME() != null)
        {
            currentNode.AddName(context.NAME().GetText(), context);
        }
        return null;
    }

    public override object VisitDotted_as_name([NotNull] CloacaParser.Dotted_as_nameContext context)
    {
        base.VisitDotted_as_name(context);
        if (context.NAME() != null)
        {
            currentNode.AddName(context.NAME().GetText(), context);
        }
        return null;
    }

    public override object VisitImport_as_name([NotNull] CloacaParser.Import_as_nameContext context)
    {
        base.VisitImport_as_name(context);
        if (context.NAME() != null && context.NAME().Length > 0)
        {
            currentNode.AddName(context.NAME()[0].GetText(), context);
        }
        return null;
    }

}
