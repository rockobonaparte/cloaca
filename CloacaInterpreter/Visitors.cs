using System;
using System.Collections.Generic;

using Antlr4.Runtime.Misc;
using Antlr4.Runtime;

using Language;
using LanguageImplementation;
using LanguageImplementation.DataTypes;
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
        return AddInstruction(ActiveProgram.Code, opcode, data);
    }

    /// <summary>
    /// Add an instruction to the end of the given program.
    /// </summary>
    /// <param name="builder">The code builder to which to add the instruction.</param>
    /// <param name="opcode">The instruction opcode</param>
    /// <param name="data">Opcode data.</param>
    /// <returns>The index of the NEXT instruction in the program.</returns>
    private int AddInstruction(CodeBuilder builder, ByteCodes opcode, int data)
    {
        builder.AddByte((byte)opcode);
        builder.AddUShort(data);
        return ActiveProgram.Code.Count;
    }

    /// <summary>
    /// Add an instruction to the end of the active program.
    /// </summary>
    /// <param name="opcode">The instruction opcode</param>
    /// <returns>The index of the NEXT instruction in the program.</returns>
    private int AddInstruction(ByteCodes opcode)
    {
        return AddInstruction(ActiveProgram.Code, opcode);
    }


    /// <summary>
    /// Add an instruction to the end of the given program.
    /// </summary>
    /// <param name="builder">The code builder to which to add the instruction.</param>
    /// <param name="opcode">The instruction opcode</param>
    /// <returns>The index of the NEXT instruction in the program.</returns>
    private int AddInstruction(CodeBuilder builder, ByteCodes opcode)
    {
        builder.AddByte((byte)opcode);
        return ActiveProgram.Code.Count;
    }

    private void generateLoadForVariable(string variableName)
    {
        // If it's in VarNames, we use it from there. If not, 
        // we assume it's global and deal with it at run time if
        // we can't find it.
        var idx = ActiveProgram.VarNames.IndexOf(variableName);
        if (idx >= 0)
        {
            AddInstruction(ByteCodes.LOAD_FAST, idx);
            return;
        }

        var nameIdx = ActiveProgram.Names.IndexOf(variableName);
        if(nameIdx >= 0)
        {
            AddInstruction(ByteCodes.LOAD_GLOBAL, nameIdx);
            return;
        }
        else
        {
            ActiveProgram.Names.Add(variableName);
            AddInstruction(ByteCodes.LOAD_GLOBAL, ActiveProgram.Names.Count - 1);
            return;
        }
    }

    private void generateStoreForVariable(string variableName)
    {
        var nameIdx = ActiveProgram.Names.IndexOf(variableName);
        if (nameIdx >= 0)
        {
            AddInstruction(ByteCodes.STORE_GLOBAL, nameIdx);
        }
        else
        {
            var idx = ActiveProgram.VarNames.IndexOf(variableName);
            if (idx >= 0)
            {
                AddInstruction(ByteCodes.STORE_FAST, idx);
            }
            else
            {
                ActiveProgram.VarNames.Add(variableName);
                idx = ActiveProgram.VarNames.Count - 1;
                AddInstruction(ByteCodes.STORE_FAST, idx);
            }
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
            generateLoadForVariable(context.GetText());
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

    public override object VisitRaise_stmt([NotNull] CloacaParser.Raise_stmtContext context)
    {
        // This will build up the exception and put it on the stack.
        // TODO: Support 'from' statement by expanding to test(1) as well--if defined.
        base.VisitTest(context.test(0));

        // For now, we only support one argument for exceptions, which will be the exception
        // created from visit the parent context.
        AddInstruction(ByteCodes.RAISE_VARARGS, 1);

        return null;
    }

    public override object VisitGlobal_stmt([NotNull] CloacaParser.Global_stmtContext context)
    {
        for(int name_i = 0; name_i < context.NAME().Length; ++name_i)
        {
            var name = context.NAME(name_i).GetText();
            if (ActiveProgram.Names.IndexOf(name) < 0)
            {
                ActiveProgram.Names.Add(name);
            }
        }
        return null;
    }

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
                generateLoadForVariable(variableName);
                base.VisitSubscriptlist(maybeAtom.trailer()[0].subscriptlist());
                AddInstruction(ByteCodes.STORE_SUBSCR);
            }
            // Object subscript (self.x)
            else if(maybeAtom.trailer().Length == 1 && maybeAtom.trailer()[0].NAME() != null)
            {
                variableName = maybeAtom.atom().GetText();
                generateLoadForVariable(variableName);

                var attrName = maybeAtom.trailer()[0].NAME().GetText();
                var attrIdx = ActiveProgram.Names.AddGetIndex(attrName);
                AddInstruction(ByteCodes.STORE_ATTR, attrIdx);
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
                AddInstruction(ByteCodes.WAIT);
                return null;
            }

            // Store value
            generateStoreForVariable(variableName);
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
        var conditional_block_fixups = new List<JumpOpcodeFixer>();
        int if_cond_i = 0;
        for (if_cond_i = 0; if_cond_i < context.test().Length; ++if_cond_i)
        {
            var comparison = context.test(if_cond_i);
            Visit(comparison);
            var jumpFalseSkip = new JumpOpcodeFixer(ActiveProgram.Code, AddInstruction(ByteCodes.JUMP_IF_FALSE, -1));
            Visit(context.suite(if_cond_i));

            // We'll need this to skip other conditional blocks, but we only need this if we actually
            // have other ones:
            if (context.test().Length > 1)
            {
                conditional_block_fixups.Add(new JumpOpcodeFixer(ActiveProgram.Code, AddInstruction(ByteCodes.JUMP_FORWARD, -1)));
            }
            jumpFalseSkip.FixupAbsolute(ActiveProgram.Code.Count);
        }

        // Handles the 'else' clause if we have one. The else is a suite without a comparison.
        if (context.suite().Length > if_cond_i + 1)
        {
            var jump_opcode_index = ActiveProgram.Code.Count - 1;
            Visit(context.suite(if_cond_i + 1));
            ActiveProgram.Code.SetUShort(jump_opcode_index, ActiveProgram.Code.Count);
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
        var setupLoopIdx = AddInstruction(ByteCodes.SETUP_LOOP, -1);
        var setupLoopFixup = new JumpOpcodeFixer(ActiveProgram.Code, setupLoopIdx);

        Visit(context.test());
        var pop_jump_fixup = new JumpOpcodeFixer(ActiveProgram.Code, AddInstruction(ByteCodes.JUMP_IF_FALSE, -1));

        Visit(context.suite(0));

        AddInstruction(ByteCodes.JUMP_ABSOLUTE, setupLoopIdx);
        int pop_block_i = AddInstruction(ByteCodes.POP_BLOCK) - 1;
        pop_jump_fixup.FixupAbsolute(pop_block_i);

        // Else clause? We will have two suites.
        if (context.suite().Length > 1)
        {
            Visit(context.suite(1));
        }

        setupLoopFixup.Fixup(ActiveProgram.Code.Count);

        return null;
    }

    /// <summary>
    /// Finds the first occurrance of the given text in the context's children.
    /// </summary>
    /// <param name="children"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    private int getFirstIndexOfText(IList<Antlr4.Runtime.Tree.IParseTree> children, string text)
    {
        for(int foundIdx = 0; foundIdx < children.Count; ++foundIdx)
        {
            if(children[foundIdx].GetText() == text)
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
            finallyTarget.Add(AddInstruction(ByteCodes.SETUP_FINALLY, -1));
        }

        hasElse = hasText(context.children, "else");
        hasExcept = context.except_clause().Length > 0;

        // Try block preamble. If there are exceptions, then we need a SETUP_EXCEPT position.
        var setupExceptTarget = new JumpOpcodeFixer(ActiveProgram.Code);
        if (hasExcept)
        {
            setupExceptTarget.Add(AddInstruction(ByteCodes.SETUP_EXCEPT, -1));
        }

        int suiteIdx = 0;
        Visit(context.suite(suiteIdx));
        ++suiteIdx;
        AddInstruction(ByteCodes.POP_BLOCK);
        var endOfTryJumpTarget = new JumpOpcodeFixer(ActiveProgram.Code, AddInstruction(ByteCodes.JUMP_FORWARD, -1));

        // Start of except statements
        var endOfExceptBlockJumpFixups = new List<JumpOpcodeFixer>();
        var finallyOffsets = new List<JumpOpcodeFixer>();
        int startOfExceptBlocks = ActiveProgram.Code.Count;
        foreach (var exceptClause in context.except_clause())
        {
            // Making a closure to represent visiting the Except_Clause. It's not a dedicated override of the default in the rule
            // because we need so much context from the entire try block
            {
                if (exceptClause.test() != null && exceptClause.test().ChildCount > 0)
                {
                    // If the exception is aliased, we need to make sure we still have a copy
                    // of it to store in the alias AFTER we have determined that we want to
                    // enter its except clause. So we'll duplicate it here, then test, then store it.
                    if (exceptClause.NAME() != null)
                    {
                        AddInstruction(ByteCodes.DUP_TOP);
                    }

                    generateLoadForVariable(exceptClause.test().GetText());
                    AddInstruction(ByteCodes.COMPARE_OP, (ushort)CompareOps.ExceptionMatch);

                    // Point to END_FINALLY to get us out of the except clause and into the finally block
                    finallyOffsets.Add(new JumpOpcodeFixer(ActiveProgram.Code, AddInstruction(ByteCodes.POP_JUMP_IF_FALSE, -1)));
                    AddInstruction(ByteCodes.POP_TOP);      // should pop the true/false from COMPARE_OP

                    if (exceptClause.NAME() != null)
                    {
                        // BTW, this pops the exception
                        generateStoreForVariable(exceptClause.NAME().GetText());
                    }
                }
            }

            Visit(context.suite(suiteIdx));
            ++suiteIdx;

            // TODO: Look into deleting aliased exceptions.
            // A DELETE_FAST was done for an aliased exception in an auto-generated END_FINALLY clause
            // Look at Python generation for TryExceptAliasBasic
            endOfExceptBlockJumpFixups.Add(new JumpOpcodeFixer(ActiveProgram.Code, AddInstruction(ByteCodes.JUMP_FORWARD, -1)));
        }

        // else block
        int startOfElseBlock = ActiveProgram.Code.Count;
        if(hasElse)
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
            AddInstruction(ByteCodes.END_FINALLY);
        }

        int endOfBlockPosition = hasFinally ? startOfFinallyBlock : ActiveProgram.Code.Count;
        endOfBlockPosition = hasElse ? startOfElseBlock : endOfBlockPosition;

        // Try block fixups
        endOfTryJumpTarget.Fixup(endOfBlockPosition);

        // Except statement fixups
        if (hasExcept)
        {
            setupExceptTarget.Fixup(startOfExceptBlocks);
            foreach (var exceptJumpOutFixup in endOfExceptBlockJumpFixups)
            {
                exceptJumpOutFixup.Fixup(endOfBlockPosition);
            }
        }

        // Finally statement fixups
        foreach (var finallyFixup in finallyOffsets)
        {
            finallyFixup.Fixup(endOfBlockPosition);
        }

        //// TODO: Investigate correctness of this END_FINALLY emitter. Looks like it's necessary to set up an END_FINALLY if none of our except clauses trigger and we don't have a finally statement either.
        //// End block. If we have a finally, we end out dumping two of these. It looks like we want one either wait (if we had an except). Dunno
        //// about try-else. Is that even legal?
        //if (!hasFinally)
        //{
        //    AddInstruction(ByteCodes.END_FINALLY);
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

        // TODO: Apparently sometimes (class methods) we need to store this using STORE_NAME. Why?
        // Class declarations need all their functions declared using STORE_NAME. I'm not sure why yet. I am speculating that it's 
        // more proper to say that *everything* needs STORE_NAME by default but we're able to optimize it in just about every other
        // case. I don't have a full grasp on namespaces yet. So we're going to do something *very cargo cult* and hacky and just 
        // decide that if our parent context is a class definition that we'll use a STORE_NAME here.
        if(context.Parent.Parent.Parent.Parent is CloacaParser.ClassdefContext)
        {
            var nameIdx = ActiveProgram.Names.AddReplaceGetIndex(funcName);
            AddInstruction(ByteCodes.STORE_NAME, nameIdx);
        }
        else
        {
            ActiveProgram.VarNames.Add(funcName);
            AddInstruction(ByteCodes.STORE_FAST, ActiveProgram.VarNames.Count - 1);
        }
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
                    AddInstruction(ByteCodes.LOAD_ATTR, attrIdx);

                    // The rest of this block determines if we need to inject self as an argument to a method.
                    //
                    // If it's a function (next trailer is parentheses) that starts with a dot, then it's a class
                    // method and we have to insert the variable represent self. If the previous trailer was a function
                    // call, then it implicitly already put it on the stack, so we skip those.
                    // That's unless it's already been put on the stack as an output from a previous function call.
                    RuleContext priorToken = context.atom();
                    if (trailer_i > 0)
                    {
                        priorToken = context.trailer(trailer_i - 1);
                    }
                   
                    if (trailer_i < context.trailer().Length - 1
                        && !priorToken.GetText().StartsWith("(")
                        && context.trailer(trailer_i+1).GetText().StartsWith("(")
                        && trailer.GetText().StartsWith("."))
                    {
                        var variableName = priorToken.GetText();
                        var selfIdx = ActiveProgram.VarNames.IndexOf(variableName);
                        if (selfIdx < 0)
                        {
                            throw new IndexOutOfRangeException("Could not find self reference for class instance '"
                                + variableName + "'");
                        }
                        AddInstruction(ByteCodes.LOAD_FAST, selfIdx);
                    }
                }
                // A function that doesn't take any arguments doesn't have an arglist, but that is what 
                // got triggered. The only way I know to make sure we trigger on it is to see if we match
                // parentheses. There has to be a better way...
                else if (trailer.arglist() != null || trailer.GetText() == "()")
                {
                    int argIdx = 0;
                    for (argIdx = 0; trailer.arglist() != null &&
                        trailer.arglist().argument(argIdx) != null; ++argIdx)
                    {
                        base.Visit(trailer.arglist().argument(argIdx));
                    }

                    // If it's a method, we have to add on one more argument to represent
                    // the self reference. We'll do this by looking backwards once and checking
                    // for a dot in the preceding term.
                    var numArgs = argIdx;
                    if(trailer_i > 0 && context.trailer(trailer_i-1).GetText().StartsWith("."))
                    {
                        numArgs += 1;
                    }
                    AddInstruction(ByteCodes.CALL_FUNCTION, numArgs);
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
        AddInstruction(ByteCodes.BUILD_MAP, context.test().Length / 2);
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

        AddInstruction(ByteCodes.LOAD_NAME, __name__idx);
        AddInstruction(ByteCodes.STORE_NAME, __module__idx);
        AddInstruction(ByteCodes.LOAD_CONST, qual_const_idx);
        AddInstruction(ByteCodes.STORE_NAME, __qualname__idx);

        // Okay, now set ourselves loose on the user-specified class body!
        base.VisitSuite(context.suite());

        // Self-insert returning None to be consistent with Python
        var return_none_idx = ActiveProgram.Constants.AddGetIndex(null);
        AddInstruction(ByteCodes.LOAD_CONST, return_none_idx);
        AddInstruction(ByteCodes.RETURN_VALUE);      // Return statement from generated function
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

        int nameIndex = findIndex<string>(className);
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
        AddInstruction(ByteCodes.LOAD_CONST, funcIndex);
        AddInstruction(ByteCodes.LOAD_CONST, nameIndex);
        AddInstruction(ByteCodes.MAKE_FUNCTION, 0);
        AddInstruction(ByteCodes.LOAD_CONST, nameIndex);

        int subclasses = 0;
        if (context.arglist() != null)
        {
            if (context.arglist().ChildCount > 1)
            {
                throw new Exception("Only one subclass is supported right now.");
            }

            for(int i = 0; i < context.arglist().ChildCount; ++i)
            {
                generateLoadForVariable(context.arglist().argument(i).GetText());
            }
            subclasses = context.arglist().ChildCount;
        }

        AddInstruction(ByteCodes.CALL_FUNCTION, 2 + subclasses);

        ActiveProgram.VarNames.Add(className);
        AddInstruction(ByteCodes.STORE_FAST, ActiveProgram.VarNames.Count - 1);
        return null;
    }
}

