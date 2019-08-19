# Cloaca

A deranged attempt at a Python 3 bytecode interpreter in C# with green threading and state serialization.

## What The Hell?
I am working on a game in my free time, or rather the tools for a game in my free time. I got caught up particularly
when it came to NPC scripting and general game rule scripting. I am using Unity, and I can write C# just fine, but:
1. I wanted something a little less dense for NPC stuff.
2. I wanted a developer console.
3. I also wanted whatever I used to handle something well with green threads. I was inspired by a GDC talk on ScummVM
   and how it ran game logic as a series of small scripts.

Looking at my options:
* A custom language probably means rediscovering problems other languages have solved.
* State machines--a typical way of managing this kind of junk in games--didn't really appeal to me since I'd end
  up writing a bunch of circles and junk on a sheet of paper to represent what I'm typing; I'd rather just type what I want.
  Also, I saw myself trying to get around that with some visual editor. If I'm going to blow that time, I might
  as well just go in on the Python train.
* IronPython could give me a REPL--and I even prototyped that--but:
  * IronPython2 doesn't have async-await
  * IronPython3 doesn't either! I got a patch from Dino tentatively incorporating it but it didn't fully work.
  * I don't even want to use async-await because every line in my scripts would probably end up prefixed with
    "await" at that rate.
  * The IronPython interpreter is construction a LINQ AST that is run at a layer I cannot mess with so I can't
    readily freeze it for green threading, let along to easily serialize script state if I want to save the state
    in a game save.
* I could try to dump CPython into .NET but I expect hell with Mono and it's not particular portable. I would also
  get murdered by the GIL trying to marshal with it.

So somehow this seemed like the smart--I mean "least dumb" idea.

### Why the name?

Cloaca is part of a snake's butt. Yes, I know Python is named from Monty Python, not snakes. I think that makes it even
more horrible. This is my own self-deprecation at work. "This is probably a horrible idea in reality but I'm doing this
in my free time so nyaaaah" doesn't really collapse into a better name.

### How are you implementing re-entrant code?
I am using IEnumerables that run various invocations of Python code objects against an interpreter. I understand that
using IEnumerables this was is soooo 2000s, but I have certain constraints:
1. Needs to work in Mono and .NET
2. Needs to work in Unity
3. Should not get into running in other, multiple threads
4. Shouldn't hog the thread in which it's started
5. Shouldn't fight against any other schedulers that could be running

I have considered some other strategies:
* Async-await by itself is insufficient if I intend to make scheduling decisions in an embedded runtime when the scripts block.
  I have to create my own awaiters that trigger the scheduler.
  * I think I'd end up having to also create my own SynchronizationContext, but I'd have to cooperate with the one Unity is using.
    I figured 
* Mono has a tasklet implementation, but it doesn't work in .NET

## Contributing

This is still a personal project in its youth so I don't really expect much attention. I will gladly accept
pull requests, but you will probably have to explain what trauma caused you to want to contribute in the first
place. You can open an issue if you're want to ask about general vision and whatnot. If you actually found an
issue and filed over it then I will be very amused that you spent the effort to have said so.

## Authors

* Adam Preble - _project mad scientist_

## License

This project is available under the Python Software Foundation License.

## Acknowledgments

* The grammar was defined from https://github.com/antlr/grammars-v4/blob/master/python3/Python3.g4
  * MIT license
