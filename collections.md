Notes for Supporting the Collections Module
===========================================

Python syntactic junk I have to understand:

* class __loader__(object) in _collections.py.
* Things in collection's __init__.py
  * __all__
  * __getattr__ (module level)

Extra syntax I'd need to support to support collections completely:
* Decorators
  * @classmethod
  * @property

Stuff I actually use right now from coding interview crap:
* deque
* defaultdict

Consider also throwing in heapq. Actually, it might just run? So maybe do this first?!


So the plan:
1. Try to create a standard library setup
2. Add heapq. Suffer.
3. Add bastardized collections for deque and defaultdict
4. [Get decorators going in general]
5. Come back to try to implement a more completion version of collections
