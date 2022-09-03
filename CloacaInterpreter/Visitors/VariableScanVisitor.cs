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
public class CodeNamesNode
{
    public CodeNamesNode Parent;
    public Dictionary<string, NameScope> NamedScopesRead;
    public Dictionary<string, NameScope> NamedScopesWrite;
    public Dictionary<string, CodeNamesNode> Children;

    public ScopeType ScopeType { get; protected set; }
    public void SetScopeType(ScopeType newType)
    {
        this.ScopeType = newType;
    }

    // GlobalsSet: Specifically for globals
    public HashSet<string> GlobalsSet;
    public HashSet<string> BuiltinsSet;

    public CodeNamesNode()
    {
        GlobalsSet = new HashSet<string>();
        BuiltinsSet = new HashSet<string>();
        NamedScopesRead = new Dictionary<string, NameScope>();
        NamedScopesWrite = new Dictionary<string, NameScope>();
        Children = new Dictionary<string, CodeNamesNode>();
        SetScopeType(ScopeType.NotClass);
    }

    // Alternate version that takes a list of variables from the outside to treat like globals.
    public CodeNamesNode(IEnumerable<string> externalGlobals, IEnumerable<string> externalBuiltins) : this()
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

    public CodeNamesNode(HashSet<string> globalsSet, HashSet<string> builtinsSet)
    {
        // Copy globals but pass builtins as a reference. Builtins are read-only. Children
        // can have different globals from the root global! Ugly!
        GlobalsSet = new HashSet<string>(globalsSet);       
        BuiltinsSet = builtinsSet;
        NamedScopesRead = new Dictionary<string, NameScope>();
        NamedScopesWrite = new Dictionary<string, NameScope>();
        Children = new Dictionary<string, CodeNamesNode>();
        SetScopeType(ScopeType.NotClass);
    }

    private bool canBePromoted(NameScope scope)
    {
        return scope == NameScope.LocalFast ||
            scope == NameScope.Name ||
            scope == NameScope.Enclosed;
    }

    // When assigned, both read and write are assigned.
    public void AssignScope(string name, NameScope nameScope, ParserRuleContext context)
    {
        AssignWriteScope(name, nameScope, context);
        AssignReadScope(name, nameScope, context);

        // "Promote" enclosed variables. The origin needs to flap to enclosed.
        if(nameScope == NameScope.Enclosed)
        {
            bool found = false;
            for(var cursor = this.Parent; cursor != null && cursor.Parent != null && !found; cursor = cursor.Parent)
            {
                if(cursor.NamedScopesWrite.ContainsKey(name) && canBePromoted(cursor.NamedScopesWrite[name]))
                {
                    found = true;
                    cursor.NamedScopesWrite[name] = NameScope.Enclosed;
                }
                if (cursor.NamedScopesRead.ContainsKey(name) && canBePromoted(cursor.NamedScopesRead[name]))
                {
                    found = true;
                    cursor.NamedScopesRead[name] = NameScope.Enclosed;
                }
            }
            if(!found)
            {
                throw new UnboundNonlocalException(name, context);
            }
        }
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

    public void NoteWrittenName(string name, ParserRuleContext context)
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

    public void NoteReadName(string name, ParserRuleContext context)
    {
        // We'll keep this here because if there's a place to debug, it's usually here.
        // That should be a sign.
        //if(name == "a" && Parent != null && Parent.Children.ContainsKey("SomeClass"))
        //{
        //    int b = 3; // DEBUG BREAKPOINT
        //}

        bool startedWithFunction = this.ScopeType == ScopeType.Function;
        if(NamedScopesRead.ContainsKey(name))
        {
            return;
        }
        else
        {
            // Time to find it upstairs. It's a goofy loop because we need to scrape off
            // the root node for final checks at the root level for globals and built-ins.
            CodeNamesNode cursor = this;
            CodeNamesNode cursorRoot = null;
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

                    // Goofy little, highly technical adjustment for LEGB resolution. If we just hit a global
                    // then we should use LEGB ordering c/o NAME instead of GLOBAL
                    //
                    // Think:
                    // a = 101
                    // class SomeClass:
                    //    a += 1
                    //
                    // SomeClass.a should be resolved as NAME instead of GLOBAL to be consistent with CPython.
                    // I don't know how much it matters though.
                    if(NamedScopesRead[name] == NameScope.Global && (!NamedScopesWrite.ContainsKey(name) || NamedScopesWrite[name] != NameScope.Global))
                    {
                        NamedScopesRead[name] = NameScope.Name;
                    }
                    return;
                }

                cursor = cursor.Parent;
            } while (cursor != null);

            // Fallback: Is it a global or built-in?
            if(cursorRoot.GlobalsSet.Contains(name)) {
                NamedScopesRead[name] = NameScope.Global;
            }
            else if (cursorRoot.BuiltinsSet.Contains(name))
            {
                NamedScopesRead[name] = NameScope.Builtin;
            }
            else
            {
                // Fallbacks of fallbacks: Set it up as a name and see at run time if it resolves.
                // It actually might! Module level variables and crap like that will get dumped in when we run.
                // We used to just throw UnboundLocalException here but we were failing tests with module
                // variables like __name__ because of it.
                NamedScopesRead[name] = NameScope.Name;
            }
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

/// <summary>
/// A first-pass vistor for parsing Python code in order to build up the scope of the different
/// variables in the program. We switched to a two-stage parser after trying to deal with the
/// nonlocal keyword in particular. If you were parsing nonlocals in a single pass, you would
/// find yourself in one layer making a variable a fast local, and then having to rewind it to
/// a cell variable. This would mean changing opcodes.
/// 
/// We didn't do that. Instead, a first pass creates a mapping of variables and their scopes, which
/// is then consulted by the code generator to determine how to generate load and stores for them.
/// At the point the second-stage, byte code parser runs, it will know exactly what opcode to use.
/// 
/// The map starts in the RootNode and is a CodeNamesNode.
/// 
/// This class is aggressively tested in the VariableScanVisitorTests. If something is failing in
/// scope resolution, you will want to try a test in there to see if the variable scopes are
/// even correct.
/// </summary>
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

    private CodeNamesNode descendNew(string new_name, ParserRuleContext context)
    {
        var newNode = new CodeNamesNode(rootNode.GlobalsSet, rootNode.GlobalsSet);

        // The same function can be defined more than once.
        // TODO: What happens if the involved function had created a nonlocal or something?
        if(currentNode.Children.ContainsKey(new_name))
        {
            currentNode.Children.Remove(new_name);
        }

        // For consistency with CPython, we'll assign Namescope at the root level but
        // then let it run amok for lower levels.
        if(currentNode.Parent == null)
        {
            currentNode.AssignScope(new_name, NameScope.Name, context);
        }
        else
        {
            currentNode.NoteWrittenName(new_name, context);
            currentNode.NoteReadName(new_name, context);
        }

        currentNode.Children.Add(new_name, newNode);
        newNode.Parent = currentNode;
        currentNode = newNode;
        return currentNode;
    }

    private CodeNamesNode ascend()
    {
        currentNode = currentNode.Parent;
        return currentNode;
    }

    public VariableScanVisitor(IEnumerable<string> globalNames, IEnumerable<string> builtinNames)
    {
        rootNode = new CodeNamesNode(globalNames, builtinNames);
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
            if(context.augassign() != null)
            {
                // Inplace operators need to note first a read, and then write for lvalue:
                Visit(context.testlist());
                VisitLValueTestlist_star_expr(context.testlist_star_expr()[0].test()[0]);
            }
            else if (context.testlist() != null)
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
            currentNode.NoteReadName(variableName, context);
        } 
        else
        {
            currentNode.NoteWrittenName(variableName, context);
        }

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

                if(trailer.GetText()[0] == '.')
                {
                    // This is an attribute dereference and everything following the dot is
                    // just using LOAD/STORE_ATTR. We don't care about them. However, trailers
                    // can also be function calls and subscripts and we might care about what's
                    // going on with them.
                    continue;
                }

                if (trailer.NAME() != null)
                {
                    var attrName = trailer.NAME().GetText();
                    currentNode.NoteReadName(attrName, context);
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
        currentNode.NoteReadName(variableName, context);
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
        var newNode = descendNew(context.NAME().GetText(), context);
        newNode.SetScopeType(ScopeType.Function);
        base.Visit(context.parameters());
        base.VisitSuite(context.suite());
        ascend();
        return null;
    }

    public override object VisitTypedargslist([NotNull] CloacaParser.TypedargslistContext context)
    {
        // Gotta pluck out defaults because the names created from them are crazy.
        // function foo's bar becomes foo_$Default_bar
        for (int child_i = 0; child_i < context.children.Count; ++child_i)
        {
            // What's my name?
            var funcName = currentNode.Parent.Children.FirstOrDefault(x => x.Value == currentNode).Key;
            Visit(context.children[child_i]);

            if (context.children[child_i].GetText() == "=")
            {
                descendNew(funcName + "_$Default_" + context.children[child_i + 1].GetText(), context);
                Visit(context.children[child_i + 1]);
                currentNode = ascend();
            }
            else
            {
                Visit(context.children[child_i]);
            }
        }
        return null;
    }

    public override object VisitClassdef([NotNull] CloacaParser.ClassdefContext context)
    {
        // Make sure to visit the subclass names if they exist.
        if (context.arglist() != null)
        {
            Visit(context.arglist());
        }

        var newNode = descendNew(context.NAME().GetText(), context);
        newNode.SetScopeType(ScopeType.Class);

        base.VisitSuite(context.suite());
        ascend();
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

    public override object VisitFor_stmt([NotNull] CloacaParser.For_stmtContext context)
    {
        Visit(context.testlist());
        foreach (var expr in context.exprlist().expr())
        {
            currentNode.NoteWrittenName(expr.GetText(), context);
        }
        Visit(context.suite(0));
        if (context.suite().Length > 1)
        {
            Visit(context.suite(1));
        }
        return null;
    }

    public override object VisitExcept_clause([NotNull] CloacaParser.Except_clauseContext context)
    {
        base.VisitExcept_clause(context);
        if(context.NAME() != null)
        {
            currentNode.NoteWrittenName(context.NAME().GetText(), context);
        }
        return null;
    }

    public override object VisitDotted_as_name([NotNull] CloacaParser.Dotted_as_nameContext context)
    {
        base.VisitDotted_as_name(context);
        if (context.NAME() != null)
        {
            currentNode.NoteReadName(context.NAME().GetText(), context);
        }
        return null;
    }

    public override object VisitDotted_name([NotNull] CloacaParser.Dotted_nameContext context)
    {
        currentNode.NoteReadName(context.NAME(0).GetText(), context);
        return null;
    }

    public override object VisitImport_as_name([NotNull] CloacaParser.Import_as_nameContext context)
    {
        base.VisitImport_as_name(context);
        if (context.NAME() != null && context.NAME().Length > 0)
        {
            currentNode.NoteReadName(context.NAME()[0].GetText(), context);
        }
        return null;
    }

}
