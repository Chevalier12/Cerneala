# License Screening

Use this as an engineering screen, not legal advice. Licensing matters when the
market report may lead to copying, adapting, linking, or redistributing an
external implementation. A paper or standard can still be relevant evidence
when no source adoption is proposed.

## Core Rule

The target project's license does not grant rights to external source code. The
external work's license controls whether that work may be copied, modified, or
redistributed, while both licenses determine whether the combined result is
compatible.

Never treat a missing or unknown license as permission. Unlicensed source is a
hard rejection for source adoption. It may be used only as factual evidence when
access itself is legitimate and no protected source is copied or closely
adapted.

## Usually Straightforward References

Permissive licenses such as the following are commonly viable candidates for
adoption, subject to verification at the selected release or commit:

- MIT
- BSD-2-Clause
- BSD-3-Clause
- Apache-2.0
- ISC
- Zlib
- BSL-1.0

Verify the license in the upstream repository at the pinned revision. Record and
preserve every required notice, attribution, patent, trademark, and
redistribution obligation. A project's policy may still rule out an otherwise
compatible dependency or attribution requirement.

## Requires Explicit Review

Treat these as conditional rather than automatically compatible:

- MPL-2.0 and LGPL variants;
- GPL or AGPL variants;
- dual-licensed repositories;
- custom, source-available, research-only, non-commercial, or field-of-use
  licenses;
- licenses with unclear patent, trademark, attribution, network-use, data, or
  model-weight obligations;
- standards or papers describing potentially patented methods.

They may remain useful for behavioral and mathematical comparison, but do not
recommend source adoption without an explicit compatibility review for the
target's distribution and deployment model.

## Reject for Source Adoption

Reject the implementation when the only useful source is:

- proprietary, decompiled, leaked, or obtained without legitimate access;
- license-unknown or missing a license grant;
- copied into a mirror or gist without trustworthy provenance;
- incompatible with a mandatory target-project policy or distribution model.

An original paper, standard, or mathematical description may support an
independent clean implementation even when a particular codebase is restricted.
Recommend that route only when specification rights and known patent risks are
reasonably clear, and label the conclusion as engineering inference.

## Required Evidence for Each Implementation Candidate

Capture:

1. Project and repository owner.
2. Stable source URL to the exact file and function.
3. Release tag or commit hash.
4. SPDX license identifier, or the exact custom-license name.
5. Stable license URL at the selected revision.
6. Notice, attribution, patent, trademark, source-sharing, network-use, data,
   model, or redistribution obligations relevant to the target.
7. Compatibility verdict and any issue requiring legal review.
