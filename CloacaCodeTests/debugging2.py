def a():
    # Create a list for each subtotal up to the total to calculate smallest
    # change for them instead. We can reference these for the last element (our total)

    foo = 0

    def b():
        nonlocal foo
        print(foo)
        return foo

    c = b()

    return foo


print(a())
