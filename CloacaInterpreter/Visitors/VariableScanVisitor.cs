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
    Enclosed,
    Global,
    Builtin,
    Name,
    LocalFast,
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

public class UnboundLocalException : VariableScanSyntaxException
{
    public UnboundLocalException(string name, ParserRuleContext context) :
        base("line " + context.Start.Line + ": local variable '" + name + "' referenced before assignment", context.Start.Line)
    {

    }
}

public class UnboundVariableException : VariableScanSyntaxException
{
    public UnboundVariableException(string name, ParserRuleContext context) :
        base("line " + context.Start.Line + ": no binding for variable '" + name + "' found", context.Start.Line)
    {

    }
}

public class ConflictingBindingException : VariableScanSyntaxException
{
    public ConflictingBindingException(string name, NameScope first, NameScope second, ParserRuleContext context) :
        base("line " + context.Start.Line + ": name '" + name + "' is " + first + " and " + second, context.Start.Line)
    {

    }

}

/// <summary>
/// Data structure for managing node names across layers of code. This is used to tell in the code generation pass
/// whether or not to treat a variable as global, enclosed, or local.
/// </summary>
public class NewCodeNamesNode
{
    public NewCodeNamesNode Parent;
    public Dictionary<string, NameScope> NamedScopesRead;
    public Dictionary<string, NameScope> NamedScopesWrite;
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
        NamedScopesRead = new Dictionary<string, NameScope>();
        NamedScopesWrite = new Dictionary<string, NameScope>();
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
        // Copy globals but pass builtins as a reference. Builtins are read-only. Children
        // can have different globals from the root global! Ugly!
        GlobalsSet = new HashSet<string>(globalsSet);       
        BuiltinsSet = builtinsSet;
        NamedScopesRead = new Dictionary<string, NameScope>();
        NamedScopesWrite = new Dictionary<string, NameScope>();
        Children = new Dictionary<string, NewCodeNamesNode>();
        SetScopeType(ScopeType.NotClass);
    }

    // When assigned, both read and write are assigned.
    public void AssignScope(string name, NameScope nameScope, ParserRuleContext context)
    {
        AssignWriteScope(name, nameScope, context);
        AssignReadScope(name, nameScope, context);
    }

    public void AssignReadScope(string name, NameScope nameScope, ParserRuleContext context)
    {
        if (NamedScopesRead.ContainsKey(name) && NamedScopesRead[name] != nameScope)
        {
            throw new VariableScanSyntaxException("Cannot assign '" + name + "' to " +
                nameScope + ": Reads are already" + NamedScopesRead[name], context.Start.Line);
        }

        if (!NamedScopesRead.ContainsKey(name))
        {
            NamedScopesRead.Add(name, nameScope);
        }

    }

    public void AssignWriteScope(string name, NameScope nameScope, ParserRuleContext context)
    {
        if (NamedScopesWrite.ContainsKey(name) && NamedScopesWrite[name] != nameScope)
        {
            throw new VariableScanSyntaxException("Cannot assign '" + name + "' to " +
                nameScope + ": Writes are already" + NamedScopesWrite[name], context.Start.Line);
        }

        if (!NamedScopesWrite.ContainsKey(name))
        {
            NamedScopesWrite.Add(name, nameScope);
            if(nameScope == NameScope.Global)
            {
                GlobalsSet.Add(name);
            }
        }
    }

    public void noteWrittenName(string name, ParserRuleContext context)
    {
        if(!NamedScopesWrite.ContainsKey(name))
        {
            // CPython likes to use NAME at global level but we'll call a spade a spade
            // because it's easier to properly resolve globals that percolate down if we
            // call them globals correctly in the first place.
            if (Parent != null)
            {
                if(this.ScopeType == ScopeType.Function)
                {
                    NamedScopesWrite.Add(name, NameScope.LocalFast);
                }
                else
                {
                    NamedScopesWrite.Add(name, NameScope.Name);
                }
            }
            else
            {
                NamedScopesWrite.Add(name, NameScope.Global);
            }
        }
    }

    public void noteReadName(string name, ParserRuleContext context)
    {
        //if(Parent != null && Parent.Children.ContainsKey("__init__") && name == "a")
        //{
        //    int b = 3; // DEBUG BREAKPOINT
        //}

        if (Parent != null && Parent.Children.ContainsKey("inner") && name == "a")
        {
            int b = 3; // DEBUG BREAKPOINT
        }

        bool startedWithFunction = this.ScopeType == ScopeType.Function;
        if(NamedScopesRead.ContainsKey(name))
        {
            return;
        }
        else
        {
            // Time to find it upstairs. It's a goofy loop because we need to scrape off
            // the root node for final checks at the root level for globals and built-ins.
            NewCodeNamesNode cursor = this;
            NewCodeNamesNode cursorRoot = null;
            do
            {
                if (cursorRoot == null && cursor.Parent == null)
                {
                    cursorRoot = cursor;
                }

                // Functions specifically skip class scopes when resolving variables.
                if (startedWithFunction && cursor.ScopeType == ScopeType.Class)
                {
                    // Don't forget to advance the cursor even though we're about to skip.
                    cursor = cursor.Parent;
                    continue;
                }
                if (cursor.NamedScopesWrite.ContainsKey(name))
                {
                    NamedScopesRead[name] = cursor.NamedScopesWrite[name];

                    // This might now be a regular variable we have to "promote" to an enclosed variable.
                    if (NamedScopesRead[name] != NameScope.Enclosed
                        && this != cursor
                        && cursor.Parent != null)
                    {
                        NamedScopesRead[name] = cursor.NamedScopesWrite[name] = NameScope.Enclosed;
                    }
                    return;
                }

                cursor = cursor.Parent;
            } while (cursor != null);

            // Fallback: Is it a global or built-in?
            if(cursorRoot.GlobalsSet.Contains(name)) {
                NamedScopesRead[name] = NameScope.Global;
            }
            else if (cursorRoot.GlobalsSet.Contains(name))
            {
                NamedScopesRead[name] = NameScope.Builtin;
            }

            throw new UnboundLocalException(name, context);
        }
    }

    public string ToReportString(int indent = 0)
    {
        var b = new StringBuilder();
        var allKeys = new SortedSet<string>(NamedScopesRead.Keys);
        foreach(var write in NamedScopesWrite.Keys)
        {
            allKeys.Add(write);
        }

        // We're not using AppendLine because it produces a classic Windows
        // \r\n and I don't want to have to put both in my assertions because
        // it looks like garbage.
        foreach (var key in allKeys)
        {
            b.Append(new string(' ', indent));
            b.Append(key);
            b.Append(":");

            if(NamedScopesRead.ContainsKey(key))
            {
                b.Append(" ");
                b.Append(NamedScopesRead[key]);
                b.Append(" Read");
            }

            if (NamedScopesWrite.ContainsKey(key))
            {
                b.Append(" ");
                b.Append(NamedScopesWrite[key]);
                b.Append(" Write");
            }
            b.Append("\n");
        }

        var childrenKeys = Children.Keys.ToList();
        childrenKeys.Sort();
        foreach (var key in childrenKeys)
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
    private NewCodeNamesNode rootNode;
    private NewCodeNamesNode currentNode;
    public string failureMessage;

    public NewCodeNamesNode RootNode
    {
        get
        {
            return rootNode;
        }
    }

    private NewCodeNamesNode descendFromName(string new_name, ParserRuleContext context)
    {
        var newNode = new NewCodeNamesNode(rootNode.GlobalsSet, rootNode.GlobalsSet);

        // The same function can be defined more than once.
        // TODO: What happens if the involved function had created a nonlocal or something?
        if(currentNode.Children.ContainsKey(new_name))
        {
            currentNode.Children.Remove(new_name);
        }

        currentNode.AssignScope(new_name, NameScope.Name, context);

        currentNode.Children.Add(new_name, newNode);
        newNode.Parent = currentNode;
        currentNode = newNode;
        return currentNode;
    }

    private NewCodeNamesNode ascendNameNode()
    {
        currentNode = currentNode.Parent;
        return currentNode;
    }

    public VariableScanVisitor(IEnumerable<string> globalNames, IEnumerable<string> builtinNames)
    {
        rootNode = new NewCodeNamesNode(globalNames, builtinNames);
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

        currentNode.noteWrittenName(variableName, context);
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
                    currentNode.noteReadName(attrName, context);
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
        currentNode.noteReadName(variableName, context);
        return null;
    }

    public override object VisitTfpdef([NotNull] CloacaParser.TfpdefContext context)
    {
        var variableName = context.GetText();
        currentNode.AssignScope(variableName, NameScope.LocalFast, context);
        return null;
    }

    public override object VisitFuncdef([NotNull] CloacaParser.FuncdefContext context)
    {
        var newNode = descendFromName(context.NAME().GetText(), context);
        newNode.SetScopeType(ScopeType.Function);
        base.Visit(context.parameters());
        base.VisitSuite(context.suite());
        ascendNameNode();
        return null;
    }

    public override object VisitClassdef([NotNull] CloacaParser.ClassdefContext context)
    {
        var newNode = descendFromName(context.NAME().GetText(), context);
        newNode.SetScopeType(ScopeType.Class);
        base.VisitSuite(context.suite());
        ascendNameNode();
        return null;
    }

    public override object VisitNonlocal_stmt([NotNull] CloacaParser.Nonlocal_stmtContext context)
    {
        foreach(var variableName in context.NAME())
        {
            currentNode.AssignScope(variableName.GetText(), NameScope.Enclosed, context);
        }
        return null;
    }

    public override object VisitGlobal_stmt([NotNull] CloacaParser.Global_stmtContext context)
    {
        foreach (var variableName in context.NAME())
        {
            currentNode.AssignScope(variableName.GetText(), NameScope.Global, context);
        }
        return null;
    }

    public override object VisitExcept_clause([NotNull] CloacaParser.Except_clauseContext context)
    {
        base.VisitExcept_clause(context);
        if(context.NAME() != null)
        {
            currentNode.noteReadName(context.NAME().GetText(), context);
        }
        return null;
    }

    public override object VisitDotted_as_name([NotNull] CloacaParser.Dotted_as_nameContext context)
    {
        base.VisitDotted_as_name(context);
        if (context.NAME() != null)
        {
            currentNode.noteReadName(context.NAME().GetText(), context);
        }
        return null;
    }

    public override object VisitImport_as_name([NotNull] CloacaParser.Import_as_nameContext context)
    {
        base.VisitImport_as_name(context);
        if (context.NAME() != null && context.NAME().Length > 0)
        {
            currentNode.noteReadName(context.NAME()[0].GetText(), context);
        }
        return null;
    }

}
