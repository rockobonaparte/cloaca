station_costs = [[3, 4],
                 [7, 8],
                 [9, 10]]

crossover_costs = [[None, None],
                   [5, 6],
                   [11, 12]]

entrance_costs = [1, 2]

exit_costs = [13, 14]


def find_cost(lr_i, station_i, current_cost):
    if station_i == len(station_costs):
        print(current_cost + exit_costs[lr_i])
        return

    new_cost = current_cost + station_costs[station_i][lr_i]
    print("\t%s -> %s" % (station_costs[station_i][lr_i], new_cost))

    # Stay in our lane
    find_cost(lr_i, station_i+1, new_cost)

    # Change lane
    if station_i < len(station_costs)-1:
        find_cost(not lr_i, station_i+1, new_cost+crossover_costs[station_i+1][lr_i])


# find_cost(0, 0, entrance_costs[0])
# find_cost(1, 0, entrance_costs[1])


def iterate_cost():
    prev_l = entrance_costs[0] + station_costs[0][0]
    prev_r = entrance_costs[1] + station_costs[1][0]
    for i in range(1, len(station_costs), 1):
        sum_l = station_costs[i][0] + min(prev_l, prev_r + crossover_costs[i][1])
        sum_r = station_costs[i][1] + min(prev_r, prev_l + crossover_costs[i][0])
        prev_l = sum_l
        prev_r = sum_r
    sum_l += exit_costs[0]
    sum_r += exit_costs[1]

    return min(sum_l, sum_r)

assert(iterate_cost() == 33)
print("Success!")
