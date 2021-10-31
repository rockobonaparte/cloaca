raise NotImplementedError("Getting an NPE trying to parse this code")

class KeyValue:
    def __init__(self, key, value):
        self.key = key
        self.value = value

    def __repr__(self):
        return f"{self.key}->{self.value}"


class MinHeap:
    def __init__(self, start_size):
        self.heap = [None] * start_size
        self.next_i = 0

    def add(self, key, value):
        self.heap[self.next_i] = KeyValue(key, value)
        child_i = self.next_i
        parent_i = child_i // 2
        while child_i != parent_i:

            if self.heap[child_i].key < self.heap[parent_i].key:
                swapper = self.heap[child_i]
                self.heap[child_i] = self.heap[parent_i]
                self.heap[parent_i] = swapper

            child_i = parent_i
            parent_i //= 2

        self.next_i += 1

    def get(self):
        if self.next_i == 0:
            return None
        elif self.next_i == 1:
            bye_bye_root = self.heap[0]
            self.heap[0] = None
            return bye_bye_root
        else:
            bye_bye_root = self.heap[0]
            self.next_i -= 1
            self.heap[0] = self.heap[self.next_i]
            self.heap[self.next_i] = None

            # Heapify
            parent_i = 0
            while 2 * parent_i < len(self.heap) and self.heap[parent_i] is not None:
                heapify_parent = self.heap[parent_i]
                lchild_i = 2*parent_i + 1
                rchild_i = 2*parent_i + 2
                lchild = self.heap[lchild_i]
                rchild = self.heap[rchild_i]

                best = heapify_parent
                best_i = parent_i

                if lchild is not None and lchild.key < best.key:
                    best = lchild
                    best_i = lchild_i
                if rchild is not None and rchild.key < best.key:
                    best = rchild
                    best_i = rchild_i

                if heapify_parent != best:
                    swapper = self.heap[best_i]
                    self.heap[best_i] = heapify_parent
                    self.heap[parent_i] = swapper

                    parent_i = best_i
                else:
                    break

            return bye_bye_root


min_heap = MinHeap(16)
min_heap.add(2, 2)
min_heap.add(3, 3)
min_heap.add(4, 4)
min_heap.add(1, 1)

print(min_heap.get().key)
print(min_heap.get().key)
print(min_heap.get().key)
print(min_heap.get().key)

