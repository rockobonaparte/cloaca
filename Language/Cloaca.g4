// Original inspiration: https://github.com/antlr/grammars-v4/blob/master/python3-cs/Python3.g4

grammar Cloaca;

tokens { INDENT, DEDENT }

@lexer::header{
using System.Linq;
using System.Text.RegularExpressions;
}

@lexer::members {
	// A queue where extra tokens are pushed on (see the NEWLINE lexer rule).
	private System.Collections.Generic.LinkedList<IToken> Tokens = new System.Collections.Generic.LinkedList<IToken>();
	// The stack that keeps track of the indentation level.
	private System.Collections.Generic.Stack<int> Indents = new System.Collections.Generic.Stack<int>();
	// The amount of opened braces, brackets and parenthesis.
	private int Opened = 0;
	// The most recently produced token.
	private IToken LastToken = null;

	public override void Emit(IToken token)
	{
	    base.Token = token;
	    Tokens.AddLast(token);
	}

	private CommonToken CommonToken(int type, string text)
	{
	    int stop = CharIndex - 1;
	    int start = text.Length == 0 ? stop : stop - text.Length + 1;
	    return new CommonToken(this._tokenFactorySourcePair, type, DefaultTokenChannel, start, stop);
	}

	private IToken CreateDedent()
	{
	    var dedent = CommonToken(CloacaParser.DEDENT, "");
	    dedent.Line = LastToken.Line;
	    return dedent;
	}

	public override IToken NextToken()
	{
	    // Check if the end-of-file is ahead and there are still some DEDENTS expected.
	    if (_input.La(1) == Eof && Indents.Count != 0)
	    {
            // Remove any trailing EOF tokens from our buffer.
            for (var node  = Tokens.First; node != null; )
            {
                var temp = node.Next;
                if (node.Value.Type == Eof)
                {
                    Tokens.Remove(node);
                }
                node = temp;
            }
            
            // First emit an extra line break that serves as the end of the statement.
            this.Emit(CommonToken(CloacaParser.NEWLINE, "\n"));

	        // Now emit as much DEDENT tokens as needed.
	        while (Indents.Count != 0)
	        {
	            Emit(CreateDedent());
	            Indents.Pop();
	        }

	        // Put the EOF back on the token stream.
	        Emit(CommonToken(CloacaParser.Eof, "<EOF>"));
	    }

	    var next = base.NextToken();
	    if (next.Channel == DefaultTokenChannel)
	    {
	        // Keep track of the last token on the default channel.
	        LastToken = next;
	    }

	    if (Tokens.Count == 0)
	    {
	        return next;
	    }
	    else
	    {
	        var x = Tokens.First.Value;
	        Tokens.RemoveFirst();
	        return x;
	    }
	}

    // Calculates the indentation of the provided spaces, taking the
    // following rules into account:
    //
    // "Tabs are replaced (from left to right) by one to eight spaces
    //  such that the total number of characters up to and including
    //  the replacement is a multiple of eight [...]"
    //
    //  -- https://docs.python.org/3.1/reference/lexical_analysis.html#indentation
    static int GetIndentationCount(string spaces)
    {
        int count = 0;
        foreach (char ch in spaces.ToCharArray())
        {
            count += ch == '\t' ? 8 - (count % 8) : 1;
        }
        return count;
    }

    bool AtStartOfInput()
    {
        return Column == 0 && Line == 1;
    }
}

single_input: NEWLINE | simple_stmt | compound_stmt NEWLINE;
file_input: (NEWLINE | stmt)* EOF;
eval_input: testlist NEWLINE* EOF;

decorator: '@' dotted_name ( '(' (arglist)? ')' )? NEWLINE;
decorators: decorator+;
decorated: decorators (classdef | funcdef | async_funcdef);

async_funcdef	: ASYNC funcdef;
funcdef			: 'def' NAME parameters ('->' test)? ':' suite;

parameters		: '(' (typedargslist)? ')';

// There's a lot of test blocks here that we're blowing off.
typedargslist	: (tfpdef (',' tfpdef)* (',' ('*' (tfpdef)? (',' tfpdef)* (',' ('**' tfpdef (',')?)?)?
				| '**' tfpdef (',')?)?)?
				| '*' (tfpdef)? (',' tfpdef)* (',' ('**' tfpdef (',')?)?)?
				| '**' tfpdef (',')?);
tfpdef: NAME (':' test)?;
varargslist: (vfpdef ('=' test)? (',' vfpdef ('=' test)?)* (',' (
        '*' (vfpdef)? (',' vfpdef ('=' test)?)* (',' ('**' vfpdef (',')?)?)?
      | '**' vfpdef (',')?)?)?
  | '*' (vfpdef)? (',' vfpdef ('=' test)?)* (',' ('**' vfpdef (',')?)?)?
  | '**' vfpdef (',')?
);
vfpdef: NAME;

stmt: simple_stmt | compound_stmt;
simple_stmt: small_stmt (';' small_stmt)* (';')? NEWLINE;
small_stmt: (expr_stmt | del_stmt | pass_stmt | flow_stmt |
             import_stmt | global_stmt | nonlocal_stmt | assert_stmt);
expr_stmt: testlist_star_expr (annassign | augassign (yield_expr|testlist) |
                     ('=' (yield_expr|testlist_star_expr))*);

annassign: ':' test ('=' test)?;
testlist_star_expr: (test|star_expr) (',' (test|star_expr))* (',')?;
augassign: ('+=' | '-=' | '*=' | '@=' | '/=' | '%=' | '&=' | '|=' | '^=' |
            '<<=' | '>>=' | '**=' | '//=');
// For normal and annotated assignments, additional restrictions enforced by the interpreter
del_stmt: 'del' exprlist;
pass_stmt: 'pass';
flow_stmt: break_stmt | continue_stmt | return_stmt | raise_stmt | yield_stmt;
break_stmt: 'break';
continue_stmt: 'continue';
return_stmt: 'return' (testlist)?;
yield_stmt: yield_expr;
raise_stmt: 'raise' (test ('from' test)?)?;
import_stmt: import_name | import_from;
import_name: 'import' dotted_as_names;
// note below: the ('.' | '...') is necessary because '...' is tokenized as ELLIPSIS
import_from: ('from' (('.' | '...')* dotted_name | ('.' | '...')+)
              'import' ('*' | '(' import_as_names ')' | import_as_names));
import_as_name: NAME ('as' NAME)?;
dotted_as_name: dotted_name ('as' NAME)?;
import_as_names: import_as_name (',' import_as_name)* (',')?;
dotted_as_names: dotted_as_name (',' dotted_as_name)*;
dotted_name: NAME ('.' NAME)*;
global_stmt: 'global' NAME (',' NAME)*;
nonlocal_stmt: 'nonlocal' NAME (',' NAME)*;
assert_stmt: 'assert' test (',' test)?;

compound_stmt: if_stmt | while_stmt | for_stmt | try_stmt | with_stmt | funcdef | classdef | decorated | async_stmt;
async_stmt: ASYNC (funcdef | with_stmt | for_stmt);
if_stmt: 'if' test ':' suite ('elif' test ':' suite)* ('else' ':' suite)?;
while_stmt: 'while' test ':' suite ('else' ':' suite)?;
for_stmt: 'for' exprlist 'in' testlist ':' suite ('else' ':' suite)?;
try_stmt: ('try' ':' suite
           ((except_clause ':' suite)+
            ('else' ':' suite)?
            ('finally' ':' suite)? |
           'finally' ':' suite));
with_stmt: 'with' with_item (',' with_item)*  ':' suite;
with_item: test ('as' expr)?;
// NB compile.c makes sure that the default except clause is last
except_clause: 'except' (test ('as' NAME)?)?;
suite: simple_stmt | NEWLINE INDENT stmt+ DEDENT;

test: or_test ('if' or_test 'else' test)? | lambdef;
test_nocond: or_test | lambdef_nocond;
lambdef: 'lambda' (varargslist)? ':' test;
lambdef_nocond: 'lambda' (varargslist)? ':' test_nocond;
or_test: and_test ('or' and_test)*;
and_test: not_test ('and' not_test)*;
not_test: 'not' not_test | comparison;
comparison: expr (comp_op expr)*;

// <> isn't actually a valid comparison operator in Python. It's here for the
// sake of a __future__ import described in PEP 401 (which really works :-)
//comp_op: '<'|'>'|'=='|'>='|'<='|'<>'|'!='|'in'|'not' 'in'|'is'|'is' 'not';
comp_op		: op=(COMP_OP_LT | COMP_OP_GT | COMP_OP_EQ | COMP_OP_GTE | COMP_OP_LTE | 
			  COMP_OP_LTGT | COMP_OP_NE | COMP_OP_IN | COMP_OP_NOT_IN | COMP_OP_IS |
			  COMP_OP_IS_NOT);
star_expr: '*' expr;
expr: xor_expr ('|' xor_expr)*;
xor_expr: and_expr ('^' and_expr)*;
and_expr: shift_expr ('&' shift_expr)*;
shift_expr: arith_expr (('<<'|'>>') arith_expr)*;
arith_expr: term (('+'|'-') term)*;
term: factor (('*'|'@'|'/'|'%'|'//') factor)*;

factor: ('+'|'-'|'~') factor | power;
power: atom_expr ('**' factor)?;
atom_expr: (AWAIT)? atom trailer*;
atom: '(' (yield_expr|testlist_comp)? ')'  		# AtomParens			
    |  '[' (testlist_comp)? ']'                 # AtomSquareBrackets    
    |  '{' (dictorsetmaker)? '}'                # AtomCurlyBrackets     
    |  WAIT                                     # AtomWait              
	|  NAME                                     # AtomName              
	|  NUMBER                                   # AtomNumber            
	|  STRING+                                  # AtomString            
	|  '...'                                    # AtomDots              
	|  'None'                                   # AtomNoneType          
	|  ('True' | 'False')                       # AtomBool
	;
testlist_comp: (test|star_expr) ( comp_for | (',' (test|star_expr))* (',')? );
trailer: '(' (arglist)? ')' | '[' subscriptlist ']' | '.' NAME;
subscriptlist: subscript (',' subscript)* (',')?;
subscript: test | (test)? ':' (test)? (sliceop)?;
sliceop: ':' (test)?;
exprlist: (expr|star_expr) (',' (expr|star_expr))* (',')?;
testlist: test (',' test)* (',')?;
dictorsetmaker: ( ((test ':' test | '**' expr)
                   (comp_for | (',' (test ':' test | '**' expr))* (',')?)) |
                  ((test | star_expr)
                   (comp_for | (',' (test | star_expr))* (',')?)) );

classdef: 'class' NAME ('(' (arglist)? ')')? ':' suite;

arglist: argument (',' argument)*  (',')?;

//// The reason that keywords are test nodes instead of NAME is that using NAME
//// results in an ambiguity. ast.c makes sure it's a NAME.
//// "test '=' test" is really "keyword '=' test", but we have no such token.
//// These need to be in a single rule to avoid grammar that is ambiguous
//// to our LL(1) parser. Even though 'test' includes '*expr' in star_expr,
//// we explicitly match '*' here, too, to give it proper precedence.
//// Illegal combinations and orderings are blocked in ast.c:
//// multiple (test comp_for) arguments are blocked; keyword unpackings
//// that precede iterable unpackings are blocked; etc.
//argument: ( test (comp_for)? |
//            test '=' test |
//            '**' test |
//            '*' test );
argument: ( test (comp_for)? |
            test '=' test |
            '**' test |
            '*' test );

comp_iter: comp_for | comp_if;
comp_for: (ASYNC)? 'for' exprlist 'in' or_test (comp_iter)?;
comp_if: 'if' test_nocond (comp_iter)?;

// not used in grammar, but may appear in "node" passed from Parser to Compiler
encoding_decl: NAME;

yield_expr: 'yield' (yield_arg)?;
yield_arg: 'from' test | testlist;


/*
 * Lexer Rules
 */
STRING
 : STRING_LITERAL
// | BYTES_LITERAL
 ;

NUMBER
 : INTEGER
 | FLOAT_NUMBER
 | IMAG_NUMBER
 ;

INTEGER
 : DECIMAL_INTEGER
 | OCT_INTEGER
 | HEX_INTEGER
 | BIN_INTEGER
 ;

// Reserved words
WAIT        : 'wait' ;

COMP_OP_LT		: '<' ;
COMP_OP_GT		: '>' ;
COMP_OP_EQ		: '==' ;
COMP_OP_GTE		: '>=' ;
COMP_OP_LTE		: '<=' ;
COMP_OP_LTGT	: '<>' ;
COMP_OP_NE      : '!=' ;
COMP_OP_IN      : 'in' ;
COMP_OP_NOT_IN  : 'not in';
COMP_OP_IS      : 'is' ;
COMP_OP_IS_NOT  : 'is not';

// General tokens
DEF			 : 'def';
MUL			 : '*' ;
DIV			 : '/' ;
MOD			 : '%';
IDIV		 : '//';
AT			 : '@';

ADD			 : '+' ;
SUB			 : '-' ;
ASSIGN		 : '=' ;
NAME		 : [a-zA-Z0-9_]+ ;
COLON        : ':' ;

NEWLINE
 : ( {AtStartOfInput()}?   SPACES
   | ( '\r'? '\n' | '\r' | '\f' ) SPACES?
   )
   {
		var newLine = (new Regex("[^\r\n\f]+")).Replace(Text, "");
		var spaces = (new Regex("[\r\n\f]+")).Replace(Text, "");

		int next = _input.La(1);
		if (Opened > 0 || next == '\r' || next == '\n' || next == '\f' || next == '#')
//		if (Opened > 0 && (next == '\r' || next == '\n' || next == '\f' || next == '#'))
//      if (Opened > 0 || next == '#')
		{
			// If we're inside a list or on a blank line, ignore all indents, 
			// dedents and line breaks.
			Skip();
		}
		else
		{
			Emit(CommonToken(NEWLINE, newLine));
			int indent = GetIndentationCount(spaces);
			int previous = Indents.Count == 0 ? 0 : Indents.Peek();
			if (indent == previous)
			{
				// skip indents of the same size as the present indent-size
				Skip();
			}
			else if (indent > previous) {
				Indents.Push(indent);
				Emit(CommonToken(CloacaParser.INDENT, spaces));
			}
			else {
				// Possibly emit more than 1 DEDENT token.
				while(Indents.Count != 0 && Indents.Peek() > indent)
				{
					this.Emit(CreateDedent());
					Indents.Pop();
				}
			}
		}
   }
 ;

/// stringliteral   ::=  [stringprefix](shortstring | longstring)
/// stringprefix    ::=  "r" | "u" | "R" | "U" | "f" | "F"
///                      | "fr" | "Fr" | "fR" | "FR" | "rf" | "rF" | "Rf" | "RF"
STRING_LITERAL
 : ( [rR] | [uU] | [fF] | ( [fF] [rR] ) | ( [rR] [fF] ) )? ( SHORT_STRING | LONG_STRING )
 ;

/// decimalinteger ::=  nonzerodigit digit* | "0"+
DECIMAL_INTEGER
 : NON_ZERO_DIGIT DIGIT*
 | '0'+
 ;

/// octinteger     ::=  "0" ("o" | "O") octdigit+
OCT_INTEGER
 : '0' [oO] OCT_DIGIT+
 ;

/// hexinteger     ::=  "0" ("x" | "X") hexdigit+
HEX_INTEGER
 : '0' [xX] HEX_DIGIT+
 ;

/// bininteger     ::=  "0" ("b" | "B") bindigit+
BIN_INTEGER
 : '0' [bB] BIN_DIGIT+
 ;

/// floatnumber   ::=  pointfloat | exponentfloat
FLOAT_NUMBER
 : POINT_FLOAT
 | EXPONENT_FLOAT
 ;

/// imagnumber ::=  (floatnumber | intpart) ("j" | "J")
IMAG_NUMBER
 : ( FLOAT_NUMBER | INT_PART ) [jJ]
 ;

OPEN_PAREN : '(' {Opened++;};
CLOSE_PAREN : ')' {Opened--;};
OPEN_BRACK : '[' {Opened++;};
CLOSE_BRACK : ']' {Opened--;};
OPEN_BRACE : '{' {Opened++;};
CLOSE_BRACE : '}' {Opened--;};

SKIP_
 : ( SPACES | COMMENT | LINE_JOINING ) -> skip
 ;

/// shortstring     ::=  "'" shortstringitem* "'" | '"' shortstringitem* '"'
/// shortstringitem ::=  shortstringchar | stringescapeseq
/// shortstringchar ::=  <any source character except "\" or newline or the quote>
fragment SHORT_STRING
 : '\'' ( STRING_ESCAPE_SEQ | ~[\\\r\n\f'] )* '\''
 | '"' ( STRING_ESCAPE_SEQ | ~[\\\r\n\f"] )* '"'
 ;
/// longstring      ::=  "'''" longstringitem* "'''" | '"""' longstringitem* '"""'
fragment LONG_STRING
 : '\'\'\'' LONG_STRING_ITEM*? '\'\'\''
 | '"""' LONG_STRING_ITEM*? '"""'
 ;

/// longstringitem  ::=  longstringchar | stringescapeseq
fragment LONG_STRING_ITEM
 : LONG_STRING_CHAR
 | STRING_ESCAPE_SEQ
 ;

/// longstringchar  ::=  <any source character except "\">
fragment LONG_STRING_CHAR
 : ~'\\'
 ;

/// stringescapeseq ::=  "\" <any source character>
fragment STRING_ESCAPE_SEQ
 : '\\' .
 | '\\' NEWLINE
 ;

/// nonzerodigit   ::=  "1"..."9"
fragment NON_ZERO_DIGIT
 : [1-9]
 ;

/// digit          ::=  "0"..."9"
fragment DIGIT
 : [0-9]
 ;

/// octdigit       ::=  "0"..."7"
fragment OCT_DIGIT
 : [0-7]
 ;

/// hexdigit       ::=  digit | "a"..."f" | "A"..."F"
fragment HEX_DIGIT
 : [0-9a-fA-F]
 ;

/// bindigit       ::=  "0" | "1"
fragment BIN_DIGIT
 : [01]
 ;

/// pointfloat    ::=  [intpart] fraction | intpart "."
fragment POINT_FLOAT
 : INT_PART? FRACTION
 | INT_PART '.'
 ;

/// exponentfloat ::=  (intpart | pointfloat) exponent
fragment EXPONENT_FLOAT
 : ( INT_PART | POINT_FLOAT ) EXPONENT
 ;

/// intpart       ::=  digit+
fragment INT_PART
 : DIGIT+
 ;

/// fraction      ::=  "." digit+
fragment FRACTION
 : '.' DIGIT+
 ;

/// exponent      ::=  ("e" | "E") ["+" | "-"] digit+
fragment EXPONENT
 : [eE] [+-]? DIGIT+
 ;

 fragment SPACES
 : [ \t]+
 ;

 fragment COMMENT
 : '#' ~[\r\n\f]*
 ;

 fragment LINE_JOINING
 : '\\' SPACES? ( '\r'? '\n' | '\r' | '\f')
 ;
