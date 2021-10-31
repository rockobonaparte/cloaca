def countGroups(related):
    groups = 0

    def follow_subusers(matrix_lookup, user_id, users_in_group):
        """
        Recursive helper to follow users across matrix that have associations possibly separated from the original
        user. Examples didn't show what happened when associations don't pass from the originating user, and we
        assume we continue to chase them around the matrix until all degrees of users are represented from the
        originator.
        """
        if user_id not in users_in_group:
            users_in_group.append(user_id)

        while len(matrix_lookup[user_id]) > 0:
            follow_subusers(matrix_lookup, matrix_lookup[user_id].pop(), users_in_group)

    # Let's make a scratch lookup table and remove connections as we create groups
    lookup = {}
    for row_i in range(len(related)):
        for col_i in range(len(related)):  # It's square so it doesn't matter what we use to reference
            if related[row_i][col_i] == '1':
                if col_i not in lookup:
                    lookup[col_i] = [row_i]
                else:
                    lookup[col_i].append(row_i)


    for from_user_id in lookup.keys():
        current_group = []

        if len(lookup[from_user_id]) > 0:
            follow_subusers(lookup, from_user_id, current_group)

        if len(current_group) > 0:
            #print(f"Found group: {current_group}")
            groups += 1

    return groups


raise NotImplementedError("We can't print the result of the call yet. We get an empty stack")
print(countGroups(["1100", "1110", "0110", "0001"]))
