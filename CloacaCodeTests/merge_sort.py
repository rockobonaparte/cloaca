def merge(left, right):
    left_len = len(left)
    right_len = len(right)
    merged = []
    left_i = 0
    right_i = 0
    while left_i < left_len and right_i < right_len:
        if left[left_i] <= right[right_i]:
            merged.append(left[left_i])
            left_i += 1
        else:
            merged.append(right[right_i])
            right_i += 1

    while left_i < left_len:
        merged.append(left[left_i])
        left_i += 1

    while right_i < right_len:
        merged.append(right[right_i])
        right_i += 1

    return merged


def merge_sort(the_list):

    def recurse(the_list, start, length):
        if length == 1:
            return [the_list[start]]
        elif length < 1:
            return []
        else:
            halfsies = length // 2
            left = recurse(the_list, start, halfsies)
            right = recurse(the_list, start + halfsies, length - halfsies)
            return merge(left, right)

    return recurse(the_list, 0, len(the_list))


print(merge_sort([10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0]))
print("Success!")
