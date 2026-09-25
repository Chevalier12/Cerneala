# Stage 1 core RED repair notes

- Removed `CleanupFailureDoesNotHidePrimaryStructuralFailure` from the new simple-collection tests. It required `OnDetached` to run on a newly adopted node after that node's `OnAttached` threw. The selected terminal structural-fault contract requires bookkeeping aligned with actual membership, not rollback-detaching that node. The simple `IEnumerable` collection has no lease-release callback that could supply a separate, genuine cleanup failure. Existing spatial-source release characterization remains untouched; a regression for another internal cleanup owner needs that owner's real resource seam.
- The model-backed terrain test checks active chunk residency and the framework-generated collider's removal, not release of `TileMap2D.FromModel`'s retained backing-model bytes.
- API REDs compile against the pre-cutover baseline. Their behavior bodies are not GREEN evidence until the public cutover allows those bodies to execute.
