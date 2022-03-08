#raise NotImplementedError("Some issue unpacking an integer for some strange reason!\nProbably\nthe_list[last_i], the_list[left] = the_list[left], the_list[last_i]")

def partition(the_list, first_i, last_i):
    pivot = the_list[last_i]
    left = first_i
    right = last_i - 1

    while left < right:
        if the_list[left] < pivot:
            left += 1
        if the_list[right] > pivot:
            right -= 1

        if the_list[left] > pivot and the_list[right] < pivot and left < right:
            the_list[left], the_list[right] = the_list[right], the_list[left]
            left += 1
            right -= 1

    the_list[last_i], the_list[left] = the_list[left], the_list[last_i]
    return left


def qsort(the_list):
    def recurse(the_list, first_i, last_i):
        if first_i >= last_i:
            return
        p = partition(the_list, first_i, last_i)
        recurse(the_list, first_i, p - 1)
        recurse(the_list, p + 1, last_i)

    recurse(the_list, 0, len(the_list) - 1)


l = [10, 0, 9, 1, 8, 2, 7, 3, 6, 4, 5]
qsort(l)
print(l)

#l = [4, 0, 3, 1, 2]
#p = partition(l, 0, 4)
#print("%s %s" % (p, l))
