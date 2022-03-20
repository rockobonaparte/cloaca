# https://www.geeksforgeeks.org/the-stock-span-problem/
prices = [100, 80, 60, 70, 60, 75, 85]

# answer should be 1, 1, 1, 2, 1, 4, 6



def span():
    spans = [None] * len(prices)
    stack = [len(prices)-1]
    for i in range(len(prices)-2,-1,-1):
        if prices[i] < prices[i + 1]:
            stack.append(i)
        else:
            while len(stack) > 0 and prices[stack[-1]] < prices[i]:
                spans[stack[-1]] = stack[-1] - i
                stack.pop()
            stack.append(i)

    spans[0] = 1
    return spans


assert(span() == [1, 1, 1, 2, 1, 4, 6])
print("Success!")
