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
                             "foo:\n" +
                             "  b: Local Read Local Write\n");
        }

        [Test]
        public void FuncParameters()
        {
            string program = "a = 1\n" +
                             "def foo(c):\n" +
                             "   b = 2\n" +
                             "   return b";
            RunTest(program, "a: Global\n" +
                             "foo:\n" +
                             "  b: Local\n" +
                             "  c: Local\n");
        }

        [Test]
        public void BasicGlobalPlumbing()
        {
            string program = "a = 1\n";
            RunTest(program, "a: Global\n", new string[] { "a" });
        }

        [Test]
        public void GlobalDeclaration()
        {
            string program = "global a\n";
            RunTest(program, "a: Global\n");
        }

        [Test]
        public void CallFuncOneArg()
        {
            string program =
                "print(a)\n";

            RunTest(program,
                             "a: Global\n" +
                             "print: Global\n", new string[] { "a", "print" });
        }

        [Test]
        public void InnerGlobalOuterLocal()
        {
            string program =
                "def outer():\n" +      // Defined but never invoked so doesn't come up as a global.
                "  a = 100\n" +
                "  def inner():\n" +
                "    global a\n" +
                "    a += 1\n" +        // PS: Actually run this will get you an error that a isn't defined.
                "    return a\n" +
                "  return a + inner()\n";

            
            RunTest(program,
                             "outer:\n" +
                             "  a: Local\n" +
                             "  inner: Local\n" +
                             "  inner:\n" +
                             "    a: Global\n");
        }

        [Test]
        public void GlobalInFunction()
        {
            string program = "def fun():\n" +
                             "  global a\n" +
                             "  a = 1\n";
            RunTest(program, "fun:\n" +
                             "  a: Global\n", new string[] { "a" });
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

            RunTest(program, "c: Global\n" +
                             "outer: Global\n" +
                             "outer:\n" +
                             "  a: EnclosedRead\n" +
                             "  b: Local\n" +
                             "  inner: Local\n" +
                             "  inner:\n" +
                             "    a: EnclosedRead\n");
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
            RunTest(program, "b: Global\n" +
                             "outer: Global\n" +
                             "outer:\n" +
                             "  a: EnclosedReadWrite\n" +
                             "  inner: Local\n" +
                             "  inner:\n" +
                             "    a: EnclosedReadWrite\n");
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

            // TODO: I think I need additional context to tell if something is enclosed and writable!
            RunTest(program, "f1:\n" +
                             "  a: EnclosedReadWrite\n" +
                             "  f2: Local\n" +
                             "  f2:\n" +
                             "    f3: Local\n" +
                             "    f3:\n" +
                             "      a: EnclosedReadWrite\n");
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
            RunTest(program, "a: Global\n" +
                             "f1:\n" +
                             "  f2: Local\n" +
                             "  f2:\n" +
                             "    f3: Local\n" +
                             "    f3:\n" +
                             "      a: Global\n");
        }

        [Test]
        public void ParsingBundledStuff()
        {
            string program = "arr = [1, b]\n" +
                             "hash = {'foo': c}\n" +
                             "tup = {2, d}\n" +
                             "set = (3, e)\n" +
                             "func = call(4, f)\n";

            RunTest(program, "arr: Global\n" +
                             "b: Global\n" +
                             "c: Global\n" +
                             "call: Global\n" +
                             "d: Global\n" +
                             "e: Global\n" +
                             "f: Global\n" +
                             "func: Global\n" +
                             "hash: Global\n" +
                             "set: Global\n" +
                             "tup: Global\n",
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

            RunTest(program, "exc_blk_var: Global\n" +
                             "exc_var: Global\n" +
                             "Exception: Global\n" +
                             "for_i: Global\n" +
                             "in_for: Global\n" +
                             "range: Global\n" +
                             "try_var: Global\n",
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

            RunTest(program, "a: Global\n" +
                             "f1: Global\n" +
                             "print: Global\n" +
                             "f1:\n" +
                             "  a: Local\n" +
                             "  f2: Local\n" +
                             "  f2:\n" +
                             "    a: EnclosedReadWrite\n" +
                             "    f3: Local\n" +
                             "    f3:\n" +
                             "      a: Global\n" +
                             "      f4: Local\n" +
                             "      f4:\n" +
                             "        a: Local\n", new string[] { "print" });
        }

        [Test]
//        [Ignore("Class members not properly parsed yet")]
        public void ClassWithMember()
        {
            string program = "class Foo:\n" +
                             "   def __init__(self, a):\n" +
                             "      self.b = a\n" +
                             "c = Foo(1)\n";

            RunTest(program, "c: Global\n" +
                             "Foo: Global\n" +
                             "Foo:\n" +
                             "  __init__:\n" +
                             "    a: Local\n" +
                             "    self: Local\n");
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

            RunTest(program, "a: Global\n" +
                             "sc: Global\n" +
                             "SomeClass: Global\n" +
                             "SomeClass:\n" +
                             "  a: Local\n" +
                             "  __init__:\n" +
                             "    a: Local\n" +
                             "    self: Local\n");
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

            RunTest(program, "a: Global\n" +
                             "sc: Global\n" +
                             "SomeClass: Global\n" +
                             "SomeClass:\n" +
                             "  a: Global\n" +
                             "  __init__:\n" +
                             "    a: Global\n" +
                             "    self: Local\n");
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
            RunTest(program, "a: Global\n" +
                             "sc: Global\n" +
                             "SomeClass: Global\n" +
                             "SomeClass:\n" +
                             "  a: Global\n" +
                             "  __init__:\n" +
                             "    a: Local\n" +
                             "    self: Local\n");
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
            RunTest(program, "a: Global\n" +
                             "sc: Global\n" +
                             "SomeClass: Global\n" +
                             "SomeClass:\n" +
                             "  a: Local\n" +
                             "  __init__:\n" +
                             "    a: Local\n" +
                             "    self: Local\n");
        }

        [Test]
        //[Ignore("Need to error properly on the nonlocal: 'SyntaxError: no binding for nonlocal 'a' found'")]
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
            RunTest(program, "a: Global\n" +
                             "sc: Global\n" +
                             "SomeClass: Global\n" +
                             "SomeClass:\n" +
                             "  __init__:\n" +
                             "    a: Global\n" +
                             "    self: Local\n");
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
                             "  a: Local Write\n" +
                             "  __init__:\n" +
                             "    a: Global Read\n" +
                             "    self: Local Read Local Write\n");

            // Issues:
            // self.a gets its own thing. I haven't really accommodated for that.
            // a gets treated as a local in __init__.
        }
    }
}
