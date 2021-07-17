using System;
using System.Collections.Generic;

using Antlr4.Runtime.Misc;
using Antlr4.Runtime;

using Language;
using LanguageImplementation;
using LanguageImplementation.DataTypes;
using CloacaInterpreter;
using System.Threading.Tasks;

/// <summary>
/// Use to raise parsing issues while we figure out a better way to do this.
/// </summary>
public class ParseException : Exception
{
    public ParseException(string message) : base(message)
    {

    }
}

/// <summary>
/// Helper class while visiting nodes that tracks loop blocks we have created. This is particularly
/// used by continue statements to figure out what it's going to end up continuing to.
/// </summary>
public class LoopBlockRecord
{
    // Integer byte code address for the instruction that starts the loop
    public int LoopOrigin
    {
        get; private set;
    }

    public LoopBlockRecord(int loopOrigin)
    {
        LoopOrigin = loopOrigin;
    }
}

public class CloacaBytecodeVisitor : CloacaBaseVisitor<object>
{
    public CodeObjectBuilder RootProgram;
    private Stack<CodeObjectBuilder> ProgramStack;
    private CodeObjectBuilder ActiveProgram;
    private Stack<LoopBlockRecord> LoopBlocks;
    private List<Func<IScheduler, Task>> postProcessActions;


    public CloacaBytecodeVisitor()
    {
        RootProgram = new CodeObjectBuilder();
        ActiveProgram = RootProgram;
        ProgramStack = new Stack<CodeObjectBuilder>();
        LoopBlocks = new Stack<LoopBlockRecord>();
        postProcessActions = new List<Func<IScheduler, Task>>();
    }

    public CloacaBytecodeVisitor(Dictionary<string, object> existingVariables) : this()
    {
        RootProgram = new CodeObjectBuilder();
        ActiveProgram = RootProgram;
        foreach (var name in existingVariables.Keys)
        {
            ActiveProgram.VarNames.Add(name);
        }
    }

    ///// <summary>
    ///// Runs post-processing steps on the byte code before committing the final code object. This is used to do things like
    ///// calculate default parameters. An interpreter instance is needed to run on these and produce the final values to be
    ///// used. Callbacks are enqueued as the visitor visits the code, and need to be run here afterwards.
    ///// </summary>
    ///// <param name="frame_context">Context from which this byte code visitor is generating code.</param>
    ///// <param name="interpreter">Interpreter instance that will execute any extra code necessary to finish off code visitation.</param>
    public async Task PostProcess(IScheduler scheduler)
    {
        foreach (var action in postProcessActions)
        {
            await action.Invoke(scheduler);
        }
        postProcessActions.Clear();
    }

    private void generateLoadForVariable(string variableName, ParserRuleContext context)
    {
        // If it's in VarNames, we use it from there. If not, 
        // we assume it's global and deal with it at run time if
        // we can't find it.
        var idx = ActiveProgram.VarNames.IndexOf(variableName);
        if (idx >= 0)
        {
            ActiveProgram.AddInstruction(ByteCodes.LOAD_FAST, idx, context);
            return;
        }

        var nameIdx = ActiveProgram.Names.IndexOf(variableName);
        if (nameIdx >= 0)
        {
            ActiveProgram.AddInstruction(ByteCodes.LOAD_GLOBAL, nameIdx, context);
            return;
        }
        else
        {
            ActiveProgram.Names.Add(variableName);
            ActiveProgram.AddInstruction(ByteCodes.LOAD_GLOBAL, ActiveProgram.Names.Count - 1, context);
            return;
        }
    }

    private void generateStoreForVariable(string variableName, ParserRuleContext context)
    {
        if (variableName == "None")
        {
            throw new Exception("SyntaxError: can't assign to keyword (tried to assign to 'None')");
        }

        var nameIdx = ActiveProgram.Names.IndexOf(variableName);
        if (nameIdx >= 0)
        {
            ActiveProgram.AddInstruction(ByteCodes.STORE_GLOBAL, nameIdx, context);
        }
        else
        {
            var idx = ActiveProgram.VarNames.AddGetIndex(variableName);
            ActiveProgram.AddInstruction(ByteCodes.STORE_FAST, idx, context);
        }
    }

    public override object VisitArith_expr([NotNull] CloacaParser.Arith_exprContext context)
    {
        // Always visit factor 0. Then pack on operations in post-order.
        // I tried to do this using an op label:
        // (op=('+'|'-'))
        // but apparently I only get one per visitor but this line could match multiple factors.
        // Assign them to lexer names breaks the context up for each field by their rate of occurrence,
        // not position. So I just have to do hard-coded string matches! Yay! =D
        Visit(context.term(0));
        for (int child_i = 2; child_i < context.children.Count; child_i += 2)
        {
            Visit(context.children[child_i]);
            string operatorTxt = context.children[child_i - 1].GetText();
            if (operatorTxt == "+")
            {
                ActiveProgram.AddInstruction(ByteCodes.BINARY_ADD, context);
            }
            else if (operatorTxt == "-")
            {
                ActiveProgram.AddInstruction(ByteCodes.BINARY_SUBTRACT, context);
            }
            else
            {
                throw new Exception("The Cloaca VisitArith_expr cannot generate code for term rule operator: " + operatorTxt + " yet");
            }
        }
        return null;
    }

    public override object VisitTerm([NotNull] CloacaParser.TermContext context)
    {
        // Always visit factor 0. Then pack on operations in post-order.
        // I tried to do this using an op label:
        // (op=('*'|'@'|'/'|'%'|'//')
        // but apparently I only get one per visitor but this line could match multiple factors.
        // Assign them to lexer names breaks the context up for each field by their rate of occurrence,
        // not position. So I just have to do hard-coded string matches! Yay! =D
        Visit(context.factor(0));
        for (int child_i = 2; child_i < context.children.Count; child_i += 2)
        {
            Visit(context.children[child_i]);
            string operatorTxt = context.children[child_i - 1].GetText();

            // term: factor (('*'|'@'|'/'|'%'|'//') factor)*;
            // Soooo I have no idea what that @ operator is all about.
            if (operatorTxt == "*")
            {
                ActiveProgram.AddInstruction(ByteCodes.BINARY_MULTIPLY, context);
            }
            else if (operatorTxt == "/")
            {
                ActiveProgram.AddInstruction(ByteCodes.BINARY_TRUE_DIVIDE, context);
            }
            else if (operatorTxt == "%")
            {
                ActiveProgram.AddInstruction(ByteCodes.BINARY_MODULO, context);
            }
            else if (operatorTxt == "//")
            {
                ActiveProgram.AddInstruction(ByteCodes.BINARY_FLOOR_DIVIDE, context);
            }
            else
            {
                throw new Exception("The Cloaca VisitTerm cannot generate code for term rule operator: " + operatorTxt + " yet");
            }
        }
        return null;
    }

    private void LoadConstantNumber(ParserRuleContext context)
    {
        ActiveProgram.Constants.Add(ConstantsFactory.CreateNumber(context));
        ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, ActiveProgram.Constants.Count - 1, context);
    }

    public override object VisitAtomName([NotNull] CloacaParser.AtomNameContext context)
    {
        // It might be a function name. Look for it in names.
        int nameIdx = ActiveProgram.VarNames.IndexOf(context.GetText());
        if (nameIdx >= 0)
        {
            ActiveProgram.AddInstruction(ByteCodes.LOAD_FAST, nameIdx, context);
        }
        else
        {
            generateLoadForVariable(context.GetText(), context);
        }
        return null;
    }

    public override object VisitAtomString([NotNull] CloacaParser.AtomStringContext context)
    {
        ActiveProgram.Constants.Add(ConstantsFactory.CreateString(context));
        ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, ActiveProgram.Constants.Count - 1, context);
        return null;
    }

    public override object VisitAtomBool([NotNull] CloacaParser.AtomBoolContext context)
    {
        ActiveProgram.Constants.Add(ConstantsFactory.CreateBool(context));
        ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, ActiveProgram.Constants.Count - 1, context);
        return null;
    }

    public override object VisitAtomNoneType([NotNull] CloacaParser.AtomNoneTypeContext context)
    {
        ActiveProgram.Constants.Add(NoneType.Instance);
        ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, ActiveProgram.Constants.Count - 1, context);
        return null;
    }

    public override object VisitFactor([NotNull] CloacaParser.FactorContext context)
    {
        // Have to sneak in here to look for things like negative numbers. Very tedious and also pretty hacky!
        if (context.GetText()[0] == '-')
        {
            LoadConstantNumber(context);
            return null;
        }
        else
        {
            return base.VisitFactor(context);
        }
    }

    public override object VisitAtomNumber([NotNull] CloacaParser.AtomNumberContext context)
    {
        LoadConstantNumber(context);
        return null;
    }

    public override object VisitAtomWait([NotNull] CloacaParser.AtomWaitContext context)
    {
        ActiveProgram.AddInstruction(ByteCodes.WAIT, context);
        return null;
    }

    public override object VisitAtomParens([NotNull] CloacaParser.AtomParensContext context)
    {
        // For now, we're assuming an atom of parentheses is a tuple
        base.VisitAtomParens(context);

        // testlist_comp: (test|star_expr) ( comp_for | (',' (test|star_expr))* (',')? );
        // If there's more than one component and it starts with a comma then we're looking at a tuple.
        // That includes single-element tuples: ("foo",)
        if (context.testlist_comp().children.Count > 1 && context.testlist_comp().children[1].GetText() == ",")
        {
            ActiveProgram.AddInstruction(ByteCodes.BUILD_TUPLE, context.testlist_comp().test().Length, context);
        }
        return null;
    }

    public override object VisitAtomSquareBrackets([NotNull] CloacaParser.AtomSquareBracketsContext context)
    {
        if (context.testlist_comp() != null && context.testlist_comp().comp_for() != null)
        {
            // What we generally have to do:
            // Create a code object for the list comprehension. The list will be called ".0"
            //
            // The list comprehension's inner context:
            //   1. Build empty list with BUILD_LIST
            //   2. Load it to .0
            //   3. Set up the for loop
            //   4. Generate code for the expression left of the for loop
            //   5. Use LIST_APPEND to take each computer result from the iteration and put it into .0
            //   6. Don't forget to return .0, which should be TOS by default.
            // In the outer context:
            //   1. Load up requirements to make the function (code object, made-up name)
            //   2. Make it with MAKE_FUNCTION
            //   3. Load list using LOAD_FAST
            //   4. Get iterator using GET_ITER
            //   5. Call list comp function

            // BOOKMARK
            // Make code object here and load it with LOAD_CONST. Call it "listcomp"
            var newFunctionCode = new CodeObjectBuilder();
            newFunctionCode.Name = "listcomp";

            ActiveProgram.Constants.Add(newFunctionCode);
            var compCodeIndex = ActiveProgram.Constants.Count - 1;

            ProgramStack.Push(ActiveProgram);
            ActiveProgram = newFunctionCode;

            Visit(context.testlist_comp().test(0));

            // Finishes list comprehension, Now we invoke it to actually run the list comprehension.
            ProgramStack.Pop();

            ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, compCodeIndex, context);         // TODO: Put code object index here!
            ActiveProgram.Constants.Add(PyString.Create(ActiveProgram.Name + ".<locals>.<listcomp>"));
            ActiveProgram.AddInstruction(ByteCodes.MAKE_FUNCTION, 0, context);

            // Loading the list we'll be using.
            Visit(context.testlist_comp().comp_for().or_test());        // Should drum up the list we're using
            ActiveProgram.AddInstruction(ByteCodes.GET_ITER, context);
            ActiveProgram.AddInstruction(ByteCodes.CALL_FUNCTION, context);

        }
        else
        {
            // For now, we're assuming an atom of parentheses is a tuple
            base.VisitAtomSquareBrackets(context);
            ActiveProgram.AddInstruction(ByteCodes.BUILD_LIST, context.testlist_comp().test().Length, context);
        }
        return null;
    }

    public override object VisitBreak_stmt([NotNull] CloacaParser.Break_stmtContext context)
    {
        ActiveProgram.AddInstruction(ByteCodes.BREAK_LOOP, context);
        return null;
    }

    public override object VisitContinue_stmt([NotNull] CloacaParser.Continue_stmtContext context)
    {
        ActiveProgram.AddInstruction(ByteCodes.JUMP_ABSOLUTE, LoopBlocks.Peek().LoopOrigin, context);
        return null;
    }

    public override object VisitReturn_stmt([NotNull] CloacaParser.Return_stmtContext context)
    {
        if (context.testlist() == null)
        {
            var noneConstIdx = ActiveProgram.Constants.AddGetIndex(NoneType.Instance);
            ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, noneConstIdx, context);
        }
        else
        {
            Visit(context.testlist());
        }
        ActiveProgram.AddInstruction(ByteCodes.RETURN_VALUE, context);
        return null;
    }

    public override object VisitRaise_stmt([NotNull] CloacaParser.Raise_stmtContext context)
    {
        // This will build up the exception and put it on the stack.
        // TODO: Support 'from' statement by expanding to test(1) as well--if defined.
        base.VisitTest(context.test(0));

        // For now, we only support one argument for exceptions, which will be the exception
        // created from visit the parent context.
        ActiveProgram.AddInstruction(ByteCodes.RAISE_VARARGS, 1, context);

        return null;
    }

    public override object VisitGlobal_stmt([NotNull] CloacaParser.Global_stmtContext context)
    {
        for (int name_i = 0; name_i < context.NAME().Length; ++name_i)
        {
            var name = context.NAME(name_i).GetText();
            ActiveProgram.Names.AddGetIndex(name);
        }
        return null;
    }

    public override object VisitExpr_stmt([NotNull] CloacaParser.Expr_stmtContext context)
    {
        if (context.testlist_star_expr().Length > 2 ||
            (context.GetToken(CloacaParser.ASSIGN, 0) == null && context.testlist_star_expr().Length == 2))
        {
            throw new Exception("Don't know how to evaluate an expr_stmt that isn't an assignment or wait statement");
        }

        // Single-statement 
        if (context.testlist_star_expr().Length == 1)
        {
            // (wait keyword) TODO: Remove when the wait keyword is turned into a function!
            if (context.testlist_star_expr(0).GetText() == "wait")
            {
                VisitLValueTestlist_star_expr(context.testlist_star_expr()[0].test()[0]);
            }
            else if (context.augassign() != null)
            {
                Visit(context.testlist());
                Visit(context.testlist_star_expr(0));               // Generates load for destination
                string augassign = context.augassign().GetText();
                if (augassign == "+=")
                {
                    ActiveProgram.AddInstruction(ByteCodes.INPLACE_ADD, context);
                }
                else if (augassign == "-=")
                {
                    ActiveProgram.AddInstruction(ByteCodes.INPLACE_SUBTRACT, context);
                }
                else if (augassign == "*=")
                {
                    ActiveProgram.AddInstruction(ByteCodes.INPLACE_MULTIPLY, context);
                }
                else if (augassign == "/=")
                {
                    ActiveProgram.AddInstruction(ByteCodes.INPLACE_TRUE_DIVIDE, context);
                }
                else if (augassign == "%=")
                {
                    ActiveProgram.AddInstruction(ByteCodes.INPLACE_MODULO, context);
                }
                else if (augassign == "//=")
                {
                    ActiveProgram.AddInstruction(ByteCodes.INPLACE_FLOOR_DIVIDE, context);
                }
                else if (augassign == "**=")
                {
                    ActiveProgram.AddInstruction(ByteCodes.INPLACE_POWER, context);
                }
                else if (augassign == "&=")
                {
                    ActiveProgram.AddInstruction(ByteCodes.INPLACE_AND, context);
                }
                else if (augassign == "|=")
                {
                    ActiveProgram.AddInstruction(ByteCodes.INPLACE_OR, context);
                }
                else if (augassign == "^=")
                {
                    ActiveProgram.AddInstruction(ByteCodes.INPLACE_XOR, context);
                }
                else if (augassign == ">>=")
                {
                    ActiveProgram.AddInstruction(ByteCodes.INPLACE_RSHIFT, context);
                }
                else if (augassign == "<<=")
                {
                    ActiveProgram.AddInstruction(ByteCodes.INPLACE_LSHIFT, context);
                }
                else
                {
                    throw new Exception("Unrecognized augassign: " + augassign);
                }

                // Re-use testlist_star_expr now as an LValue to store the result
                VisitLValueTestlist_star_expr(context.testlist_star_expr()[0].test()[0]);
            }
            else
            {
                Visit(context.testlist_star_expr(0));
            }
            return null;
        }

        // BOOKMARK: Try to take this for the INPLACE operations. However, you'll have to scrap passing down the testlist. See if you
        // can use test() instead. Augassign -> testlist -> test. Then you can use that LValueTestList_star_expr, although you'll still
        // have to figure out where to deal with the operators (probably implement VisitAugassign

        // RValue is testlist_star_expr[1]
        // LValue is testlist_star_expr[0]
        // Traverse the right hand side to get the assignment value on to the data stack
        // Then go down a special LValue version of the visitors for storing it.
        Visit(context.testlist_star_expr()[1]);
        VisitLValueTestlist_star_expr(context.testlist_star_expr()[0].test()[0]);

        return null;
        //return base.VisitExpr_stmt(context);
    }

    public object VisitLValueTestlist_star_expr([NotNull] CloacaParser.TestContext context)
    {
        // Okay, take a deep breath. We're going to skip most of the crap and get straight to the lvalue
        // name. We have to go through the whole cascade defined in the grammar to get to the expr.
        // Arguably, we could keep going even deeper. We'll probably be refining this once it becomes more
        // obvious what other kind of lvalues we could be dealing with.

        // TODO: Experiment with creating a tree traverser here to try to walk down to the atom.
        var maybeAtom = context.or_test()[0].and_test()[0].not_test()[0].comparison().expr()[0].
            xor_expr()[0].and_expr()[0].shift_expr()[0].arith_expr()[0].term()[0].factor()[0].power().atom_expr();
        var variableName = context.or_test()[0].and_test()[0].not_test()[0].comparison().expr()[0].GetText();
        if (maybeAtom.trailer().Length > 0)
        {
            // Is it subscriptable?
            if (maybeAtom.trailer()[0].subscriptlist() != null)
            {
                // Order to push on stack: assignment value (should already be specified before we got here), container, index
                variableName = maybeAtom.atom().GetText();
                generateLoadForVariable(variableName, context);
                base.VisitSubscriptlist(maybeAtom.trailer()[0].subscriptlist());
                ActiveProgram.AddInstruction(ByteCodes.STORE_SUBSCR, context);
            }
            // Object subscript (self.x)
            // 04/05/2020: This was updated to reference multiple attributes at once (mesh_renderer.material.color = abc)
            else if (maybeAtom.trailer().Length >= 1 && maybeAtom.trailer()[0].NAME() != null)
            {
                variableName = maybeAtom.atom().GetText();
                generateLoadForVariable(variableName, context);

                int last_trailer_idx = maybeAtom.trailer().Length - 1;
                for (int trailer_idx = 0; trailer_idx < last_trailer_idx; ++trailer_idx)
                {
                    var loadAttrName = maybeAtom.trailer()[trailer_idx].NAME().GetText();
                    var loadAttrIdx = ActiveProgram.Names.AddGetIndex(loadAttrName);
                    ActiveProgram.AddInstruction(ByteCodes.LOAD_ATTR, loadAttrIdx, context);
                }

                var attrName = maybeAtom.trailer()[last_trailer_idx].NAME().GetText();
                var attrIdx = ActiveProgram.Names.AddGetIndex(attrName);
                ActiveProgram.AddInstruction(ByteCodes.STORE_ATTR, attrIdx, context);
                return null;
            }
            else
            {
                // Function call?
                base.Visit(context);
            }
        }
        else
        {
            // Reserved word: wait
            if (variableName == "wait")
            {
                ActiveProgram.AddInstruction(ByteCodes.WAIT, context);
                return null;
            }

            // Store value
            generateStoreForVariable(variableName, context);
        }
        return null;
    }

    public override object VisitFile_input([NotNull] CloacaParser.File_inputContext context)
    {
        for (int i = 0; i < context.stmt().Length; ++i)
        {
            base.VisitStmt(context.stmt(i));
        }
        return null;
    }

    public override object VisitIf_stmt([NotNull] CloacaParser.If_stmtContext context)
    {
        var conditional_block_fixups = new List<JumpOpcodeFixer>();
        int if_cond_i;
        for (if_cond_i = 0; if_cond_i < context.test().Length; ++if_cond_i)
        {
            var comparison = context.test(if_cond_i);
            Visit(comparison);
            var jumpFalseSkip = new JumpOpcodeFixer(ActiveProgram.Code, ActiveProgram.AddInstruction(ByteCodes.POP_JUMP_IF_FALSE, -1, context));
            Visit(context.suite(if_cond_i));

            // We'll need this to skip other conditional blocks, but we only need this if we actually
            // have other ones:
            if (context.suite().Length > 1)
            {
                conditional_block_fixups.Add(new JumpOpcodeFixer(ActiveProgram.Code, ActiveProgram.AddInstruction(ByteCodes.JUMP_FORWARD, -1, context)));
            }
            jumpFalseSkip.FixupAbsolute(ActiveProgram.Code.Count);
        }

        // Handles the 'else' clause if we have one. The else is a suite without a comparison.
        if (context.suite().Length > if_cond_i)
        {
            Visit(context.suite(if_cond_i));
        }

        // Fixup any forward jumps we might have. They should all come to our current program position.
        foreach (var fixup in conditional_block_fixups)
        {
            fixup.Fixup(ActiveProgram.Code.Count);
        }
        return null;
    }

    public override object VisitWhile_stmt([NotNull] CloacaParser.While_stmtContext context)
    {
        /*
            >>> def while_loop(x):
            ...   x = 0
            ...   while(x < 10):
            ...     x += 1
            ...   else:
            ...     x = 11
            ...
            >>> dis.dis(while_loop)
              2           0 LOAD_CONST               1 (0)
                          2 STORE_FAST               0 (x)

              3           4 SETUP_LOOP              24 (to 30)
                    >>    6 LOAD_FAST                0 (x)
                          8 LOAD_CONST               2 (10)
                         10 COMPARE_OP               0 (<)
                         12 POP_JUMP_IF_FALSE       24

              4          14 LOAD_FAST                0 (x)
                         16 LOAD_CONST               3 (1)
                         18 INPLACE_ADD
                         20 STORE_FAST               0 (x)
                         22 JUMP_ABSOLUTE            6
                    >>   24 POP_BLOCK

              6          26 LOAD_CONST               4 (11)
                         28 STORE_FAST               0 (x)
                    >>   30 LOAD_CONST               0 (None)
                         32 RETURN_VALUE
        */
        // We'll have to fix this up after generating the loop
        // SETUP_LOOP takes a delta from the next instruction address
        var setupLoopIdx = ActiveProgram.AddInstruction(ByteCodes.SETUP_LOOP, -1, context);
        var setupLoopFixup = new JumpOpcodeFixer(ActiveProgram.Code, setupLoopIdx);

        Visit(context.test());
        var pop_jump_fixup = new JumpOpcodeFixer(ActiveProgram.Code, ActiveProgram.AddInstruction(ByteCodes.POP_JUMP_IF_FALSE, -1, context));
        LoopBlocks.Push(new LoopBlockRecord(setupLoopIdx));       // Remember we're getting the location after adding an instruction, not before.
        try
        {
            Visit(context.suite(0));

            ActiveProgram.AddInstruction(ByteCodes.JUMP_ABSOLUTE, setupLoopIdx, context);
            int pop_block_i = ActiveProgram.AddInstruction(ByteCodes.POP_BLOCK, context) - 1;
            pop_jump_fixup.FixupAbsolute(pop_block_i);

            // Else clause? We will have two suites.
            if (context.suite().Length > 1)
            {
                Visit(context.suite(1));
            }

            setupLoopFixup.Fixup(ActiveProgram.Code.Count);

            return null;
        }
        finally
        {
            LoopBlocks.Pop();
        }
    }

    public override object VisitFor_stmt([NotNull] CloacaParser.For_stmtContext context)
    {
        /*
         * >>> def for_loop():
           ...   a = 0
           ...   for i in range(0, 10, 1):
           ...     a += i
           ...   return a
           ...
           >>> from dis import dis
           >>> dis(for_loop)
             2           0 LOAD_CONST               1 (0)
                         2 STORE_FAST               0 (a)

             3           4 SETUP_LOOP              28 (to 34)
                         6 LOAD_GLOBAL              0 (range)
                         8 LOAD_CONST               1 (0)
                         10 LOAD_CONST               2 (10)
                         12 LOAD_CONST               3 (1)
                         14 CALL_FUNCTION            3
                         16 GET_ITER
                    >>   18 FOR_ITER                12 (to 32)
                         20 STORE_FAST               1 (i)

              4          22 LOAD_FAST                0 (a)
                         24 LOAD_FAST                1 (i)
                         26 INPLACE_ADD
                         28 STORE_FAST               0 (a)
                         30 JUMP_ABSOLUTE           18
                    >>   32 POP_BLOCK

              5     >>   34 LOAD_FAST                0 (a)
                         36 RETURN_VALUE
        */
        // for_stmt: 'for' exprlist 'in' testlist ':' suite ('else' ':' suite)?;
        // exprlist: (expr|star_expr) (',' (expr|star_expr))* (',')?;
        var setupLoopIdx = ActiveProgram.AddInstruction(ByteCodes.SETUP_LOOP, -1, context);
        var setupLoopFixup = new JumpOpcodeFixer(ActiveProgram.Code, setupLoopIdx);

        Visit(context.testlist());

        var forIterIdx = ActiveProgram.AddInstruction(ByteCodes.GET_ITER, context);
        var postForIterIdx = ActiveProgram.AddInstruction(ByteCodes.FOR_ITER, -1, context);
        LoopBlocks.Push(new LoopBlockRecord(forIterIdx));       // Remember we're getting the location after adding an instruction, not before.
        try
        {
            var forIterFixup = new JumpOpcodeFixer(ActiveProgram.Code, postForIterIdx);

            // If there's actually more than one expression here then we're dealing with a tuple and we need to have
            // it unpacked.
            if (context.exprlist().expr().Length > 1)
            {
                ActiveProgram.AddInstruction(ByteCodes.UNPACK_SEQUENCE, context.exprlist().expr().Length, context);
            }

            foreach (var expr in context.exprlist().expr())
            {
                // I don't have complete confidence in setting the names explicitly like this, but visiting
                // the expr winds up just creating LOAD_GLOBAL instead of the STORE_FAST we actually need.
                generateStoreForVariable(expr.GetText(), context);
                //Visit(expr);
            }

            Visit(context.suite(0));

            ActiveProgram.AddInstruction(ByteCodes.JUMP_ABSOLUTE, forIterIdx, context);
            int pop_block_i = ActiveProgram.AddInstruction(ByteCodes.POP_BLOCK, context) - 1;
            forIterFixup.Fixup(pop_block_i);

            // else-clause. If there's no else then we're done with the loop.
            if (context.suite().Length > 1)
            {
                Visit(context.suite(1));
            }
            setupLoopFixup.Fixup(ActiveProgram.Code.Count);

            return null;
        }
        finally
        {
            LoopBlocks.Pop();
        }
    }

    /// <summary>
    /// Finds the first occurrance of the given text in the context's children.
    /// </summary>
    /// <param name="children"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    private int getFirstIndexOfText(IList<Antlr4.Runtime.Tree.IParseTree> children, string text)
    {
        for (int foundIdx = 0; foundIdx < children.Count; ++foundIdx)
        {
            if (children[foundIdx].GetText() == text)
            {
                return foundIdx;
            }
        }
        return -1;
    }

    /// <summary>
    /// Wrapper around getFirstIndexOfText that returns true if the text exists at all in the children.
    /// This is a more readable version of a basic existence test.
    /// </summary>
    /// <param name="children">Children of the context to check.</param>
    /// <param name="text">The text to find.</param>
    /// <returns>True if the text exists anywhere in the list of the children, false otherwise.</returns>
    private bool hasText(IList<Antlr4.Runtime.Tree.IParseTree> children, string text)
    {
        return getFirstIndexOfText(children, text) >= 0;
    }

    public override object VisitTry_stmt([NotNull] CloacaParser.Try_stmtContext context)
    {
        // Block setup for SETUP_EXCEPT, SETUP_FINALLY:
        // We have to emit them with a target an offset from the end of this instruction to where it would go for the first except check
        // We will emit the instruction, remember our place, and fix it up once we have emitted the entire try block.
        // Determine if we have a finally block. This HAS to be the last major statement in the block. So its suite is the last suite.
        // Its child in the list is length-3
        bool hasFinally = false;
        bool hasElse = false;
        bool hasExcept = false;
        var finallyTarget = new JumpOpcodeFixer(ActiveProgram.Code);

        if (hasText(context.children, "finally"))
        {
            hasFinally = true;
            finallyTarget.Add(ActiveProgram.AddInstruction(ByteCodes.SETUP_FINALLY, -1, context));
        }

        hasElse = hasText(context.children, "else");
        hasExcept = context.except_clause().Length > 0;

        // Try block preamble. If there are exceptions, then we need a SETUP_EXCEPT position.
        var setupExceptTarget = new JumpOpcodeFixer(ActiveProgram.Code);
        if (hasExcept)
        {
            setupExceptTarget.Add(ActiveProgram.AddInstruction(ByteCodes.SETUP_EXCEPT, -1, context));
        }

        int suiteIdx = 0;
        Visit(context.suite(suiteIdx));
        ++suiteIdx;
        ActiveProgram.AddInstruction(ByteCodes.POP_BLOCK, context);
        var endOfTryJumpTarget = new JumpOpcodeFixer(ActiveProgram.Code, ActiveProgram.AddInstruction(ByteCodes.JUMP_FORWARD, -1, context));

        // Start of except statements
        var endOfExceptBlockJumpFixups = new List<JumpOpcodeFixer>();
        var exceptionMatchTestFixups = new List<JumpOpcodeFixer>();
        var startOfExcepts = new List<int>();

        foreach (var exceptClause in context.except_clause())
        {
            // Making a closure to represent visiting the Except_Clause. It's not a dedicated override of the default in the rule
            // because we need so much context from the entire try block
            {
                startOfExcepts.Add(ActiveProgram.Code.Count);
                if (exceptClause.test() != null && exceptClause.test().ChildCount > 0)
                {
                    // If the exception is aliased, we need to make sure we still have a copy
                    // of it to store in the alias AFTER we have determined that we want to
                    // enter its except clause. So we'll duplicate it here, then test, then store it.
                    if (exceptClause.NAME() != null)
                    {
                        ActiveProgram.AddInstruction(ByteCodes.DUP_TOP, context);
                    }

                    generateLoadForVariable(exceptClause.test().GetText(), context);
                    ActiveProgram.AddInstruction(ByteCodes.COMPARE_OP, (ushort)CompareOps.ExceptionMatch, context);

                    // Point to next except clause to test for a match to this exception
                    exceptionMatchTestFixups.Add(new JumpOpcodeFixer(ActiveProgram.Code, ActiveProgram.AddInstruction(ByteCodes.POP_JUMP_IF_FALSE, -1, context)));

                    if (exceptClause.NAME() != null)
                    {
                        // BTW, this pops the exception
                        generateStoreForVariable(exceptClause.NAME().GetText(), context);
                    }
                }
            }

            Visit(context.suite(suiteIdx));
            ++suiteIdx;

            // TODO: Look into deleting aliased exceptions.
            // A DELETE_FAST was done for an aliased exception in an auto-generated END_FINALLY clause
            // Look at Python generation for TryExceptAliasBasic
            endOfExceptBlockJumpFixups.Add(new JumpOpcodeFixer(ActiveProgram.Code, ActiveProgram.AddInstruction(ByteCodes.JUMP_FORWARD, -1, context)));
        }

        // else block
        int startOfElseBlock = ActiveProgram.Code.Count;
        if (hasElse)
        {
            Visit(context.suite(suiteIdx));
            ++suiteIdx;
        }

        // finally block
        int startOfFinallyBlock = ActiveProgram.Code.Count;
        if (hasFinally)
        {
            Visit(context.suite(suiteIdx));
            ++suiteIdx;
            finallyTarget.Fixup(startOfFinallyBlock);
            ActiveProgram.AddInstruction(ByteCodes.END_FINALLY, context);
        }

        int endOfBlockPosition = hasFinally ? startOfFinallyBlock : ActiveProgram.Code.Count;
        endOfBlockPosition = hasElse ? startOfElseBlock : endOfBlockPosition;

        // Try block fixups
        endOfTryJumpTarget.Fixup(endOfBlockPosition);

        // Except statement fixups
        if (hasExcept)
        {
            setupExceptTarget.Fixup(startOfExcepts[0]);
            foreach (var exceptJumpOutFixup in endOfExceptBlockJumpFixups)
            {
                exceptJumpOutFixup.Fixup(endOfBlockPosition);
            }
        }

        // Exception matching block fixups
        for (int except_i = 0; except_i < exceptionMatchTestFixups.Count; ++except_i)
        {
            var exceptTestFixup = exceptionMatchTestFixups[except_i];
            if (except_i < exceptionMatchTestFixups.Count - 1)
            {
                exceptTestFixup.FixupAbsolute(startOfExcepts[except_i + 1]);
            }
            else
            {
                exceptTestFixup.FixupAbsolute(endOfBlockPosition);
            }
        }

        //// TODO: Investigate correctness of this END_FINALLY emitter. Looks like it's necessary to set up an END_FINALLY if none of our except clauses trigger and we don't have a finally statement either.
        //// End block. If we have a finally, we end out dumping two of these. It looks like we want one either wait (if we had an except). Dunno
        //// about try-else. Is that even legal?
        //if (!hasFinally)
        //{
        //    ActiveProgram.AddInstruction(ByteCodes.END_FINALLY);
        //}

        return null;
    }

    public override object VisitFuncdef([NotNull] CloacaParser.FuncdefContext context)
    {
        //
        // MAKE_FUNCTION(argc)
        //    Pushes a new function object on the stack. From bottom to top, the consumed stack must consist of values if the argument carries a specified flag value
        //
        //    0x01 a tuple of default values for positional-only and positional-or-keyword parameters in positional order
        //    0x02 a dictionary of keyword-only parameters’ default values
        //    0x04 an annotation dictionary
        //    0x08 a tuple containing cells for free variables, making a closure
        //    the code associated with the function (at TOS1)
        //    the qualified name of the function (at TOS)
        // 
        var funcName = context.NAME().GetText();        // TODO: This isn't the fully-qualified name and will need to be improved.
        var newFunctionCode = new CodeObjectBuilder();
        newFunctionCode.Name = funcName;

        // We'll replace an existing name if we have one because assholes may overwrite a function.
        int funcIndex = findFunctionIndex(funcName);
        if (funcIndex < 0)
        {
            ActiveProgram.Constants.Add(newFunctionCode);
            funcIndex = ActiveProgram.Constants.Count - 1;
        }
        else
        {
            ActiveProgram.Constants[funcIndex] = newFunctionCode;
        }

        int nameIndex = findConstantIndex<string>(funcName);
        if (nameIndex < 0)
        {
            ActiveProgram.Constants.Add(funcName);
            nameIndex = ActiveProgram.Constants.Count - 1;
        }

        ProgramStack.Push(ActiveProgram);
        // This should fill into newFunctionCode.
        ActiveProgram = newFunctionCode;

        // Let's have our parameters set first. This should go to VisitTfpdef in particular.
        base.Visit(context.parameters());

        base.VisitSuite(context.suite());

        // Did we end with a RETURN_VALUE? If not, return None. Kind of hacky but the alternative is tracking the
        // state for this, and it gets ugly if there are multiple blocks with returns in them.
        if (ActiveProgram.Code.Count < 1 || ActiveProgram.Code[ActiveProgram.Code.Count - 2] != (byte)ByteCodes.RETURN_VALUE)
        {
            var noneConstIdx = ActiveProgram.Constants.AddGetIndex(NoneType.Instance);
            ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, noneConstIdx, context);
            ActiveProgram.AddInstruction(ByteCodes.RETURN_VALUE, context);
        }

        ActiveProgram.AddInstruction(ByteCodes.RETURN_VALUE, context);      // Return statement from generated function

        // This should restore us back to the original function with which we started.
        ActiveProgram = ProgramStack.Pop();

        // We don't support any additional flags yet.       
        ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, funcIndex, context);
        ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, nameIndex, context);
        ActiveProgram.AddInstruction(ByteCodes.MAKE_FUNCTION, 0, context);

        // TODO: Apparently sometimes (class methods) we need to store this using STORE_NAME. Why?
        // Class declarations need all their functions declared using STORE_NAME. I'm not sure why yet. I am speculating that it's 
        // more proper to say that *everything* needs STORE_NAME by default but we're able to optimize it in just about every other
        // case. I don't have a full grasp on namespaces yet. So we're going to do something *very cargo cult* and hacky and just 
        // decide that if our parent context is a class definition that we'll use a STORE_NAME here.
        //
        // I noticed that the REPL would screw up parsing function declarations based on all these upwards Parent lookups.
        if (context.Parent.Parent.Parent != null && context.Parent.Parent.Parent.Parent != null &&
            context.Parent.Parent.Parent.Parent is CloacaParser.ClassdefContext)
        {
            var nameIdx = ActiveProgram.Names.AddGetIndex(funcName);
            ActiveProgram.AddInstruction(ByteCodes.STORE_NAME, nameIdx, context);
        }
        else
        {
            var nameIdx = ActiveProgram.VarNames.AddGetIndex(funcName);
            ActiveProgram.AddInstruction(ByteCodes.STORE_FAST, nameIdx, context);
        }

        return null;
    }

    public override object VisitNot_test([NotNull] CloacaParser.Not_testContext context)
    {
        // not_test: 'not' not_test | comparison;
        var inner_not_test = context.not_test();
        if (inner_not_test != null)
        {
            base.Visit(inner_not_test);
            ActiveProgram.AddInstruction(ByteCodes.UNARY_NOT, context);
            return null;
        }
        else
        {
            return base.VisitNot_test(context);
        }
    }

    // TODO: This should not be BINARY_ADD, which should be x & y
    // This appears to be implemented using some jump opcodes
    public override object VisitAnd_test([NotNull] CloacaParser.And_testContext context)
    {
        // not_test: 'not' not_test | comparison;
        var inner_not_tests = context.not_test();
        if (inner_not_tests.Length == 2)
        {
            base.Visit(inner_not_tests[0]);
            base.Visit(inner_not_tests[1]);
            ActiveProgram.AddInstruction(ByteCodes.BINARY_AND, context);
            return null;
        }
        else
        {
            return base.VisitAnd_test(context);
        }
    }

    // TODO: This should not be BINARY_OR, which should be x | y
    // This appears to be implemented using some jump opcodes
    public override object VisitOr_test([NotNull] CloacaParser.Or_testContext context)
    {
        // not_test: 'not' not_test | comparison;
        var inner_and_tests = context.and_test();
        if (inner_and_tests.Length == 2)
        {
            base.Visit(inner_and_tests[0]);
            base.Visit(inner_and_tests[1]);
            ActiveProgram.AddInstruction(ByteCodes.BINARY_OR, context);
            return null;
        }
        else
        {
            return base.VisitOr_test(context);
        }
    }

    public override object VisitExpr([NotNull] CloacaParser.ExprContext context)
    {
        var inner_and_tests = context.xor_expr();
        if (inner_and_tests.Length >= 2)
        {
            base.Visit(inner_and_tests[0]);
            for (int child_i = 2; child_i < context.children.Count; child_i += 2)
            {
                Visit(context.children[child_i]);
                Visit(context.children[child_i - 1]);
                ActiveProgram.AddInstruction(ByteCodes.BINARY_OR, context);
            }
            return null;
        }
        else
        {
            return base.VisitExpr(context);
        }

    }

    public override object VisitXor_expr([NotNull] CloacaParser.Xor_exprContext context)
    {
        var inner_and_tests = context.and_expr();
        if (inner_and_tests.Length >= 2)
        {
            base.Visit(inner_and_tests[0]);
            for (int child_i = 2; child_i < context.children.Count; child_i += 2)
            {
                Visit(context.children[child_i]);
                Visit(context.children[child_i - 1]);
                ActiveProgram.AddInstruction(ByteCodes.BINARY_XOR, context);
            }
            return null;
        }
        else
        {
            return base.VisitXor_expr(context);
        }
    }

    public override object VisitAnd_expr([NotNull] CloacaParser.And_exprContext context)
    {
        var inner_and_tests = context.shift_expr();
        if (inner_and_tests.Length >= 2)
        {
            base.Visit(inner_and_tests[0]);
            for (int child_i = 2; child_i < context.children.Count; child_i += 2)
            {
                Visit(context.children[child_i]);
                Visit(context.children[child_i - 1]);
                ActiveProgram.AddInstruction(ByteCodes.BINARY_AND, context);
            }
            return null;
        }
        else
        {
            return base.VisitAnd_expr(context);
        }
    }

    public override object VisitPower([NotNull] CloacaParser.PowerContext context)
    {
        if (context.factor() != null)
        {
            base.Visit(context.atom_expr());
            base.Visit(context.factor());
            ActiveProgram.AddInstruction(ByteCodes.BINARY_POWER, context);
        }
        else
        {
            base.Visit(context.atom_expr());
        }
        return null;
    }

    public override object VisitShift_expr([NotNull] CloacaParser.Shift_exprContext context)
    {
        base.Visit(context.arith_expr(0));
        for (int child_i = 2; child_i < context.children.Count; child_i += 2)
        {
            Visit(context.children[child_i]);
            string operatorTxt = context.children[child_i - 1].GetText();
            if (operatorTxt == "<<")
            {
                ActiveProgram.AddInstruction(ByteCodes.BINARY_LSHIFT, context);
            }
            else if (operatorTxt == ">>")
            {
                ActiveProgram.AddInstruction(ByteCodes.BINARY_RSHIFT, context);
            }
            else
            {
                throw new Exception("The Cloaca VisitArith_expr cannot generate code for term rule operator: " + operatorTxt + " yet");
            }
        }
        return null;
    }

    public override object VisitSubscriptlist([NotNull] CloacaParser.SubscriptlistContext context)
    {
        base.VisitSubscriptlist(context);
        ActiveProgram.AddInstruction(ByteCodes.BINARY_SUBSCR, context);
        return null;
    }

    public override object VisitTypedargslist([NotNull] CloacaParser.TypedargslistContext context)
    {
        if (ActiveProgram.Defaults == null)
        {
            ActiveProgram.Defaults = new List<object>();
        }
        if (ActiveProgram.KWDefaults == null)
        {
            ActiveProgram.KWDefaults = new List<object>();
        }

        // TODO [KEYWORD-POSITIONAL-ONLY] Implement positional-only (/) and keyword-only (*) arguments
        // Hunting for defaults, *args, and **kwargs. Oh, and regular ole' parameter names without any gravy.
        for (int child_i = 0; child_i < context.children.Count; ++child_i)
        {
            if (context.children[child_i].GetText() == "*")
            {
                ActiveProgram.Flags |= CodeObject.CO_FLAGS_VARGS;
                Visit(context.children[child_i]);
            }
            else if (context.children[child_i].GetText() == "**")
            {
                ActiveProgram.Flags |= CodeObject.CO_FLAGS_KWARGS;
                Visit(context.children[child_i]);
                // TODO: [**kwargs] Support kwargs
                throw new NotImplementedException("Keyword args using **kwargs format are not yet supported.");
            }
            else if (context.children[child_i].GetText() == "=")
            {
                var defaultText = context.children[child_i + 1].GetText();

                // We need to freeze some state for our lambdas or else the meaning of these will change as we parse other stuff.
                int visit_child_i_copy = child_i + 1;
                string visit_builder_name = ActiveProgram.Name + "_$Default_" + context.children[visit_child_i_copy].GetText();

                // Use defaults normally but hw kwdefaults if vargs was defined. We set this up in advance for the lambda so it uses
                // the proper list when it finally runs.
                List<object> defaultsList = ActiveProgram.Defaults;
                if(ActiveProgram.HasVargs)
                {
                    defaultsList = ActiveProgram.KWDefaults;
                    ActiveProgram.KWOnlyArgCount += 1;
                }
                
                postProcessActions.Add(async (scheduler) =>
                {
                    // Cute hack: Pre-populate the defaults with the code objects that will calculate the final value for each default.
                    // We will execute all of these defaults before the code object is finalized. This will ensure we execute defaults
                    // at the same time CPython does (right after definition).
                    //
                    // Funny story: If I set up this default builder outside this lambda, it'll be correctly set, but by the
                    // time the lambda runs, it'll contain the function body's code instead and error. I still have no idea how
                    // that actually happens!
                    var defaultBuilder = new CodeObjectBuilder();
                    defaultBuilder.Name = visit_builder_name;

                    // Now we're parsing the default assignment. It's a program even if it's just a simple declaration like None. They will be
                    // processed right after the definition is created. So we will enqueue interpreting all of them in the post process action
                    // list and have the defaults set up with them as we go. We will get these breadth-first which is the order CPython would
                    // do them too.
                    ProgramStack.Push(ActiveProgram);
                    ActiveProgram = defaultBuilder;
                    Visit(context.children[visit_child_i_copy]);
                    ProgramStack.Pop();

                    var currentTask = scheduler.GetCurrentTask();
                    var defaultPrecalcCode = defaultBuilder.Build();
                    var task = scheduler.Schedule(defaultPrecalcCode);

                    // There isn't anything to actual unblock us when the code finished and the result is ready, so we need
                    // to kick the task in the ass in order to unblock the task.
                    task.WhenTaskCompleted += (ignored) =>
                    {
                        task.Continue();
                    };
                    var receipt = await task;

                    defaultsList.Add(receipt.Frame.DataStack.Pop());
                });

                // Move beyond the default assignment tokens so the for-loop starts at the next parameter.
                child_i += 1;
            }
            else
            {
                // Count this as a positional argument if we haven't encountered *args or **kwargs yet
                if (context.children[child_i].GetText() != "," &&
                    (ActiveProgram.Flags & (CodeObject.CO_FLAGS_VARGS | CodeObject.CO_FLAGS_KWARGS)) == 0)
                {
                    ActiveProgram.ArgCount += 1;
                }
                Visit(context.children[child_i]);
            }
        }
        return null;
    }

    private void Task_WhenTaskCompleted(TaskEventRecord record)
    {
        throw new NotImplementedException();
    }

    public override object VisitTfpdef([NotNull] CloacaParser.TfpdefContext context)
    {
        var variableName = context.NAME().GetText();

        ActiveProgram.ArgVarNames.Add(variableName);
        ActiveProgram.VarNames.Add(variableName);

        return null;
    }

    private int findConstantIndex<T>(T constant) where T : class
    {
        for (int i = 0; i < ActiveProgram.Constants.Count; ++i)
        {
            if (ActiveProgram.Constants[i] is T)
            {
                var asT = ActiveProgram.Constants[i] as T;
                if (constant == asT)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    private int findFunctionIndex(string funcName)
    {
        int existingFuncIndex = 0;
        for (existingFuncIndex = 0; existingFuncIndex < ActiveProgram.Constants.Count; ++existingFuncIndex)
        {
            if (ActiveProgram.Constants[existingFuncIndex] is CodeObject)
            {
                var asCodeObject = ActiveProgram.Constants[existingFuncIndex] as CodeObject;
                if (asCodeObject.Name == funcName)
                {
                    return existingFuncIndex;
                }
            }
        }
        return -1;
    }

    public override object VisitAtom_expr([NotNull] CloacaParser.Atom_exprContext context)
    {
        if (context.trailer().Length == 0)
        {
            return base.VisitAtom_expr(context);
        }
        else
        {
            // Example input that winds up here:
            // super().__init__()
            //
            // context = super().__init__()
            // atom = super
            // trailers:
            //   0: ()
            //   1: .__init__
            //   2: ()
            //
            // So we have to determine if we're looking at arguments to a function or a continuation of
            // object attribute lookups.
            Visit(context.atom());
            for (int trailer_i = 0; trailer_i < context.trailer().Length; ++trailer_i)
            {
                var trailer = context.trailer(trailer_i);
                if (trailer.NAME() != null)
                {
                    var attrName = trailer.NAME().GetText();
                    var attrIdx = ActiveProgram.Names.AddGetIndex(attrName);
                    ActiveProgram.AddInstruction(ByteCodes.LOAD_ATTR, attrIdx, context);

                }

                // A function that doesn't take any arguments doesn't have an arglist, but that is what 
                // got triggered. The only way I know to make sure we trigger on it is to see if we match
                // parentheses. There has to be a better way...
                else if (trailer.arglist() != null || trailer.GetText() == "()")
                {
                    // Keyword argument names. Start setting this up if we run into a "foo=bar" argument.
                    List<object> specifiedKeywords = null;

                    int argIdx = 0;
                    for (argIdx = 0; trailer.arglist() != null &&
                        trailer.arglist().argument(argIdx) != null; ++argIdx)
                    {
                        if (trailer.arglist().argument(argIdx).test().Length > 1)
                        {
                            // Keyword argument! Note we're not using C# 8.0 so we can't null coalesce this.
                            if (specifiedKeywords == null)
                            {
                                specifiedKeywords = new List<object>();
                            }
                            specifiedKeywords.Add(PyString.Create(trailer.arglist().argument(argIdx).test(0).GetText()));
                            base.Visit(trailer.arglist().argument(argIdx).test(1));
                        }
                        else
                        {
                            base.Visit(trailer.arglist().argument(argIdx));
                        }
                    }

                    if (specifiedKeywords != null)
                    {
                        var keywordTuple = PyTuple.Create(specifiedKeywords);
                        var keywordTupleIdx = ActiveProgram.Constants.AddGetIndex(keywordTuple);
                        ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, keywordTupleIdx, context);
                        ActiveProgram.AddInstruction(ByteCodes.CALL_FUNCTION_KW, argIdx, context);
                    }
                    else
                    {
                        ActiveProgram.AddInstruction(ByteCodes.CALL_FUNCTION, argIdx, context);
                    }
                }
                else
                {
                    base.Visit(trailer);
                }
            }
        }
        return null;
    }

    // TODO: Consider switching to same method that VisitAtomParens is using here, which might make this general-purpose enough.
    public override object VisitDictorsetmaker([NotNull] CloacaParser.DictorsetmakerContext context)
    {
        // For now, we are assuming we're building simple dictionaries! We're not doing sets, nor
        // are the elements complex statements (yet).
        foreach (var test in context.test())
        {
            Visit(test);
        }
        ActiveProgram.AddInstruction(ByteCodes.BUILD_MAP, context.test().Length / 2, context);
        return null;
    }

    public override object VisitComparison([NotNull] CloacaParser.ComparisonContext context)
    {
        // This might just be a pass-through to greener pastures (atoms). If no operator
        // is defined, then just keep it chugging. Otherwise, it's for us.
        if (context.comp_op().Length == 0)
        {
            return base.VisitComparison(context);
        }

        // Still here? Comparison time!
        Visit(context.expr(0));
        Visit(context.expr(1));
        Visit(context.comp_op(0));
        return null;
    }

    public override object VisitComp_op([NotNull] CloacaParser.Comp_opContext context)
    {
        switch (context.op.Type)
        {
            case CloacaParser.COMP_OP_LT:
                ActiveProgram.Code.AddByte((byte)ByteCodes.COMPARE_OP);
                ActiveProgram.Code.AddUShort((ushort)CompareOps.Lt);
                break;
            case CloacaParser.COMP_OP_GT:
                ActiveProgram.Code.AddByte((byte)ByteCodes.COMPARE_OP);
                ActiveProgram.Code.AddUShort((ushort)CompareOps.Gt);
                break;
            case CloacaParser.COMP_OP_EQ:
                ActiveProgram.Code.AddByte((byte)ByteCodes.COMPARE_OP);
                ActiveProgram.Code.AddUShort((ushort)CompareOps.Eq);
                break;
            case CloacaParser.COMP_OP_GTE:
                ActiveProgram.Code.AddByte((byte)ByteCodes.COMPARE_OP);
                ActiveProgram.Code.AddUShort((ushort)CompareOps.Ge);
                break;
            case CloacaParser.COMP_OP_LTE:
                ActiveProgram.Code.AddByte((byte)ByteCodes.COMPARE_OP);
                ActiveProgram.Code.AddUShort((ushort)CompareOps.Le);
                break;
            case CloacaParser.COMP_OP_LTGT:
                ActiveProgram.Code.AddByte((byte)ByteCodes.COMPARE_OP);
                ActiveProgram.Code.AddUShort((ushort)CompareOps.LtGt);
                break;
            case CloacaParser.COMP_OP_NE:
                ActiveProgram.Code.AddByte((byte)ByteCodes.COMPARE_OP);
                ActiveProgram.Code.AddUShort((ushort)CompareOps.Ne);
                break;
            case CloacaParser.COMP_OP_IN:
                ActiveProgram.Code.AddByte((byte)ByteCodes.COMPARE_OP);
                ActiveProgram.Code.AddUShort((ushort)CompareOps.In);
                break;
            case CloacaParser.COMP_OP_NOT_IN:
                ActiveProgram.Code.AddByte((byte)ByteCodes.COMPARE_OP);
                ActiveProgram.Code.AddUShort((ushort)CompareOps.NotIn);
                break;
            case CloacaParser.COMP_OP_IS:
                ActiveProgram.Code.AddByte((byte)ByteCodes.COMPARE_OP);
                ActiveProgram.Code.AddUShort((ushort)CompareOps.Is);
                break;
            case CloacaParser.COMP_OP_IS_NOT:
                ActiveProgram.Code.AddByte((byte)ByteCodes.COMPARE_OP);
                ActiveProgram.Code.AddUShort((ushort)CompareOps.IsNot);
                break;
            default:
                throw new Exception("Unexpected comparison operator: " + context.op);
        }

        return null;
    }

    public override object VisitClassdef([NotNull] CloacaParser.ClassdefContext context)
    {
        // TODO: We don't recognize the arglist yet (inheritance) for the class

        var className = context.NAME().GetText();
        var newFunctionCode = new CodeObjectBuilder();
        newFunctionCode.Name = className;

        // Descend into the constructor's body as its own program
        ProgramStack.Push(ActiveProgram);
        ActiveProgram = newFunctionCode;

        // This is what happens in a class code object that just passes __init__
        //
        // class Foo:
        //   def __init__(self):
        //     pass
        //
        //      2           0 LOAD_NAME                0 (__name__)
        //                  2 STORE_NAME               1 (__module__)
        //                  4 LOAD_CONST               0 ('def_constructor.<locals>.Foo')
        //                  6 STORE_NAME               2 (__qualname__)
        //
        //      3           8 LOAD_CONST               1 (<code object __init__ at 0x0000021BD5908C00, file "<stdin>", line 3>)
        //                 10 LOAD_CONST               2 ('def_constructor.<locals>.Foo.__init__')
        //                 12 MAKE_FUNCTION            0
        //                 14 STORE_NAME               3 (__init__)
        //                 16 LOAD_CONST               3 (None)
        //                 18 RETURN_VALUE
        //
        // We'll do some of this on our own for now until we figure out another convention.
        var __name__idx = ActiveProgram.Names.AddGetIndex("__name__");
        var __module__idx = ActiveProgram.Names.AddGetIndex("__module__");
        var __qualname__idx = ActiveProgram.Names.AddGetIndex("__qualname__");
        var qual_const_idx = ActiveProgram.Constants.AddGetIndex(className);

        ActiveProgram.AddInstruction(ByteCodes.LOAD_NAME, __name__idx, context);
        ActiveProgram.AddInstruction(ByteCodes.STORE_NAME, __module__idx, context);
        ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, qual_const_idx, context);
        ActiveProgram.AddInstruction(ByteCodes.STORE_NAME, __qualname__idx, context);

        // Okay, now set ourselves loose on the user-specified class body!
        base.VisitSuite(context.suite());

        // Self-insert returning None to be consistent with Python
        var return_none_idx = ActiveProgram.Constants.AddGetIndex(null);
        ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, return_none_idx, context);
        ActiveProgram.AddInstruction(ByteCodes.RETURN_VALUE, context);      // Return statement from generated function
        ActiveProgram = ProgramStack.Pop();

        // We'll replace an existing name if we have one because assholes may overwrite a function.
        int funcIndex = findFunctionIndex(className);
        if (funcIndex < 0)
        {
            funcIndex = ActiveProgram.Constants.AddGetIndex(newFunctionCode);
        }
        else
        {
            ActiveProgram.Constants[funcIndex] = newFunctionCode;
        }

        int nameIndex = findConstantIndex<string>(className);
        if (nameIndex < 0)
        {
            nameIndex = ActiveProgram.Constants.AddGetIndex(className);
        }

        //      >>> def def_constructor():
        //      ...   class Foo:
        //      ...     def __init__(self):
        //      ...       pass
        //      ...
        //      >>> dis(def_constructor)
        // 2     0 LOAD_BUILD_CLASS
        //       2 LOAD_CONST               1 (<code object Foo at 0x0000021BD59175D0, file "<stdin>", line 2>)
        //       4 LOAD_CONST               2 ('Foo')
        //       6 MAKE_FUNCTION            0
        //       8 LOAD_CONST               2 ('Foo')
        //     (xx LOAD_FAST                y1 (Subclass1))
        //     (xx LOAD_FAST                y2 (Subclass2))... etc
        //     (If no explicit subclass is given, those LOAD_FASTS don't happen)
        //      10 CALL_FUNCTION            2
        //      12 STORE_FAST               0 (Foo)
        //      14 LOAD_CONST               0 (None)
        //      16 RETURN_VALUE
        ActiveProgram.Code.AddByte((byte)ByteCodes.BUILD_CLASS);
        ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, funcIndex, context);
        ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, nameIndex, context);
        ActiveProgram.AddInstruction(ByteCodes.MAKE_FUNCTION, 0, context);
        ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, nameIndex, context);

        int subclasses = 0;
        if (context.arglist() != null)
        {
            if (context.arglist().ChildCount > 1)
            {
                throw new Exception("Only one subclass is supported right now.");
            }

            for (int i = 0; i < context.arglist().ChildCount; ++i)
            {
                generateLoadForVariable(context.arglist().argument(i).GetText(), context);
            }
            subclasses = context.arglist().ChildCount;
        }

        ActiveProgram.AddInstruction(ByteCodes.CALL_FUNCTION, 2 + subclasses, context);

        var idx = ActiveProgram.VarNames.AddGetIndex(className);
        ActiveProgram.AddInstruction(ByteCodes.STORE_FAST, idx, context);
        return null;
    }

    private void generateImport(string originalModuleName, string moduleAs, string[] moduleFromList, string[] moduleFromAliases, ParserRuleContext context)
    {
        // Munge on the name to determine import level. Eat up all leading dots as levels from which to import.
        string moduleName = originalModuleName.TrimStart('.');
        var moduleNameIndex = ActiveProgram.Names.AddGetIndex(moduleName);
        int importLevelInt = originalModuleName.Length - moduleName.Length;
        var importLevelConstIdx = ActiveProgram.Constants.AddGetIndex(PyInteger.Create(importLevelInt));

        int fromListIndex = -1;
        if (moduleFromList == null || moduleFromList.Length == 0)
        {
            fromListIndex = ActiveProgram.Constants.AddGetIndex(NoneType.Instance);
        }
        else
        {
            var moduleListPyStrings = new PyObject[moduleFromList.Length];
            for (int i = 0; i < moduleFromList.Length; ++i)
            {
                moduleListPyStrings[i] = PyString.Create(moduleFromList[i]);
            }
            var fromListTuple = PyTuple.Create(moduleListPyStrings);
            fromListIndex = ActiveProgram.Constants.AddGetIndex(fromListTuple);
        }

        ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, importLevelConstIdx, context);
        ActiveProgram.AddInstruction(ByteCodes.LOAD_CONST, fromListIndex, context);
        ActiveProgram.AddInstruction(ByteCodes.IMPORT_NAME, moduleNameIndex, context);

        // General imports not using import-from.
        // One do STORE_FAST if this isn't an import-from
        if (moduleFromList == null)
        {
            string aliasedName = moduleAs == null ? moduleName : moduleAs;
            var moduleNameFastIndex = ActiveProgram.VarNames.AddGetIndex(aliasedName);
            ActiveProgram.AddInstruction(ByteCodes.STORE_FAST, moduleNameFastIndex, context);
        }
        // Import-from code generation.
        //if(moduleFromList != null && moduleFromList.Length > 0)
        else if (moduleFromList[0] == "*")
        {
            ActiveProgram.AddInstruction(ByteCodes.IMPORT_STAR, context);
        }
        else
        {
            for (int fromIdx = 0; fromIdx < moduleFromList.Length; ++fromIdx)
            {
                var fromNameConstIdx = ActiveProgram.Constants.AddGetIndex(PyString.Create(moduleFromList[fromIdx]));
                ActiveProgram.AddInstruction(ByteCodes.IMPORT_FROM, fromNameConstIdx, context);
                var fromName = moduleFromList[fromIdx];
                if (moduleFromAliases != null && moduleFromAliases[fromIdx] != null)
                {
                    fromName = moduleFromAliases[fromIdx];
                }
                var fromNameFastStoreIdx = ActiveProgram.VarNames.AddGetIndex(fromName);
                ActiveProgram.AddInstruction(ByteCodes.STORE_FAST, fromNameFastStoreIdx, context);
            }

            // IMPORT_FROM Peeks the module without popping it so it can do multiple import-froms.
            // We nuke it off the stack here because we're done with it.
            ActiveProgram.AddInstruction(ByteCodes.POP_TOP, context);
        }
    }

    public override object VisitImport_name([NotNull] CloacaParser.Import_nameContext context)
    {
        // context.dotted_as_names().GetChild(2).GetText()
        // Every middle element is a comma between modules; skip
        var dotted_as_names = context.dotted_as_names();
        for (int import_i = 0; import_i < dotted_as_names.ChildCount; import_i += 2)
        {
            var importChild = dotted_as_names.GetChild(import_i);
            var moduleName = dotted_as_names.GetChild(import_i).GetText();
            var aliasedName = moduleName;
            if (importChild.ChildCount > 1)
            {
                // Oh, it's aliased. Throw that out and go a level deeper.
                moduleName = importChild.GetChild(0).GetText();
                aliasedName = importChild.GetChild(2).GetText();
            }
            generateImport(moduleName, aliasedName, null, null, context);
        }
        return null;
    }

    public override object VisitImport_from([NotNull] CloacaParser.Import_fromContext context)
    {
        // import_from: ('from' (('.' | '...')* dotted_name | ('.' | '...')+)
        // 'import'('*' | '(' import_as_names ')' | import_as_names));
        //
        // Look at the children:
        // [0]: from
        // [1+]: name to import (or lots of dots)
        // [-2]: import
        // [-1]: ... ignore that and poke import_as_names directly if you can, otherwise use it.

        // Gotta keep going until we run out of dots. If we never had any dots in the first place,
        // then the first index we used is the module from which to import.
        var moduleName = context.GetChild(1).GetText();
        for (int dotted_i = 2; context.GetChild(dotted_i).GetText().StartsWith("."); ++dotted_i)
        {
            moduleName += context.GetChild(dotted_i).GetText();
        }

        var fromNames = new List<string>();
        var asNames = new List<string>();
        var endOfChildren = context.children.Count;
        if (context.GetChild(endOfChildren - 1).GetText() == "*")
        {
            fromNames.Add("*");
        }
        else
        {
            var import_as_names = context.import_as_names();
            if (import_as_names.ChildCount > 0)
            {
                // Skip the commas so we do every-other child.
                for (int import_as_names_i = 0; import_as_names_i < import_as_names.ChildCount; import_as_names_i += 2)
                {
                    var import_as_name = import_as_names.GetChild(import_as_names_i);
                    if (import_as_name.ChildCount > 1)
                    {
                        fromNames.Add(import_as_name.GetChild(0).GetText());
                        asNames.Add(import_as_name.GetChild(2).GetText());
                    }
                    else
                    {
                        fromNames.Add(import_as_name.GetText());
                        asNames.Add(import_as_name.GetText());
                    }
                }
            }
            else
            {
                fromNames.Add(context.GetChild(endOfChildren - 1).GetText());
            }
        }

        generateImport(
            moduleName,
            null,
            fromNames.ToArray(),
            asNames.Count > 0 ? asNames.ToArray() : null,
            context);
        return null;
    }
}
