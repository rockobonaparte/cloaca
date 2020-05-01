## Emedding

## Adding Functions
If you wish to embed a .NET function into the runtime that's directly interacting with it, you will need to create a WrappedCodeObject for it.
Here's an example from the REPL demo. We wanted to embed print() as a function that prints to a text box:

```
        public async void print_func(IInterpreter interpreter, FrameContext context, PyObject to_print)
        {
            var str_func = (IPyCallable) to_print.__getattribute__(PyClass.__STR__);

            var returned = await str_func.Call(interpreter, context, new object[] { to_print });
            if (returned != null)
            {
                var asPyString = (PyString)returned;
                richTextBox1.AppendText(asPyString.str);
                SetCursorToEnd();
            }
        }
```

Here's what we gave the interpreter:
```
repl.Interpreter.AddBuiltin(new WrappedCodeObject("print", typeof(Form1).GetMethod("print_func"), this));
```
This will tell the interpreter we have a root built-in called "print" that should call a wrapper around print_func that do any
necessary Python data conversions to/from the call.

These parameters will be injected without appearing in the arguments for the function in the interpreter:
* IInterpreter
* FrameContext
* IScheduler


## Modules

### Adding Import Capability

The Cloaca interpreter will parse and attempt to execute import statements. Relative imports are not yet supported; I can't figure
out how to properly set those up! See this [post](https://groups.google.com/forum/#!topic/comp.lang.python/AnFJbDMsKAo). These imports
won't know where to find anything unless you get the interpreter some "spec finders" to use to resolve imports. Module specs are an
intermediate layer between requests for modules and the loaded modules themselves that describe *how* to load a module. They're
necessary because modules can come from multiple sources.

The Cloaca Interpreter has a method `AddModuleFinder` that takes the spec finder that will locate modules. You can pass in multiples
of the same ISpecFinder to resolve different things. They are designed to return null when they don't find a module. The interpreter
will then go on to the next one. This is similar to sys.meta_path.

We support two spec finders and loaders implementations right now:
1. Injected modules. CloacaInterpreter.ModuleImporting has InjectedModuleRepository for finding modules and InjectedModuleSpec for
   doing the final load. This is a store for hand-written PyModules that you wanted to provide to do your own internal stuff. You
   will probably expose your host runtime using PyModules dumped into this. These modules are assumed to already be created and loaded;
   the load step just hands the PyModule over.
2. Files. These are your package imports as you'd expect in Python source calling other Python source. CloacaInterpreter.ModuleImporting
   has FileBasedModuleFinder and FileBasedModuleLoader for taking care of this. You give the module finder the module roots you want to use
   for searching for files.

### Deeper Details about Modules and Imports
The module import system is defaulting to PEP-451 when possible but is far from compatible nor complete. In particular, spec finders
and module loaders cannot be written in Python right now. This is an obscure topic for most people and shouldn't bother you if you're
just trying to embed Cloaca into some other .NET application for your own scripting.

The Python module import system has a lot of steps that settle into two main components:
1. Finding information about how to load the module (locating the "module spec").
2. Actually importing the module.

## The clr Module

We're mimicking IronPython (also Python.NET) with the clr module. However, it's a work-in-progress. The clr module (standing for
"Common Language Runtime") interfaces with Microsoft .NET's clr to import its assemblies (.NET DLLs) so we can poke them with a stick
in our scripts.

These are the general use cases:
1. clr.AddReference:
   1. Basic form: `clr.AddReference("System")`
   2. Expanded form: `clr.AddReference("System.Xml, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")`
2. clr.References to list added references. Directly manipulated this will probably not be supported right away (or ever?). If I can
   incorporate it simply then I will.
3. Actual import sequence:
   """
      clr.AddReference("System")
      from System import Environment
      print(Environment)
   """
