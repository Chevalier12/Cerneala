const comparisonRows = [
  ['Runtime', '`Application` + STA Dispatcher', '`GeneratedWindowApplication` / host-owned startup', 'different', 'Startup is host-specific. Cerneala does not reproduce WPF Application and Dispatcher semantics.'],
  ['Runtime', '`DispatcherObject` thread affinity', 'No general public DispatcherObject analogue', 'absent', 'Do not assume VerifyAccess, priority dispatch, or WPF cross-thread marshaling contracts.'],
  ['Runtime', 'Priority Dispatcher queues', '`UiFrameScheduler` phase queues', 'different', 'Cerneala snapshots deterministic retained phases during Update instead of pumping arbitrary UI work by DispatcherPriority.'],
  ['Runtime', 'Implicit desktop message/render lifecycle', '`UiHost.Update` + `UiHost.Draw`', 'native', 'The host advances input and retained work explicitly, then submits cached drawing commands.'],
  ['Runtime', 'Background WPF rendering thread', 'Backend submission from the host draw path', 'different', 'There is no claim of a WPF-style hidden compositor thread.'],
  ['Runtime', 'Layout work scheduled by WPF', 'Invalidation flags + ordered queue snapshots', 'different', 'Same-phase enqueue is deferred; downstream phases may still run in the current frame.'],
  ['Runtime', 'Dispatcher idle behavior', 'O(1) queue `HasWork` after warmup', 'native', 'Queue Engine uses direct counts and a shared visual-order index rather than probing work through snapshots.'],
  ['Runtime', 'WPF device-independent units', '`UiViewport` + `UiCoordinateMapper` + DPI services', 'different', 'Logical-to-physical mapping is explicit at host/backend boundaries; exact WPF DPI behavior is not promised.'],

  ['Authoring', '`.xaml`', '`.cui.xml`', 'different', 'The file is XML-shaped Cerneala markup, not WPF XAML.'],
  ['Authoring', 'BAML / WPF XAML build tasks', 'Roslyn incremental source generator', 'native', 'Markup is an AdditionalFile and generates typed partial C# at compile time.'],
  ['Authoring', '`.xaml.cs` code-behind', '`.cui.xml.cs` partial class', 'close', 'The paired-file mental model transfers, but generated members and grammar are Cerneala-specific.'],
  ['Authoring', '`x:Name` + namescopes', '`Name` + generated typed members / template parts', 'different', 'Names are resolved by the generator and template-part instances; WPF namescope parity is not claimed.'],
  ['Authoring', 'WPF markup extensions', 'Documented directives and resource references', 'partial', 'There is no general `{Extension ...}` ecosystem equivalent in the preview contract.'],
  ['Authoring', '`XamlReader` / loose runtime XAML', 'No general runtime XAML interpretation', 'absent', 'Cerneala deliberately supports a compile-time authoring path instead.'],
  ['Authoring', 'XML namespace assembly mapping', 'Roslyn semantic type/property resolution', 'different', 'The generator resolves the project type system rather than implementing WPF namespace behavior wholesale.'],
  ['Authoring', 'Visual Studio / Blend XAML designer', 'Playground and runtime diagnostics', 'absent', 'There is no WPF designer compatibility promise.'],

  ['Properties', '`DependencyObject`', '`UiObject`', 'close', 'Both provide retained property storage; ownership and expression systems are not identical.'],
  ['Properties', '`DependencyProperty`', '`UiProperty<T>`', 'close', 'Cerneala makes the property value type part of the API.'],
  ['Properties', '`DependencyPropertyKey`', '`UiPropertyKey<T>`', 'close', 'Read-only registered properties have a familiar key-based pattern.'],
  ['Properties', '`PropertyMetadata` / `FrameworkPropertyMetadata`', '`UiPropertyMetadata<T>`', 'close', 'Defaults, equality, validation/coercion hooks, inheritance, and invalidation options serve the retained runtime.'],
  ['Properties', 'WPF value precedence', 'Local > animation > Aspect state > Aspect base > inherited > default', 'different', 'Never port code that relies on exact WPF precedence without revalidating it.'],
  ['Properties', 'Attached dependency properties', 'Owner-specific static placement APIs such as `Grid.SetRow`', 'partial', 'Attached-looking APIs exist without promising the full WPF attached-property expression model.'],
  ['Properties', '`Freezable`', 'No direct framework-wide analogue', 'absent', 'Freeze, clone, inheritance context, and Freezable animation behavior do not transfer.'],
  ['Properties', '`SetCurrentValue` / expression preservation', 'Explicit value sources and mutations', 'different', 'WPF expression-preservation behavior must not be assumed.'],

  ['Data', '`Binding` + string `PropertyPath`', '`Binding<T>` + `BindingOperations`', 'different', 'Typed bindings are the supported hot path.'],
  ['Data', '`INotifyPropertyChanged`', '`ObservableValue<T>` / adapters', 'different', 'Explicit typed observables and adapters make subscription lifetime and value type visible.'],
  ['Data', '`INotifyCollectionChanged`', '`ObservableList<T>` / `IObservableList<T>`', 'close', 'Collection changes drive retained item paths, with a smaller explicit contract.'],
  ['Data', '`IValueConverter`', '`IValueConverter<TIn,TOut>`', 'close', 'The Cerneala converter boundary is generic.'],
  ['Data', '`MultiBinding` / `PriorityBinding`', 'No documented equivalent', 'absent', 'Compose typed observables or explicit adapters instead.'],
  ['Data', '`CollectionViewSource` / default views', '`CollectionView<T>` + typed sort/filter', 'partial', 'Do not assume WPF currency, grouping, live shaping, or default-view behavior.'],
  ['Data', 'Inherited `DataContext`', 'Retained `DataContext` + typed binding sources', 'close', 'The familiar ownership concept exists, but binding construction and source rules differ.'],

  ['Styling', '`Style` + `Setter`', '`AspectPackage` + typed declarations', 'different', 'Aspects are a typed design-system engine, not a renamed WPF Style object.'],
  ['Styling', 'Static/Dynamic resources', '`ResourceDictionary`, resource providers, typed Aspect tokens', 'different', 'Lookup, observation, dependency tracking, and precedence use Cerneala contracts.'],
  ['Styling', 'Property/Data/Multi triggers', 'Aspect conditions + generated `@when` / `@if`', 'different', 'Reactive branches are compiled and tied to typed property/part observation.'],
  ['Styling', '`ControlTemplate`', '`ComponentTemplate<TControl>`', 'close', 'Both replace control chrome, but slots, ownership, projection, precedence, and lifetime differ.'],
  ['Styling', '`DataTemplate`', '`ContentTemplate<TData>`', 'close', 'Cerneala resolves templates through an explicit registry, keys, predicates, type distance, priority, and registration order.'],
  ['Styling', '`TemplateBinding`', '`TemplateBinding<T>` / owner bindings', 'close', 'The current generated template path supports owner and token bindings with narrower grammar.'],
  ['Styling', 'Template parts by string convention', '`TemplatePartMap` + typed Aspect slots', 'different', 'Generated parts are instance-isolated and slots give Aspect rules explicit targets.'],
  ['Styling', '`VisualStateManager`', 'Aspect states + `MotionVisualStateController`', 'different', 'Visual states and motion are coordinated through Cerneala state and motion systems.'],
  ['Styling', 'Implicit style lookup', 'Aspect target/cascade resolution', 'different', 'Resolution considers package origin, layer, specificity, variants, states, and typed dependencies.'],

  ['Layout', 'Logical tree', 'Retained logical ownership', 'close', 'Content, resources, commands, focus, and semantics follow retained ownership.'],
  ['Layout', 'Visual tree', 'Retained visual ownership', 'close', 'Layout, rendering, hit testing, clipping, and visual ordering use the visual tree.'],
  ['Layout', '`MeasureOverride` / `ArrangeOverride`', '`MeasureCore` / `ArrangeCore`', 'close', 'Natural-size and final-rect reasoning transfers.'],
  ['Layout', 'WPF layout invalidation manager', '`LayoutQueue` + frame scheduler', 'different', 'Measure and arrange are distinct queues with stable phase snapshots and explicit promotion rules.'],
  ['Layout', '`Grid`', '`Grid` with rows, columns, Auto/Pixel/Star and spans', 'close', 'The core placement model is familiar; edge cases and exact sizing parity still require tests.'],
  ['Layout', '`StackPanel` / `Canvas`', '`StackPanel` / `Canvas`', 'close', 'Familiar names and basic responsibilities, not a blanket parity guarantee.'],
  ['Layout', '`VirtualizingStackPanel`', '`VirtualizingStackPanel` + realization context', 'partial', 'Retained list virtualization exists, but WPF virtualization modes and container behavior are not promised wholesale.'],
  ['Layout', '`DockPanel`, `WrapPanel`, `UniformGrid`', 'No documented controls', 'absent', 'Build the required panel or restructure the layout.'],

  ['Rendering', 'WPF retained vector compositor', 'Retained element command caches', 'different', 'Cerneala caches backend-neutral drawing intent and submits it through a host.'],
  ['Rendering', '`DrawingContext` immediate drawing API', '`DrawingContext` command recorder', 'different', 'Cerneala DrawingContext appends to DrawCommandList; it does not directly paint.'],
  ['Rendering', '`Visual` composition tree', '`UIElement` render caches + root command list', 'different', 'The retained UI tree owns cache boundaries rather than reproducing WPF Visual internals.'],
  ['Rendering', 'DirectX compositor', '`IDrawingBackend` abstraction', 'native', 'The public drawing core is backend-neutral.'],
  ['Rendering', 'Vector geometry and shapes', 'Rectangle, ellipse, line/path commands and Media geometry types', 'partial', 'Useful 2D coverage exists; WPF geometry/composition breadth is much larger.'],
  ['Rendering', 'WPF Brush hierarchy', 'Solid, gradient, image, tile, drawing and visual brush descriptors', 'partial', 'Names are familiar, but backend coverage and exact mapping must be verified per feature.'],
  ['Rendering', 'WPF text stack', 'HarfBuzz shaping + Skia rasterization + retained text services', 'different', 'Text is prepared explicitly and cached for backend drawing.'],
  ['Rendering', '3D, documents and media composition', 'No supported parity surface', 'absent', 'Cerneala currently focuses on retained 2D UI.'],

  ['Input', 'WPF raw input pipeline', 'Host snapshots -> `InputFrame`', 'different', 'Pointer, keyboard, and text changes enter through explicit frame payloads.'],
  ['Input', '`RoutedEvent`', '`RoutedEvent`', 'close', 'Canonical identity, owners, direct/bubble/tunnel routing, handled state, and CLR wrappers are familiar.'],
  ['Input', '`UIElement.AddHandler` / `RemoveHandler`', 'Same familiar handler APIs', 'close', 'Handled-events-too behavior is implemented, but event catalog breadth varies.'],
  ['Input', 'Preview and bubbling input events', 'Tunnel and bubble event pairs', 'close', 'Mouse, keyboard, focus, and text event names follow WPF-like conventions.'],
  ['Input', 'Mouse capture and hit testing', '`PointerCaptureManager` + retained hit routes', 'close', 'Routes are refreshed from current visual order and arranged/render bounds.'],
  ['Input', 'Keyboard focus and tab navigation', '`FocusManager` + `KeyboardNavigationController`', 'close', 'Retained focus state and activation exist; exhaustive WPF parity is not implied.'],
  ['Input', 'Stylus/touch/manipulation/drag-drop breadth', 'Bridges, controllers and routed surfaces', 'partial', 'Public surface exists, while advanced behavior remains an evolving area.'],
  ['Input', 'Full IME and rich text editing', 'Text composition MVP and single-line editing focus', 'partial', 'Full IME, multiline rich text, and document editing are deferred.'],

  ['Motion', 'AnimationTimeline / AnimationClock', '`MotionSpec<T>` + motion nodes', 'different', 'Cerneala samples typed specs inside a root-owned motion graph.'],
  ['Motion', '`Storyboard`', 'No Storyboard compatibility', 'absent', 'Do not attempt to carry Storyboard XAML across unchanged.'],
  ['Motion', 'Double/Color/etc. animation classes', 'Value mixer registry', 'different', 'Typed mixers interpolate floats, doubles, colors, brushes, transforms, draw points/sizes/rectangles, and thickness.'],
  ['Motion', 'Property animation precedence', 'Motion property store + channels/priorities', 'different', 'Conflicts, retargeting, clearing, and handoff use Cerneala rules.'],
  ['Motion', 'Layout animation by property mutation', 'FLIP layout correction', 'native', 'Normal layout computes final bounds; render correction carries visual continuity back to identity.'],
  ['Motion', 'Enter/exit storyboard conventions', 'Presence coordinator and render sidecar', 'native', 'Exit can leave public layout immediately while remaining renderable and non-interactive until completion.'],
  ['Motion', 'System animation settings', 'Root-owned reduced-motion policy', 'close', 'The intent transfers through explicit Cerneala policy and sources.'],

  ['Controls', '`Button`, toggles, ranges', 'Button, ToggleButton, CheckBox, RadioButton, Slider, ProgressBar, ScrollBar', 'close', 'The standard retained primitive set is present with templated/aspected visuals.'],
  ['Controls', '`ItemsControl`, `ListBox`', 'ItemsControl, ListBox, generator/recycle pool', 'close', 'The retained list path includes observation, generation, recycling, scrolling, and selection.'],
  ['Controls', '`ComboBox`', 'ComboBox without promised WPF popup lifecycle', 'partial', 'Do not assume DropDownOpened/Closed behavior or full popup interaction.'],
  ['Controls', '`TextBlock`, `TextBox`, `PasswordBox`', 'Matching core text controls', 'partial', 'Core text/display/editing exists; full WPF editing, IME, rich text, and command breadth do not.'],
  ['Controls', '`TabControl`', 'TabControl + TabItem', 'close', 'Selection and item-container instincts transfer with narrower overall control-suite context.'],
  ['Controls', '`ToolTip`', 'ToolTip with Opened/Closed', 'partial', 'A current control exists; WPF popup placement and service parity are not blanket claims.'],
  ['Controls', '`InkCanvas`', 'InkCanvas + stroke collection/events', 'partial', 'No WPF erasing, gesture recognition, or stroke-selection parity.'],
  ['Controls', '`DataGrid`, `TreeView`, menus, calendar', 'No documented equivalents', 'absent', 'These major WPF families require application-specific alternatives or future framework work.'],
  ['Controls', '`RichTextBox`, documents, navigation', 'No supported equivalents', 'absent', 'Flow documents, navigation journals, document viewers, and rich editing are outside preview scope.'],
  ['Controls', '`MediaElement`, 3D viewport, printing', 'No supported equivalents', 'absent', 'The current scope is retained 2D application/game UI.'],

  ['Platform', 'Windows-only WPF', 'Current `net8.0-windows` snapshot', 'close', 'Both are Windows-targeted today, though Cerneala keeps backend and platform seams explicit.'],
  ['Platform', 'WPF Window/Application hosting', 'Native Win32/WindowsDX runtime', 'different', 'Cerneala owns its window runtime rather than hosting WPF.'],
  ['Platform', 'Game-loop embedding is external interop', '`MonoGameUiHost`', 'native', 'A first-class host maps input, content services, viewport, update, and backend drawing.'],
  ['Platform', 'Clipboard/cursor/dialog services via WPF', '`IPlatformServices` family', 'different', 'Platform capabilities are explicit injectable seams.'],
  ['Platform', 'UI Automation peers + native integration', 'Semantics tree + automation peers', 'partial', 'Platform-neutral semantics exist; native accessibility adapter completion is deferred.'],
  ['Platform', 'Live Visual Tree / designer diagnostics', 'Frame/tree/cache/Aspect/motion diagnostics', 'different', 'Cerneala diagnostics are runtime/frame-oriented rather than Visual Studio WPF tooling.'],
  ['Platform', 'Mature framework packaging and servicing', 'Single evolving project plus source generator', 'partial', 'Package split and compatibility policy are still deferred.'],
  ['Platform', 'WPF localization infrastructure', 'No documented equivalent breadth', 'absent', 'Plan localization explicitly instead of assuming XAML localization tooling.']
];

const statusLabels = {
  close: 'Close analogue',
  different: 'Different contract',
  native: 'Cerneala-native',
  partial: 'Partial / evolving',
  absent: 'Absent / deferred'
};

const matrixBody = document.getElementById('matrix-body');
const matrixSearch = document.getElementById('matrix-search');
const matrixCount = document.getElementById('matrix-count');
const matrixEmpty = document.getElementById('matrix-empty');
const filterButtons = Array.from(document.querySelectorAll('#matrix-filters button'));
const main = document.querySelector('main');
const progress = document.getElementById('reading-progress');
const rail = document.getElementById('chapter-rail');
const railToggle = document.getElementById('rail-toggle');
const railScrim = document.getElementById('rail-scrim');
const chapterLinks = Array.from(document.querySelectorAll('.chapter-rail nav a'));
let activeStatus = 'all';

function escapeHtml(value) {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

function formatCell(value) {
  return escapeHtml(value).replace(/`([^`]+)`/g, '<code>$1</code>');
}

function renderMatrix() {
  const query = matrixSearch.value.trim().toLowerCase();
  const visibleRows = comparisonRows.filter(row => {
    const statusMatches = activeStatus === 'all' || row[3] === activeStatus;
    const queryMatches = !query || row.join(' ').toLowerCase().includes(query);
    return statusMatches && queryMatches;
  });

  matrixBody.innerHTML = visibleRows.map(row => (
    '<tr>' +
      '<td>' + formatCell(row[0]) + '</td>' +
      '<td>' + formatCell(row[1]) + '</td>' +
      '<td>' + formatCell(row[2]) + '</td>' +
      '<td><span class="status status--' + row[3] + '">' + statusLabels[row[3]] + '</span></td>' +
      '<td>' + formatCell(row[4]) + '</td>' +
    '</tr>'
  )).join('');

  matrixCount.textContent = visibleRows.length + ' of ' + comparisonRows.length + ' entries';
  matrixEmpty.hidden = visibleRows.length !== 0;
}

function setRailOpen(isOpen) {
  document.body.classList.toggle('is-rail-open', isOpen);
  railToggle.setAttribute('aria-expanded', String(isOpen));
}

function updateReadingProgress() {
  const available = main.scrollHeight - main.clientHeight;
  const ratio = available > 0 ? main.scrollTop / available : 0;
  progress.style.width = Math.max(0, Math.min(1, ratio)) * 100 + '%';
}

function updateActiveChapter() {
  const sections = Array.from(document.querySelectorAll('main > section'));
  let current = sections[0];
  for (const section of sections) {
    if (section.offsetTop <= main.scrollTop + 130) current = section;
  }
  chapterLinks.forEach(link => {
    link.classList.toggle('is-active', link.getAttribute('href') === '#' + current.id);
  });
}

function categoryFromSource(source) {
  if (!source) return 'Root';
  const normalized = source.replace(/\\/g, '/');
  const slash = normalized.lastIndexOf('/');
  if (slash === -1) return 'Root';
  const folder = normalized.slice(0, slash);
  return folder === 'Cerneala.SourceGen' ? 'SourceGen' : folder.split('/').join('.');
}

async function loadSnapshotMetrics() {
  try {
    const response = await fetch('documentation/manifest.json');
    if (!response.ok) return;
    const docs = await response.json();
    document.getElementById('api-count').textContent = docs.length.toString();
    document.getElementById('namespace-count').textContent = new Set(docs.map(doc => categoryFromSource(doc.source))).size.toString();
  } catch {
    // The dated fallback values remain visible when the page is opened without a server.
  }
}

filterButtons.forEach(button => {
  button.addEventListener('click', () => {
    activeStatus = button.dataset.status;
    filterButtons.forEach(item => item.classList.toggle('is-active', item === button));
    renderMatrix();
  });
});

matrixSearch.addEventListener('input', renderMatrix);
railToggle.addEventListener('click', () => setRailOpen(true));
railScrim.addEventListener('click', () => setRailOpen(false));
chapterLinks.forEach(link => {
  link.addEventListener('click', () => {
    if (window.matchMedia('(max-width: 900px)').matches) setRailOpen(false);
  });
});
main.addEventListener('scroll', () => {
  updateReadingProgress();
  updateActiveChapter();
}, { passive: true });

document.addEventListener('keydown', event => {
  const isTyping = event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement;
  if (event.key === '/' && !isTyping) {
    event.preventDefault();
    matrixSearch.focus();
    matrixSearch.select();
  }
  if (event.key === 'Escape') {
    if (document.body.classList.contains('is-rail-open')) {
      setRailOpen(false);
    } else if (document.activeElement === matrixSearch && matrixSearch.value) {
      matrixSearch.value = '';
      renderMatrix();
    }
  }
});

renderMatrix();
updateReadingProgress();
updateActiveChapter();
loadSnapshotMetrics();
