using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DataFormat;

[Tool("json-viz", "JSON Tree Viewer", ToolCategory.DataFormat,
    "Explore a JSON document as a lazily-expanding, searchable tree.",
    Keywords = "json,tree,viewer,visualize,explore,crack,inspect,path", Order = 30)]
public partial class JsonTreeView : UserControl
{
    private const long AutoLoadMaxBytes = 2 * 1024 * 1024;

    private readonly TextEditor _input;
    private readonly DispatcherTimer _loadDebounce;
    private readonly DispatcherTimer _searchDebounce;

    private JsonTree? _tree;
    private JsonNodeVM? _rootVm;
    private int _loadRequestId;
    private int _nodesShown;

    public JsonTreeView()
    {
        InitializeComponent();

        _loadDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _loadDebounce.Tick += (_, _) =>
        {
            _loadDebounce.Stop();
            MaybeAutoLoad();
        };

        _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            RunSearch();
        };

        _input = CodeEditors.Create("Json");
        InputHost.Child = _input;
        _input.TextChanged += (_, _) => Debounce(_loadDebounce);

        // Coming back to a cached view whose tree we disposed on Unloaded: rebuild it from the input
        // still sitting in the editor so the TREE tab isn't left showing stale nodes over a disposed
        // document (touching one of those throws ObjectDisposedException deep in WPF's expand path).
        Loaded += (_, _) =>
        {
            if (_tree is null && !string.IsNullOrWhiteSpace(_input.Text))
                _ = LoadAsync();
        };

        // Stop pending work when navigated away, and dispose the current document so a
        // cached-but-hidden view doesn't keep holding onto (or reading from) it. Bumping the request
        // id first invalidates any parse Task.Run still in flight; ResetTreeUi drops the whole VM
        // tree so nothing left on screen can call Children() on a node into the now-disposed document.
        Unloaded += (_, _) =>
        {
            _loadDebounce.Stop();
            _searchDebounce.Stop();
            _loadRequestId++;
            _tree?.Dispose();
            _tree = null;
            ResetTreeUi();
        };
    }

    private static void Debounce(DispatcherTimer timer)
    {
        timer.Stop();
        timer.Start();
    }

    // ---------------- loading ----------------

    private void MaybeAutoLoad()
    {
        if (System.Text.Encoding.UTF8.GetByteCount(_input.Text) <= AutoLoadMaxBytes)
            _ = LoadAsync();
    }

    private void Load_Click(object sender, RoutedEventArgs e) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        var text = _input.Text;
        var requestId = ++_loadRequestId;

        if (string.IsNullOrWhiteSpace(text))
        {
            ApplyLoadResult(requestId, null, null);
            return;
        }

        try
        {
            var tree = await Task.Run(() => JsonTreeTool.Load(text));
            ApplyLoadResult(requestId, tree, null);
        }
        catch (Exception ex)
        {
            ApplyLoadResult(requestId, null, ex.Message);
        }
    }

    private void ApplyLoadResult(int requestId, JsonTree? tree, string? error)
    {
        if (requestId != _loadRequestId)
        {
            tree?.Dispose(); // superseded by a newer edit — don't leak this document
            return;
        }

        _tree?.Dispose(); // dispose the document being replaced now that nothing else can read it
        _tree = tree;

        if (error is not null)
        {
            InputErrorText.Text = error;
            InputErrorText.Visibility = Visibility.Visible;
            ResetTreeUi();
            return;
        }

        InputErrorText.Visibility = Visibility.Collapsed;
        if (tree is null)
        {
            ResetTreeUi();
            return;
        }

        _rootVm = new JsonNodeVM(tree.Root, requestId);
        Tree.ItemsSource = new[] { _rootVm };
        _nodesShown = 1;
        UpdateNodesShownStatus();
        ShowDetail(null);
        RunSearch(); // re-apply any existing search text against the freshly loaded tree
    }

    private void ResetTreeUi()
    {
        _rootVm = null;
        Tree.ItemsSource = null;
        _nodesShown = 0;
        UpdateNodesShownStatus();
        ShowDetail(null);
        SearchStatusText.Text = "";
    }

    // ---------------- lazy expansion ----------------

    private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem { DataContext: JsonNodeVM { ChildrenLoaded: false } vm })
            LoadChildrenInto(vm);
    }

    /// <summary>Materializes <paramref name="vm"/>'s immediate children (via <see cref="JsonTreeNode.Children"/>,
    /// itself lazy one level at a time) and replaces its placeholder — this is the only place a
    /// document actually gets walked beyond the root, and only as far as the user expands.</summary>
    private void LoadChildrenInto(JsonNodeVM vm)
    {
        // Generation guard: only ever walk a node that belongs to the *current* tree. A VM built for
        // an earlier document holds a JsonElement into a JsonDocument we've since disposed, so calling
        // Children() on it throws ObjectDisposedException — which WPF swallows silently inside the
        // expand/measure pass. Reassigning ItemsSource on reload can still deliver an Expanded event
        // to a not-yet-torn-down old container, so this is the line that actually makes reload safe.
        if (vm.IsPlaceholder || vm.Generation != _loadRequestId)
            return;

        vm.ChildrenLoaded = true;
        vm.Children.Clear();
        foreach (var child in vm.Node.Children())
        {
            vm.Children.Add(new JsonNodeVM(child, vm.Generation));
            _nodesShown++;
        }
        UpdateNodesShownStatus();
    }

    private void UpdateNodesShownStatus() =>
        NodesShownText.Text = _nodesShown == 0 ? "" : $"{_nodesShown} node{(_nodesShown == 1 ? "" : "s")} shown";

    // ---------------- search ----------------

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
            Debounce(_searchDebounce);
    }

    private void RunSearch()
    {
        var query = SearchBox.Text;
        if (_tree is null || string.IsNullOrEmpty(query))
        {
            SearchStatusText.Text = "";
            return;
        }

        // One walk, not two. SearchAll returns both the match paths (for the count) and the
        // root-to-first-match chain (to reveal it). This runs synchronously on the UI thread — over a
        // ~60k-node document a full no-match walk measures ~95 ms, acceptable behind the 300 ms
        // debounce, and staying on the UI thread means it can't race the reload path's Dispose the way
        // a background search would. Calling Search + FindFirstMatchChain separately walked it twice.
        var result = JsonTreeTool.SearchAll(_tree, query, max: 500);
        SearchStatusText.Text = result.Paths.Count switch
        {
            0 => "No matches",
            500 => "500+ matches",
            1 => "1 match",
            _ => $"{result.Paths.Count} matches",
        };

        if (result.FirstChain is not null)
            RevealChain(result.FirstChain);
    }

    /// <summary>Expands every ancestor (materializing children as needed) down to the first match
    /// and selects it — a purely data-side walk (mutating the VM tree directly), so it works
    /// regardless of which containers the virtualized TreeView happens to have realized.</summary>
    private void RevealChain(IReadOnlyList<JsonTreeNode> chain)
    {
        if (_rootVm is null || chain.Count == 0)
            return;

        var current = _rootVm;
        for (var i = 1; i < chain.Count; i++)
        {
            if (!current.ChildrenLoaded)
                LoadChildrenInto(current);
            current.IsExpanded = true;

            var next = current.Children.FirstOrDefault(c => !c.IsPlaceholder && c.Node.Path == chain[i].Path);
            if (next is null)
                return; // defensive; shouldn't happen since the chain came from this same tree
            current = next;
        }

        var target = current;
        // Fill the detail row straight from the node's cached strings. The target can sit deep inside a
        // large, virtualized array whose container is never realized, so SelectedItemChanged may never
        // fire for it — populate PATH/POINTER/VALUE directly rather than depending on selection.
        ShowDetail(target.Node);

        // Selecting and scrolling still need a realized container; the IsExpanded changes above only
        // take visual effect on the next layout pass, so defer the lookup. If the target is virtualized
        // away, ContainerFromItem returns null and we simply don't scroll — we deliberately do NOT walk
        // or realize its (up to tens of thousands of) siblings to reach it, so virtualization holds.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (Tree.ItemContainerGenerator.ContainerFromItem(target) is TreeViewItem item)
            {
                item.IsSelected = true;
                item.BringIntoView();
            }
        });
    }

    // ---------------- selection / detail row ----------------

    private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) =>
        ShowDetail(e.NewValue is JsonNodeVM { IsPlaceholder: false } vm ? vm.Node : null);

    private void ShowDetail(JsonTreeNode? node)
    {
        PathBox.Text = node?.Path ?? "";
        PointerBox.Text = node?.Pointer ?? "";
        ValueBox.Text = node?.Preview ?? "";
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e) => Ui.Copy(PathBox.Text, CopyPathBtn);

    private void CopyPointer_Click(object sender, RoutedEventArgs e) => Ui.Copy(PointerBox.Text, CopyPointerBtn);

    private void CopyValue_Click(object sender, RoutedEventArgs e) => Ui.Copy(ValueBox.Text, CopyValueBtn);

    /// <summary>Bindable wrapper around a <see cref="JsonTreeNode"/> for the TreeView. A node with
    /// children starts with a single placeholder child (<see cref="IsPlaceholder"/>) purely so the
    /// expand arrow shows — <see cref="TreeViewItem_Expanded"/> replaces it with the real children
    /// on first expand, which is what keeps a large document from being walked all at once.</summary>
    private sealed class JsonNodeVM : INotifyPropertyChanged
    {
        private bool _isExpanded;

        public JsonNodeVM(JsonTreeNode node, int generation)
        {
            Node = node;
            Generation = generation;
            if (node.ChildCount > 0)
                Children.Add(new JsonNodeVM(generation));
        }

        /// <summary>Placeholder constructor — <see cref="Node"/> must never be read when <see cref="IsPlaceholder"/> is true.
        /// Carries the same generation as its parent so the guard in LoadChildrenInto stays uniform.</summary>
        private JsonNodeVM(int generation)
        {
            Node = null!;
            Generation = generation;
        }

        public JsonTreeNode Node { get; }

        /// <summary>The load-request id of the tree this VM was built for; a VM whose generation no
        /// longer matches the view's current request id is stale and must not be walked (its backing
        /// JsonDocument may be disposed).</summary>
        public int Generation { get; }

        public bool IsPlaceholder => Node is null;

        public bool ChildrenLoaded { get; set; }

        public System.Collections.ObjectModel.ObservableCollection<JsonNodeVM> Children { get; } = [];

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value)
                    return;
                _isExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }

        public string Header => IsPlaceholder ? "…" : Node.Key is null ? Node.Preview : $"{Node.Key}: {Node.Preview}";

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
