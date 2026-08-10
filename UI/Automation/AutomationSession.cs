using System.Xml.Linq;
using System.Xml.XPath;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.UI.Automation;

public sealed class AutomationSession
{
    private readonly UIElement root;
    private readonly Action<string>? screenshot;

    public AutomationSession(
        UIElement root,
        IAutomationInputDriver input,
        Action<string>? screenshot = null)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
        Input = input ?? throw new ArgumentNullException(nameof(input));
        this.screenshot = screenshot;
    }

    public IAutomationInputDriver Input { get; }

    public AutomationElement FindByAutomationId(string automationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        return RequireSingle(
            DescendantsAndSelf(root).Where(element =>
                string.Equals(
                    AutomationProperties.GetAutomationId(element),
                    automationId,
                    StringComparison.Ordinal)),
            $"AutomationId '{automationId}'");
    }

    public AutomationElement FindByXPath(string xpath)
    {
        IReadOnlyList<AutomationElement> matches = FindAllByXPath(xpath);
        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException($"XPath '{xpath}' did not match any UI element."),
            _ => throw new InvalidOperationException($"XPath '{xpath}' matched {matches.Count} UI elements; expected exactly one.")
        };
    }

    public IReadOnlyList<AutomationElement> FindAllByXPath(string xpath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xpath);
        XElement tree = BuildXPathTree();
        return tree
            .XPathSelectElements(xpath)
            .Select(node => node.Annotation<UIElement>())
            .Where(element => element is not null)
            .Select(element => new AutomationElement(this, element!))
            .ToArray();
    }

    public void PressKey(InputKey key, AutomationModifiers modifiers = AutomationModifiers.None)
    {
        Input.PressKey(key, modifiers);
    }

    public void SendText(string text)
    {
        Input.SendText(text);
    }

    public void SaveScreenshot(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (screenshot is null)
        {
            throw new NotSupportedException("This automation session has no screenshot provider.");
        }

        screenshot(path);
    }

    private AutomationElement RequireSingle(IEnumerable<UIElement> matches, string selector)
    {
        UIElement[] materialized = matches.Take(2).ToArray();
        return materialized.Length switch
        {
            1 => new AutomationElement(this, materialized[0]),
            0 => throw new InvalidOperationException($"{selector} did not match any UI element."),
            _ => throw new InvalidOperationException($"{selector} matched multiple UI elements; identifiers must be unique within a session.")
        };
    }

    private XElement BuildXPathTree()
    {
        XElement tree = new("AutomationTree");
        tree.Add(BuildXPathNode(root));
        return tree;
    }

    private static XElement BuildXPathNode(UIElement element)
    {
        XElement node = new(XmlTypeName(element.GetType()),
            new XAttribute("Type", element.GetType().FullName ?? element.GetType().Name),
            new XAttribute("IsEnabled", element.IsEnabled),
            new XAttribute("Visibility", element.Visibility));
        node.AddAnnotation(element);

        string? automationId = AutomationProperties.GetAutomationId(element);
        if (automationId is not null)
        {
            node.Add(new XAttribute("AutomationId", automationId));
        }

        string? name = AccessibleName.GetName(element);
        if (!string.IsNullOrWhiteSpace(name))
        {
            node.Add(new XAttribute("Name", name));
        }

        foreach (UIElement child in element.VisualChildren)
        {
            node.Add(BuildXPathNode(child));
        }

        return node;
    }

    private static IEnumerable<UIElement> DescendantsAndSelf(UIElement element)
    {
        yield return element;
        foreach (UIElement child in element.VisualChildren)
        {
            foreach (UIElement descendant in DescendantsAndSelf(child))
            {
                yield return descendant;
            }
        }
    }

    private static string XmlTypeName(Type type)
    {
        string name = type.Name;
        int genericMarker = name.IndexOf('`');
        return genericMarker >= 0 ? name[..genericMarker] : name;
    }
}
