## Building the Project
The Cloaca solution leans on multiple projects that lean on various Nuget dependencies that should fix themselves. However, it will be very
grouchy to wake up for the first time. This is because ANTLR4 has to generate some files before the grammar is usable by the other projects.
The trick is the build the 'Language' project first. This should the create all the ANTLR4 visitors used by other projects.

## Embedding

## Working With The Scheduler

### ScheduledTaskRecord
When you schedule a task, you will get a TaskEventRecord as a receipt. The task won't immediately run; the scheduler has to be ticked using
Tick(). This give you some time to, say, inject variables into the context using the receipt's Frame.

There is an ExtraMedadata object field you can use to assign some stuff to identify particular tasks. This is because some event handlers will
receive all variety of these and have disassociated what came from where. This can be particularly use when logging errors. Maybe you don't want
your stream-of-conscious typing into the REPL to get echoed into the log, so you can have your hook to the log suppress them.

Since the whole system is running asynchronously, the submission doesn't block and things just keep going. Also, errors normally just get buried
because the exceptions from them have nowhere above them to escape. Hence, task completion and task exceptions are sent to the receipt, which
has events associated with it that'll then fire:

* WhenTaskCompleted(TaskEventRecord): Called in normal script termination. The record is the receipt of the task that finished.
* WhenTaskExceptionEscaped(TaskEventRecord, ExceptionDispatchInfo). The TaskEventRecord is the receipt of the task that finished, and the
    ExceptionDispatchInfo contains information about the failure.

Attaching to WhenTaskExceptionEscaped is particularly useful to log scripting errors.

## Adding Functions
If you wish to embed a .NET function into the runtime that's directly interacting with it, you will need to create a WrappedCodeObject for it.
Here's an example from the REPL demo. We wanted to embed print() as a function that prints to a text box.
Note that the interpreter will look for this function for the PRINT_EXPR opcode, which is generated if you use
the CloacaByteCodeVisitor with replMode set to true. This seems goofy but is very close to how CPython does it.

The function takes a regular object as an argument because you could be mixing .NET types into everything._

```
    public async void print_func(IInterpreter interpreter, FrameContext context, object to_print)
    {
        if (to_print is PyObject)
        {
            var str_func = (IPyCallable) ((PyObject) to_print).__getattribute__(PyClass.__STR__);

            var returned = await str_func.Call(interpreter, context, new object[0]);
            if (returned != null)
            {
                var asPyString = (PyString)returned;
                richTextBox1.AppendText(asPyString);

                richTextBox1.AppendText(asPyString.InternalValue);
                SetCursorToEnd();
            }
        }
        else
        {
            richTextBox1.AppendText(to_print.ToString());
        }
        SetCursorToEnd();
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

### Default References

The Interpreter.ClrFinder will expose the ClrModuleFinder included in itself for the sake of importing .NET assemblies. You can use the
Interpreter.ClrFinder.AddDefaultAssembly to add assemblies that should be available to all modules without having to use clr.AddReference.

_The CLR module does not have any default references to anything._ IronPython will include System and mscorlib by default. We don't know
if you want to have those involved in your embedding environment, or if you want to be loaded by default, or if you'd rather by loading
other stuff instead.

### A Word About Importing Assemblies

Make sure you understand the difference between assemblies and namespaces. Practical points:
1. Use the assembly for clr.AddReference
2. Use the namespace for your import statement
3. If there's no namespace around what you're importing, use the global 'import x' statement.

You need to remember the difference between assemblies and namespaces. For testing, we liked to check System.Environment.MachineName.
Environment is in the System namespace, but it's in the mscorlib assembly. You can usually find this in the online documentation up at
the top ("Assembly: mscorlib.dll"). You've probably missed this for years... like me! Also, make sure you're referencing the proper .NET
runtime version when you're looking this up because the assemblies for various things in .NET can be different. The biggest thing is
bringing up .NET core documentation when you wanted .NET framework. It can have the same API but everything's in different assemblies.
