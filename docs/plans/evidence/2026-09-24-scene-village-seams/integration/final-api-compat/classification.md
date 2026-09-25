# Strict current-Core API comparison

`command.txt`, `output.log`, and `exit.txt` preserve the exact strict comparison without suppression. The immutable pre-sampling, post-Scene2D `Cerneala.dll` baseline is SHA-256 `EC8BEE87C67F0558E85B4D637903AA087C980931EDDE9DD4922B58498774D87B`. The current Release candidate after the final full solution build is SHA-256 `99BB2CA9FF02B6FBC6EC99872D0BD093672242FD5FC60F71B6EB51D09DC8AA1E`; the candidate hash did not change across the PreviewCompiler-only repair or final full build.

The tool exited **1** and emitted exactly three `CP0002` diagnostics. Each is an intentional additive part of the user-approved per-instance `Sprite2D.Sampling` API:

| Exact reported member | Disposition |
| --- | --- |
| `Cerneala.UI.Core.UiProperty<Cerneala.Drawing.DrawSamplingMode> Cerneala.UI.Controls.Sprite2D.SamplingProperty` | Approved new property key; no preexisting member removed. |
| `Cerneala.Drawing.DrawSamplingMode Cerneala.UI.Controls.Sprite2D.Sampling.get` | Approved new instance property getter. |
| `void Cerneala.UI.Controls.Sprite2D.Sampling.set` | Approved new instance property setter. |

No other `CP` diagnostic appeared. The private SDL image-domain implementation and the internal PreviewCompiler repair introduced no further reported public/protected Core difference. The nonzero strict tool exit is not represented as a passing tool invocation; this exact-member classification is the review of the approved additive difference. No blanket namespace allowlist or suppression file was used.
