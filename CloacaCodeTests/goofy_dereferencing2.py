class ObjectWithList:
    def __init__(self):
        self.l = []
        self.zero = 0

a = ObjectWithList()
a.l.append(1)
a.l[a.zero] = 2
assert(a.l[0] == 2)
print("Success!")
