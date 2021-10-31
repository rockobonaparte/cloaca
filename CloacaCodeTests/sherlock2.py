raise NotImplementedError("'int' object has no attribute named '__getitem__'")

def subset(left_orig, right):
    if right is None or len(right) == 0:
        print(left_orig)
        return

    # Append permutations of right to left
    for group_count in range(1, len(right)+1, 1):
        if group_count == 1:
            left = [list(left_orig)]
            left.append(right)
            subset(left, [])

        if group_count == 2:
            left = [list(left_orig)]
            left.append(right[0:1])
            subset(left, right[1:])

            left = [list(left_orig)]
            left.append(right[1:2])
            subset(left, right[0:0])




    # for i in range(0, len(right), 1):
    #     left = [list(left_orig)]
    #     left.append(right[i:])
    #     subset(left, right[i+1:])

subset([1], [2, 3])


# [1], [2, 3]
# [1], [2], [3]

