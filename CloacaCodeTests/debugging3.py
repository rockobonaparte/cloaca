def outer():
    a = 100

    def inner():
        # We don't need to declare nonlocal since we're just reading the variable.
        return a + 1
    
    b = inner()             # Currently this is null in Cloaca. Fix!
    return b

c = outer()
print(c)

