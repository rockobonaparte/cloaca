# Smallest amount of change (coins, whatever)
# We don't actually need to know which coins, just how few we can use.
#
# Scenario 1
# Coin types: [1, 2, 5]
# Change: 11
# Solution: 3  [5, 5, 1]
#
# Scenario 2
# Coin types: [1, 4, 5]
# Change: 13
# Solution: [5, 4, 4]
# (This one is the one that keeps us from just going right-to-left, decrementing until we can't any more)
#
############################################################################################################

def smallest_change(denominations: list[int], total):
    # Create a list for each subtotal up to the total to calculate smallest
    # change for them instead. We can reference these for the last element (our total)

    subtotals = [0] * (total + 1)

    def get_subdenomination(subtotal, denomination):
        nonlocal subtotals
        reduced = subtotal - denomination
        if reduced < 0:
            return None
        return 1 + subtotals[reduced]

    for i in range(1, len(subtotals), 1):
        best = total
        for denom in denominations:
            subcount = get_subdenomination(i, denom)
            if subcount is None:
                break
            best = min(best, subcount)
        subtotals[i] = best

    return subtotals[-1]


print(smallest_change([1, 2, 5], 11))
print(smallest_change([1, 4, 5], 13))

