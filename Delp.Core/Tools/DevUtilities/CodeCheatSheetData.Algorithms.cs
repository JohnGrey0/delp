namespace Delp.Core.Tools.DevUtilities;

public static partial class CodeCheatSheetData
{
    internal static IReadOnlyList<CheatTopic> AlgorithmsTopics { get; } = new List<CheatTopic>
    {
        new(
            "bubble-sort",
            "Bubble Sort",
            "Algorithms",
            "Repeatedly steps through the list, swapping adjacent elements that are out of order, so the largest unsorted element \"bubbles\" to the end each pass. Simple to implement but rarely used in practice due to its O(n^2) time complexity; useful mainly for teaching or tiny/near-sorted inputs. Space complexity is O(1) since it sorts in place.",
            new List<CodeSnippet>
            {
                new("C#", """
                    using System;

                    class Program
                    {
                        static void BubbleSort(int[] arr)
                        {
                            for (int i = 0; i < arr.Length - 1; i++)
                            {
                                bool swapped = false;
                                for (int j = 0; j < arr.Length - 1 - i; j++)
                                {
                                    if (arr[j] > arr[j + 1])
                                    {
                                        (arr[j], arr[j + 1]) = (arr[j + 1], arr[j]);
                                        swapped = true;
                                    }
                                }
                                if (!swapped) break;
                            }
                        }

                        static void Main()
                        {
                            var nums = new[] { 5, 3, 8, 4, 2 };
                            BubbleSort(nums);
                            Console.WriteLine(string.Join(",", nums));
                        }
                    }
                    """),
                new("Python", """
                    def bubble_sort(arr):
                        n = len(arr)
                        for i in range(n - 1):
                            swapped = False
                            for j in range(n - 1 - i):
                                if arr[j] > arr[j + 1]:
                                    arr[j], arr[j + 1] = arr[j + 1], arr[j]
                                    swapped = True
                            if not swapped:
                                break
                        return arr


                    if __name__ == "__main__":
                        nums = [5, 3, 8, 4, 2]
                        print(bubble_sort(nums))
                    """),
                new("JavaScript", """
                    function bubbleSort(arr) {
                      for (let i = 0; i < arr.length - 1; i++) {
                        let swapped = false;
                        for (let j = 0; j < arr.length - 1 - i; j++) {
                          if (arr[j] > arr[j + 1]) {
                            [arr[j], arr[j + 1]] = [arr[j + 1], arr[j]];
                            swapped = true;
                          }
                        }
                        if (!swapped) break;
                      }
                      return arr;
                    }

                    console.log(bubbleSort([5, 3, 8, 4, 2]));
                    """),
                new("TypeScript", """
                    function bubbleSort(arr: number[]): number[] {
                      for (let i = 0; i < arr.length - 1; i++) {
                        let swapped = false;
                        for (let j = 0; j < arr.length - 1 - i; j++) {
                          if (arr[j] > arr[j + 1]) {
                            [arr[j], arr[j + 1]] = [arr[j + 1], arr[j]];
                            swapped = true;
                          }
                        }
                        if (!swapped) break;
                      }
                      return arr;
                    }

                    console.log(bubbleSort([5, 3, 8, 4, 2]));
                    """),
                new("Java", """
                    import java.util.Arrays;

                    public class Main {
                        static void bubbleSort(int[] arr) {
                            for (int i = 0; i < arr.length - 1; i++) {
                                boolean swapped = false;
                                for (int j = 0; j < arr.length - 1 - i; j++) {
                                    if (arr[j] > arr[j + 1]) {
                                        int tmp = arr[j];
                                        arr[j] = arr[j + 1];
                                        arr[j + 1] = tmp;
                                        swapped = true;
                                    }
                                }
                                if (!swapped) break;
                            }
                        }

                        public static void main(String[] args) {
                            int[] nums = {5, 3, 8, 4, 2};
                            bubbleSort(nums);
                            System.out.println(Arrays.toString(nums));
                        }
                    }
                    """),
                new("C++", """
                    #include <iostream>
                    #include <vector>

                    void bubbleSort(std::vector<int>& arr) {
                        for (size_t i = 0; i + 1 < arr.size(); i++) {
                            bool swapped = false;
                            for (size_t j = 0; j + 1 < arr.size() - i; j++) {
                                if (arr[j] > arr[j + 1]) {
                                    std::swap(arr[j], arr[j + 1]);
                                    swapped = true;
                                }
                            }
                            if (!swapped) break;
                        }
                    }

                    int main() {
                        std::vector<int> nums = {5, 3, 8, 4, 2};
                        bubbleSort(nums);
                        for (int n : nums) std::cout << n << " ";
                        std::cout << std::endl;
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    func bubbleSort(arr []int) {
                        n := len(arr)
                        for i := 0; i < n-1; i++ {
                            swapped := false
                            for j := 0; j < n-1-i; j++ {
                                if arr[j] > arr[j+1] {
                                    arr[j], arr[j+1] = arr[j+1], arr[j]
                                    swapped = true
                                }
                            }
                            if !swapped {
                                break
                            }
                        }
                    }

                    func main() {
                        nums := []int{5, 3, 8, 4, 2}
                        bubbleSort(nums)
                        fmt.Println(nums)
                    }
                    """),
                new("Rust", """
                    fn bubble_sort(arr: &mut [i32]) {
                        let n = arr.len();
                        for i in 0..n.saturating_sub(1) {
                            let mut swapped = false;
                            for j in 0..n - 1 - i {
                                if arr[j] > arr[j + 1] {
                                    arr.swap(j, j + 1);
                                    swapped = true;
                                }
                            }
                            if !swapped {
                                break;
                            }
                        }
                    }

                    fn main() {
                        let mut nums = vec![5, 3, 8, 4, 2];
                        bubble_sort(&mut nums);
                        println!("{:?}", nums);
                    }
                    """),
            }),
        new(
            "insertion-sort",
            "Insertion Sort",
            "Algorithms",
            "Builds the final sorted array one item at a time, taking each new element and inserting it into its correct position among the already-sorted elements to its left. It runs in O(n^2) time in the worst case but is efficient for small or nearly-sorted datasets and sorts in place using O(1) extra space. It is also stable, preserving the relative order of equal elements.",
            new List<CodeSnippet>
            {
                new("C#", """
                    using System;

                    class Program
                    {
                        static void InsertionSort(int[] arr)
                        {
                            for (int i = 1; i < arr.Length; i++)
                            {
                                int key = arr[i];
                                int j = i - 1;
                                while (j >= 0 && arr[j] > key)
                                {
                                    arr[j + 1] = arr[j];
                                    j--;
                                }
                                arr[j + 1] = key;
                            }
                        }

                        static void Main()
                        {
                            var nums = new[] { 5, 3, 8, 4, 2 };
                            InsertionSort(nums);
                            Console.WriteLine(string.Join(",", nums));
                        }
                    }
                    """),
                new("Python", """
                    def insertion_sort(arr):
                        for i in range(1, len(arr)):
                            key = arr[i]
                            j = i - 1
                            while j >= 0 and arr[j] > key:
                                arr[j + 1] = arr[j]
                                j -= 1
                            arr[j + 1] = key
                        return arr


                    if __name__ == "__main__":
                        nums = [5, 3, 8, 4, 2]
                        print(insertion_sort(nums))
                    """),
                new("JavaScript", """
                    function insertionSort(arr) {
                      for (let i = 1; i < arr.length; i++) {
                        const key = arr[i];
                        let j = i - 1;
                        while (j >= 0 && arr[j] > key) {
                          arr[j + 1] = arr[j];
                          j--;
                        }
                        arr[j + 1] = key;
                      }
                      return arr;
                    }

                    console.log(insertionSort([5, 3, 8, 4, 2]));
                    """),
                new("TypeScript", """
                    function insertionSort(arr: number[]): number[] {
                      for (let i = 1; i < arr.length; i++) {
                        const key = arr[i];
                        let j = i - 1;
                        while (j >= 0 && arr[j] > key) {
                          arr[j + 1] = arr[j];
                          j--;
                        }
                        arr[j + 1] = key;
                      }
                      return arr;
                    }

                    console.log(insertionSort([5, 3, 8, 4, 2]));
                    """),
                new("Java", """
                    import java.util.Arrays;

                    public class Main {
                        static void insertionSort(int[] arr) {
                            for (int i = 1; i < arr.length; i++) {
                                int key = arr[i];
                                int j = i - 1;
                                while (j >= 0 && arr[j] > key) {
                                    arr[j + 1] = arr[j];
                                    j--;
                                }
                                arr[j + 1] = key;
                            }
                        }

                        public static void main(String[] args) {
                            int[] nums = {5, 3, 8, 4, 2};
                            insertionSort(nums);
                            System.out.println(Arrays.toString(nums));
                        }
                    }
                    """),
                new("C++", """
                    #include <iostream>
                    #include <vector>

                    void insertionSort(std::vector<int>& arr) {
                        for (size_t i = 1; i < arr.size(); i++) {
                            int key = arr[i];
                            int j = static_cast<int>(i) - 1;
                            while (j >= 0 && arr[j] > key) {
                                arr[j + 1] = arr[j];
                                j--;
                            }
                            arr[j + 1] = key;
                        }
                    }

                    int main() {
                        std::vector<int> nums = {5, 3, 8, 4, 2};
                        insertionSort(nums);
                        for (int n : nums) std::cout << n << " ";
                        std::cout << std::endl;
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    func insertionSort(arr []int) {
                        for i := 1; i < len(arr); i++ {
                            key := arr[i]
                            j := i - 1
                            for j >= 0 && arr[j] > key {
                                arr[j+1] = arr[j]
                                j--
                            }
                            arr[j+1] = key
                        }
                    }

                    func main() {
                        nums := []int{5, 3, 8, 4, 2}
                        insertionSort(nums)
                        fmt.Println(nums)
                    }
                    """),
                new("Rust", """
                    fn insertion_sort(arr: &mut [i32]) {
                        for i in 1..arr.len() {
                            let key = arr[i];
                            let mut j = i as isize - 1;
                            while j >= 0 && arr[j as usize] > key {
                                arr[(j + 1) as usize] = arr[j as usize];
                                j -= 1;
                            }
                            arr[(j + 1) as usize] = key;
                        }
                    }

                    fn main() {
                        let mut nums = vec![5, 3, 8, 4, 2];
                        insertion_sort(&mut nums);
                        println!("{:?}", nums);
                    }
                    """),
            }),
        new(
            "merge-sort",
            "Merge Sort",
            "Algorithms",
            "A divide-and-conquer algorithm that recursively splits the array in half, sorts each half, then merges the two sorted halves back together. It guarantees O(n log n) time in all cases and is stable, making it a good choice when predictable performance matters, at the cost of O(n) extra space for the merge step.",
            new List<CodeSnippet>
            {
                new("C#", """
                    using System;
                    using System.Linq;

                    class Program
                    {
                        static int[] MergeSort(int[] arr)
                        {
                            if (arr.Length <= 1) return arr;

                            int mid = arr.Length / 2;
                            int[] left = MergeSort(arr[..mid]);
                            int[] right = MergeSort(arr[mid..]);
                            return Merge(left, right);
                        }

                        static int[] Merge(int[] left, int[] right)
                        {
                            var result = new int[left.Length + right.Length];
                            int i = 0, j = 0, k = 0;
                            while (i < left.Length && j < right.Length)
                                result[k++] = left[i] <= right[j] ? left[i++] : right[j++];
                            while (i < left.Length) result[k++] = left[i++];
                            while (j < right.Length) result[k++] = right[j++];
                            return result;
                        }

                        static void Main()
                        {
                            var nums = new[] { 5, 3, 8, 4, 2 };
                            Console.WriteLine(string.Join(",", MergeSort(nums)));
                        }
                    }
                    """),
                new("Python", """
                    def merge_sort(arr):
                        if len(arr) <= 1:
                            return arr

                        mid = len(arr) // 2
                        left = merge_sort(arr[:mid])
                        right = merge_sort(arr[mid:])
                        return merge(left, right)


                    def merge(left, right):
                        result = []
                        i = j = 0
                        while i < len(left) and j < len(right):
                            if left[i] <= right[j]:
                                result.append(left[i])
                                i += 1
                            else:
                                result.append(right[j])
                                j += 1
                        result.extend(left[i:])
                        result.extend(right[j:])
                        return result


                    if __name__ == "__main__":
                        print(merge_sort([5, 3, 8, 4, 2]))
                    """),
                new("JavaScript", """
                    function mergeSort(arr) {
                      if (arr.length <= 1) return arr;

                      const mid = Math.floor(arr.length / 2);
                      const left = mergeSort(arr.slice(0, mid));
                      const right = mergeSort(arr.slice(mid));
                      return merge(left, right);
                    }

                    function merge(left, right) {
                      const result = [];
                      let i = 0, j = 0;
                      while (i < left.length && j < right.length) {
                        result.push(left[i] <= right[j] ? left[i++] : right[j++]);
                      }
                      return result.concat(left.slice(i), right.slice(j));
                    }

                    console.log(mergeSort([5, 3, 8, 4, 2]));
                    """),
                new("TypeScript", """
                    function mergeSort(arr: number[]): number[] {
                      if (arr.length <= 1) return arr;

                      const mid = Math.floor(arr.length / 2);
                      const left = mergeSort(arr.slice(0, mid));
                      const right = mergeSort(arr.slice(mid));
                      return merge(left, right);
                    }

                    function merge(left: number[], right: number[]): number[] {
                      const result: number[] = [];
                      let i = 0, j = 0;
                      while (i < left.length && j < right.length) {
                        result.push(left[i] <= right[j] ? left[i++] : right[j++]);
                      }
                      return result.concat(left.slice(i), right.slice(j));
                    }

                    console.log(mergeSort([5, 3, 8, 4, 2]));
                    """),
                new("Java", """
                    import java.util.Arrays;

                    public class Main {
                        static int[] mergeSort(int[] arr) {
                            if (arr.length <= 1) return arr;
                            int mid = arr.length / 2;
                            int[] left = mergeSort(Arrays.copyOfRange(arr, 0, mid));
                            int[] right = mergeSort(Arrays.copyOfRange(arr, mid, arr.length));
                            return merge(left, right);
                        }

                        static int[] merge(int[] left, int[] right) {
                            int[] result = new int[left.length + right.length];
                            int i = 0, j = 0, k = 0;
                            while (i < left.length && j < right.length)
                                result[k++] = left[i] <= right[j] ? left[i++] : right[j++];
                            while (i < left.length) result[k++] = left[i++];
                            while (j < right.length) result[k++] = right[j++];
                            return result;
                        }

                        public static void main(String[] args) {
                            int[] nums = {5, 3, 8, 4, 2};
                            System.out.println(Arrays.toString(mergeSort(nums)));
                        }
                    }
                    """),
                new("C++", """
                    #include <iostream>
                    #include <vector>

                    std::vector<int> merge(const std::vector<int>& left, const std::vector<int>& right) {
                        std::vector<int> result;
                        size_t i = 0, j = 0;
                        while (i < left.size() && j < right.size())
                            result.push_back(left[i] <= right[j] ? left[i++] : right[j++]);
                        while (i < left.size()) result.push_back(left[i++]);
                        while (j < right.size()) result.push_back(right[j++]);
                        return result;
                    }

                    std::vector<int> mergeSort(const std::vector<int>& arr) {
                        if (arr.size() <= 1) return arr;
                        size_t mid = arr.size() / 2;
                        auto left = mergeSort(std::vector<int>(arr.begin(), arr.begin() + mid));
                        auto right = mergeSort(std::vector<int>(arr.begin() + mid, arr.end()));
                        return merge(left, right);
                    }

                    int main() {
                        std::vector<int> nums = {5, 3, 8, 4, 2};
                        for (int n : mergeSort(nums)) std::cout << n << " ";
                        std::cout << std::endl;
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    func merge(left, right []int) []int {
                        result := make([]int, 0, len(left)+len(right))
                        i, j := 0, 0
                        for i < len(left) && j < len(right) {
                            if left[i] <= right[j] {
                                result = append(result, left[i])
                                i++
                            } else {
                                result = append(result, right[j])
                                j++
                            }
                        }
                        result = append(result, left[i:]...)
                        result = append(result, right[j:]...)
                        return result
                    }

                    func mergeSort(arr []int) []int {
                        if len(arr) <= 1 {
                            return arr
                        }
                        mid := len(arr) / 2
                        left := mergeSort(arr[:mid])
                        right := mergeSort(arr[mid:])
                        return merge(left, right)
                    }

                    func main() {
                        nums := []int{5, 3, 8, 4, 2}
                        fmt.Println(mergeSort(nums))
                    }
                    """),
                new("Rust", """
                    fn merge(left: &[i32], right: &[i32]) -> Vec<i32> {
                        let mut result = Vec::with_capacity(left.len() + right.len());
                        let (mut i, mut j) = (0, 0);
                        while i < left.len() && j < right.len() {
                            if left[i] <= right[j] {
                                result.push(left[i]);
                                i += 1;
                            } else {
                                result.push(right[j]);
                                j += 1;
                            }
                        }
                        result.extend_from_slice(&left[i..]);
                        result.extend_from_slice(&right[j..]);
                        result
                    }

                    fn merge_sort(arr: &[i32]) -> Vec<i32> {
                        if arr.len() <= 1 {
                            return arr.to_vec();
                        }
                        let mid = arr.len() / 2;
                        let left = merge_sort(&arr[..mid]);
                        let right = merge_sort(&arr[mid..]);
                        merge(&left, &right)
                    }

                    fn main() {
                        let nums = vec![5, 3, 8, 4, 2];
                        println!("{:?}", merge_sort(&nums));
                    }
                    """),
            }),
        new(
            "quicksort",
            "Quicksort",
            "Algorithms",
            "A divide-and-conquer algorithm that picks a pivot, partitions the array so smaller elements land left of the pivot and larger ones land right, then recursively sorts each partition. Average time complexity is O(n log n) with O(log n) space for the recursion stack, though a poor pivot choice can degrade the worst case to O(n^2); it is widely used because it sorts in place and has excellent constant factors in practice.",
            new List<CodeSnippet>
            {
                new("C#", """
                    using System;

                    class Program
                    {
                        static void QuickSort(int[] arr, int low, int high)
                        {
                            if (low >= high) return;
                            int p = Partition(arr, low, high);
                            QuickSort(arr, low, p - 1);
                            QuickSort(arr, p + 1, high);
                        }

                        static int Partition(int[] arr, int low, int high)
                        {
                            int pivot = arr[high];
                            int i = low - 1;
                            for (int j = low; j < high; j++)
                            {
                                if (arr[j] < pivot)
                                {
                                    i++;
                                    (arr[i], arr[j]) = (arr[j], arr[i]);
                                }
                            }
                            (arr[i + 1], arr[high]) = (arr[high], arr[i + 1]);
                            return i + 1;
                        }

                        static void Main()
                        {
                            var nums = new[] { 5, 3, 8, 4, 2 };
                            QuickSort(nums, 0, nums.Length - 1);
                            Console.WriteLine(string.Join(",", nums));
                        }
                    }
                    """),
                new("Python", """
                    def quicksort(arr, low=0, high=None):
                        if high is None:
                            high = len(arr) - 1
                        if low < high:
                            p = partition(arr, low, high)
                            quicksort(arr, low, p - 1)
                            quicksort(arr, p + 1, high)
                        return arr


                    def partition(arr, low, high):
                        pivot = arr[high]
                        i = low - 1
                        for j in range(low, high):
                            if arr[j] < pivot:
                                i += 1
                                arr[i], arr[j] = arr[j], arr[i]
                        arr[i + 1], arr[high] = arr[high], arr[i + 1]
                        return i + 1


                    if __name__ == "__main__":
                        print(quicksort([5, 3, 8, 4, 2]))
                    """),
                new("JavaScript", """
                    function quickSort(arr, low = 0, high = arr.length - 1) {
                      if (low < high) {
                        const p = partition(arr, low, high);
                        quickSort(arr, low, p - 1);
                        quickSort(arr, p + 1, high);
                      }
                      return arr;
                    }

                    function partition(arr, low, high) {
                      const pivot = arr[high];
                      let i = low - 1;
                      for (let j = low; j < high; j++) {
                        if (arr[j] < pivot) {
                          i++;
                          [arr[i], arr[j]] = [arr[j], arr[i]];
                        }
                      }
                      [arr[i + 1], arr[high]] = [arr[high], arr[i + 1]];
                      return i + 1;
                    }

                    console.log(quickSort([5, 3, 8, 4, 2]));
                    """),
                new("TypeScript", """
                    function quickSort(arr: number[], low = 0, high = arr.length - 1): number[] {
                      if (low < high) {
                        const p = partition(arr, low, high);
                        quickSort(arr, low, p - 1);
                        quickSort(arr, p + 1, high);
                      }
                      return arr;
                    }

                    function partition(arr: number[], low: number, high: number): number {
                      const pivot = arr[high];
                      let i = low - 1;
                      for (let j = low; j < high; j++) {
                        if (arr[j] < pivot) {
                          i++;
                          [arr[i], arr[j]] = [arr[j], arr[i]];
                        }
                      }
                      [arr[i + 1], arr[high]] = [arr[high], arr[i + 1]];
                      return i + 1;
                    }

                    console.log(quickSort([5, 3, 8, 4, 2]));
                    """),
                new("Java", """
                    import java.util.Arrays;

                    public class Main {
                        static void quickSort(int[] arr, int low, int high) {
                            if (low >= high) return;
                            int p = partition(arr, low, high);
                            quickSort(arr, low, p - 1);
                            quickSort(arr, p + 1, high);
                        }

                        static int partition(int[] arr, int low, int high) {
                            int pivot = arr[high];
                            int i = low - 1;
                            for (int j = low; j < high; j++) {
                                if (arr[j] < pivot) {
                                    i++;
                                    int tmp = arr[i];
                                    arr[i] = arr[j];
                                    arr[j] = tmp;
                                }
                            }
                            int tmp = arr[i + 1];
                            arr[i + 1] = arr[high];
                            arr[high] = tmp;
                            return i + 1;
                        }

                        public static void main(String[] args) {
                            int[] nums = {5, 3, 8, 4, 2};
                            quickSort(nums, 0, nums.length - 1);
                            System.out.println(Arrays.toString(nums));
                        }
                    }
                    """),
                new("C++", """
                    #include <iostream>
                    #include <vector>

                    int partition(std::vector<int>& arr, int low, int high) {
                        int pivot = arr[high];
                        int i = low - 1;
                        for (int j = low; j < high; j++) {
                            if (arr[j] < pivot) {
                                i++;
                                std::swap(arr[i], arr[j]);
                            }
                        }
                        std::swap(arr[i + 1], arr[high]);
                        return i + 1;
                    }

                    void quickSort(std::vector<int>& arr, int low, int high) {
                        if (low >= high) return;
                        int p = partition(arr, low, high);
                        quickSort(arr, low, p - 1);
                        quickSort(arr, p + 1, high);
                    }

                    int main() {
                        std::vector<int> nums = {5, 3, 8, 4, 2};
                        quickSort(nums, 0, static_cast<int>(nums.size()) - 1);
                        for (int n : nums) std::cout << n << " ";
                        std::cout << std::endl;
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    func partition(arr []int, low, high int) int {
                        pivot := arr[high]
                        i := low - 1
                        for j := low; j < high; j++ {
                            if arr[j] < pivot {
                                i++
                                arr[i], arr[j] = arr[j], arr[i]
                            }
                        }
                        arr[i+1], arr[high] = arr[high], arr[i+1]
                        return i + 1
                    }

                    func quickSort(arr []int, low, high int) {
                        if low < high {
                            p := partition(arr, low, high)
                            quickSort(arr, low, p-1)
                            quickSort(arr, p+1, high)
                        }
                    }

                    func main() {
                        nums := []int{5, 3, 8, 4, 2}
                        quickSort(nums, 0, len(nums)-1)
                        fmt.Println(nums)
                    }
                    """),
                new("Rust", """
                    fn partition(arr: &mut [i32]) -> usize {
                        let high = arr.len() - 1;
                        let pivot = arr[high];
                        let mut i = 0;
                        for j in 0..high {
                            if arr[j] < pivot {
                                arr.swap(i, j);
                                i += 1;
                            }
                        }
                        arr.swap(i, high);
                        i
                    }

                    fn quick_sort(arr: &mut [i32]) {
                        if arr.len() <= 1 {
                            return;
                        }
                        let p = partition(arr);
                        quick_sort(&mut arr[..p]);
                        quick_sort(&mut arr[p + 1..]);
                    }

                    fn main() {
                        let mut nums = vec![5, 3, 8, 4, 2];
                        quick_sort(&mut nums);
                        println!("{:?}", nums);
                    }
                    """),
            }),
        new(
            "binary-search",
            "Binary Search",
            "Algorithms",
            "Finds a target value in a sorted array by repeatedly halving the search range: compare the middle element to the target and discard the half that cannot contain it. Requires the input to be sorted, and runs in O(log n) time with O(1) space (iterative form), making it far faster than a linear scan for large datasets.",
            new List<CodeSnippet>
            {
                new("C#", """
                    using System;

                    class Program
                    {
                        static int BinarySearch(int[] arr, int target)
                        {
                            int low = 0, high = arr.Length - 1;
                            while (low <= high)
                            {
                                int mid = low + (high - low) / 2;
                                if (arr[mid] == target) return mid;
                                if (arr[mid] < target) low = mid + 1;
                                else high = mid - 1;
                            }
                            return -1;
                        }

                        static void Main()
                        {
                            var nums = new[] { 1, 3, 4, 5, 8, 9, 12 };
                            Console.WriteLine(BinarySearch(nums, 5));
                            Console.WriteLine(BinarySearch(nums, 7));
                        }
                    }
                    """),
                new("Python", """
                    def binary_search(arr, target):
                        low, high = 0, len(arr) - 1
                        while low <= high:
                            mid = low + (high - low) // 2
                            if arr[mid] == target:
                                return mid
                            if arr[mid] < target:
                                low = mid + 1
                            else:
                                high = mid - 1
                        return -1


                    if __name__ == "__main__":
                        nums = [1, 3, 4, 5, 8, 9, 12]
                        print(binary_search(nums, 5))
                        print(binary_search(nums, 7))
                    """),
                new("JavaScript", """
                    function binarySearch(arr, target) {
                      let low = 0, high = arr.length - 1;
                      while (low <= high) {
                        const mid = low + Math.floor((high - low) / 2);
                        if (arr[mid] === target) return mid;
                        if (arr[mid] < target) low = mid + 1;
                        else high = mid - 1;
                      }
                      return -1;
                    }

                    const nums = [1, 3, 4, 5, 8, 9, 12];
                    console.log(binarySearch(nums, 5));
                    console.log(binarySearch(nums, 7));
                    """),
                new("TypeScript", """
                    function binarySearch(arr: number[], target: number): number {
                      let low = 0, high = arr.length - 1;
                      while (low <= high) {
                        const mid = low + Math.floor((high - low) / 2);
                        if (arr[mid] === target) return mid;
                        if (arr[mid] < target) low = mid + 1;
                        else high = mid - 1;
                      }
                      return -1;
                    }

                    const nums: number[] = [1, 3, 4, 5, 8, 9, 12];
                    console.log(binarySearch(nums, 5));
                    console.log(binarySearch(nums, 7));
                    """),
                new("Java", """
                    public class Main {
                        static int binarySearch(int[] arr, int target) {
                            int low = 0, high = arr.length - 1;
                            while (low <= high) {
                                int mid = low + (high - low) / 2;
                                if (arr[mid] == target) return mid;
                                if (arr[mid] < target) low = mid + 1;
                                else high = mid - 1;
                            }
                            return -1;
                        }

                        public static void main(String[] args) {
                            int[] nums = {1, 3, 4, 5, 8, 9, 12};
                            System.out.println(binarySearch(nums, 5));
                            System.out.println(binarySearch(nums, 7));
                        }
                    }
                    """),
                new("C++", """
                    #include <iostream>
                    #include <vector>

                    int binarySearch(const std::vector<int>& arr, int target) {
                        int low = 0, high = static_cast<int>(arr.size()) - 1;
                        while (low <= high) {
                            int mid = low + (high - low) / 2;
                            if (arr[mid] == target) return mid;
                            if (arr[mid] < target) low = mid + 1;
                            else high = mid - 1;
                        }
                        return -1;
                    }

                    int main() {
                        std::vector<int> nums = {1, 3, 4, 5, 8, 9, 12};
                        std::cout << binarySearch(nums, 5) << std::endl;
                        std::cout << binarySearch(nums, 7) << std::endl;
                    }
                    """),
                new("Go", """
                    package main

                    import "fmt"

                    func binarySearch(arr []int, target int) int {
                        low, high := 0, len(arr)-1
                        for low <= high {
                            mid := low + (high-low)/2
                            if arr[mid] == target {
                                return mid
                            } else if arr[mid] < target {
                                low = mid + 1
                            } else {
                                high = mid - 1
                            }
                        }
                        return -1
                    }

                    func main() {
                        nums := []int{1, 3, 4, 5, 8, 9, 12}
                        fmt.Println(binarySearch(nums, 5))
                        fmt.Println(binarySearch(nums, 7))
                    }
                    """),
                new("Rust", """
                    fn binary_search(arr: &[i32], target: i32) -> Option<usize> {
                        let mut low = 0i32;
                        let mut high = arr.len() as i32 - 1;
                        while low <= high {
                            let mid = low + (high - low) / 2;
                            if arr[mid as usize] == target {
                                return Some(mid as usize);
                            } else if arr[mid as usize] < target {
                                low = mid + 1;
                            } else {
                                high = mid - 1;
                            }
                        }
                        None
                    }

                    fn main() {
                        let nums = vec![1, 3, 4, 5, 8, 9, 12];
                        println!("{:?}", binary_search(&nums, 5));
                        println!("{:?}", binary_search(&nums, 7));
                    }
                    """),
            }),
    };
}
