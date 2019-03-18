using System;
using System.Collections.Generic;

using Antlr4.Runtime.Misc;
using Antlr4.Runtime;

using Language;
using LanguageImplementation;
using CloacaInterpreter;

/// <summary>
/// Use to raise parsing issues while we figure out a better way to do this.
/// </summary>
public class ParseException : Exception
{
    public ParseException(string message) : base(message)
    {

    }
}

public class CloacaBytecodeVisitor : CloacaBaseVisitor<object>
{
    public CodeObjectBuilder RootProgram;
    private Stack<CodeObjectBuilder> ProgramStack;
    private CodeObjectBuilder ActiveProgram;

    public CloacaBytecodeVisitor()
    {
        RootProgram = new CodeObjectBuilder();
        ActiveProgram = RootProgram;
        ProgramStack = new Stack<CodeObjectBuilder>();
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

    /// <summary>
    /// Add an instruction to the end of the active program.
    /// </summary>
    /// <param name="opcode">The instruction opcode</param>
    /// <param name="data">Opcode data.</param>
    /// <returns>The index of the NEXT instruction in the program.</returns>
    private int AddInstruction(ByteCodes opcode, int data)
    {
        ActiveProgram.Code.AddByte((byte) opcode);
        ActiveProgram.Code.AddUShort(data);
        return ActiveProgram.Code.Count;
    }

    /// <summary>
    /// Add an instruction to the end of the active program.
    /// </summary>
    /// <param name="opcode">The instruction opcode</param>
    /// <returns>The index of the NEXT instruction in the program.</returns>
    private int AddInstruction(ByteCodes opcode)
    {
        ActiveProgram.Code.AddByte((byte)opcode);
        return ActiveProgram.Code.Count;
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
                AddInstruction(ByteCodes.BINARY_ADD);
            }
            else if (operatorTxt == "-")
            {
                AddInstruction(ByteCodes.BINARY_SUBTRACT);
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
        for(int child_i = 2; child_i < context.children.Count; child_i+=2)
        {
            Visit(context.children[child_i]);
            string operatorTxt = context.children[child_i - 1].GetText();
            if(operatorTxt == "*")
            {
                AddInstruction(ByteCodes.BINARY_MULTIPLY);
            }
            else if(operatorTxt == "/")
            {
                AddInstruction(ByteCodes.BINARY_DIVIDE);
            }
            else
            {
                throw new Exception("The Cloaca VisitTerm cannot generate code for term rule operator: " + operatorTxt + " yet");
            }
        }
        return null;
    }

    //public override object VisitParens([NotNull] CloacaParser.ParensContext context)
    //{
    //    return Visit(context.expr());
    //}

    // Return false if variable not found
    private void LoadVariable(RuleContext context)
    {
        var variableName = context.GetText();
        //var idx = ActiveProgram.ArgVarNames.IndexOf(variableName);
        //if (idx >= 0)
        //{
        //    AddInstruction(ByteCodes.LOAD_FAST, idx);
        //    return;
        //}

        var idx = ActiveProgram.VarNames.IndexOf(variableName);
        if (idx >= 0)
        {
            AddInstruction(ByteCodes.LOAD_FAST, idx);
            return;
        }

        throw new ParseException("Use of undeclared variable: " + variableName);
    }

    private void LoadConstantNumber(RuleContext context)
    {
        ActiveProgram.Constants.Add(ConstantsFactory.CreateNumber(context));
        AddInstruction(ByteCodes.LOAD_CONST, ActiveProgram.Constants.Count - 1);
    }

    public override object VisitAtomName([NotNull] CloacaParser.AtomNameContext context)
    {
        // It might be a function name. Look for it in names.
        int nameIdx = ActiveProgram.VarNames.IndexOf(context.GetText());
        if (nameIdx >= 0)
        {
            AddInstruction(ByteCodes.LOAD_FAST, nameIdx);
        }
        else
        {
            LoadVariable(context);
        }
        return null;
    }

    public override object VisitAtomString([NotNull] CloacaParser.AtomStringContext context)
    {
        ActiveProgram.Constants.Add(ConstantsFactory.CreateString(context));
        AddInstruction(ByteCodes.LOAD_CONST, ActiveProgram.Constants.Count - 1);
        return null;
    }

    public override object VisitAtomBool([NotNull] CloacaParser.AtomBoolContext context)
    {
        ActiveProgram.Constants.Add(ConstantsFactory.CreateBool(context));
        AddInstruction(ByteCodes.LOAD_CONST, ActiveProgram.Constants.Count - 1);
        return null;
    }

    public override object VisitAtomNoneType([NotNull] CloacaParser.AtomNoneTypeContext context)
    {
        ActiveProgram.Constants.Add(NoneType.Instance);
        AddInstruction(ByteCodes.LOAD_CONST, ActiveProgram.Constants.Count - 1);
        return null;
    }

    public override object VisitAtomNumber([NotNull] CloacaParser.AtomNumberContext context)
    {
        LoadConstantNumber(context);
        return null;
    }

    public override object VisitAtomWait([NotNull] CloacaParser.AtomWaitContext context)
    {
        AddInstruction(ByteCodes.WAIT);
        return null;
    }

    public override object VisitAtomParens([NotNull] CloacaParser.AtomParensContext context)
    {
        // For now, we're assuming an atom of parentheses is a tuple
        base.VisitAtomParens(context);

        if (context.testlist_comp().test().Length > 1)
        {
            AddInstruction(ByteCodes.BUILD_TUPLE, context.testlist_comp().test().Length);
        }
        return null;
    }

    public override object VisitAtomSquareBrackets([NotNull] CloacaParser.AtomSquareBracketsContext context)
    {
        // For now, we're assuming an atom of parentheses is a tuple
        base.VisitAtomSquareBrackets(context);
        AddInstruction(ByteCodes.BUILD_LIST, context.testlist_comp().test().Length);
        return null;
    }

    //public override object VisitAtomCurlyBrackets([NotNull] CloacaParser.AtomCurlyBracketsContext context)
    //{
    //    // Assuming a dictionary!
    //    return null;
    //}

    public override object VisitExpr_stmt([NotNull] CloacaParser.Expr_stmtContext context)
    {
        if(context.testlist_star_expr().Length > 2 || 
            (context.GetToken(CloacaParser.EQUAL, 0) == null && context.testlist_star_expr().Length == 2))
        {
            throw new Exception("Don't know how to evaluate an expr_stmt that isn't an assignment or wait statement");
        }

        // Single-statement (wait keyword)
        if(context.testlist_star_expr().Length == 1)
        {
            VisitLValueTestlist_star_expr(context.testlist_star_expr()[0]);
            return null;
        }

        // RValue is testlist_star_expr[1]
        // LValue is testlist_star_expr[0]
        // Traverse the right hand side to get the assignment value on to the data stack
        // Then go down a special LValue version of the visitors for storing it.
        Visit(context.testlist_star_expr()[1]);
        VisitLValueTestlist_star_expr(context.testlist_star_expr()[0]);

        return null;
        //return base.VisitExpr_stmt(context);
    }

    public object VisitLValueTestlist_star_expr([NotNull] CloacaParser.Testlist_star_exprContext context)
    {
        // Okay, take a deep breath. We're going to skip most of the crap and get straight to the lvalue
        // name. We have to go through the whole cascade defined in the grammar to get to the expr.
        // Arguably, we could keep going even deeper. We'll probably be refining this once it becomes more
        // obvious what other kind of lvalues we could be dealing with.

        // TODO: Experiment with creating a tree traverser here to try to walk down to the atom.
        var maybeAtom = context.test()[0].or_test()[0].and_test()[0].not_test()[0].comparison().expr()[0].
            xor_expr()[0].and_expr()[0].shift_expr()[0].arith_expr()[0].term()[0].factor()[0].power().atom_expr();
        var variableName = context.test()[0].or_test()[0].and_test()[0].not_test()[0].comparison().expr()[0].GetText();
        if (maybeAtom.trailer().Length > 0)
        {
            // Is it subscriptable?
            if (maybeAtom.trailer()[0].subscriptlist() != null)
            {
                // Order to push on stack: assignment value (should already be specified before we got here), container, index
                variableName = maybeAtom.atom().GetText();
                var idx = ActiveProgram.VarNames.IndexOf(variableName);
                AddInstruction(ByteCodes.LOAD_FAST, idx);
                base.VisitSubscriptlist(maybeAtom.trailer()[0].subscriptlist());
                AddInstruction(ByteCodes.STORE_SUBSCR);
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
                AddInstruction(ByteCodes.WAIT);
                return null;
            }

            // Local (store fast)
            var idx = ActiveProgram.VarNames.IndexOf(variableName);
            if (idx < 0)
            {
                ActiveProgram.VarNames.Add(variableName);
                idx = ActiveProgram.VarNames.Count - 1;
            }
            AddInstruction(ByteCodes.STORE_FAST, idx);
        }
        return null;
    }

    public override object VisitProgram([NotNull] CloacaParser.ProgramContext context)
    {
        for(int i = 0; i < context.stmt().Length; ++i)
        {
            base.VisitStmt(context.stmt(i));
        }
        return null;
    }

    public override object VisitIf_stmt([NotNull] CloacaParser.If_stmtContext context)
    {
        List<int> conditional_block_fixups = new List<int>();
        int if_cond_i = 0;
        for (if_cond_i = 0; if_cond_i < context.test().Length; ++if_cond_i)
        {
            var comparison = context.test(if_cond_i);
            Visit(comparison);
            AddInstruction(ByteCodes.JUMP_IF_FALSE, 0xFFFF);
            var jump_opcode_index = ActiveProgram.Code.Count - 2;
            Visit(context.suite(if_cond_i));

            // We'll need this to skip other conditional blocks, but we only need this if we actually
            // have other ones:
            if (context.test().Length > 1)
            {
                conditional_block_fixups.Add(AddInstruction(ByteCodes.JUMP_FORWARD, 0xFFFF) - 2);
            }
            ActiveProgram.Code.SetUShort(jump_opcode_index, ActiveProgram.Code.Count);
        }

        // Handles the 'else' clause if we have one. The else is a suite without a comparison.
        if (context.suite().Length > if_cond_i + 1)
        {
            var jump_opcode_index = ActiveProgram.Code.Count - 1;
            Visit(context.suite(if_cond_i + 1));
            ActiveProgram.Code.SetUShort(jump_opcode_index, ActiveProgram.Code.Count);
        }

        // Fixup any forward jumps we might have. They should all come to our current program position.
        foreach (int fixupPosition in conditional_block_fixups)
        {
            ActiveProgram.Code.SetUShort(fixupPosition, ActiveProgram.Code.Count - fixupPosition - 2);
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
        var loop_fixup = AddInstruction(ByteCodes.SETUP_LOOP, 0xFFFF) - 2;

        Visit(context.test());
        int pop_jump_fixup = AddInstruction(ByteCodes.JUMP_IF_FALSE, 0xFFFF) - 2;               // Another one to fixup

        Visit(context.suite(0));

        AddInstruction(ByteCodes.JUMP_ABSOLUTE, loop_fixup + 2);
        int pop_block_i = AddInstruction(ByteCodes.POP_BLOCK) - 1;
        ActiveProgram.Code.SetUShort(pop_jump_fixup, pop_block_i);

        // Else clause? We will have two suites.
        if (context.suite().Length > 1)
        {
            Visit(context.suite(1));
        }

        // We're outside the loop block so we use this for fixups
        int out_of_loop = ActiveProgram.Code.Count;
        ActiveProgram.Code.SetUShort(loop_fixup, out_of_loop - loop_fixup);

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

        int nameIndex = findIndex<string>(funcName);
        if(nameIndex < 0)
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
        AddInstruction(ByteCodes.RETURN_VALUE);      // Return statement from generated function

        // This should restore us back to the original function with which we started.
        ActiveProgram = ProgramStack.Pop();

        // We don't support any additional flags yet.       
        AddInstruction(ByteCodes.LOAD_CONST, funcIndex);
        AddInstruction(ByteCodes.LOAD_CONST, nameIndex);
        AddInstruction(ByteCodes.MAKE_FUNCTION, 0);

        ActiveProgram.VarNames.Add(funcName);
        AddInstruction(ByteCodes.STORE_FAST, ActiveProgram.VarNames.Count-1);
        return null;
    }

    public override object VisitSubscriptlist([NotNull] CloacaParser.SubscriptlistContext context)
    {
        base.VisitSubscriptlist(context);
        AddInstruction(ByteCodes.BINARY_SUBSCR);
        return null;
    }

    public override object VisitTfpdef([NotNull] CloacaParser.TfpdefContext context)
    {
        var variableName = context.NAME().GetText();

        ActiveProgram.ArgVarNames.Add(variableName);
        ActiveProgram.VarNames.Add(variableName);

        return null;
    }

    private int findIndex<T>(T constant) where T:class
    {
        for(int i = 0; i < ActiveProgram.Constants.Count; ++i)
        {
            if(ActiveProgram.Constants[i] is T)
            {
                var asT = ActiveProgram.Constants[i] as T;
                if(constant == asT)
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
        // This is a little rough, but we'll assume for now that if there are any trailers
        // defined that we're working with a function. If no trailer is defined, then we'll
        // pass it along to the atom visitor.
        if (context.trailer().Length > 0)
        {
            // Get the function name loaded on the stack first!
            Visit(context.atom());

            // A function that doesn't take any arguments doesn't have an arglist, but that is what 
            // got triggered. The only way I know to make sure we trigger on it is to see if we match
            // parentheses. There has to be a better way...
            if (context.trailer(0).arglist() != null || context.trailer(0).GetText() == "()")
            {
                int argIdx = 0;
                for (argIdx = 0; context.trailer(0).arglist() != null &&
                    context.trailer(0).arglist().argument(argIdx) != null; ++argIdx)
                {
                    base.Visit(context.trailer(0).arglist().argument(argIdx));
                }
                AddInstruction(ByteCodes.CALL_FUNCTION, argIdx);
            }
            else
            {
                foreach(var trailer in context.trailer())
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

    // TODO: Consider switching to same method that VisitAtomParens is using here, which might make this general-purpose enough.
    public override object VisitDictorsetmaker([NotNull] CloacaParser.DictorsetmakerContext context)
    {
        // For now, we are assuming we're building simple dictionaries! We're not doing sets, nor
        // are the elements complex statements (yet).
        foreach (var test in context.test())
        {
            Visit(test);
        }
        AddInstruction(ByteCodes.BUILD_MAP, context.test().Length / 2);
        return null;
    }

    //public override object VisitAtom_expr([NotNull] CloacaParser.Atom_exprContext context)
    //{
    //    // Calling a function--I hope.
    //    var funcname = context.atom().NAME().GetText();

    //    var funcIdx = findFunctionIndex(funcname);
    //    if(funcIdx < 0)
    //    {
    //        throw new Exception("Unknown function: " + funcname);
    //    }

    //    AddInstruction(ByteCodes.LOAD_NAME, funcIdx);

    //    // TODO: Expand argument to be any kind of statement since people can run all kinds of code as arguments to a function
    //    int argIdx = 0;
    //    if (context.trailer(0).arglist() != null)
    //    {
    //        for (argIdx = 0; context.trailer(0).arglist().argument(argIdx) != null; ++argIdx)
    //        {
    //            base.Visit(context.trailer(0).arglist().argument(argIdx));
    //        }
    //    }

    //    AddInstruction(ByteCodes.CALL_FUNCTION, argIdx);
    //    return null;
    //}

    //public override object VisitArgument([NotNull] CloacaParser.ArgumentContext context)
    //{
    //    if (context.NAME() != null)
    //    {
    //        LoadVariable(context);
    //    }
    //    else
    //    {
    //        LoadConstantNumber(context);
    //    }
    //    return null;
    //}

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
                ActiveProgram.Code.AddUShort((ushort)CompareOps.Gte);
                break;
            case CloacaParser.COMP_OP_LTE:
                ActiveProgram.Code.AddByte((byte)ByteCodes.COMPARE_OP);
                ActiveProgram.Code.AddUShort((ushort)CompareOps.Lte);
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
        var className = context.NAME().GetText();

        // Load a dummy constructor for now.
        CodeObject constructor = new CodeObject(new byte[0]);
        constructor.Name = className;

        // We don't recognize the arglist yet (inheritance) for the class
        ActiveProgram.Code.AddByte((byte)ByteCodes.BUILD_CLASS);

        ActiveProgram.Constants.Add(constructor);
        AddInstruction(ByteCodes.LOAD_CONST, ActiveProgram.Constants.Count - 1);

        ActiveProgram.Constants.Add(className);
        AddInstruction(ByteCodes.LOAD_CONST, ActiveProgram.Constants.Count - 1);

        AddInstruction(ByteCodes.MAKE_FUNCTION, 0);

        AddInstruction(ByteCodes.LOAD_CONST, ActiveProgram.Constants.Count - 1);
        AddInstruction(ByteCodes.CALL_FUNCTION, 2);

        ActiveProgram.VarNames.Add(className);
        AddInstruction(ByteCodes.STORE_FAST, ActiveProgram.VarNames.Count - 1);

        return null;
    }

}

