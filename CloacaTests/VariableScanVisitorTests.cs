using Antlr4.Runtime;
using NUnit.Framework;

using Language;
using LanguageImplementation;

namespace CloacaTests
{
    [TestFixture]
    class VariableScanVisitorTests
    {
        public void RunTest(string program, string compare_dump, 
            string[] globals = null, string[] builtins = null)
        {
            var inputStream = new AntlrInputStream(program);
            var lexer = new CloacaLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            var errorListener = new ParseErrorListener();
            var parser = new CloacaParser(commonTokenStream);
            parser.AddErrorListener(errorListener);

            var antlrVisitorContext = parser.file_input();

            errorListener.AssertNoErrors();

            string result;

            var visitor = new VariableScanVisitor(globals=globals ?? new string[0],
                                                  builtins ?? new string[0]);
            visitor.TryVisit(antlrVisitorContext);
            //var rootNamesKeys = visitor.RootNode.NamedScopes.Keys;
            result = visitor.RootNode.ToReportString();
            result = visitor.failureMessage == null ? result : visitor.failureMessage;

            Assert.That(result, Is.EqualTo(compare_dump));
        }

        [Test]
        public void Hello()
        {
            // Root is global!!!
            string program = "a = 1\n";
            RunTest(program, "a: Global Write\n");
        }

        [Test]
        public void HelloFunc()
        {
            string program = "a = 1\n" +
                             "def foo():\n" +
                             "   b = 2\n" +
                             "   return b";
            RunTest(program, "a: Global Write\n" +
                             "foo: Name Read Name Write\n" +
                             "foo:\n" +
                             "  b: LocalFast Read LocalFast Write\n");
        }

        [Test]
        public void FuncParameters()
        {
            string program = "a = 1\n" +
                             "def foo(c):\n" +
                             "   b = 2\n" +
                             "   return b";
            RunTest(program, "a: Global Write\n" +
                             "foo: Name Read Name Write\n" +
                             "foo:\n" +
                             "  b: LocalFast Read LocalFast Write\n" +
                             "  c: LocalFast Read LocalFast Write\n");
        }

        [Test]
        public void BasicGlobalPlumbing()
        {
            string program = "a = 1\n";
            RunTest(program, "a: Global Write\n", new string[] { "a" });
        }

        [Test]
        public void GlobalDeclaration()
        {
            string program = "global a\n";
            RunTest(program, "a: Global Read Global Write\n");
        }

        [Test]
        public void CallBuiltinFuncOneArg()
        {
            string program =
                "print(a)\n";

            RunTest(program,
                             "a: Global Read Global Write\n" +
                             "print: Builtin Read Global Write\n", new string[] { "a", }, new string[] { "print" });
        }

        [Test]
        public void InnerGlobalOuterLocal()
        {
            string program =
                "def outer():\n" +      
                "  a = 100\n" +
                "  def inner():\n" +
                "    global a\n" +
                "    a += 1\n" +        // PS: Actually running outer() will get you an error that 'a' isn't defined.
                "    return a\n" +
                "  return a + inner()\n";

            
            RunTest(program,
                             "outer: Name Read Name Write\n" +
                             "outer:\n" +
                             "  a: LocalFast Read LocalFast Write\n" +
                             "  inner: LocalFast Read LocalFast Write\n" +
                             "  inner:\n" +
                             "    a: Global Read Global Write\n");
        }

        [Test]
        public void GlobalInFunction()
        {
            string program = "def fun():\n" +
                             "  global a\n" +
                             "  a = 1\n";
            RunTest(program, "fun: Name Read Name Write\n" +
                             "fun:\n" +
                             "  a: Global Read Global Write\n", new string[] { "a" });
        }

        /// <summary>
        /// This came from ScopeTests and was one of two that motivated adding a first-pass to get
        /// variable context.
        /// </summary>        
        [Test]
        public void InnerFunctionReadsOuter()
        {
            string program =
                "def outer():\n" +
                "  a = 100\n" +
                "  def inner():\n" +
                "    return a + 1\n" +
                "  b = inner()\n" +
                "  return b\n" +
                "c = outer()\n";

            RunTest(program, "c: Global Write\n" +
                             "outer: Name Read Name Write\n" +
                             "outer:\n" +
                             "  a: EnclosedCell Read EnclosedCell Write\n" +
                             "  b: LocalFast Read LocalFast Write\n" +
                             "  inner: LocalFast Read LocalFast Write\n" +
                             "  inner:\n" +
                             "    a: EnclosedFree Read LocalFast Write\n");
        }

        /// <summary>
        /// This came from ScopeTests and was test two of two that motivated adding a first-pass to get
        /// variable context.
        /// 
        /// It also adds the nonlocal to make variable 'a' writeable.
        /// </summary>
        [Test]
        public void InnerFunctionWritersOuterNonlocal()
        {
            string program =
                "def outer():\n" +
                "  a = 100\n" +
                "  def inner():\n" +
                "    nonlocal a\n" +
                "    a += 1\n" +
                "    return a\n" +
                "  return a + inner()\n" +
                "b = outer()\n";

            // TODO: I think I need additional context to tell if something is enclosed and writable!
            RunTest(program, "b: Global Write\n" +
                             "outer: Name Read Name Write\n" +
                             "outer:\n" +
                             "  a: EnclosedCell Read EnclosedCell Write\n" +
                             "  inner: LocalFast Read LocalFast Write\n" +
                             "  inner:\n" +
                             "    a: EnclosedFree Read EnclosedFree Write\n");
        }

        [Test]
        public void MultilevelNonlocal()
        {
            string program =
                            "def f1():\n" +
                            "  a = 2\n" +
                            "  def f2():\n" +
                            "    def f3():\n" +
                            "      nonlocal a\n" +
                            "      a = 3\n" +
                            "    f3()\n" +
                            "  f2()\n" +
                            "  return a\n";

            RunTest(program,
                             "f1: Name Read Name Write\n" +
                             "f1:\n" +
                             "  a: EnclosedCell Read EnclosedCell Write\n" +
                             "  f2: LocalFast Read LocalFast Write\n" +
                             "  f2:\n" +
                             "    f3: LocalFast Read LocalFast Write\n" +
                             "    f3:\n" +
                             "      a: EnclosedFree Read EnclosedFree Write\n");
        }

        // This one's tricky because the maker's parameter, which is a localfast, goes
        // straight down into the made function. This mixes up localfasts and enclosed free variables.
        // What a twist!
        [Test]
        public void FactoryMethodParameterFreevar()
        {
            string program =
                            "def maker(p):\n" +
                            "  def made(x):\n" +
                            "    return p + x\n" +
                            "m = maker(1)\n";

            RunTest(program,
                             "m: Global Write\n" +
                             "maker: Name Read Name Write\n" +
                             "maker:\n" +
                             "  made: LocalFast Read LocalFast Write\n" +
                             "  p: EnclosedCell Read EnclosedCell Write\n" +
                             "  made:\n" +
                             "    p: EnclosedFree Read LocalFast Write\n" +
                             "    x: LocalFast Read LocalFast Write\n");
        }

        [Test]
        public void EnclosedMultilevelPromotion()
        {
            string program =
                            "def f1():\n" +
                            "  a = 1\n" +
                            "  def f2():\n" +
                            "    b = a + 2\n" +
                            "    def f3():\n" +
                            "      c = a + b\n" +
                            "      return c\n" +
                            "    return b + f3()\n" +
                            "  return a + f2()\n";

            RunTest(program,
                             "f1: Name Read Name Write\n" +
                             "f1:\n" +
                             "  a: EnclosedCell Read EnclosedCell Write\n" +
                             "  f2: LocalFast Read LocalFast Write\n" +
                             "  f2:\n" +
                             "    a: EnclosedFree Read EnclosedFree Write\n" +
                             "    b: EnclosedCell Read EnclosedCell Write\n" +
                             "    f3: LocalFast Read LocalFast Write\n" +
                             "    f3:\n" +
                             "      a: EnclosedFree Read LocalFast Write\n" +       // I think the LocalFast Writes are correct here...
                             "      b: EnclosedFree Read LocalFast Write\n" +
                             "      c: LocalFast Read LocalFast Write\n");
        }

        [Test]
        public void MultilevelGlobal()
        {
            string program =
                            "a = 2\n" +
                            "def f1():\n" +
                            "  def f2():\n" +
                            "    def f3():\n" +
                            "      global a\n" +
                            "      a = 3\n" +
                            "    f3()\n" +
                            "  f2()\n";

            // TODO: I think I need additional context to tell if something is enclosed and writable!
            RunTest(program, "a: Global Write\n" +
                             "f1: Name Read Name Write\n" +
                             "f1:\n" +
                             "  f2: LocalFast Read LocalFast Write\n" +
                             "  f2:\n" +
                             "    f3: LocalFast Read LocalFast Write\n" +
                             "    f3:\n" +
                             "      a: Global Read Global Write\n");
        }

        [Test]
        public void ParsingBundledStuff()
        {
            string program = "arr = [1, b]\n" +
                             "hash = {'foo': c}\n" +
                             "tup = {2, d}\n" +
                             "set = (3, e)\n" +
                             "func = call(4, f)\n";

            RunTest(program, "arr: Global Write\n" +
                             "b: Global Read Global Write\n" +
                             "c: Global Read Global Write\n" +
                             "call: Global Read Global Write\n" +
                             "d: Global Read Global Write\n" +
                             "e: Global Read Global Write\n" +
                             "f: Global Read Global Write\n" +
                             "func: Global Write\n" +
                             "hash: Global Write\n" +
                             "set: Global Write\n" +
                             "tup: Global Write\n",
                             new string[] { "b", "c", "d", "e", "f", "call" }
                             );
        }

        // TODO: Add a test that tries various grammars to make sure we get variables in things like:
        // for-loops
        // try-except blocks
        // conditionals
        // Arrays
        // Functions inside functions
        // ...and more...
        [Test]
        public void ParseVariousBlocks()
        {
            string program =
                "for for_i in range(10):\n" +
                "  in_for = for_i + 1\n" +
                "try:\n" +
                "  try_var = 3\n" +
                "except Exception as exc_var:" +
                "  exc_blk_var = exc_var\n";

            RunTest(program, "exc_blk_var: Global Write\n" +
                             "exc_var: Global Read Global Write\n" +
                             "Exception: Builtin Read Global Write\n" +
                             "for_i: Global Read Global Write\n" +
                             "in_for: Global Write\n" +
                             "range: Builtin Read Global Write\n" +
                             "try_var: Global Write\n",
                             new string[0],
                             new string[] { "Exception", "range" }
                             );
        }

        [Test]
        public void MashedUpPileOfLocalGlobalNonlocal()
        {
            string program =
                "a = -1\n" +
                "def f1():\n" +
                "  a = 2\n" +
                "  def f2():\n" +
                "    def f3():\n" +
                "      global a\n" +
                "      a = 10000\n" +
                "      def f4():\n" +
                "        a = 5\n" +
                "        return a\n" +
                "      a += f4()\n" +
                "      return a\n" +
                "    \n" +
                "    nonlocal a\n" +
                "    a -= f3()\n" +
                "    return a\n" +
                "  a += f2()\n" +
                "  return a\n" +
                "a += f1()\n" +
                "print(a)\n";

            RunTest(program, "a: Global Read Global Write\n" +
                             "f1: Name Read Name Write\n" +
                             "print: Builtin Read Global Write\n" +
                             "f1:\n" +
                             "  a: EnclosedCell Read EnclosedCell Write\n" +
                             "  f2: LocalFast Read LocalFast Write\n" +
                             "  f2:\n" +
                             "    a: EnclosedFree Read EnclosedFree Write\n" +
                             "    f3: LocalFast Read LocalFast Write\n" +
                             "    f3:\n" +
                             "      a: Global Read Global Write\n" +
                             "      f4: LocalFast Read LocalFast Write\n" +
                             "      f4:\n" +
                             "        a: LocalFast Read LocalFast Write\n", new string[0], new string[] { "print" });
        }

        [Test]
        public void ClassWithMember()
        {
            string program = "class Foo:\n" +
                             "   def __init__(self, a):\n" +
                             "      self.b = a\n" +
                             "c = Foo(1)\n";

            RunTest(program, "c: Global Write\n" +
                             "Foo: Name Read Name Write\n" +
                             "Foo:\n" +
                             "  __init__: Name Read Name Write\n" +
                             "  __init__:\n" +
                             "    a: LocalFast Read LocalFast Write\n" +
                             "    self: LocalFast Read LocalFast Write\n");
        }

        // Had a bug come up in actual integration. We were not managing derefencing class
        // members properly at least at the root level.
        [Test]
        public void ClassWithUsedMembers()
        {
            string program =    "class Foo:\n" +
                                "   def __init__(self, a):\n" +
                                "      self.b = a\n" +
                                "c = Foo(1)\n" +
                                "d = c.b\n";

            RunTest(program,    "c: Global Read Global Write\n" +
                                "d: Global Write\n" +
                                "Foo: Name Read Name Write\n" +
                                "Foo:\n" +
                                "  __init__: Name Read Name Write\n" +
                                "  __init__:\n" +
                                "    a: LocalFast Read LocalFast Write\n" +
                                "    self: LocalFast Read LocalFast Write\n");
        }

        // Had a bug come up in actual integration. We were not managing derefencing class
        // members properly at least at the root level.
        [Test]
        public void ClassWithUsedMembersLValue()
        {
            string program = "class Foo:\n" +
                                "   def __init__(self, a):\n" +
                                "      self.b = a\n" +
                                "c = Foo(1)\n" +
                                "c.b = 2\n";

            RunTest(program, "c: Global Read Global Write\n" +
                                "Foo: Name Read Name Write\n" +
                                "Foo:\n" +
                                "  __init__: Name Read Name Write\n" +
                                "  __init__:\n" +
                                "    a: LocalFast Read LocalFast Write\n" +
                                "    self: LocalFast Read LocalFast Write\n");
        }

        // Naming convention for ClassWith123:
        // 1. Global Scope
        // 2. Class Scope
        // 3. Class Init Local Scope
        //
        // X = absent
        // G = Global
        // N = NonLocal
        // C = Class
        [Test]
        public void ClassWithLLL()
        {
            string program = "a = 100\n" +
                             "\n" +
                             "class SomeClass:\n" +
                             "    a = 101\n" +
                             "    def __init__(self):\n" +
                             "        a = 102\n" +
                             "\n" +
                             "sc = SomeClass()\n";

            // a = 100
            // SomeClass.a = 101

            RunTest(program, "a: Global Write\n" +
                             "sc: Global Write\n" +
                             "SomeClass: Name Read Name Write\n" +
                             "SomeClass:\n" +
                             "  __init__: Name Read Name Write\n" +
                             "  a: Name Write\n" +
                             "  __init__:\n" +
                             "    a: LocalFast Write\n" +
                             "    self: LocalFast Read LocalFast Write\n");
        }

        [Test]
        public void ClassWithGGG()
        {
            string program = "a = 100\n" +
                             "class SomeClass:\n" +
                             "    global a\n" +
                             "    a = 101\n" +
                             "    def __init__(self):\n" +
                             "        global a\n" +
                             "        a = 102\n" +
                             "\n" +
                             "sc = SomeClass()\n";

            // a = 102
            // SomeClass.a = 102

            RunTest(program, "a: Global Write\n" +
                             "sc: Global Write\n" +
                             "SomeClass: Name Read Name Write\n" +
                             "SomeClass:\n" +
                             "  __init__: Name Read Name Write\n" +
                             "  a: Global Read Global Write\n" +
                             "  __init__:\n" +
                             "    a: Global Read Global Write\n" +
                             "    self: LocalFast Read LocalFast Write\n");
        }

        [Test]
        public void ClassWithGGX()
        {
            string program = "a = 100\n" +
                             "class SomeClass:\n" +
                             "    global a\n" +
                             "    a = 101\n" +
                             "    def __init__(self):\n" +
                             "        a = 102\n" +
                             "\n" +
                             "sc = SomeClass()\n";

            // a = 101
            // SomeClass.a = 101
            RunTest(program, "a: Global Write\n" +
                             "sc: Global Write\n" +
                             "SomeClass: Name Read Name Write\n" +
                             "SomeClass:\n" +
                             "  __init__: Name Read Name Write\n" +
                             "  a: Global Read Global Write\n" +
                             "  __init__:\n" +
                             "    a: LocalFast Write\n" +
                             "    self: LocalFast Read LocalFast Write\n");
        }

        [Test]
        public void ClassWithGLL()
        {
            // Note to self: FASTs are NOT a thing outside of functions so use regular LEGB variable
            // resolution in the class body. That should cause a to properly copy in and become a local,
            // independent variable.
            string program = "a = 100\n" +
                             "class SomeClass:\n" +
                             "    a += 1\n" +
                             "    def __init__(self):\n" +
                             "        a = 102\n" +
                             "\n" +
                             "sc = SomeClass()\n";

            // a = 100
            // SomeClass.a = 101
            // sc.a = 102
            RunTest(program, "a: Global Write\n" +
                             "sc: Global Write\n" +
                             "SomeClass: Name Read Name Write\n" +
                             "SomeClass:\n" +
                             "  __init__: Name Read Name Write\n" +
                             "  a: Name Read Name Write\n" +
                             "  __init__:\n" +
                             "    a: LocalFast Write\n" +
                             "    self: LocalFast Read LocalFast Write\n");
        }

        [Test]
        public void ClassWithGXN()
        {
            string program = "a = 100\n" +
                             "class SomeClass:\n" +
                             "    def __init__(self):\n" +
                             "        nonlocal a\n" +
                             "        a = 102\n" +
                             "\n" +
                             "sc = SomeClass()\n";

            // SyntaxError: no binding for nonlocal 'a' found
            RunTest(program, "line 4: no binding for nonlocal 'a' found");
        }

        [Test]
        public void ClassWithGXG()
        {
            string program = "a = 100\n" +
                             "class SomeClass:\n" +
                             "    def __init__(self):\n" +
                             "        global a\n" +
                             "        a = 102\n" +
                             "\n" +
                             "sc = SomeClass()\n";

            // a = 102
            RunTest(program, "a: Global Write\n" +
                             "sc: Global Write\n" +
                             "SomeClass: Name Read Name Write\n" +
                             "SomeClass:\n" +
                             "  __init__: Name Read Name Write\n" +
                             "  __init__:\n" +
                             "    a: Global Read Global Write\n" +
                             "    self: LocalFast Read LocalFast Write\n");
        }

        [Test]
        public void ClassWithGLC()
        {
            // This one will probably suck. Getting the class a to the local scope
            // is probably something peculiar I have to deal with.
            string program = "a = 100\n" +
                             "class SomeClass:\n" +
                             "    a = 101\n" +
                             "    def __init__(self):\n" +
                             "        self.a = a + 1\n" +
                             "\n" +
                             "sc = SomeClass()\n";

            // a = 101
            // sc.a = 102
            RunTest(program, "a: Global Write\n" +
                             "sc: Global Write\n" +
                             "SomeClass: Name Read Name Write\n" +
                             "SomeClass:\n" +
                             "  __init__: Name Read Name Write\n" +
                             "  a: Name Write\n" +
                             "  __init__:\n" +
                             "    a: Name Read LocalFast Write\n" +
                             "    self: LocalFast Read LocalFast Write\n");

            // Issues:
            // self.a gets its own thing. I haven't really accommodated for that.
            // a gets treated as a local in __init__.
        }

        [Test]
        public void BasicListComprehension()
        {
            // This one will probably suck. Getting the class a to the local scope
            // is probably something peculiar I have to deal with.
            string program = "x = [a for a in [1]]\n";

            // a = 101
            // sc.a = 102
            RunTest(program,
                             "<listcomp>: Name Read Name Write\n" +
                             "x: Global Write\n" +
                             "<listcomp>:\n" +
                             "  a: LocalFast Read LocalFast Write\n");


            // Issues:
            // self.a gets its own thing. I haven't really accommodated for that.
            // a gets treated as a local in __init__.
        }
    }
}
