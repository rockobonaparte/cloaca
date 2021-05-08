# Cloaca

A deranged attempt at a Python 3-like bytecode interpreter in C# with green threading and state serialization.

## What The Hell?
I am working on a game in my free time, or rather I keep winding up working on the tools for a game in my free time
because I such at concentrating. I got caught up when it came to NPC scripting and general game rule scripting. I
am using Unity, and I can write C# just fine, but:
1. I wanted something a little less dense for NPC stuff.
2. I wanted a developer console aka a REPL.
3. I also wanted whatever I used to handle something well with green threads. I was inspired by a GDC talk on ScummVM
   and how it ran game logic as a series of small scripts way back on a 286.
4. I hate Lua. I'm sorry.

Looking at my options:
* A custom language probably means rediscovering problems other languages have solved.
* State machines--a typical way of managing this kind of junk in games--didn't really appeal to me since I'd end
  up writing a bunch of circles and arrows on a sheet of paper to represent what I'm typing; I'd rather just run 
  what I'm typing in the first place.
  Also, I saw myself trying to get around that with some visual editor. If I'm going to blow that time, I might
  as well just get on the Python train.
* IronPython could give me a REPL--and I even prototyped that--but:
  * IronPython2 doesn't have async-await
  * IronPython3 doesn't either! I got a patch from Dino Viehland tentatively incorporating it but it didn't fully work.
    Then Dino left Microsoft and IronPython got buried in a pile of cobwebs, so I don't expect Ipy3 to move 
	very quickly. Side note: People are still working on it! You can too! I just suck!
  * I don't even want to use async-await because every line in my scripts would probably end up prefixed with
    "await" because every line would block on internal subsystems.
  * The IronPython interpreter is constructing a LINQ AST that is run at a layer I cannot mess with so I can't
    readily freeze it for green threading, let along to easily serialize script state if I want to save the state
    in a game save. I had to pick and choose my battles and learning how to generate .NET byte code was not my battle.
* I could try to dump a native Python build into .NET but it'll scratch and claw the Mono runtime. I would also
  get murdered by the GIL trying to marshal with it. It already took pictures of me sleeping in my bed last night so
  it knows where I live, and I don't want to agitate it.

So somehow this seemed like the smart--I mean "least dumb" idea.

### Why are you so nuts about coroutines?
If this was single-threaded and you decided to sleep in your script, you'd hang everything. So what happens instead is
that a sleep statement will just block that one script's particular context, take it out of scheduling, and other stuff
can run. This includes, well, the main game loop. Very important!

You could try to multithread this but every time that script's context wants to touch anything it doesn't own--including
a lot of the interpreter's own runtime stuff--it's going to risk blowing something up that another script is trying to
touch at the same time. Each thread also devours about a megabyte just for the right to exist.

### Why the name?

Cloaca is part of a snake's butt. Yes, I know Python is named from Monty Python, not snakes. I think that makes it even
more horrible. This is my own self-deprecation at work. Calling the project "this is a bad use of time but I'm doing it
for personal reasons so nyaaaah" is too long. What's funny is that other people have also called their own projects
"Cloaca!" I can change it but if somebody complains to me that they're the one entitled to name their project after
a snake's butt then I'm just sorry for you. I'll rename it to Blue Steel or Magnum.

### How are you implementing re-entrant code?
I started with IEnumerables but that implementation turned feral and had to be put out of its misery behind the tool shed.
Cloaca now uses async-await like all the grown-ups expect in this era. I was afraid I'd have to make my own synchronization
context, but I'm able to trap all the relevant awaiters using a specific custom awaiter. A scheduler traps these and moves
on to the next queued script.

I have considered some other strategies:
* IEnumerables as previously mentioned. The real killer here was having to enumerate every other call in the .NET code because
  they all could possibly yield. I also found scheduling between these to be cumbersome.
* Mono has a tasklet implementation, but it doesn't work in .NET

## How far have you driven this train wreck?

I have started embedding it into my personal Unity projects both as scripts I schedule and a scripting console. These
are simple little commands right now. However, it has at least proven basic async-await scheduling works within Unity, and
that I can interoperate between my interpreter's base types and .NET/Unity types. For basic commands, it's actually working
really well. I have a lot of triggers kicking off Cloaca scripts now. It probably leaks memory like crazy.

I do have the basics in Python:
1. The basic syntax.
2. Objects with basic inheritance. I am not sure what will happen if you try multiple inheritance, other than the fact that
   the greater developer community will set your bed on fire out of sheer scorn.
3. Exception handling.
4. Basic data types.
5. Iterators.
6. Scripting and .NET interoperations to a certain level. I can call all kinds of .NET functions--including with params fields--
   and convert arguments as necessary.

Then add on top of this a scheduler that can gleefully leap between scripts as they get blocked on awaitables.

## State serialization?

Uhh I haven't really pushed on this yet. My intention ultimately is to be able to save a script's context using a serializer,
and be able to deserialize and resume it later. This would assume everything in that context can be serialized. However, you
know some stuff can't and there would have to be some means of resolving those for it to be robust at all. So I haven't really
hammered on it yet.

My game serializer is another ridiculous thing. When I described it to somebody, they said, "Well, that's kind of how Skyrim
does it so it's not unheard of." I then realized I had done a bad thing.

## How do I use this glorious mess?

A good start is to look at Form1_Load in Form1.cs in the CloacaGuiDemo project. It shows setting up a basic runtime, adding
your own embeddable stuff to it, scheduling independent scripts, and interacting with the REPL. A method of sleeping without
pausing the thread is included there.

## Contributing

This is still a personal project in its youth so I don't really expect much attention. I will gladly accept
pull requests, but you will probably have to explain what trauma caused you to want to contribute in the first
place. You can open an issue if you want to ask about general vision and whatnot. If you actually found an
issue and filed it, then I will be very amused that you spent the effort to have said so.

## Authors

* Adam Preble - _project mad scientist_

## License

This project is available under the Python Software Foundation License.

## Acknowledgments

* The grammar was defined from https://github.com/antlr/grammars-v4/blob/master/python3/Python3.g4
  * MIT license
