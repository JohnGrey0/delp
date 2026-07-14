namespace Delp.Core.Tools.DevUtilities;

public static partial class CodeCheatSheetData
{
    internal static IReadOnlyList<CheatTopic> DataStructuresTopics { get; } = new List<CheatTopic>
    {
        new(
            "stack",
            "Stack",
            "Data Structures",
            "A LIFO (last-in, first-out) collection where elements are added and removed from the same end, the \"top.\" Push and pop are both O(1), and stacks are the natural fit for undo history, expression parsing, backtracking, and call-stack-style recursion simulation.",
            new List<CodeSnippet>
            {
                new("C#", """
                    using System;
                    using System.Collections.Generic;

                    class Program
                    {
                        static void Main()
                        {
                            var stack = new Stack<int>();
                            stack.Push(1);
                            stack.Push(2);
                            stack.Push(3);

                            Console.WriteLine(stack.Peek()); // 3
                            Console.WriteLine(stack.Pop());  // 3
                            Console.WriteLine(stack.Pop());  // 2
                            Console.WriteLine(stack.Count);  // 1
                        }
                    }
                    """),
                new("Python", """
                    stack = []
                    stack.append(1)
                    stack.append(2)
                    stack.append(3)

                    print(stack[-1])   # 3 (peek)
                    print(stack.pop()) # 3
                    print(stack.pop()) # 2
                    print(len(stack))  # 1
                    """),
                new("JavaScript", """
                    const stack = [];
                    stack.push(1);
                    stack.push(2);
                    stack.push(3);

                    console.log(stack[stack.length - 1]); // 3 (peek)
                    console.log(stack.pop());             // 3
                    console.log(stack.pop());             // 2
                    console.log(stack.length);            // 1
                    """),
                new("TypeScript", """
                    const stack: number[] = [];
                    stack.push(1);
                    stack.push(2);
                    stack.push(3);

                    console.log(stack[stack.length - 1]); // 3 (peek)
                    console.log(stack.pop());             // 3
                    console.log(stack.pop());             // 2
                    console.log(stack.length);            // 1
                    """),
                new("Java", """
                    import java.util.ArrayDeque;
                    import java.util.Deque;

                    public class Main {
                        public static void main(String[] args) {
                            Deque<Integer> stack = new ArrayDeque<>();
                            stack.push(1);
                            stack.push(2);
                            stack.push(3);

                            System.out.println(stack.peek()); // 3
                            System.out.println(stack.pop());  // 3
                            System.out.println(stack.pop());  // 2
                            System.out.println(stack.size()); // 1
                        }
                    }
                    """),
                new("C++", """
                    #include <iostream>
                    #include <stack>

                    int main() {
                        std::stack<int> s;
                        s.push(1);
                        s.push(2);
                        s.push(3);

                        std::cout << s.top() << std::endl; // 3
                        s.pop();
                        std::cout << s.top() << std::endl; // 2
                        s.pop();
                        std::cout << s.size() << std::endl; // 1
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    func main() {
                        var stack []int
                        stack = append(stack, 1)
                        stack = append(stack, 2)
                        stack = append(stack, 3)

                        top := stack[len(stack)-1]
                        fmt.Println(top) // 3

                        stack = stack[:len(stack)-1]
                        fmt.Println(stack[len(stack)-1]) // 2

                        stack = stack[:len(stack)-1]
                        fmt.Println(len(stack)) // 1
                    }
                    """),
                new("Rust", """
                    fn main() {
                        let mut stack: Vec<i32> = Vec::new();
                        stack.push(1);
                        stack.push(2);
                        stack.push(3);

                        println!("{:?}", stack.last());  // Some(3)
                        println!("{:?}", stack.pop());    // Some(3)
                        println!("{:?}", stack.pop());    // Some(2)
                        println!("{}", stack.len());       // 1
                    }
                    """),
            }),
        new(
            "queue",
            "Queue",
            "Data Structures",
            "A FIFO (first-in, first-out) collection where elements are added at the back and removed from the front. Well-suited data structures give O(1) enqueue and dequeue; queues are used for breadth-first search, task scheduling, and buffering producer/consumer work.",
            new List<CodeSnippet>
            {
                new("C#", """
                    using System;
                    using System.Collections.Generic;

                    class Program
                    {
                        static void Main()
                        {
                            var queue = new Queue<int>();
                            queue.Enqueue(1);
                            queue.Enqueue(2);
                            queue.Enqueue(3);

                            Console.WriteLine(queue.Peek());   // 1
                            Console.WriteLine(queue.Dequeue()); // 1
                            Console.WriteLine(queue.Dequeue()); // 2
                            Console.WriteLine(queue.Count);     // 1
                        }
                    }
                    """),
                new("Python", """
                    from collections import deque

                    queue = deque()
                    queue.append(1)
                    queue.append(2)
                    queue.append(3)

                    print(queue[0])         # 1 (peek)
                    print(queue.popleft())  # 1
                    print(queue.popleft())  # 2
                    print(len(queue))       # 1
                    """),
                new("JavaScript", """
                    const queue = [];
                    queue.push(1);
                    queue.push(2);
                    queue.push(3);

                    console.log(queue[0]);     // 1 (peek)
                    console.log(queue.shift()); // 1
                    console.log(queue.shift()); // 2
                    console.log(queue.length);  // 1
                    """),
                new("TypeScript", """
                    const queue: number[] = [];
                    queue.push(1);
                    queue.push(2);
                    queue.push(3);

                    console.log(queue[0]);      // 1 (peek)
                    console.log(queue.shift()); // 1
                    console.log(queue.shift()); // 2
                    console.log(queue.length);  // 1
                    """),
                new("Java", """
                    import java.util.ArrayDeque;
                    import java.util.Deque;

                    public class Main {
                        public static void main(String[] args) {
                            Deque<Integer> queue = new ArrayDeque<>();
                            queue.offer(1);
                            queue.offer(2);
                            queue.offer(3);

                            System.out.println(queue.peek()); // 1
                            System.out.println(queue.poll());  // 1
                            System.out.println(queue.poll());  // 2
                            System.out.println(queue.size());  // 1
                        }
                    }
                    """),
                new("C++", """
                    #include <iostream>
                    #include <queue>

                    int main() {
                        std::queue<int> q;
                        q.push(1);
                        q.push(2);
                        q.push(3);

                        std::cout << q.front() << std::endl; // 1
                        q.pop();
                        std::cout << q.front() << std::endl; // 2
                        q.pop();
                        std::cout << q.size() << std::endl; // 1
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    func main() {
                        var queue []int
                        queue = append(queue, 1)
                        queue = append(queue, 2)
                        queue = append(queue, 3)

                        front := queue[0]
                        fmt.Println(front) // 1

                        queue = queue[1:]
                        fmt.Println(queue[0]) // 2

                        queue = queue[1:]
                        fmt.Println(len(queue)) // 1
                    }
                    """),
                new("Rust", """
                    use std::collections::VecDeque;

                    fn main() {
                        let mut queue: VecDeque<i32> = VecDeque::new();
                        queue.push_back(1);
                        queue.push_back(2);
                        queue.push_back(3);

                        println!("{:?}", queue.front());       // Some(1)
                        println!("{:?}", queue.pop_front());    // Some(1)
                        println!("{:?}", queue.pop_front());    // Some(2)
                        println!("{}", queue.len());             // 1
                    }
                    """),
            }),
        new(
            "hash-map",
            "Hash Map Usage",
            "Data Structures",
            "A hash map (dictionary) stores key/value pairs and gives average O(1) insertion, lookup, and deletion by hashing the key to a bucket. It is the go-to structure whenever you need fast lookups by a unique key, counting/grouping, or building an index, at the cost of no guaranteed ordering (unless the language variant preserves it).",
            new List<CodeSnippet>
            {
                new("C#", """
                    using System;
                    using System.Collections.Generic;

                    class Program
                    {
                        static void Main()
                        {
                            var ages = new Dictionary<string, int>
                            {
                                ["Alice"] = 30,
                                ["Bob"] = 25,
                            };
                            ages["Carol"] = 41;

                            if (ages.TryGetValue("Bob", out int bobAge))
                                Console.WriteLine($"Bob is {bobAge}");

                            if (!ages.TryGetValue("Dave", out _))
                                Console.WriteLine("Dave not found");

                            foreach (var (name, age) in ages)
                                Console.WriteLine($"{name}: {age}");
                        }
                    }
                    """),
                new("Python", """
                    ages = {"Alice": 30, "Bob": 25}
                    ages["Carol"] = 41

                    bob_age = ages.get("Bob")
                    if bob_age is not None:
                        print(f"Bob is {bob_age}")

                    dave_age = ages.get("Dave")
                    if dave_age is None:
                        print("Dave not found")

                    for name, age in ages.items():
                        print(f"{name}: {age}")
                    """),
                new("JavaScript", """
                    const ages = new Map([
                      ["Alice", 30],
                      ["Bob", 25],
                    ]);
                    ages.set("Carol", 41);

                    if (ages.has("Bob")) {
                      console.log(`Bob is ${ages.get("Bob")}`);
                    }

                    if (!ages.has("Dave")) {
                      console.log("Dave not found");
                    }

                    for (const [name, age] of ages) {
                      console.log(`${name}: ${age}`);
                    }
                    """),
                new("TypeScript", """
                    const ages = new Map<string, number>([
                      ["Alice", 30],
                      ["Bob", 25],
                    ]);
                    ages.set("Carol", 41);

                    const bobAge = ages.get("Bob");
                    if (bobAge !== undefined) {
                      console.log(`Bob is ${bobAge}`);
                    }

                    if (!ages.has("Dave")) {
                      console.log("Dave not found");
                    }

                    for (const [name, age] of ages) {
                      console.log(`${name}: ${age}`);
                    }
                    """),
                new("Java", """
                    import java.util.HashMap;
                    import java.util.Map;

                    public class Main {
                        public static void main(String[] args) {
                            Map<String, Integer> ages = new HashMap<>();
                            ages.put("Alice", 30);
                            ages.put("Bob", 25);
                            ages.put("Carol", 41);

                            Integer bobAge = ages.get("Bob");
                            if (bobAge != null) {
                                System.out.println("Bob is " + bobAge);
                            }

                            if (!ages.containsKey("Dave")) {
                                System.out.println("Dave not found");
                            }

                            for (Map.Entry<String, Integer> entry : ages.entrySet()) {
                                System.out.println(entry.getKey() + ": " + entry.getValue());
                            }
                        }
                    }
                    """),
                new("C++", """
                    #include <iostream>
                    #include <string>
                    #include <unordered_map>

                    int main() {
                        std::unordered_map<std::string, int> ages{
                            {"Alice", 30},
                            {"Bob", 25},
                        };
                        ages["Carol"] = 41;

                        auto it = ages.find("Bob");
                        if (it != ages.end())
                            std::cout << "Bob is " << it->second << std::endl;

                        if (ages.find("Dave") == ages.end())
                            std::cout << "Dave not found" << std::endl;

                        for (const auto& [name, age] : ages)
                            std::cout << name << ": " << age << std::endl;
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    func main() {
                        ages := map[string]int{
                            "Alice": 30,
                            "Bob":   25,
                        }
                        ages["Carol"] = 41

                        if bobAge, ok := ages["Bob"]; ok {
                            fmt.Println("Bob is", bobAge)
                        }

                        if _, ok := ages["Dave"]; !ok {
                            fmt.Println("Dave not found")
                        }

                        for name, age := range ages {
                            fmt.Println(name, age)
                        }
                    }
                    """),
                new("Rust", """
                    use std::collections::HashMap;

                    fn main() {
                        let mut ages: HashMap<String, i32> = HashMap::new();
                        ages.insert("Alice".to_string(), 30);
                        ages.insert("Bob".to_string(), 25);
                        ages.insert("Carol".to_string(), 41);

                        if let Some(bob_age) = ages.get("Bob") {
                            println!("Bob is {}", bob_age);
                        }

                        if ages.get("Dave").is_none() {
                            println!("Dave not found");
                        }

                        for (name, age) in &ages {
                            println!("{}: {}", name, age);
                        }
                    }
                    """),
            }),
        new(
            "linked-list",
            "Linked List",
            "Data Structures",
            "A singly-linked list is a sequence of nodes where each node holds a value and a reference/pointer to the next node, allowing O(1) insertion at the head and O(n) traversal or search. It avoids the contiguous-memory resize cost of arrays, making it useful when frequent insertions/removals at known positions matter more than random access.",
            new List<CodeSnippet>
            {
                new("C#", """
                    using System;

                    class Node
                    {
                        public int Value;
                        public Node? Next;
                        public Node(int value) => Value = value;
                    }

                    class LinkedList
                    {
                        private Node? _head;

                        public void InsertFront(int value)
                        {
                            var node = new Node(value) { Next = _head };
                            _head = node;
                        }

                        public void Print()
                        {
                            for (var cur = _head; cur is not null; cur = cur.Next)
                                Console.Write($"{cur.Value} -> ");
                            Console.WriteLine("null");
                        }
                    }

                    class Program
                    {
                        static void Main()
                        {
                            var list = new LinkedList();
                            list.InsertFront(3);
                            list.InsertFront(2);
                            list.InsertFront(1);
                            list.Print(); // 1 -> 2 -> 3 -> null
                        }
                    }
                    """),
                new("Python", """
                    class Node:
                        def __init__(self, value):
                            self.value = value
                            self.next = None


                    class LinkedList:
                        def __init__(self):
                            self.head = None

                        def insert_front(self, value):
                            node = Node(value)
                            node.next = self.head
                            self.head = node

                        def __str__(self):
                            values = []
                            cur = self.head
                            while cur:
                                values.append(str(cur.value))
                                cur = cur.next
                            return " -> ".join(values + ["None"])


                    if __name__ == "__main__":
                        lst = LinkedList()
                        lst.insert_front(3)
                        lst.insert_front(2)
                        lst.insert_front(1)
                        print(lst)  # 1 -> 2 -> 3 -> None
                    """),
                new("JavaScript", """
                    class Node {
                      constructor(value) {
                        this.value = value;
                        this.next = null;
                      }
                    }

                    class LinkedList {
                      constructor() {
                        this.head = null;
                      }

                      insertFront(value) {
                        const node = new Node(value);
                        node.next = this.head;
                        this.head = node;
                      }

                      toString() {
                        const values = [];
                        let cur = this.head;
                        while (cur) {
                          values.push(cur.value);
                          cur = cur.next;
                        }
                        return values.concat("null").join(" -> ");
                      }
                    }

                    const list = new LinkedList();
                    list.insertFront(3);
                    list.insertFront(2);
                    list.insertFront(1);
                    console.log(list.toString()); // 1 -> 2 -> 3 -> null
                    """),
                new("TypeScript", """
                    class Node<T> {
                      value: T;
                      next: Node<T> | null = null;
                      constructor(value: T) {
                        this.value = value;
                      }
                    }

                    class LinkedList<T> {
                      private head: Node<T> | null = null;

                      insertFront(value: T): void {
                        const node = new Node(value);
                        node.next = this.head;
                        this.head = node;
                      }

                      toString(): string {
                        const values: string[] = [];
                        let cur = this.head;
                        while (cur) {
                          values.push(String(cur.value));
                          cur = cur.next;
                        }
                        return values.concat("null").join(" -> ");
                      }
                    }

                    const list = new LinkedList<number>();
                    list.insertFront(3);
                    list.insertFront(2);
                    list.insertFront(1);
                    console.log(list.toString()); // 1 -> 2 -> 3 -> null
                    """),
                new("Java", """
                    public class Main {
                        static class Node {
                            int value;
                            Node next;
                            Node(int value) { this.value = value; }
                        }

                        static class LinkedList {
                            Node head;

                            void insertFront(int value) {
                                Node node = new Node(value);
                                node.next = head;
                                head = node;
                            }

                            public String toString() {
                                StringBuilder sb = new StringBuilder();
                                for (Node cur = head; cur != null; cur = cur.next)
                                    sb.append(cur.value).append(" -> ");
                                sb.append("null");
                                return sb.toString();
                            }
                        }

                        public static void main(String[] args) {
                            LinkedList list = new LinkedList();
                            list.insertFront(3);
                            list.insertFront(2);
                            list.insertFront(1);
                            System.out.println(list); // 1 -> 2 -> 3 -> null
                        }
                    }
                    """),
                new("C++", """
                    #include <iostream>

                    struct Node {
                        int value;
                        Node* next;
                        explicit Node(int v) : value(v), next(nullptr) {}
                    };

                    class LinkedList {
                    public:
                        ~LinkedList() {
                            while (head) {
                                Node* next = head->next;
                                delete head;
                                head = next;
                            }
                        }

                        void insertFront(int value) {
                            Node* node = new Node(value);
                            node->next = head;
                            head = node;
                        }

                        void print() const {
                            for (Node* cur = head; cur; cur = cur->next)
                                std::cout << cur->value << " -> ";
                            std::cout << "null" << std::endl;
                        }

                    private:
                        Node* head = nullptr;
                    };

                    int main() {
                        LinkedList list;
                        list.insertFront(3);
                        list.insertFront(2);
                        list.insertFront(1);
                        list.print(); // 1 -> 2 -> 3 -> null
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    type Node struct {
                        Value int
                        Next  *Node
                    }

                    type LinkedList struct {
                        Head *Node
                    }

                    func (l *LinkedList) InsertFront(value int) {
                        l.Head = &Node{Value: value, Next: l.Head}
                    }

                    func (l *LinkedList) String() string {
                        s := ""
                        for cur := l.Head; cur != nil; cur = cur.Next {
                            s += fmt.Sprintf("%d -> ", cur.Value)
                        }
                        return s + "nil"
                    }

                    func main() {
                        list := &LinkedList{}
                        list.InsertFront(3)
                        list.InsertFront(2)
                        list.InsertFront(1)
                        fmt.Println(list) // 1 -> 2 -> 3 -> nil
                    }
                    """),
                new("Rust", """
                    struct Node {
                        value: i32,
                        next: Option<Box<Node>>,
                    }

                    struct LinkedList {
                        head: Option<Box<Node>>,
                    }

                    impl LinkedList {
                        fn new() -> Self {
                            LinkedList { head: None }
                        }

                        fn insert_front(&mut self, value: i32) {
                            let new_node = Box::new(Node {
                                value,
                                next: self.head.take(),
                            });
                            self.head = Some(new_node);
                        }

                        fn print(&self) {
                            let mut cur = &self.head;
                            while let Some(node) = cur {
                                print!("{} -> ", node.value);
                                cur = &node.next;
                            }
                            println!("None");
                        }
                    }

                    fn main() {
                        let mut list = LinkedList::new();
                        list.insert_front(3);
                        list.insert_front(2);
                        list.insert_front(1);
                        list.print(); // 1 -> 2 -> 3 -> None
                    }
                    """),
            }),
        new(
            "binary-tree-traversal",
            "Binary Tree + Traversal (In/Pre/Post-order)",
            "Data Structures",
            "A binary tree is a hierarchical structure where each node has at most two children (left/right); a binary search tree additionally keeps left descendants smaller and right descendants larger than the node, giving average O(log n) insert/search. The three classic depth-first traversals visit nodes in different orders: in-order (left, node, right) yields sorted output for a BST, pre-order (node, left, right) is useful for copying/serializing the tree, and post-order (left, right, node) is useful for safely deleting or evaluating expression trees bottom-up.",
            new List<CodeSnippet>
            {
                new("C#", """
                    using System;
                    using System.Collections.Generic;

                    class TreeNode
                    {
                        public int Value;
                        public TreeNode? Left, Right;
                        public TreeNode(int value) => Value = value;
                    }

                    class Program
                    {
                        static TreeNode? Insert(TreeNode? node, int value)
                        {
                            if (node is null) return new TreeNode(value);
                            if (value < node.Value) node.Left = Insert(node.Left, value);
                            else node.Right = Insert(node.Right, value);
                            return node;
                        }

                        static void InOrder(TreeNode? n, List<int> acc)
                        {
                            if (n is null) return;
                            InOrder(n.Left, acc);
                            acc.Add(n.Value);
                            InOrder(n.Right, acc);
                        }

                        static void PreOrder(TreeNode? n, List<int> acc)
                        {
                            if (n is null) return;
                            acc.Add(n.Value);
                            PreOrder(n.Left, acc);
                            PreOrder(n.Right, acc);
                        }

                        static void PostOrder(TreeNode? n, List<int> acc)
                        {
                            if (n is null) return;
                            PostOrder(n.Left, acc);
                            PostOrder(n.Right, acc);
                            acc.Add(n.Value);
                        }

                        static void Main()
                        {
                            TreeNode? root = null;
                            foreach (int v in new[] { 5, 3, 8, 1, 4 }) root = Insert(root, v);

                            var inOrder = new List<int>(); InOrder(root, inOrder);
                            var preOrder = new List<int>(); PreOrder(root, preOrder);
                            var postOrder = new List<int>(); PostOrder(root, postOrder);

                            Console.WriteLine("In-order: " + string.Join(",", inOrder));
                            Console.WriteLine("Pre-order: " + string.Join(",", preOrder));
                            Console.WriteLine("Post-order: " + string.Join(",", postOrder));
                        }
                    }
                    """),
                new("Python", """
                    class TreeNode:
                        def __init__(self, value):
                            self.value = value
                            self.left = None
                            self.right = None


                    def insert(node, value):
                        if node is None:
                            return TreeNode(value)
                        if value < node.value:
                            node.left = insert(node.left, value)
                        else:
                            node.right = insert(node.right, value)
                        return node


                    def in_order(node, acc):
                        if node is None:
                            return
                        in_order(node.left, acc)
                        acc.append(node.value)
                        in_order(node.right, acc)


                    def pre_order(node, acc):
                        if node is None:
                            return
                        acc.append(node.value)
                        pre_order(node.left, acc)
                        pre_order(node.right, acc)


                    def post_order(node, acc):
                        if node is None:
                            return
                        post_order(node.left, acc)
                        post_order(node.right, acc)
                        acc.append(node.value)


                    if __name__ == "__main__":
                        root = None
                        for v in [5, 3, 8, 1, 4]:
                            root = insert(root, v)

                        in_acc, pre_acc, post_acc = [], [], []
                        in_order(root, in_acc)
                        pre_order(root, pre_acc)
                        post_order(root, post_acc)
                        print("In-order:", in_acc)
                        print("Pre-order:", pre_acc)
                        print("Post-order:", post_acc)
                    """),
                new("JavaScript", """
                    class TreeNode {
                      constructor(value) {
                        this.value = value;
                        this.left = null;
                        this.right = null;
                      }
                    }

                    function insert(node, value) {
                      if (node === null) return new TreeNode(value);
                      if (value < node.value) node.left = insert(node.left, value);
                      else node.right = insert(node.right, value);
                      return node;
                    }

                    function inOrder(node, acc) {
                      if (node === null) return;
                      inOrder(node.left, acc);
                      acc.push(node.value);
                      inOrder(node.right, acc);
                    }

                    function preOrder(node, acc) {
                      if (node === null) return;
                      acc.push(node.value);
                      preOrder(node.left, acc);
                      preOrder(node.right, acc);
                    }

                    function postOrder(node, acc) {
                      if (node === null) return;
                      postOrder(node.left, acc);
                      postOrder(node.right, acc);
                      acc.push(node.value);
                    }

                    let root = null;
                    for (const v of [5, 3, 8, 1, 4]) root = insert(root, v);

                    const inAcc = [], preAcc = [], postAcc = [];
                    inOrder(root, inAcc);
                    preOrder(root, preAcc);
                    postOrder(root, postAcc);
                    console.log("In-order:", inAcc);
                    console.log("Pre-order:", preAcc);
                    console.log("Post-order:", postAcc);
                    """),
                new("TypeScript", """
                    class TreeNode {
                      value: number;
                      left: TreeNode | null = null;
                      right: TreeNode | null = null;
                      constructor(value: number) {
                        this.value = value;
                      }
                    }

                    function insert(node: TreeNode | null, value: number): TreeNode {
                      if (node === null) return new TreeNode(value);
                      if (value < node.value) node.left = insert(node.left, value);
                      else node.right = insert(node.right, value);
                      return node;
                    }

                    function inOrder(node: TreeNode | null, acc: number[]): void {
                      if (node === null) return;
                      inOrder(node.left, acc);
                      acc.push(node.value);
                      inOrder(node.right, acc);
                    }

                    function preOrder(node: TreeNode | null, acc: number[]): void {
                      if (node === null) return;
                      acc.push(node.value);
                      preOrder(node.left, acc);
                      preOrder(node.right, acc);
                    }

                    function postOrder(node: TreeNode | null, acc: number[]): void {
                      if (node === null) return;
                      postOrder(node.left, acc);
                      postOrder(node.right, acc);
                      acc.push(node.value);
                    }

                    let root: TreeNode | null = null;
                    for (const v of [5, 3, 8, 1, 4]) root = insert(root, v);

                    const inAcc: number[] = [], preAcc: number[] = [], postAcc: number[] = [];
                    inOrder(root, inAcc);
                    preOrder(root, preAcc);
                    postOrder(root, postAcc);
                    console.log("In-order:", inAcc);
                    console.log("Pre-order:", preAcc);
                    console.log("Post-order:", postAcc);
                    """),
                new("Java", """
                    import java.util.ArrayList;
                    import java.util.List;

                    public class Main {
                        static class TreeNode {
                            int value;
                            TreeNode left, right;
                            TreeNode(int value) { this.value = value; }
                        }

                        static TreeNode insert(TreeNode node, int value) {
                            if (node == null) return new TreeNode(value);
                            if (value < node.value) node.left = insert(node.left, value);
                            else node.right = insert(node.right, value);
                            return node;
                        }

                        static void inOrder(TreeNode n, List<Integer> acc) {
                            if (n == null) return;
                            inOrder(n.left, acc);
                            acc.add(n.value);
                            inOrder(n.right, acc);
                        }

                        static void preOrder(TreeNode n, List<Integer> acc) {
                            if (n == null) return;
                            acc.add(n.value);
                            preOrder(n.left, acc);
                            preOrder(n.right, acc);
                        }

                        static void postOrder(TreeNode n, List<Integer> acc) {
                            if (n == null) return;
                            postOrder(n.left, acc);
                            postOrder(n.right, acc);
                            acc.add(n.value);
                        }

                        public static void main(String[] args) {
                            TreeNode root = null;
                            for (int v : new int[] {5, 3, 8, 1, 4})
                                root = insert(root, v);

                            List<Integer> inAcc = new ArrayList<>();
                            List<Integer> preAcc = new ArrayList<>();
                            List<Integer> postAcc = new ArrayList<>();
                            inOrder(root, inAcc);
                            preOrder(root, preAcc);
                            postOrder(root, postAcc);
                            System.out.println("In-order: " + inAcc);
                            System.out.println("Pre-order: " + preAcc);
                            System.out.println("Post-order: " + postAcc);
                        }
                    }
                    """),
                new("C++", """
                    #include <iostream>
                    #include <vector>

                    struct TreeNode {
                        int value;
                        TreeNode* left = nullptr;
                        TreeNode* right = nullptr;
                        explicit TreeNode(int v) : value(v) {}
                    };

                    TreeNode* insert(TreeNode* node, int value) {
                        if (!node) return new TreeNode(value);
                        if (value < node->value) node->left = insert(node->left, value);
                        else node->right = insert(node->right, value);
                        return node;
                    }

                    void inOrder(TreeNode* n, std::vector<int>& acc) {
                        if (!n) return;
                        inOrder(n->left, acc);
                        acc.push_back(n->value);
                        inOrder(n->right, acc);
                    }

                    void preOrder(TreeNode* n, std::vector<int>& acc) {
                        if (!n) return;
                        acc.push_back(n->value);
                        preOrder(n->left, acc);
                        preOrder(n->right, acc);
                    }

                    void postOrder(TreeNode* n, std::vector<int>& acc) {
                        if (!n) return;
                        postOrder(n->left, acc);
                        postOrder(n->right, acc);
                        acc.push_back(n->value);
                    }

                    void printVec(const std::vector<int>& v) {
                        for (int x : v) std::cout << x << " ";
                        std::cout << std::endl;
                    }

                    int main() {
                        TreeNode* root = nullptr;
                        for (int v : {5, 3, 8, 1, 4}) root = insert(root, v);

                        std::vector<int> inAcc, preAcc, postAcc;
                        inOrder(root, inAcc);
                        preOrder(root, preAcc);
                        postOrder(root, postAcc);
                        printVec(inAcc);
                        printVec(preAcc);
                        printVec(postAcc);
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    type TreeNode struct {
                        Value       int
                        Left, Right *TreeNode
                    }

                    func insert(node *TreeNode, value int) *TreeNode {
                        if node == nil {
                            return &TreeNode{Value: value}
                        }
                        if value < node.Value {
                            node.Left = insert(node.Left, value)
                        } else {
                            node.Right = insert(node.Right, value)
                        }
                        return node
                    }

                    func inOrder(n *TreeNode, acc *[]int) {
                        if n == nil {
                            return
                        }
                        inOrder(n.Left, acc)
                        *acc = append(*acc, n.Value)
                        inOrder(n.Right, acc)
                    }

                    func preOrder(n *TreeNode, acc *[]int) {
                        if n == nil {
                            return
                        }
                        *acc = append(*acc, n.Value)
                        preOrder(n.Left, acc)
                        preOrder(n.Right, acc)
                    }
                    func postOrder(n *TreeNode, acc *[]int) {
                        if n == nil {
                            return
                        }
                        postOrder(n.Left, acc)
                        postOrder(n.Right, acc)
                        *acc = append(*acc, n.Value)
                    }
                    func main() {
                        var root *TreeNode
                        for _, v := range []int{5, 3, 8, 1, 4} {
                            root = insert(root, v)
                        }
                        var inAcc, preAcc, postAcc []int
                        inOrder(root, &inAcc)
                        preOrder(root, &preAcc)
                        postOrder(root, &postAcc)
                        fmt.Println("In-order:", inAcc)
                        fmt.Println("Pre-order:", preAcc)
                        fmt.Println("Post-order:", postAcc)
                    }
                    """),
                new("Rust", """
                    struct TreeNode {
                        value: i32,
                        left: Option<Box<TreeNode>>,
                        right: Option<Box<TreeNode>>,
                    }

                    fn insert(node: Option<Box<TreeNode>>, value: i32) -> Option<Box<TreeNode>> {
                        match node {
                            None => Some(Box::new(TreeNode { value, left: None, right: None })),
                            Some(mut n) => {
                                if value < n.value {
                                    n.left = insert(n.left.take(), value);
                                } else {
                                    n.right = insert(n.right.take(), value);
                                }
                                Some(n)
                            }
                        }
                    }

                    fn in_order(node: &Option<Box<TreeNode>>, acc: &mut Vec<i32>) {
                        if let Some(n) = node {
                            in_order(&n.left, acc);
                            acc.push(n.value);
                            in_order(&n.right, acc);
                        }
                    }

                    fn pre_order(node: &Option<Box<TreeNode>>, acc: &mut Vec<i32>) {
                        if let Some(n) = node {
                            acc.push(n.value);
                            pre_order(&n.left, acc);
                            pre_order(&n.right, acc);
                        }
                    }

                    fn post_order(node: &Option<Box<TreeNode>>, acc: &mut Vec<i32>) {
                        if let Some(n) = node {
                            post_order(&n.left, acc);
                            post_order(&n.right, acc);
                            acc.push(n.value);
                        }
                    }

                    fn main() {
                        let mut root: Option<Box<TreeNode>> = None;
                        for v in [5, 3, 8, 1, 4] {
                            root = insert(root, v);
                        }

                        let (mut in_acc, mut pre_acc, mut post_acc) = (Vec::new(), Vec::new(), Vec::new());
                        in_order(&root, &mut in_acc);
                        pre_order(&root, &mut pre_acc);
                        post_order(&root, &mut post_acc);
                        println!("In-order: {:?}", in_acc);
                        println!("Pre-order: {:?}", pre_acc);
                        println!("Post-order: {:?}", post_acc);
                    }
                    """),
            }),
    };
}
