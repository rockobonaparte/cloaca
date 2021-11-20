"""
https://www.hackerrank.com/challenges/stockmax/problem

Your algorithms have become so good at predicting the market that you now know what the share price of Wooden Orange
Toothpicks Inc. (WOT) will be for the next number of days.


Each day, you can either buy one share of WOT, sell any number of shares of WOT that you own, or not make any
transaction at all. What is the maximum profit you can obtain with an optimum trading strategy?

For example, if you know that prices for the next two days are , you should buy one share day one, and sell it day two
for a profit of. If they are instead, no profit can be made so you don't buy or sell stock those days.
"""

def stockmax(prices):
    profit = 0
    best_max = 0
    for i in range(len(prices)-1, -1, -1):
        best_max = max(best_max, prices[i])
        profit += best_max - prices[i]          # We never record a loss because we always know the best outcome

    return profit


assert stockmax([5, 4, 3, 2, 1]) == 0
assert stockmax([1, 2, 3, 4, 5]) == 10
print("Success!")
