namespace Delp.Core.Tools.DevUtilities;

public static partial class CodeCheatSheetData
{
    internal static IReadOnlyList<CheatTopic> LanguageConstructsTopics { get; } = new List<CheatTopic>
    {
        new(
            "class-inheritance",
            "Class + Inheritance",
            "Language Constructs",
            "Inheritance lets a derived type reuse and specialize the behavior of a base type by overriding "
            + "virtual/overridable members. Use it to model true \"is-a\" relationships where subtypes share "
            + "structure but need different behavior for one or more operations. Calling the overridden member "
            + "through a base-typed reference (polymorphism) dispatches to the derived implementation at runtime. "
            + "Languages without classical inheritance (Go, Rust) favor composition-based alternatives instead.",
            new List<CodeSnippet>
            {
                new("C#", """
                    abstract class Animal
                    {
                        public string Name { get; }
                        protected Animal(string name) => Name = name;

                        public virtual string Speak() => $"{Name} makes a sound";
                    }

                    class Dog : Animal
                    {
                        public Dog(string name) : base(name) { }

                        public override string Speak() => $"{Name} says Woof";
                    }

                    Animal animal = new Dog("Rex");
                    Console.WriteLine(animal.Speak()); // Rex says Woof
                    """),
                new("Python", """
                    class Animal:
                        def __init__(self, name: str) -> None:
                            self.name = name

                        def speak(self) -> str:
                            return f"{self.name} makes a sound"


                    class Dog(Animal):
                        def speak(self) -> str:
                            return f"{self.name} says Woof"


                    animal: Animal = Dog("Rex")
                    print(animal.speak())  # Rex says Woof
                    """),
                new("JavaScript", """
                    class Animal {
                      constructor(name) {
                        this.name = name;
                      }

                      speak() {
                        return `${this.name} makes a sound`;
                      }
                    }

                    class Dog extends Animal {
                      speak() {
                        return `${this.name} says Woof`;
                      }
                    }

                    const animal = new Dog("Rex");
                    console.log(animal.speak()); // Rex says Woof
                    """),
                new("TypeScript", """
                    class Animal {
                      constructor(protected name: string) {}

                      speak(): string {
                        return `${this.name} makes a sound`;
                      }
                    }

                    class Dog extends Animal {
                      override speak(): string {
                        return `${this.name} says Woof`;
                      }
                    }

                    const animal: Animal = new Dog("Rex");
                    console.log(animal.speak()); // Rex says Woof
                    """),
                new("Java", """
                    abstract class Animal {
                        protected final String name;

                        protected Animal(String name) {
                            this.name = name;
                        }

                        public String speak() {
                            return name + " makes a sound";
                        }
                    }

                    class Dog extends Animal {
                        public Dog(String name) {
                            super(name);
                        }

                        @Override
                        public String speak() {
                            return name + " says Woof";
                        }
                    }

                    Animal animal = new Dog("Rex");
                    System.out.println(animal.speak()); // Rex says Woof
                    """),
                new("C++", """
                    #include <iostream>
                    #include <string>
                    #include <memory>

                    class Animal {
                    public:
                        explicit Animal(std::string name) : name_(std::move(name)) {}
                        virtual ~Animal() = default;

                        virtual std::string speak() const {
                            return name_ + " makes a sound";
                        }

                    protected:
                        std::string name_;
                    };

                    class Dog : public Animal {
                    public:
                        explicit Dog(std::string name) : Animal(std::move(name)) {}

                        std::string speak() const override {
                            return name_ + " says Woof";
                        }
                    };

                    int main() {
                        std::unique_ptr<Animal> animal = std::make_unique<Dog>("Rex");
                        std::cout << animal->speak() << "\n"; // Rex says Woof
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    // Go has no classical inheritance; struct embedding + method
                    // promotion is the idiomatic equivalent of "inherit and override".
                    type Animal struct {
                        Name string
                    }

                    func (a Animal) Speak() string {
                        return a.Name + " makes a sound"
                    }

                    type Dog struct {
                        Animal // embedded: promotes Animal's fields/methods
                    }

                    // Redeclaring Speak on Dog shadows the embedded Animal.Speak.
                    func (d Dog) Speak() string {
                        return d.Name + " says Woof"
                    }

                    func main() {
                        d := Dog{Animal{Name: "Rex"}}
                        fmt.Println(d.Speak()) // Rex says Woof
                    }
                    """),
                new("Rust", """
                    // Rust has no classical inheritance; a trait with a default
                    // method plus per-type overrides, dispatched via trait objects,
                    // is the idiomatic equivalent.
                    trait Animal {
                        fn name(&self) -> &str;

                        fn speak(&self) -> String {
                            format!("{} makes a sound", self.name())
                        }
                    }

                    struct Dog {
                        name: String,
                    }

                    impl Animal for Dog {
                        fn name(&self) -> &str {
                            &self.name
                        }

                        fn speak(&self) -> String {
                            format!("{} says Woof", self.name())
                        }
                    }

                    fn main() {
                        let animal: Box<dyn Animal> = Box::new(Dog { name: "Rex".into() });
                        println!("{}", animal.speak()); // Rex says Woof
                    }
                    """),
            }),
        new(
            "interface-trait",
            "Interface / Trait",
            "Language Constructs",
            "An interface (or trait) defines a contract of behavior — a set of method signatures — without "
            + "prescribing an implementation, letting unrelated types be used interchangeably as long as they "
            + "satisfy the contract. Program against the interface rather than a concrete type to decouple "
            + "callers from implementations and enable substitution/testing. Some languages check conformance "
            + "structurally (Go's implicit satisfaction, TypeScript's structural typing) while others require "
            + "explicit declaration (C#, Java, Rust).",
            new List<CodeSnippet>
            {
                new("C#", """
                    interface IShape
                    {
                        double Area();
                    }

                    class Circle : IShape
                    {
                        private readonly double _radius;
                        public Circle(double radius) => _radius = radius;

                        public double Area() => Math.PI * _radius * _radius;
                    }

                    IShape shape = new Circle(2);
                    Console.WriteLine(shape.Area()); // ~12.57
                    """),
                new("Python", """
                    from abc import ABC, abstractmethod
                    import math


                    class Shape(ABC):
                        @abstractmethod
                        def area(self) -> float: ...


                    class Circle(Shape):
                        def __init__(self, radius: float) -> None:
                            self.radius = radius

                        def area(self) -> float:
                            return math.pi * self.radius ** 2


                    shape: Shape = Circle(2)
                    print(shape.area())  # ~12.57
                    """),
                new("JavaScript", """
                    // JS has no native interfaces; the idiom is a base class that
                    // throws for unimplemented members, documenting the contract.
                    class Shape {
                      area() {
                        throw new Error("area() not implemented");
                      }
                    }

                    class Circle extends Shape {
                      constructor(radius) {
                        super();
                        this.radius = radius;
                      }

                      area() {
                        return Math.PI * this.radius ** 2;
                      }
                    }

                    const shape = new Circle(2);
                    console.log(shape.area()); // ~12.57
                    """),
                new("TypeScript", """
                    interface Shape {
                      area(): number;
                    }

                    class Circle implements Shape {
                      constructor(private radius: number) {}

                      area(): number {
                        return Math.PI * this.radius ** 2;
                      }
                    }

                    const shape: Shape = new Circle(2);
                    console.log(shape.area()); // ~12.57
                    """),
                new("Java", """
                    interface Shape {
                        double area();
                    }

                    class Circle implements Shape {
                        private final double radius;

                        Circle(double radius) {
                            this.radius = radius;
                        }

                        @Override
                        public double area() {
                            return Math.PI * radius * radius;
                        }
                    }

                    Shape shape = new Circle(2);
                    System.out.println(shape.area()); // ~12.57
                    """),
                new("C++", """
                    #include <iostream>
                    #include <memory>

                    class Shape {
                    public:
                        virtual ~Shape() = default;
                        virtual double area() const = 0; // pure virtual = interface
                    };

                    class Circle : public Shape {
                    public:
                        explicit Circle(double radius) : radius_(radius) {}

                        double area() const override {
                            return 3.14159265358979 * radius_ * radius_;
                        }

                    private:
                        double radius_;
                    };

                    int main() {
                        std::unique_ptr<Shape> shape = std::make_unique<Circle>(2.0);
                        std::cout << shape->area() << "\n"; // ~12.57
                    }
                    """),
                new("Go", """
                    package main

                    import (
                        "fmt"
                        "math"
                    )

                    type Shape interface {
                        Area() float64
                    }

                    type Circle struct {
                        Radius float64
                    }

                    // Circle satisfies Shape implicitly: no "implements" keyword.
                    func (c Circle) Area() float64 {
                        return math.Pi * c.Radius * c.Radius
                    }

                    func main() {
                        var shape Shape = Circle{Radius: 2}
                        fmt.Println(shape.Area()) // ~12.57
                    }
                    """),
                new("Rust", """
                    trait Shape {
                        fn area(&self) -> f64;
                    }

                    struct Circle {
                        radius: f64,
                    }

                    impl Shape for Circle {
                        fn area(&self) -> f64 {
                            std::f64::consts::PI * self.radius * self.radius
                        }
                    }

                    fn main() {
                        let shape: Box<dyn Shape> = Box::new(Circle { radius: 2.0 });
                        println!("{}", shape.area()); // ~12.57
                    }
                    """),
            }),
        new(
            "struct-record",
            "Struct / Record",
            "Language Constructs",
            "A struct/record is a lightweight data type that groups related fields together, typically compared "
            + "and copied by value rather than by reference identity. Use it for plain data carriers (points, "
            + "coordinates, DTOs) where you want value semantics, cheap equality, and readable construction "
            + "instead of full class behavior. Many modern languages generate equality, printing, and copy "
            + "logic for you so you don't hand-write boilerplate.",
            new List<CodeSnippet>
            {
                new("C#", """
                    // Records give value-based equality and a compact ToString for free.
                    record Point(double X, double Y);

                    var a = new Point(1, 2);
                    var b = new Point(1, 2);

                    Console.WriteLine(a);        // Point { X = 1, Y = 2 }
                    Console.WriteLine(a == b);   // True (value equality)
                    """),
                new("Python", """
                    from dataclasses import dataclass


                    @dataclass(frozen=True)
                    class Point:
                        x: float
                        y: float


                    a = Point(1, 2)
                    b = Point(1, 2)

                    print(a)         # Point(x=1, y=2)
                    print(a == b)    # True (value equality)
                    """),
                new("JavaScript", """
                    // No native struct/record; a small factory returning a frozen
                    // plain object is the idiomatic immutable-value substitute.
                    function makePoint(x, y) {
                      return Object.freeze({ x, y });
                    }

                    const a = makePoint(1, 2);
                    const b = makePoint(1, 2);

                    console.log(a); // { x: 1, y: 2 }
                    console.log(JSON.stringify(a) === JSON.stringify(b)); // true
                    """),
                new("TypeScript", """
                    interface Point {
                      readonly x: number;
                      readonly y: number;
                    }

                    const a: Point = { x: 1, y: 2 };
                    const b: Point = { x: 1, y: 2 };

                    console.log(a); // { x: 1, y: 2 }
                    console.log(JSON.stringify(a) === JSON.stringify(b)); // true
                    """),
                new("Java", """
                    // Records (Java 16+) generate equals/hashCode/toString and are immutable.
                    record Point(double x, double y) {}

                    Point a = new Point(1, 2);
                    Point b = new Point(1, 2);

                    System.out.println(a);          // Point[x=1.0, y=2.0]
                    System.out.println(a.equals(b)); // true
                    """),
                new("C++", """
                    #include <iostream>

                    struct Point {
                        double x;
                        double y;

                        bool operator==(const Point& other) const {
                            return x == other.x && y == other.y;
                        }
                    };

                    int main() {
                        Point a{1, 2};
                        Point b{1, 2};

                        std::cout << "(" << a.x << ", " << a.y << ")\n"; // (1, 2)
                        std::cout << (a == b) << "\n"; // 1 (true)
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    type Point struct {
                        X, Y float64
                    }

                    func main() {
                        a := Point{X: 1, Y: 2}
                        b := Point{X: 1, Y: 2}

                        fmt.Println(a)      // {1 2}
                        fmt.Println(a == b) // true (structs compare by value)
                    }
                    """),
                new("Rust", """
                    #[derive(Debug, Clone, Copy, PartialEq)]
                    struct Point {
                        x: f64,
                        y: f64,
                    }

                    fn main() {
                        let a = Point { x: 1.0, y: 2.0 };
                        let b = a; // Copy: value semantics, not a move

                        println!("{:?}", a);   // Point { x: 1.0, y: 2.0 }
                        println!("{}", a == b); // true
                    }
                    """),
            }),
        new(
            "enum",
            "Enum",
            "Language Constructs",
            "An enum defines a fixed, named set of possible values for a type, replacing magic numbers/strings "
            + "with self-documenting, compiler-checked constants. Use it whenever a variable can only be one of "
            + "a small closed set of options, and pair it with a switch/match so the compiler (in languages that "
            + "support exhaustiveness checking) can flag missing cases. Some languages (notably Rust) let each "
            + "variant carry its own associated data, turning the enum into a lightweight tagged union.",
            new List<CodeSnippet>
            {
                new("C#", """
                    enum TrafficLight { Red, Yellow, Green }

                    string Describe(TrafficLight light) => light switch
                    {
                        TrafficLight.Red => "Stop",
                        TrafficLight.Yellow => "Caution",
                        TrafficLight.Green => "Go",
                        _ => throw new ArgumentOutOfRangeException(nameof(light)),
                    };

                    Console.WriteLine(Describe(TrafficLight.Green)); // Go
                    """),
                new("Python", """
                    from enum import Enum, auto


                    class TrafficLight(Enum):
                        RED = auto()
                        YELLOW = auto()
                        GREEN = auto()


                    def describe(light: TrafficLight) -> str:
                        match light:
                            case TrafficLight.RED:
                                return "Stop"
                            case TrafficLight.YELLOW:
                                return "Caution"
                            case TrafficLight.GREEN:
                                return "Go"


                    print(describe(TrafficLight.GREEN))  # Go
                    """),
                new("JavaScript", """
                    // No native enum; a frozen object of constants is the idiom.
                    const TrafficLight = Object.freeze({
                      RED: "RED",
                      YELLOW: "YELLOW",
                      GREEN: "GREEN",
                    });

                    function describe(light) {
                      switch (light) {
                        case TrafficLight.RED:
                          return "Stop";
                        case TrafficLight.YELLOW:
                          return "Caution";
                        case TrafficLight.GREEN:
                          return "Go";
                      }
                    }

                    console.log(describe(TrafficLight.GREEN)); // Go
                    """),
                new("TypeScript", """
                    enum TrafficLight {
                      Red,
                      Yellow,
                      Green,
                    }

                    function describe(light: TrafficLight): string {
                      switch (light) {
                        case TrafficLight.Red:
                          return "Stop";
                        case TrafficLight.Yellow:
                          return "Caution";
                        case TrafficLight.Green:
                          return "Go";
                      }
                    }

                    console.log(describe(TrafficLight.Green)); // Go
                    """),
                new("Java", """
                    enum TrafficLight { RED, YELLOW, GREEN }

                    static String describe(TrafficLight light) {
                        return switch (light) {
                            case RED -> "Stop";
                            case YELLOW -> "Caution";
                            case GREEN -> "Go";
                        };
                    }

                    System.out.println(describe(TrafficLight.GREEN)); // Go
                    """),
                new("C++", """
                    #include <iostream>
                    #include <string>

                    enum class TrafficLight { Red, Yellow, Green };

                    std::string describe(TrafficLight light) {
                        switch (light) {
                            case TrafficLight::Red: return "Stop";
                            case TrafficLight::Yellow: return "Caution";
                            case TrafficLight::Green: return "Go";
                        }
                        return "Unknown";
                    }

                    int main() {
                        std::cout << describe(TrafficLight::Green) << "\n"; // Go
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    type TrafficLight int

                    const (
                        Red TrafficLight = iota
                        Yellow
                        Green
                    )

                    func describe(light TrafficLight) string {
                        switch light {
                        case Red:
                            return "Stop"
                        case Yellow:
                            return "Caution"
                        case Green:
                            return "Go"
                        default:
                            return "Unknown"
                        }
                    }

                    func main() {
                        fmt.Println(describe(Green)) // Go
                    }
                    """),
                new("Rust", """
                    // Rust enum variants can carry their own data (tagged union).
                    enum TrafficLight {
                        Red,
                        Yellow,
                        Green,
                        Custom(String), // variant with associated data
                    }

                    fn describe(light: &TrafficLight) -> String {
                        match light {
                            TrafficLight::Red => "Stop".to_string(),
                            TrafficLight::Yellow => "Caution".to_string(),
                            TrafficLight::Green => "Go".to_string(),
                            TrafficLight::Custom(label) => format!("Custom: {label}"),
                        }
                    }

                    fn main() {
                        println!("{}", describe(&TrafficLight::Green)); // Go
                    }
                    """),
            }),
        new(
            "generics",
            "Generics",
            "Language Constructs",
            "Generics let a function, class, or container be written once and parameterized over a type, "
            + "preserving type safety without duplicating code for every concrete type. Use them for reusable "
            + "containers (lists, boxes, pairs) and algorithms (max, sort, map) that behave the same regardless "
            + "of the element type. Constraints (bounds/where-clauses/trait bounds) let you require the type "
            + "parameter to support specific operations, such as comparison.",
            new List<CodeSnippet>
            {
                new("C#", """
                    static T Max<T>(T a, T b) where T : IComparable<T> =>
                        a.CompareTo(b) >= 0 ? a : b;

                    Console.WriteLine(Max(3, 7));       // 7
                    Console.WriteLine(Max("ant", "zip")); // zip

                    class Box<T>
                    {
                        public T Value { get; }
                        public Box(T value) => Value = value;
                    }

                    var box = new Box<int>(42);
                    Console.WriteLine(box.Value); // 42
                    """),
                new("Python", """
                    from typing import TypeVar, Generic, Protocol


                    class Comparable(Protocol):
                        def __lt__(self, other: object) -> bool: ...


                    T = TypeVar("T", bound=Comparable)


                    def max_of(a: T, b: T) -> T:
                        return a if a > b else b


                    print(max_of(3, 7))          # 7
                    print(max_of("ant", "zip"))  # zip


                    class Box(Generic[T]):
                        def __init__(self, value: T) -> None:
                            self.value = value


                    box = Box(42)
                    print(box.value)  # 42
                    """),
                new("JavaScript", """
                    // JS has no compile-time generics; a container just holds any
                    // value at runtime. Real static generics require TypeScript.
                    class Box {
                      constructor(value) {
                        this.value = value;
                      }
                    }

                    function max(a, b) {
                      return a >= b ? a : b;
                    }

                    console.log(max(3, 7));         // 7
                    console.log(new Box(42).value); // 42
                    """),
                new("TypeScript", """
                    function max<T>(a: T, b: T, compare: (a: T, b: T) => number): T {
                      return compare(a, b) >= 0 ? a : b;
                    }

                    console.log(max(3, 7, (a, b) => a - b)); // 7

                    class Box<T> {
                      constructor(public value: T) {}
                    }

                    const box = new Box<number>(42);
                    console.log(box.value); // 42
                    """),
                new("Java", """
                    static <T extends Comparable<T>> T max(T a, T b) {
                        return a.compareTo(b) >= 0 ? a : b;
                    }

                    System.out.println(max(3, 7));            // 7
                    System.out.println(max("ant", "zip"));     // zip

                    class Box<T> {
                        private final T value;
                        Box(T value) { this.value = value; }
                        T get() { return value; }
                    }

                    Box<Integer> box = new Box<>(42);
                    System.out.println(box.get()); // 42
                    """),
                new("C++", """
                    #include <iostream>

                    template <typename T>
                    T max_of(T a, T b) {
                        return a >= b ? a : b;
                    }

                    template <typename T>
                    class Box {
                    public:
                        explicit Box(T value) : value_(value) {}
                        const T& value() const { return value_; }

                    private:
                        T value_;
                    };

                    int main() {
                        std::cout << max_of(3, 7) << "\n";   // 7
                        Box<int> box(42);
                        std::cout << box.value() << "\n";    // 42
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    type Ordered interface {
                        ~int | ~int64 | ~float64 | ~string
                    }

                    func Max[T Ordered](a, b T) T {
                        if a >= b {
                            return a
                        }
                        return b
                    }

                    type Box[T any] struct {
                        Value T
                    }

                    func main() {
                        fmt.Println(Max(3, 7))       // 7
                        fmt.Println(Max("ant", "zip")) // zip

                        box := Box[int]{Value: 42}
                        fmt.Println(box.Value) // 42
                    }
                    """),
                new("Rust", """
                    fn max_of<T: PartialOrd>(a: T, b: T) -> T {
                        if a >= b { a } else { b }
                    }

                    struct Box<T> {
                        value: T,
                    }

                    impl<T> Box<T> {
                        fn new(value: T) -> Self {
                            Box { value }
                        }
                    }

                    fn main() {
                        println!("{}", max_of(3, 7));           // 7
                        println!("{}", max_of("ant", "zip"));   // zip

                        let boxed = Box::new(42);
                        println!("{}", boxed.value); // 42
                    }
                    """),
            }),
        new(
            "closure-lambda",
            "Closure / Lambda",
            "Language Constructs",
            "A closure is a function value that captures variables from its enclosing scope, letting that "
            + "state persist and be mutated across calls even after the outer function has returned. Use "
            + "closures for factories that produce stateful callbacks (counters, accumulators, memoizers) "
            + "or for passing small inline behavior to higher-order functions. The key nuance is capture "
            + "semantics: by reference (JS, Python's nonlocal, C# lambdas) versus by explicit move/borrow "
            + "(Rust) versus by value copy (C++ `[=]`).",
            new List<CodeSnippet>
            {
                new("C#", """
                    Func<int> MakeCounter()
                    {
                        int count = 0;
                        return () => ++count; // captures 'count' by reference
                    }

                    var counter = MakeCounter();
                    Console.WriteLine(counter()); // 1
                    Console.WriteLine(counter()); // 2
                    Console.WriteLine(counter()); // 3
                    """),
                new("Python", """
                    def make_counter():
                        count = 0

                        def counter():
                            nonlocal count
                            count += 1
                            return count

                        return counter


                    counter = make_counter()
                    print(counter())  # 1
                    print(counter())  # 2
                    print(counter())  # 3
                    """),
                new("JavaScript", """
                    function makeCounter() {
                      let count = 0;
                      return function () {
                        count += 1;
                        return count;
                      };
                    }

                    const counter = makeCounter();
                    console.log(counter()); // 1
                    console.log(counter()); // 2
                    console.log(counter()); // 3
                    """),
                new("TypeScript", """
                    function makeCounter(): () => number {
                      let count = 0;
                      return () => {
                        count += 1;
                        return count;
                      };
                    }

                    const counter = makeCounter();
                    console.log(counter()); // 1
                    console.log(counter()); // 2
                    console.log(counter()); // 3
                    """),
                new("Java", """
                    import java.util.function.IntSupplier;
                    import java.util.concurrent.atomic.AtomicInteger;

                    // Java lambdas can only capture effectively-final locals, so
                    // mutable state needs a holder like AtomicInteger.
                    static IntSupplier makeCounter() {
                        AtomicInteger count = new AtomicInteger(0);
                        return count::incrementAndGet;
                    }

                    IntSupplier counter = makeCounter();
                    System.out.println(counter.getAsInt()); // 1
                    System.out.println(counter.getAsInt()); // 2
                    System.out.println(counter.getAsInt()); // 3
                    """),
                new("C++", """
                    #include <iostream>
                    #include <functional>

                    std::function<int()> makeCounter() {
                        int count = 0;
                        // capture by value (mutable lets the closure mutate its own
                        // copy); count is a local, so capturing by reference here
                        // would leave a dangling reference once makeCounter returns
                        return [count]() mutable {
                            return ++count;
                        };
                    }

                    int main() {
                        auto counter = makeCounter();
                        std::cout << counter() << "\n"; // 1
                        std::cout << counter() << "\n"; // 2
                        std::cout << counter() << "\n"; // 3
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    func makeCounter() func() int {
                        count := 0
                        return func() int {
                            count++
                            return count
                        }
                    }

                    func main() {
                        counter := makeCounter()
                        fmt.Println(counter()) // 1
                        fmt.Println(counter()) // 2
                        fmt.Println(counter()) // 3
                    }
                    """),
                new("Rust", """
                    fn make_counter() -> impl FnMut() -> i32 {
                        let mut count = 0;
                        move || {
                            count += 1;
                            count
                        }
                    }

                    fn main() {
                        let mut counter = make_counter();
                        println!("{}", counter()); // 1
                        println!("{}", counter()); // 2
                        println!("{}", counter()); // 3
                    }
                    """),
            }),
        new(
            "error-handling",
            "Error Handling (try/catch + custom error)",
            "Language Constructs",
            "Robust code distinguishes expected failure conditions from programmer bugs by defining a custom "
            + "error type and handling it explicitly at the call site. Exception-based languages throw and "
            + "catch typed exceptions, unwinding the stack until a matching handler is found; Go and Rust "
            + "instead treat errors as ordinary return values (`error`, `Result<T, E>`), forcing the caller to "
            + "handle or explicitly propagate them. Prefer a specific custom error type over generic exceptions "
            + "so callers can distinguish failure causes.",
            new List<CodeSnippet>
            {
                new("C#", """
                    class InsufficientFundsException : Exception
                    {
                        public InsufficientFundsException(string message) : base(message) { }
                    }

                    void Withdraw(decimal balance, decimal amount)
                    {
                        if (amount > balance)
                            throw new InsufficientFundsException($"Cannot withdraw {amount}, balance is {balance}");
                    }

                    try
                    {
                        Withdraw(50m, 100m);
                    }
                    catch (InsufficientFundsException ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                    """),
                new("Python", """
                    class InsufficientFundsError(Exception):
                        pass


                    def withdraw(balance: float, amount: float) -> None:
                        if amount > balance:
                            raise InsufficientFundsError(
                                f"Cannot withdraw {amount}, balance is {balance}"
                            )


                    try:
                        withdraw(50, 100)
                    except InsufficientFundsError as err:
                        print(f"Error: {err}")
                    """),
                new("JavaScript", """
                    class InsufficientFundsError extends Error {
                      constructor(message) {
                        super(message);
                        this.name = "InsufficientFundsError";
                      }
                    }

                    function withdraw(balance, amount) {
                      if (amount > balance) {
                        throw new InsufficientFundsError(
                          `Cannot withdraw ${amount}, balance is ${balance}`
                        );
                      }
                    }

                    try {
                      withdraw(50, 100);
                    } catch (err) {
                      console.log(`Error: ${err.message}`);
                    }
                    """),
                new("TypeScript", """
                    class InsufficientFundsError extends Error {
                      constructor(message: string) {
                        super(message);
                        this.name = "InsufficientFundsError";
                      }
                    }

                    function withdraw(balance: number, amount: number): void {
                      if (amount > balance) {
                        throw new InsufficientFundsError(
                          `Cannot withdraw ${amount}, balance is ${balance}`
                        );
                      }
                    }

                    try {
                      withdraw(50, 100);
                    } catch (err) {
                      if (err instanceof InsufficientFundsError) {
                        console.log(`Error: ${err.message}`);
                      }
                    }
                    """),
                new("Java", """
                    class InsufficientFundsException extends Exception {
                        InsufficientFundsException(String message) {
                            super(message);
                        }
                    }

                    static void withdraw(double balance, double amount) throws InsufficientFundsException {
                        if (amount > balance) {
                            throw new InsufficientFundsException(
                                "Cannot withdraw " + amount + ", balance is " + balance);
                        }
                    }

                    try {
                        withdraw(50, 100);
                    } catch (InsufficientFundsException ex) {
                        System.out.println("Error: " + ex.getMessage());
                    }
                    """),
                new("C++", """
                    #include <iostream>
                    #include <stdexcept>
                    #include <string>

                    class InsufficientFundsError : public std::runtime_error {
                    public:
                        explicit InsufficientFundsError(const std::string& message)
                            : std::runtime_error(message) {}
                    };

                    void withdraw(double balance, double amount) {
                        if (amount > balance) {
                            throw InsufficientFundsError(
                                "Cannot withdraw " + std::to_string(amount) +
                                ", balance is " + std::to_string(balance));
                        }
                    }

                    int main() {
                        try {
                            withdraw(50, 100);
                        } catch (const InsufficientFundsError& ex) {
                            std::cout << "Error: " << ex.what() << "\n";
                        }
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    type InsufficientFundsError struct {
                        Balance, Amount float64
                    }

                    func (e *InsufficientFundsError) Error() string {
                        return fmt.Sprintf("cannot withdraw %.2f, balance is %.2f", e.Amount, e.Balance)
                    }

                    func withdraw(balance, amount float64) error {
                        if amount > balance {
                            return &InsufficientFundsError{Balance: balance, Amount: amount}
                        }
                        return nil
                    }

                    func main() {
                        if err := withdraw(50, 100); err != nil {
                            fmt.Println("Error:", err)
                        }
                    }
                    """),
                new("Rust", """
                    use std::fmt;

                    #[derive(Debug)]
                    struct InsufficientFundsError {
                        balance: f64,
                        amount: f64,
                    }

                    impl fmt::Display for InsufficientFundsError {
                        fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
                            write!(f, "cannot withdraw {}, balance is {}", self.amount, self.balance)
                        }
                    }

                    impl std::error::Error for InsufficientFundsError {}

                    fn withdraw(balance: f64, amount: f64) -> Result<(), InsufficientFundsError> {
                        if amount > balance {
                            return Err(InsufficientFundsError { balance, amount });
                        }
                        Ok(())
                    }

                    fn main() {
                        if let Err(err) = withdraw(50.0, 100.0) {
                            println!("Error: {err}");
                        }
                    }
                    """),
            }),
        new(
            "async-await",
            "Async/Await",
            "Language Constructs",
            "Async/await lets code that waits on I/O or other latency (network calls, timers, disk) suspend "
            + "without blocking the underlying thread, resuming once the awaited operation completes. Use it "
            + "for I/O-bound work so a single thread can service many concurrent operations instead of one "
            + "thread per blocked call. Not every language has the literal `async`/`await` keywords: Go uses "
            + "goroutines and channels, and C++ uses `std::async`/`std::future`, as their idiomatic equivalents.",
            new List<CodeSnippet>
            {
                new("C#", """
                    async Task<string> FetchDataAsync()
                    {
                        await Task.Delay(100); // simulated I/O
                        return "data";
                    }

                    async Task Main()
                    {
                        string result = await FetchDataAsync();
                        Console.WriteLine(result); // data
                    }
                    """),
                new("Python", """
                    import asyncio


                    async def fetch_data() -> str:
                        await asyncio.sleep(0.1)  # simulated I/O
                        return "data"


                    async def main() -> None:
                        result = await fetch_data()
                        print(result)  # data


                    asyncio.run(main())
                    """),
                new("JavaScript", """
                    function delay(ms) {
                      return new Promise((resolve) => setTimeout(resolve, ms));
                    }

                    async function fetchData() {
                      await delay(100); // simulated I/O
                      return "data";
                    }

                    async function main() {
                      const result = await fetchData();
                      console.log(result); // data
                    }

                    main();
                    """),
                new("TypeScript", """
                    function delay(ms: number): Promise<void> {
                      return new Promise((resolve) => setTimeout(resolve, ms));
                    }

                    async function fetchData(): Promise<string> {
                      await delay(100); // simulated I/O
                      return "data";
                    }

                    async function main(): Promise<void> {
                      const result = await fetchData();
                      console.log(result); // data
                    }

                    main();
                    """),
                new("Java", """
                    import java.util.concurrent.CompletableFuture;
                    import java.util.concurrent.Executors;
                    import java.time.Duration;

                    // Java has no async/await keyword; CompletableFuture is the
                    // idiomatic equivalent for composing non-blocking work.
                    static CompletableFuture<String> fetchData() {
                        return CompletableFuture.supplyAsync(() -> {
                            try {
                                Thread.sleep(100); // simulated I/O
                            } catch (InterruptedException e) {
                                Thread.currentThread().interrupt();
                            }
                            return "data";
                        }, Executors.newVirtualThreadPerTaskExecutor());
                    }

                    fetchData().thenAccept(System.out::println).join(); // data
                    """),
                new("C++", """
                    #include <iostream>
                    #include <future>
                    #include <chrono>
                    #include <thread>
                    #include <string>

                    // C++ has no async/await keyword; std::async/std::future is
                    // the idiomatic equivalent for asynchronous work.
                    std::string fetchData() {
                        std::this_thread::sleep_for(std::chrono::milliseconds(100));
                        return "data";
                    }

                    int main() {
                        std::future<std::string> fut = std::async(std::launch::async, fetchData);
                        std::cout << fut.get() << "\n"; // data
                    }
                    """),
                new("Go", """
                    package main

                    import (
                        "fmt"
                        "time"
                    )

                    // Go has no async/await; goroutines + channels are the idiomatic
                    // concurrency primitive for the same "do work, get result later" idea.
                    func fetchData(result chan<- string) {
                        time.Sleep(100 * time.Millisecond) // simulated I/O
                        result <- "data"
                    }

                    func main() {
                        result := make(chan string)
                        go fetchData(result)
                        fmt.Println(<-result) // data
                    }
                    """),
                new("Rust", """
                    use std::time::Duration;

                    // Assumes a Tokio runtime (e.g. #[tokio::main] or Runtime::new()).
                    async fn fetch_data() -> String {
                        tokio::time::sleep(Duration::from_millis(100)).await; // simulated I/O
                        "data".to_string()
                    }

                    #[tokio::main]
                    async fn main() {
                        let result = fetch_data().await;
                        println!("{result}"); // data
                    }
                    """),
            }),
    };
}
