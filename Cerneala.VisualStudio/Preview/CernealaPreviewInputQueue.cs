namespace Cerneala.VisualStudio.Preview;

using System.Collections.Generic;
using Cerneala.Preview;

internal sealed class CernealaPreviewInputQueue
{
    private readonly LinkedList<PreviewRequest> requests = new();

    public int Count => requests.Count;

    public void Enqueue(PreviewRequest request)
    {
        if (request.Kind == PreviewRequestKind.PointerMove &&
            requests.Last?.Value.Kind == PreviewRequestKind.PointerMove)
        {
            requests.Last.Value = request;
            return;
        }

        requests.AddLast(request);
    }

    public bool TryDequeue(out PreviewRequest? request)
    {
        LinkedListNode<PreviewRequest>? first = requests.First;
        if (first == null)
        {
            request = null;
            return false;
        }

        request = first.Value;
        requests.RemoveFirst();
        return true;
    }

    public void Clear() => requests.Clear();
}
