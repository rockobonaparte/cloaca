using System.Collections.Generic;

using Antlr4.Runtime.Misc;

using Language;

public enum NameScope
{
    Global,
    Local,
    Enclosed,
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

    public CodeNamesNode()
    {
        NamedScopes = new Dictionary<string, NameScope>();
        Children = new Dictionary<string, CodeNamesNode>();
    }

    // Alternate version that takes a list of variables from the outside to treat like globals.
    public CodeNamesNode(IEnumerable<string> externalGlobals) : this()
    {
        foreach(var global in externalGlobals)
        {
            NamedScopes.Add(global, NameScope.Global);
        }
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
        currentNode.NamedScopes.Add(variableName, NameScope.Local);
        return null;
    }
}
