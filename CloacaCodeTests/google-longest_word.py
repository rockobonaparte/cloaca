# I guess I can't index by character. Time to harden PyString.
raise NotImplementedError("Does not yet produce correct output")

# Given a string S and a set of words D, find the longest word in D that is a subsequence of S.
#
# Word W is a subsequence of S if some number of characters, possibly zero, can be deleted from S to form W, without
# reordering the remaining characters.
#
# Note: D can appear in any format (list, hash table, prefix tree, etc.
#
# For example, given the input of S = "abppplee" and D = {"able", "ale", "apple", "bale", "kangaroo"} the correct output
# would be "apple"
#
# The words "able" and "ale" are both subsequences of S, but they are shorter than "apple".
# The word "bale" is not a subsequence of S because even though S has all the right letters, they are not in the right
# order.
#
# The word "kangaroo" is the longest word in D, but it isn't a subsequence of S.

def longest_word(s: str, d: set[str]):
    by_length = sorted(list(d), key=len, reverse=True)
    for word in by_length:
        s_i = 0
        w_i = 0
        while s_i < len(s) and w_i < len(word):
            if s[s_i] == word[w_i]:
                w_i += 1
            s_i += 1
        if w_i >= len(word):
            return word

    return None


test_str = "abpppleease"
test_set1 = {"able", "ale", "apple", "bale", "kangaroo"}
test_set2 = {"able", "ale", "apple", "bale", "kangaroo", "please"}

assert longest_word(test_str, test_set1) == "apple"
assert longest_word(test_str, test_set2) == "please"
