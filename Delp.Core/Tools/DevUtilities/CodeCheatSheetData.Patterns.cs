namespace Delp.Core.Tools.DevUtilities;

public static partial class CodeCheatSheetData
{
    internal static IReadOnlyList<CheatTopic> PatternsTopics { get; } = new List<CheatTopic>
    {
        new(
            "singleton",
            "Singleton",
            "Patterns",
            "The Singleton pattern restricts a class to a single instance and provides a global access point to it, useful for shared resources like loggers, configuration, or connection pools where having more than one instance would waste resources or cause inconsistent state. Modern implementations favor lazy, thread-safe initialization built into the language runtime rather than manual locking. Because it introduces global mutable state and hides a dependency inside client code, it can make unit testing harder; prefer passing a single shared instance via dependency injection when testability matters.",
            new List<CodeSnippet>
            {
                new("C#", """
                    public sealed class Logger
                    {
                        private static readonly Lazy<Logger> _instance = new(() => new Logger());
                        public static Logger Instance => _instance.Value;

                        private Logger() { }

                        public void Log(string message) => Console.WriteLine($"[LOG] {message}");
                    }

                    public static class Program
                    {
                        public static void Main()
                        {
                            var a = Logger.Instance;
                            var b = Logger.Instance;
                            a.Log("Hello from singleton");
                            Console.WriteLine(ReferenceEquals(a, b)); // True
                        }
                    }
                    """),
                new("Python", """
                    class Logger:
                        _instance = None

                        def __new__(cls):
                            if cls._instance is None:
                                cls._instance = super().__new__(cls)
                            return cls._instance

                        def log(self, message: str) -> None:
                            print(f"[LOG] {message}")


                    if __name__ == "__main__":
                        a = Logger()
                        b = Logger()
                        a.log("Hello from singleton")
                        print(a is b)  # True
                    """),
                new("JavaScript", """
                    const Logger = (() => {
                      let instance;

                      class LoggerImpl {
                        log(message) {
                          console.log(`[LOG] ${message}`);
                        }
                      }

                      return {
                        getInstance() {
                          if (!instance) instance = new LoggerImpl();
                          return instance;
                        },
                      };
                    })();

                    const a = Logger.getInstance();
                    const b = Logger.getInstance();
                    a.log("Hello from singleton");
                    console.log(a === b); // true
                    """),
                new("TypeScript", """
                    class Logger {
                      private static instance: Logger;

                      private constructor() {}

                      static getInstance(): Logger {
                        if (!Logger.instance) {
                          Logger.instance = new Logger();
                        }
                        return Logger.instance;
                      }

                      log(message: string): void {
                        console.log(`[LOG] ${message}`);
                      }
                    }

                    const a = Logger.getInstance();
                    const b = Logger.getInstance();
                    a.log("Hello from singleton");
                    console.log(a === b); // true
                    """),
                new("Java", """
                    public final class Logger {
                        private Logger() { }

                        private static class Holder {
                            private static final Logger INSTANCE = new Logger();
                        }

                        public static Logger getInstance() {
                            return Holder.INSTANCE;
                        }

                        public void log(String message) {
                            System.out.println("[LOG] " + message);
                        }

                        public static void main(String[] args) {
                            Logger a = Logger.getInstance();
                            Logger b = Logger.getInstance();
                            a.log("Hello from singleton");
                            System.out.println(a == b); // true
                        }
                    }
                    """),
                new("C++", """
                    #include <iostream>
                    #include <string>

                    class Logger {
                    public:
                        static Logger& instance() {
                            static Logger instance; // thread-safe in C++11+
                            return instance;
                        }

                        void log(const std::string& message) const {
                            std::cout << "[LOG] " << message << "\n";
                        }

                        Logger(const Logger&) = delete;
                        Logger& operator=(const Logger&) = delete;

                    private:
                        Logger() = default;
                    };

                    int main() {
                        Logger& a = Logger::instance();
                        Logger& b = Logger::instance();
                        a.log("Hello from singleton");
                        std::cout << (&a == &b) << "\n"; // 1
                    }
                    """),
                new("Go", """
                    package main

                    import (
                        "fmt"
                        "sync"
                    )

                    type Logger struct{}

                    func (l *Logger) Log(message string) {
                        fmt.Printf("[LOG] %s\n", message)
                    }

                    var (
                        instance *Logger
                        once     sync.Once
                    )

                    func GetInstance() *Logger {
                        once.Do(func() {
                            instance = &Logger{}
                        })
                        return instance
                    }

                    func main() {
                        a := GetInstance()
                        b := GetInstance()
                        a.Log("Hello from singleton")
                        fmt.Println(a == b) // true
                    }
                    """),
                new("Rust", """
                    use std::sync::OnceLock;

                    struct Logger;

                    impl Logger {
                        fn log(&self, message: &str) {
                            println!("[LOG] {message}");
                        }
                    }

                    fn instance() -> &'static Logger {
                        static INSTANCE: OnceLock<Logger> = OnceLock::new();
                        INSTANCE.get_or_init(|| Logger)
                    }

                    fn main() {
                        let a = instance();
                        let b = instance();
                        a.log("Hello from singleton");
                        println!("{}", std::ptr::eq(a, b)); // true
                    }
                    """),
            }),
        new(
            "factory",
            "Factory",
            "Patterns",
            "The Factory pattern centralizes object creation behind a function or class so that client code depends only on a shared interface or abstract type, never on concrete constructors. It solves the problem of scattering type-selection logic across a codebase, and makes it easy to add new concrete types without touching callers. Reach for it when the concrete type to construct is chosen at runtime from a family of related implementations; for a single concrete type, a plain constructor is simpler and a factory just adds indirection.",
            new List<CodeSnippet>
            {
                new("C#", """
                    public interface IShape
                    {
                        double Area();
                    }

                    public sealed class Circle(double radius) : IShape
                    {
                        public double Area() => Math.PI * radius * radius;
                    }

                    public sealed class Square(double side) : IShape
                    {
                        public double Area() => side * side;
                    }

                    public static class ShapeFactory
                    {
                        public static IShape Create(string kind, double size) => kind switch
                        {
                            "circle" => new Circle(size),
                            "square" => new Square(size),
                            _ => throw new ArgumentException($"Unknown shape: {kind}"),
                        };
                    }

                    public static class Program
                    {
                        public static void Main()
                        {
                            IShape shape = ShapeFactory.Create("circle", 2.0);
                            Console.WriteLine($"{shape.GetType().Name}: {shape.Area():F2}");
                        }
                    }
                    """),
                new("Python", """
                    from abc import ABC, abstractmethod


                    class Shape(ABC):
                        @abstractmethod
                        def area(self) -> float: ...


                    class Circle(Shape):
                        def __init__(self, radius: float):
                            self.radius = radius

                        def area(self) -> float:
                            return 3.14159 * self.radius ** 2


                    class Square(Shape):
                        def __init__(self, side: float):
                            self.side = side

                        def area(self) -> float:
                            return self.side ** 2


                    def create_shape(kind: str, size: float) -> Shape:
                        shapes = {"circle": Circle, "square": Square}
                        if kind not in shapes:
                            raise ValueError(f"Unknown shape: {kind}")
                        return shapes[kind](size)


                    if __name__ == "__main__":
                        shape = create_shape("circle", 2.0)
                        print(f"{type(shape).__name__}: {shape.area():.2f}")
                    """),
                new("JavaScript", """
                    class Circle {
                      constructor(radius) {
                        this.radius = radius;
                      }
                      area() {
                        return Math.PI * this.radius ** 2;
                      }
                    }

                    class Square {
                      constructor(side) {
                        this.side = side;
                      }
                      area() {
                        return this.side ** 2;
                      }
                    }

                    function createShape(kind, size) {
                      switch (kind) {
                        case "circle":
                          return new Circle(size);
                        case "square":
                          return new Square(size);
                        default:
                          throw new Error(`Unknown shape: ${kind}`);
                      }
                    }

                    const shape = createShape("circle", 2.0);
                    console.log(`${shape.constructor.name}: ${shape.area().toFixed(2)}`);
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

                    class Square implements Shape {
                      constructor(private side: number) {}
                      area(): number {
                        return this.side ** 2;
                      }
                    }

                    function createShape(kind: "circle" | "square", size: number): Shape {
                      switch (kind) {
                        case "circle":
                          return new Circle(size);
                        case "square":
                          return new Square(size);
                      }
                    }

                    const shape = createShape("circle", 2.0);
                    console.log(`${shape.constructor.name}: ${shape.area().toFixed(2)}`);
                    """),
                new("Java", """
                    interface Shape {
                        double area();
                    }

                    class Circle implements Shape {
                        private final double radius;
                        Circle(double radius) { this.radius = radius; }
                        public double area() { return Math.PI * radius * radius; }
                    }

                    class Square implements Shape {
                        private final double side;
                        Square(double side) { this.side = side; }
                        public double area() { return side * side; }
                    }

                    class ShapeFactory {
                        static Shape create(String kind, double size) {
                            return switch (kind) {
                                case "circle" -> new Circle(size);
                                case "square" -> new Square(size);
                                default -> throw new IllegalArgumentException("Unknown shape: " + kind);
                            };
                        }
                    }

                    public class Main {
                        public static void main(String[] args) {
                            Shape shape = ShapeFactory.create("circle", 2.0);
                            System.out.printf("%s: %.2f%n", shape.getClass().getSimpleName(), shape.area());
                        }
                    }
                    """),
                new("C++", """
                    #include <iostream>
                    #include <memory>
                    #include <stdexcept>
                    #include <string>

                    class Shape {
                    public:
                        virtual ~Shape() = default;
                        virtual double area() const = 0;
                        virtual std::string name() const = 0;
                    };

                    class Circle : public Shape {
                    public:
                        explicit Circle(double radius) : radius_(radius) {}
                        double area() const override { return 3.14159 * radius_ * radius_; }
                        std::string name() const override { return "Circle"; }
                    private:
                        double radius_;
                    };

                    class Square : public Shape {
                    public:
                        explicit Square(double side) : side_(side) {}
                        double area() const override { return side_ * side_; }
                        std::string name() const override { return "Square"; }
                    private:
                        double side_;
                    };

                    std::unique_ptr<Shape> createShape(const std::string& kind, double size) {
                        if (kind == "circle") return std::make_unique<Circle>(size);
                        if (kind == "square") return std::make_unique<Square>(size);
                        throw std::invalid_argument("Unknown shape: " + kind);
                    }

                    int main() {
                        auto shape = createShape("circle", 2.0);
                        std::cout << shape->name() << ": " << shape->area() << "\n";
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

                    type Circle struct{ Radius float64 }

                    func (c Circle) Area() float64 { return math.Pi * c.Radius * c.Radius }

                    type Square struct{ Side float64 }

                    func (s Square) Area() float64 { return s.Side * s.Side }

                    func NewShape(kind string, size float64) (Shape, error) {
                        switch kind {
                        case "circle":
                            return Circle{Radius: size}, nil
                        case "square":
                            return Square{Side: size}, nil
                        default:
                            return nil, fmt.Errorf("unknown shape: %s", kind)
                        }
                    }

                    func main() {
                        shape, err := NewShape("circle", 2.0)
                        if err != nil {
                            panic(err)
                        }
                        fmt.Printf("%T: %.2f\n", shape, shape.Area())
                    }
                    """),
                new("Rust", """
                    trait Shape {
                        fn area(&self) -> f64;
                        fn name(&self) -> &'static str;
                    }

                    struct Circle {
                        radius: f64,
                    }

                    impl Shape for Circle {
                        fn area(&self) -> f64 {
                            std::f64::consts::PI * self.radius * self.radius
                        }
                        fn name(&self) -> &'static str {
                            "Circle"
                        }
                    }

                    struct Square {
                        side: f64,
                    }

                    impl Shape for Square {
                        fn area(&self) -> f64 {
                            self.side * self.side
                        }
                        fn name(&self) -> &'static str {
                            "Square"
                        }
                    }

                    fn create_shape(kind: &str, size: f64) -> Box<dyn Shape> {
                        match kind {
                            "circle" => Box::new(Circle { radius: size }),
                            "square" => Box::new(Square { side: size }),
                            _ => panic!("unknown shape: {kind}"),
                        }
                    }

                    fn main() {
                        let shape = create_shape("circle", 2.0);
                        println!("{}: {:.2}", shape.name(), shape.area());
                    }
                    """),
            }),
        new(
            "observer",
            "Observer",
            "Patterns",
            "The Observer pattern lets a subject maintain a list of dependents and notify them automatically whenever its state changes, decoupling the thing that changes from the things that react to the change. It is the backbone of event systems, UI data binding, and publish/subscribe messaging. It is a great fit whenever multiple parts of a system need to react to the same event; watch for memory leaks from observers that are never unsubscribed, and be careful about the order and reentrancy of notifications.",
            new List<CodeSnippet>
            {
                new("C#", """
                    public sealed class StockTicker
                    {
                        public event Action<decimal>? PriceChanged;

                        public void SetPrice(decimal price) => PriceChanged?.Invoke(price);
                    }

                    public static class Program
                    {
                        public static void Main()
                        {
                            var ticker = new StockTicker();

                            ticker.PriceChanged += price => Console.WriteLine($"Dashboard: price is now {price:C}");
                            ticker.PriceChanged += price => Console.WriteLine($"Logger: recorded price {price}");

                            ticker.SetPrice(101.50m);
                        }
                    }
                    """),
                new("Python", """
                    class StockTicker:
                        def __init__(self):
                            self._observers = []

                        def subscribe(self, observer) -> None:
                            self._observers.append(observer)

                        def set_price(self, price: float) -> None:
                            for observer in self._observers:
                                observer(price)


                    def dashboard(price: float) -> None:
                        print(f"Dashboard: price is now {price:.2f}")


                    def logger(price: float) -> None:
                        print(f"Logger: recorded price {price}")


                    if __name__ == "__main__":
                        ticker = StockTicker()
                        ticker.subscribe(dashboard)
                        ticker.subscribe(logger)
                        ticker.set_price(101.50)
                    """),
                new("JavaScript", """
                    class StockTicker {
                      #observers = [];

                      subscribe(observer) {
                        this.#observers.push(observer);
                      }

                      setPrice(price) {
                        for (const observer of this.#observers) observer(price);
                      }
                    }

                    const ticker = new StockTicker();

                    ticker.subscribe((price) => console.log(`Dashboard: price is now ${price.toFixed(2)}`));
                    ticker.subscribe((price) => console.log(`Logger: recorded price ${price}`));

                    ticker.setPrice(101.5);
                    """),
                new("TypeScript", """
                    type Observer = (price: number) => void;

                    class StockTicker {
                      private observers: Observer[] = [];

                      subscribe(observer: Observer): void {
                        this.observers.push(observer);
                      }

                      setPrice(price: number): void {
                        for (const observer of this.observers) observer(price);
                      }
                    }

                    const ticker = new StockTicker();

                    ticker.subscribe((price) => console.log(`Dashboard: price is now ${price.toFixed(2)}`));
                    ticker.subscribe((price) => console.log(`Logger: recorded price ${price}`));

                    ticker.setPrice(101.5);
                    """),
                new("Java", """
                    import java.util.ArrayList;
                    import java.util.List;

                    interface PriceObserver {
                        void onPriceChanged(double price);
                    }

                    class StockTicker {
                        private final List<PriceObserver> observers = new ArrayList<>();

                        void subscribe(PriceObserver observer) {
                            observers.add(observer);
                        }

                        void setPrice(double price) {
                            for (PriceObserver observer : observers) {
                                observer.onPriceChanged(price);
                            }
                        }
                    }

                    public class Main {
                        public static void main(String[] args) {
                            StockTicker ticker = new StockTicker();

                            ticker.subscribe(price -> System.out.printf("Dashboard: price is now %.2f%n", price));
                            ticker.subscribe(price -> System.out.println("Logger: recorded price " + price));

                            ticker.setPrice(101.50);
                        }
                    }
                    """),
                new("C++", """
                    #include <functional>
                    #include <iostream>
                    #include <vector>

                    class StockTicker {
                    public:
                        void subscribe(std::function<void(double)> observer) {
                            observers_.push_back(std::move(observer));
                        }

                        void setPrice(double price) {
                            for (auto& observer : observers_) observer(price);
                        }

                    private:
                        std::vector<std::function<void(double)>> observers_;
                    };

                    int main() {
                        StockTicker ticker;

                        ticker.subscribe([](double price) {
                            std::cout << "Dashboard: price is now " << price << "\n";
                        });
                        ticker.subscribe([](double price) {
                            std::cout << "Logger: recorded price " << price << "\n";
                        });

                        ticker.setPrice(101.50);
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    type StockTicker struct {
                        observers []func(float64)
                    }

                    func (t *StockTicker) Subscribe(observer func(float64)) {
                        t.observers = append(t.observers, observer)
                    }

                    func (t *StockTicker) SetPrice(price float64) {
                        for _, observer := range t.observers {
                            observer(price)
                        }
                    }

                    func main() {
                        ticker := &StockTicker{}

                        ticker.Subscribe(func(price float64) {
                            fmt.Printf("Dashboard: price is now %.2f\n", price)
                        })
                        ticker.Subscribe(func(price float64) {
                            fmt.Printf("Logger: recorded price %.2f\n", price)
                        })

                        ticker.SetPrice(101.50)
                    }
                    """),
                new("Rust", """
                    struct StockTicker {
                        observers: Vec<Box<dyn Fn(f64)>>,
                    }

                    impl StockTicker {
                        fn new() -> Self {
                            Self { observers: Vec::new() }
                        }

                        fn subscribe(&mut self, observer: impl Fn(f64) + 'static) {
                            self.observers.push(Box::new(observer));
                        }

                        fn set_price(&self, price: f64) {
                            for observer in &self.observers {
                                observer(price);
                            }
                        }
                    }

                    fn main() {
                        let mut ticker = StockTicker::new();

                        ticker.subscribe(|price| println!("Dashboard: price is now {price:.2}"));
                        ticker.subscribe(|price| println!("Logger: recorded price {price}"));

                        ticker.set_price(101.50);
                    }
                    """),
            }),
        new(
            "builder",
            "Builder",
            "Patterns",
            "The Builder pattern separates the step-by-step construction of a complex object from its finished representation, exposing chainable configuration methods that end in a call which produces the final object. It solves the telescoping-constructor problem where an object has many optional parameters, letting callers set only the ones they care about in a readable order. It shines for objects with many optional fields or multi-step assembly; for objects with just a few fields, a plain constructor, object initializer, or the language's native keyword/named arguments is usually simpler.",
            new List<CodeSnippet>
            {
                new("C#", """
                    public sealed record Pizza(string Size, IReadOnlyList<string> Toppings, bool ExtraCheese);

                    public sealed class PizzaBuilder
                    {
                        private string _size = "medium";
                        private readonly List<string> _toppings = [];
                        private bool _extraCheese;

                        public PizzaBuilder WithSize(string size)
                        {
                            _size = size;
                            return this;
                        }

                        public PizzaBuilder AddTopping(string topping)
                        {
                            _toppings.Add(topping);
                            return this;
                        }

                        public PizzaBuilder WithExtraCheese()
                        {
                            _extraCheese = true;
                            return this;
                        }

                        public Pizza Build() => new(_size, _toppings, _extraCheese);
                    }

                    public static class Program
                    {
                        public static void Main()
                        {
                            var pizza = new PizzaBuilder()
                                .WithSize("large")
                                .AddTopping("pepperoni")
                                .AddTopping("mushroom")
                                .WithExtraCheese()
                                .Build();

                            Console.WriteLine($"{pizza.Size} pizza with {string.Join(", ", pizza.Toppings)}, extra cheese: {pizza.ExtraCheese}");
                        }
                    }
                    """),
                new("Python", """
                    class Pizza:
                        def __init__(self, size: str, toppings: list[str], extra_cheese: bool):
                            self.size = size
                            self.toppings = toppings
                            self.extra_cheese = extra_cheese

                        def __str__(self) -> str:
                            return f"{self.size} pizza with {', '.join(self.toppings)}, extra cheese: {self.extra_cheese}"


                    class PizzaBuilder:
                        def __init__(self):
                            self._size = "medium"
                            self._toppings: list[str] = []
                            self._extra_cheese = False

                        def with_size(self, size: str) -> "PizzaBuilder":
                            self._size = size
                            return self

                        def add_topping(self, topping: str) -> "PizzaBuilder":
                            self._toppings.append(topping)
                            return self

                        def with_extra_cheese(self) -> "PizzaBuilder":
                            self._extra_cheese = True
                            return self

                        def build(self) -> Pizza:
                            return Pizza(self._size, self._toppings, self._extra_cheese)


                    if __name__ == "__main__":
                        pizza = (
                            PizzaBuilder()
                            .with_size("large")
                            .add_topping("pepperoni")
                            .add_topping("mushroom")
                            .with_extra_cheese()
                            .build()
                        )
                        print(pizza)
                    """),
                new("JavaScript", """
                    class Pizza {
                      constructor(size, toppings, extraCheese) {
                        this.size = size;
                        this.toppings = toppings;
                        this.extraCheese = extraCheese;
                      }

                      toString() {
                        return `${this.size} pizza with ${this.toppings.join(", ")}, extra cheese: ${this.extraCheese}`;
                      }
                    }

                    class PizzaBuilder {
                      #size = "medium";
                      #toppings = [];
                      #extraCheese = false;

                      withSize(size) {
                        this.#size = size;
                        return this;
                      }

                      addTopping(topping) {
                        this.#toppings.push(topping);
                        return this;
                      }

                      withExtraCheese() {
                        this.#extraCheese = true;
                        return this;
                      }

                      build() {
                        return new Pizza(this.#size, this.#toppings, this.#extraCheese);
                      }
                    }

                    const pizza = new PizzaBuilder()
                      .withSize("large")
                      .addTopping("pepperoni")
                      .addTopping("mushroom")
                      .withExtraCheese()
                      .build();

                    console.log(pizza.toString());
                    """),
                new("TypeScript", """
                    class Pizza {
                      constructor(
                        public readonly size: string,
                        public readonly toppings: readonly string[],
                        public readonly extraCheese: boolean,
                      ) {}

                      toString(): string {
                        return `${this.size} pizza with ${this.toppings.join(", ")}, extra cheese: ${this.extraCheese}`;
                      }
                    }

                    class PizzaBuilder {
                      private size = "medium";
                      private toppings: string[] = [];
                      private extraCheese = false;

                      withSize(size: string): this {
                        this.size = size;
                        return this;
                      }

                      addTopping(topping: string): this {
                        this.toppings.push(topping);
                        return this;
                      }

                      withExtraCheese(): this {
                        this.extraCheese = true;
                        return this;
                      }

                      build(): Pizza {
                        return new Pizza(this.size, this.toppings, this.extraCheese);
                      }
                    }

                    const pizza = new PizzaBuilder()
                      .withSize("large")
                      .addTopping("pepperoni")
                      .addTopping("mushroom")
                      .withExtraCheese()
                      .build();

                    console.log(pizza.toString());
                    """),
                new("Java", """
                    import java.util.ArrayList;
                    import java.util.List;

                    final class Pizza {
                        private final String size;
                        private final List<String> toppings;
                        private final boolean extraCheese;

                        private Pizza(Builder builder) {
                            this.size = builder.size;
                            this.toppings = builder.toppings;
                            this.extraCheese = builder.extraCheese;
                        }

                        @Override
                        public String toString() {
                            return size + " pizza with " + toppings + ", extra cheese: " + extraCheese;
                        }

                        static class Builder {
                            private String size = "medium";
                            private final List<String> toppings = new ArrayList<>();
                            private boolean extraCheese;

                            Builder withSize(String size) {
                                this.size = size;
                                return this;
                            }

                            Builder addTopping(String topping) {
                                toppings.add(topping);
                                return this;
                            }

                            Builder withExtraCheese() {
                                extraCheese = true;
                                return this;
                            }

                            Pizza build() {
                                return new Pizza(this);
                            }
                        }
                    }

                    public class Main {
                        public static void main(String[] args) {
                            Pizza pizza = new Pizza.Builder()
                                .withSize("large")
                                .addTopping("pepperoni")
                                .addTopping("mushroom")
                                .withExtraCheese()
                                .build();

                            System.out.println(pizza);
                        }
                    }
                    """),
                new("C++", """
                    #include <iostream>
                    #include <string>
                    #include <vector>

                    class Pizza {
                    public:
                        Pizza(std::string size, std::vector<std::string> toppings, bool extraCheese)
                            : size_(std::move(size)), toppings_(std::move(toppings)), extraCheese_(extraCheese) {}

                        void describe() const {
                            std::cout << size_ << " pizza with ";
                            for (size_t i = 0; i < toppings_.size(); ++i) {
                                std::cout << toppings_[i] << (i + 1 < toppings_.size() ? ", " : "");
                            }
                            std::cout << ", extra cheese: " << std::boolalpha << extraCheese_ << "\n";
                        }

                    private:
                        std::string size_;
                        std::vector<std::string> toppings_;
                        bool extraCheese_;
                    };

                    class PizzaBuilder {
                    public:
                        PizzaBuilder& withSize(std::string size) {
                            size_ = std::move(size);
                            return *this;
                        }

                        PizzaBuilder& addTopping(std::string topping) {
                            toppings_.push_back(std::move(topping));
                            return *this;
                        }

                        PizzaBuilder& withExtraCheese() {
                            extraCheese_ = true;
                            return *this;
                        }

                        Pizza build() const {
                            return Pizza(size_, toppings_, extraCheese_);
                        }

                    private:
                        std::string size_ = "medium";
                        std::vector<std::string> toppings_;
                        bool extraCheese_ = false;
                    };
                    int main() {
                        Pizza pizza = PizzaBuilder()
                            .withSize("large")
                            .addTopping("pepperoni")
                            .addTopping("mushroom")
                            .withExtraCheese()
                            .build();

                        pizza.describe();
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    type Pizza struct {
                        Size        string
                        Toppings    []string
                        ExtraCheese bool
                    }

                    type PizzaBuilder struct {
                        size        string
                        toppings    []string
                        extraCheese bool
                    }

                    func NewPizzaBuilder() *PizzaBuilder {
                        return &PizzaBuilder{size: "medium"}
                    }

                    func (b *PizzaBuilder) WithSize(size string) *PizzaBuilder {
                        b.size = size
                        return b
                    }

                    func (b *PizzaBuilder) AddTopping(topping string) *PizzaBuilder {
                        b.toppings = append(b.toppings, topping)
                        return b
                    }

                    func (b *PizzaBuilder) WithExtraCheese() *PizzaBuilder {
                        b.extraCheese = true
                        return b
                    }

                    func (b *PizzaBuilder) Build() Pizza {
                        return Pizza{Size: b.size, Toppings: b.toppings, ExtraCheese: b.extraCheese}
                    }

                    func main() {
                        pizza := NewPizzaBuilder().
                            WithSize("large").
                            AddTopping("pepperoni").
                            AddTopping("mushroom").
                            WithExtraCheese().
                            Build()

                        fmt.Printf("%s pizza with %v, extra cheese: %t\n", pizza.Size, pizza.Toppings, pizza.ExtraCheese)
                    }
                    """),
                new("Rust", """
                    #[derive(Debug)]
                    struct Pizza {
                        size: String,
                        toppings: Vec<String>,
                        extra_cheese: bool,
                    }

                    struct PizzaBuilder {
                        size: String,
                        toppings: Vec<String>,
                        extra_cheese: bool,
                    }

                    impl PizzaBuilder {
                        fn new() -> Self {
                            Self { size: "medium".to_string(), toppings: Vec::new(), extra_cheese: false }
                        }

                        fn with_size(mut self, size: &str) -> Self {
                            self.size = size.to_string();
                            self
                        }

                        fn add_topping(mut self, topping: &str) -> Self {
                            self.toppings.push(topping.to_string());
                            self
                        }

                        fn with_extra_cheese(mut self) -> Self {
                            self.extra_cheese = true;
                            self
                        }

                        fn build(self) -> Pizza {
                            Pizza { size: self.size, toppings: self.toppings, extra_cheese: self.extra_cheese }
                        }
                    }

                    fn main() {
                        let pizza = PizzaBuilder::new()
                            .with_size("large")
                            .add_topping("pepperoni")
                            .add_topping("mushroom")
                            .with_extra_cheese()
                            .build();

                        println!("{pizza:?}");
                    }
                    """),
            }),
    };
}
