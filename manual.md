## Modules

The module import system is defaulting to PEP-451 when possible but is far from compatible nor complete. In particular, spec finders
and module loaders cannot be written in Python right now. This is an obscure topic for most people and shouldn't bother you if you're
just trying to embed Cloaca into some other .NET application for your own scripting.

The Python module import system has a lot of steps that settle into two main components:
1. Finding information about how to load the module (locating the "module spec").
2. Actually importing the module.

It's a two-step process because modules come in from multiple sources. If you're importing sys, then that's coming from an internal
built-in module that doesn't have corresponding source. Afterwards, maybe you import a Python package from your own project in
another folder that *does* come from source. Python's two-step system gives enough indirection to resolve and satisfy this, so we try
to do similar.

The Cloaca Interpreter has a method `AddModuleFinder` that takes the spec finder that will locate modules. You can pass in multiples
of the same ISpecFinder to resolve different things. They are designed to return null when they don't find a module. The interpreter
will then go on to the next one. This is similar to sys.meta_path.

We support two spec finders and loaders right now:
1. Injected modules. CloacaInterpreter.ModuleImporting has InjectedModuleRepository for finding modules and InjectedModuleSpec for
   doing the final load. This is a store for hand-written PyModules that you wanted to provide to do your own internal stuff. You
   will probably expose your host runtime using PyModules dumped into this. These modules are assumed to already be created and loaded;
   the load step just hands the PyModule over.
2. Files. These are your package imports as you'd expect in Python source calling other Python source. CloacaInterpreter.ModuleImporting
   has FileBasedModuleFinder and FileBasedModuleLoader for taking care of this. You give the module finder the module roots you want to use
   for searching for files.
