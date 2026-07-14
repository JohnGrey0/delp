namespace Delp.Core.Tools.DevUtilities;

public static partial class CodeCheatSheetData
{
    internal static IReadOnlyList<CheatTopic> EverydayTopics { get; } = new List<CheatTopic>
    {
        new(
            "read-write-file",
            "Read/Write a Text File",
            "Everyday",
            "Writing a string to a file and reading it back is one of the most common everyday tasks. Idiomatic solutions favor resource-safe APIs that always close the underlying file handle, whether through an explicit using block, a Python context manager, or a Rust Result-returning helper. Text encoding (UTF-8 by default in most modern standard libraries) matters when files move between platforms or tools.",
            new List<CodeSnippet>
            {
                new("C#", """
                    using System;
                    using System.IO;

                    File.WriteAllText("notes.txt", "Hello, file!");
                    string contents = File.ReadAllText("notes.txt");
                    Console.WriteLine(contents);
                    """),
                new("Python", """
                    with open("notes.txt", "w", encoding="utf-8") as f:
                        f.write("Hello, file!")

                    with open("notes.txt", "r", encoding="utf-8") as f:
                        contents = f.read()

                    print(contents)
                    """),
                new("JavaScript", """
                    // Node.js (not browser) - uses the built-in 'fs' module
                    const fs = require('fs');

                    fs.writeFileSync('notes.txt', 'Hello, file!', 'utf8');
                    const contents = fs.readFileSync('notes.txt', 'utf8');
                    console.log(contents);
                    """),
                new("TypeScript", """
                    // Node.js (not browser) - uses the built-in 'fs' module
                    import * as fs from 'fs';

                    fs.writeFileSync('notes.txt', 'Hello, file!', 'utf8');
                    const contents: string = fs.readFileSync('notes.txt', 'utf8');
                    console.log(contents);
                    """),
                new("Java", """
                    import java.io.IOException;
                    import java.nio.file.Files;
                    import java.nio.file.Path;

                    public class FileExample {
                        public static void main(String[] args) throws IOException {
                            Path path = Path.of("notes.txt");
                            Files.writeString(path, "Hello, file!");
                            String contents = Files.readString(path);
                            System.out.println(contents);
                        }
                    }
                    """),
                new("C++", """
                    #include <fstream>
                    #include <sstream>
                    #include <iostream>
                    #include <string>

                    int main() {
                        {
                            std::ofstream out("notes.txt");
                            out << "Hello, file!";
                        } // destructor closes the file here

                        std::ifstream in("notes.txt");
                        std::stringstream buffer;
                        buffer << in.rdbuf();
                        std::string contents = buffer.str();

                        std::cout << contents << std::endl;
                        return 0;
                    }
                    """),
                new("Go", """
                    package main

                    import (
                        "fmt"
                        "os"
                    )

                    func main() {
                        err := os.WriteFile("notes.txt", []byte("Hello, file!"), 0644)
                        if err != nil {
                            panic(err)
                        }

                        data, err := os.ReadFile("notes.txt")
                        if err != nil {
                            panic(err)
                        }

                        fmt.Println(string(data))
                    }
                    """),
                new("Rust", """
                    use std::fs;

                    fn main() -> std::io::Result<()> {
                        fs::write("notes.txt", "Hello, file!")?;
                        let contents = fs::read_to_string("notes.txt")?;
                        println!("{contents}");
                        Ok(())
                    }
                    """),
            }),
        new(
            "parse-json",
            "Parse JSON",
            "Everyday",
            "Parsing JSON turns a text payload into native data structures so individual fields can be read, and serializing does the reverse to produce JSON text again. Most languages offer either an untyped parse into a map/document or a typed model that mirrors the JSON shape; C++ and Rust have no JSON support in their standard libraries, so the ecosystem-standard nlohmann/json and serde_json libraries are used instead. Watch for missing or null fields - untyped access returns null/None or throws depending on the language, while typed deserializers may fail outright unless fields are marked optional.",
            new List<CodeSnippet>
            {
                new("C#", """
                    using System;
                    using System.Text.Json;

                    string json = "{\"name\":\"Ada\",\"age\":36}";

                    // Parse and read a single field without a model
                    using JsonDocument doc = JsonDocument.Parse(json);
                    string name = doc.RootElement.GetProperty("name").GetString()!;
                    Console.WriteLine(name);

                    // Deserialize into a strongly-typed record. System.Text.Json matches
                    // property names case-sensitively by default, so opt in to
                    // case-insensitive matching to bind the lowercase JSON keys.
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    Person person = JsonSerializer.Deserialize<Person>(json, options)!;
                    Console.WriteLine(person.Age);

                    // Serialize back to JSON
                    string output = JsonSerializer.Serialize(person);
                    Console.WriteLine(output);

                    record Person(string Name, int Age);
                    """),
                new("Python", """
                    import json

                    data = '{"name": "Ada", "age": 36}'

                    # Parse into a native dict
                    parsed = json.loads(data)
                    print(parsed["name"])

                    # Serialize back to a JSON string
                    output = json.dumps(parsed)
                    print(output)
                    """),
                new("JavaScript", """
                    const data = '{"name": "Ada", "age": 36}';

                    // Parse into a native object
                    const parsed = JSON.parse(data);
                    console.log(parsed.name);

                    // Serialize back to a JSON string
                    const output = JSON.stringify(parsed);
                    console.log(output);
                    """),
                new("TypeScript", """
                    interface Person {
                        name: string;
                        age: number;
                    }

                    const data = '{"name": "Ada", "age": 36}';

                    // Parse into a typed object
                    const parsed: Person = JSON.parse(data);
                    console.log(parsed.name);

                    // Serialize back to a JSON string
                    const output: string = JSON.stringify(parsed);
                    console.log(output);
                    """),
                new("Java", """
                    // Requires the Jackson dependency: com.fasterxml.jackson.core:jackson-databind
                    import com.fasterxml.jackson.databind.ObjectMapper;

                    public class JsonExample {
                        record Person(String name, int age) {}

                        public static void main(String[] args) throws Exception {
                            String json = "{\"name\":\"Ada\",\"age\":36}";
                            ObjectMapper mapper = new ObjectMapper();

                            Person person = mapper.readValue(json, Person.class);
                            System.out.println(person.name());

                            String output = mapper.writeValueAsString(person);
                            System.out.println(output);
                        }
                    }
                    """),
                new("C++", """
                    // Requires the nlohmann/json single-header library
                    // (https://github.com/nlohmann/json)
                    #include <nlohmann/json.hpp>
                    #include <iostream>
                    #include <string>

                    using json = nlohmann::json;

                    int main() {
                        std::string data = R"({"name": "Ada", "age": 36})";

                        json parsed = json::parse(data);
                        std::string name = parsed["name"];
                        std::cout << name << std::endl;

                        std::string output = parsed.dump();
                        std::cout << output << std::endl;
                        return 0;
                    }
                    """),
                new("Go", """
                    package main

                    import (
                        "encoding/json"
                        "fmt"
                    )

                    type Person struct {
                        Name string `json:"name"`
                        Age  int    `json:"age"`
                    }

                    func main() {
                        data := []byte(`{"name": "Ada", "age": 36}`)

                        var person Person
                        if err := json.Unmarshal(data, &person); err != nil {
                            panic(err)
                        }
                        fmt.Println(person.Name)

                        output, err := json.Marshal(person)
                        if err != nil {
                            panic(err)
                        }
                        fmt.Println(string(output))
                    }
                    """),
                new("Rust", """
                    // Requires serde and serde_json in Cargo.toml:
                    // serde = { version = "1", features = ["derive"] }
                    // serde_json = "1"
                    use serde::{Deserialize, Serialize};

                    #[derive(Serialize, Deserialize)]
                    struct Person {
                        name: String,
                        age: u32,
                    }

                    fn main() -> Result<(), serde_json::Error> {
                        let data = r#"{"name": "Ada", "age": 36}"#;

                        let person: Person = serde_json::from_str(data)?;
                        println!("{}", person.name);

                        let output = serde_json::to_string(&person)?;
                        println!("{output}");
                        Ok(())
                    }
                    """),
            }),
        new(
            "http-get",
            "HTTP GET Request",
            "Everyday",
            "Making an HTTP GET request and reading the response body or status code underlies nearly every API integration. Reuse a single client or connection pool across requests instead of creating one per call - HttpClient in C# and Java, and the client types in Go and Rust, are all designed to be shared, and constructing one per request can exhaust sockets under load. C++ and Rust have no HTTP client in their standard libraries, so libcurl and reqwest are the de facto standards, respectively.",
            new List<CodeSnippet>
            {
                new("C#", """
                    using System;
                    using System.Net.Http;
                    using System.Threading.Tasks;

                    class Program
                    {
                        // Reuse a single HttpClient; creating one per request can exhaust sockets.
                        private static readonly HttpClient client = new();

                        static async Task Main()
                        {
                            HttpResponseMessage response = await client.GetAsync("https://example.com");
                            response.EnsureSuccessStatusCode();

                            string body = await response.Content.ReadAsStringAsync();
                            Console.WriteLine(body);
                        }
                    }
                    """),
                new("Python", """
                    # Standard library only; the third-party 'requests' package is the
                    # common real-world alternative (requests.get(url).text).
                    import urllib.request

                    with urllib.request.urlopen("https://example.com") as response:
                        status = response.status
                        body = response.read().decode("utf-8")

                    print(status)
                    print(body)
                    """),
                new("JavaScript", """
                    // fetch is built in to modern Node.js (18+) and all browsers
                    const response = await fetch("https://example.com");
                    const status = response.status;
                    const body = await response.text();

                    console.log(status);
                    console.log(body);
                    """),
                new("TypeScript", """
                    // fetch is built in to modern Node.js (18+) and all browsers
                    async function getExample(): Promise<void> {
                        const response: Response = await fetch("https://example.com");
                        const status: number = response.status;
                        const body: string = await response.text();

                        console.log(status);
                        console.log(body);
                    }

                    await getExample();
                    """),
                new("Java", """
                    import java.net.URI;
                    import java.net.http.HttpClient;
                    import java.net.http.HttpRequest;
                    import java.net.http.HttpResponse;

                    public class HttpExample {
                        public static void main(String[] args) throws Exception {
                            HttpClient client = HttpClient.newHttpClient();
                            HttpRequest request = HttpRequest.newBuilder()
                                    .uri(URI.create("https://example.com"))
                                    .GET()
                                    .build();

                            HttpResponse<String> response =
                                    client.send(request, HttpResponse.BodyHandlers.ofString());

                            System.out.println(response.statusCode());
                            System.out.println(response.body());
                        }
                    }
                    """),
                new("C++", """
                    // Requires libcurl (https://curl.se/libcurl/) - link with -lcurl
                    #include <curl/curl.h>
                    #include <iostream>
                    #include <string>

                    static size_t writeCallback(char* ptr, size_t size, size_t nmemb, void* userdata) {
                        auto* body = static_cast<std::string*>(userdata);
                        body->append(ptr, size * nmemb);
                        return size * nmemb;
                    }

                    int main() {
                        CURL* curl = curl_easy_init();
                        std::string body;

                        if (curl) {
                            curl_easy_setopt(curl, CURLOPT_URL, "https://example.com");
                            curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, writeCallback);
                            curl_easy_setopt(curl, CURLOPT_WRITEDATA, &body);

                            CURLcode res = curl_easy_perform(curl);
                            long statusCode = 0;
                            curl_easy_getinfo(curl, CURLINFO_RESPONSE_CODE, &statusCode);

                            if (res == CURLE_OK) {
                                std::cout << statusCode << std::endl;
                                std::cout << body << std::endl;
                            }
                            curl_easy_cleanup(curl);
                        }
                        return 0;
                    }
                    """),
                new("Go", """
                    package main

                    import (
                        "fmt"
                        "io"
                        "net/http"
                    )

                    func main() {
                        response, err := http.Get("https://example.com")
                        if err != nil {
                            panic(err)
                        }
                        defer response.Body.Close()

                        body, err := io.ReadAll(response.Body)
                        if err != nil {
                            panic(err)
                        }

                        fmt.Println(response.StatusCode)
                        fmt.Println(string(body))
                    }
                    """),
                new("Rust", """
                    // Requires the reqwest crate in Cargo.toml:
                    // reqwest = { version = "0.12", features = ["blocking"] }
                    use reqwest::blocking::get;

                    fn main() -> Result<(), reqwest::Error> {
                        let response = get("https://example.com")?;
                        let status = response.status();
                        let body = response.text()?;

                        println!("{status}");
                        println!("{body}");
                        Ok(())
                    }
                    """),
            }),
        new(
            "string-formatting",
            "String Formatting",
            "Everyday",
            "Building a formatted string that interpolates typed values, such as a name and a number rendered to a fixed number of decimal places, is a routine task in every language. Modern languages favor inline interpolation syntax (C# interpolated strings, Python f-strings, JS/TS template literals) while others use a format-string-plus-arguments call (Java's String.format, Go's fmt.Sprintf, Rust's format! macro). Always check the format specifier for floating-point precision, since default to-string conversions often show a variable number of digits.",
            new List<CodeSnippet>
            {
                new("C#", """
                    using System;

                    string name = "Ada";
                    double amount = 1234.5;

                    string message = $"Hello, {name}! Your balance is {amount:F2}.";
                    Console.WriteLine(message);
                    """),
                new("Python", """
                    name = "Ada"
                    amount = 1234.5

                    message = f"Hello, {name}! Your balance is {amount:.2f}."
                    print(message)
                    """),
                new("JavaScript", """
                    const name = "Ada";
                    const amount = 1234.5;

                    const message = `Hello, ${name}! Your balance is ${amount.toFixed(2)}.`;
                    console.log(message);
                    """),
                new("TypeScript", """
                    const name: string = "Ada";
                    const amount: number = 1234.5;

                    const message: string = `Hello, ${name}! Your balance is ${amount.toFixed(2)}.`;
                    console.log(message);
                    """),
                new("Java", """
                    public class FormatExample {
                        public static void main(String[] args) {
                            String name = "Ada";
                            double amount = 1234.5;

                            String message = String.format("Hello, %s! Your balance is %.2f.", name, amount);
                            System.out.println(message);
                        }
                    }
                    """),
                new("C++", """
                    #include <format>
                    #include <iostream>
                    #include <string>

                    int main() {
                        std::string name = "Ada";
                        double amount = 1234.5;

                        // std::format is the C++20 standard-library idiom.
                        std::string message = std::format("Hello, {}! Your balance is {:.2f}.", name, amount);
                        std::cout << message << std::endl;
                        return 0;
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    func main() {
                        name := "Ada"
                        amount := 1234.5

                        message := fmt.Sprintf("Hello, %s! Your balance is %.2f.", name, amount)
                        fmt.Println(message)
                    }
                    """),
                new("Rust", """
                    fn main() {
                        let name = "Ada";
                        let amount = 1234.5;

                        let message = format!("Hello, {name}! Your balance is {amount:.2}.");
                        println!("{message}");
                    }
                    """),
            }),
    };
}
