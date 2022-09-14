# I was having problems nested "stuff" in general so I wrong a standalone test for it.
class ObjectWithList:
    def __init__(self):
        self.l = []
        self.zero = 0

a = ObjectWithList()
a.l.append(ObjectWithList())
a.l[0].l.append(ObjectWithList())
assert(len(a.l[a.zero].l) == 1)         # It's the l.zero that does it.
