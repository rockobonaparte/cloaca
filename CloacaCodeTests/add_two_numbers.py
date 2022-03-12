#raise NotImplementedError("Get 'System.Exception: TypeError: __init__ takes 3 position arguments but 2 were given'")

# Definition for singly-linked list.
class ListNode:
    def __init__(self, val=0, next=None):
        self.val = val
        self.next = next


def addTwoNumbers(l1: ListNode, l2: ListNode) -> ListNode:
    digit_sum = None
    carry = 0

    l1_iter = l1
    l2_iter = l2
    digit_iter = None

    while l1_iter is not None or l2_iter is not None:
        a = 0 if l1_iter is None else l1_iter.val
        b = 0 if l2_iter is None else l2_iter.val

        local_sum = a + b + carry
        carry = local_sum // 10
        local_sum %= 10

        if digit_sum is None:
            # First digit creates digit_sum reference we ultimately return.
            digit_iter = digit_sum = ListNode(local_sum)
        else:
            # Subsequent digits are tacked on and then we move up to them on the output.
            next_digit = ListNode(local_sum)
            digit_iter.next = next_digit
            digit_iter = next_digit

        l1_iter = l1_iter.next if l1_iter is not None else None
        l2_iter = l2_iter.next if l2_iter is not None else None

    # Process the rest of the carry
    while carry > 0:
        next_digit = ListNode(carry % 10)
        digit_iter.next = next_digit
        digit_iter = next_digit
        carry //= 10

    return digit_sum


def makeListNode(l: list):
    root = None
    for element in l:
        new_node = ListNode(element)
        if root is None:
            root = list_iter = new_node
        else:
            list_iter.next = new_node
            list_iter = new_node
    return root


def joinDigits(node: ListNode):
    joined = ""
    iter = node
    while iter is not None:
        joined += str(iter.val)
        iter = iter.next
    return joined

# Should be [8,9,9,9,0,0,0,1]
print(joinDigits(addTwoNumbers(makeListNode([9,9,9,9,9,9,9]), makeListNode([9,9,9,9]))))

