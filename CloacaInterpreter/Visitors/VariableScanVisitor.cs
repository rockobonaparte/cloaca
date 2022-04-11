using System.Collections.Generic;
using System.Linq;
using System.Text;

using Antlr4.Runtime.Misc;

using Language;

public enum NameScope
{
    Local,
    EnclosedRead,
    EnclosedReadWrite,
    Global,
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
    public HashSet<string> GlobalsSet;

    public CodeNamesNode()
    {
        GlobalsSet = new HashSet<string>();
        NamedScopes = new Dictionary<string, NameScope>();
        Children = new Dictionary<string, CodeNamesNode>();
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
    }

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

    public void AddName(string name)
    {
        AddName(name, NameScope.Local);
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

    public void AddName(string name, NameScope scope)
    {
        // I think this can be optimized to stop when a local scope becomes enclosing or global, but I
        // need to see how things proceed with them before I go all-in on the optimization. I'm running on
        // a notion that we don't have tons and tons and tons of functions inside each other but who knows.
        var selectedScope = scope;
        if(GlobalsSet.Contains(name))
        {
            selectedScope = NameScope.Global;
        } 
        else if(scope == NameScope.Global)
        {
            GlobalsSet.Add(name);
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

        CodeNamesNode lastFoundAbove = this;
        for (CodeNamesNode itr = Parent; itr != null; itr = itr.Parent)
        {
            if (itr.NamedScopes.ContainsKey(name))
            {
                lastFoundAbove = itr;
            }
        }

        if (lastFoundAbove != this)
        {
            if(selectedScope == NameScope.Local)
            {
                selectedScope = NameScope.EnclosedRead;
            }
            lastFoundAbove.updateScope(name, selectedScope);
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

public class VariableScanVisitor : CloacaBaseVisitor<object>
{
    private CodeNamesNode rootNode;
    private CodeNamesNode currentNode;

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

    public override object VisitExpr_stmt([NotNull] CloacaParser.Expr_stmtContext context)
    {
        VisitLValueTestlist_star_expr(context.testlist_star_expr()[0].test()[0]);
        return null;
    }

    public object VisitLValueTestlist_star_expr([NotNull] CloacaParser.TestContext context)
    {
        var variableName = context.or_test()[0].and_test()[0].not_test()[0].comparison().expr()[0].GetText();
        currentNode.AddName(variableName);
        return null;
    }

    public override object VisitAtom_expr([NotNull] CloacaParser.Atom_exprContext context)
    {
        if(context.trailer().Length > 0)
        {
            return null;
        }
        else
        {
            return base.VisitAtom_expr(context);
        }
    }

    public override object VisitAtomName([NotNull] CloacaParser.AtomNameContext context)
    {
        var variableName = context.GetText();
        currentNode.AddName(variableName);
        return null;
    }

    public override object VisitTfpdef([NotNull] CloacaParser.TfpdefContext context)
    {
        var variableName = context.GetText();
        currentNode.AddName(variableName);
        return null;
    }

    public override object VisitFuncdef([NotNull] CloacaParser.FuncdefContext context)
    {
        descendFromName(context.NAME().GetText());
        base.Visit(context.parameters());
        base.VisitSuite(context.suite());
        ascendNameNode();
        return null;
    }

    public override object VisitNonlocal_stmt([NotNull] CloacaParser.Nonlocal_stmtContext context)
    {
        foreach(var variableName in context.NAME())
        {
            currentNode.AddName(variableName.GetText(), NameScope.EnclosedReadWrite);
        }
        return null;
    }

    public override object VisitExcept_clause([NotNull] CloacaParser.Except_clauseContext context)
    {
        base.VisitExcept_clause(context);
        if(context.NAME() != null)
        {
            currentNode.AddName(context.NAME().GetText());
        }
        return null;
    }

    public override object VisitDotted_as_name([NotNull] CloacaParser.Dotted_as_nameContext context)
    {
        base.VisitDotted_as_name(context);
        if (context.NAME() != null)
        {
            currentNode.AddName(context.NAME().GetText());
        }
        return null;
    }

    public override object VisitImport_as_name([NotNull] CloacaParser.Import_as_nameContext context)
    {
        base.VisitImport_as_name(context);
        if (context.NAME() != null && context.NAME().Length > 0)
        {
            currentNode.AddName(context.NAME()[0].GetText());
        }
        return null;
    }
}
