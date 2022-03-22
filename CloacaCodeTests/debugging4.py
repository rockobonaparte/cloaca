def outer():
    a = 100

    def inner():
        nonlocal a          # Must use this to write to a otherwise it is referenced before assignment 
        a += 1
        return a
    
    return a + inner()

c = outer()
print(c)
