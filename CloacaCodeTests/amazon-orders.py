raise NotImplementedError("find not implemented for str yet, nor is sorted")

def prioritizedOrders(numOrders, orderList):
    # Will assume orderList should not be mutated, since a new list is expected
    # as output.

    # Split between prime and non-prime orders
    # Appending non-prime orders will preserve their original order
    prime = []
    non_prime = []
    for order in orderList:
        first_space = order.find(" ")
        # We are only test the first character of the metadata. Prime
        # orders must only use alphabet characters, and non-prime orders
        # must only use numeric characters
        if order[first_space + 1:][0].isdigit():
            non_prime.append(order)
        else:
            prime.append(order)

    # Helper to sorted that will search first by the metadata then by the
    # lexagraphic ID. We can do this by using [metadata][Id] as the key
    # First identifier is only used when there's a tie with the previous metadata
    def metadata_then_id_key(x):
        first_space = x.find(" ")
        return x[first_space + 1:] + x[:first_space]

    return sorted(prime, key=metadata_then_id_key) + non_prime


# def prioritizedOrders(numOrders, orderList):
#     # Will assume orderList should not be mutated, since a new list is expected
#     # as output.
#
#     # Split between prime and non-prime orders
#     # Appending non-prime orders will preserve their original order
#     prime = []
#     non_prime = []
#     for order in orderList:
#         first_space = order.find(" ")
#         # We are only test the first character of the metadata. Prime
#         # orders must only use alphabet characters, and non-prime orders
#         # must only use numeric characters
#         if order[first_space + 1:].lstrip()[0].isdigit():
#             non_prime.append(order)
#         else:
#             prime.append(order)
#
#     # Helper to sorted that will search first by the metadata then by the
#     # lexagraphic ID. We can do this by using [metadata][Id] as the key
#     # First identifier is only used when there's a tie with the previous metadata
#     def metadata_then_id_key(x):
#         first_space = x.find(" ")
#         return x[first_space + 1:].lstrip() + x[:first_space]
#
#     return sorted(prime, key=metadata_then_id_key) + non_prime


orders = ["zld 93 12", "fp kindle book", "10a echo show", "17g 12 25 6", "ab1 kindle book", "125 echo dot second generation"]
#print(prioritizedOrders(6, orders))
#print(prioritizedOrders(4, ["fp kindle book", "10a echo show", "ab1 kindle book", "125 echo dot second generation"]))
#print(prioritizedOrders(4, ["f kindle book", "1 echo show", "a kindle book", "1 echo dot second generation"]))
#print(prioritizedOrders(3, ["1 aaa a", "2 aa aa", "3 a aaa"]))
print(prioritizedOrders(2, ["zld 93 12", "17g 12 25 6"]))
print(prioritizedOrders(2, ["17g 12 25 6", "zld 93 12"]))


