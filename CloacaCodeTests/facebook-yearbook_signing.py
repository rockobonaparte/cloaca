raise NotImplementedError("Another empty stack")

# There are n students, numbered from 1 to n, each with their own yearbook. They would like
# to pass their yearbooks around and get them signed by other students.
#
# You're given a list of n integers arr[1..n], which is guaranteed to be a permutation of 1..n
# (in other words, it includes the integers from 1 to n exactly once each, in some order). The
# meaning of this list is described below.
#
# Initially, each student is holding their own yearbook. The students will then repeat the
# following two steps each minute: Each student i will first sign the yearbook that they're
# currently holding (which may either belong to themselves or to another student), and then
# they'll pass it to student arr[i-1]. It's possible that arr[i-1] = i for any given i, in
# which case student i will pass their yearbook back to themselves. Once a student has received
# their own yearbook back, they will hold on to it and no longer participate in the passing process.
#
# It's guaranteed that, for any possible valid input, each student will eventually receive their
# own yearbook back and will never end up holding more than one yearbook at a time.
#
# You must compute a list of n integers output, whose element at i-1 is equal to the number of
# signatures that will be present in student i's yearbook once they receive it back.

def findSignatureCounts(arr):
    # Write your code here
    paths = {}
    for i, next_student in enumerate(arr):
        paths[i] = next_student - 1  # Convert 1-indexed students to 0 index.

    signatures = [0] * len(arr)

    for i in range(len(arr)):
        if signatures[i] == 0:
            next_i = paths[i]
            clique = [i]
            while next_i != i:
                clique.append(next_i)
                next_i = paths[next_i]

            for friend in clique:
                signatures[friend] = len(clique)
    return signatures


arr_1 = [2, 1]
expected_1 = [2, 2]
output_1 = findSignatureCounts(arr_1)
print(output_1)

arr_2 = [1, 2]
expected_2 = [1, 1]
output_2 = findSignatureCounts(arr_2)
print(output_2)

