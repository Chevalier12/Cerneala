-- Builds a deterministic 32x32, 6-frame sprite for Cerneala's Aseprite import tests.
-- Usage: aseprite -b --script-param out=<path.aseprite> --script-param repeat=<n> --script make-hero.lua
local out = app.params["out"]
local repeatCount = tonumber(app.params["repeat"] or "0")
local sprite = Sprite(32, 32, ColorMode.RGB)
local layer = sprite.layers[1]
for i = 2, 6 do sprite:newEmptyFrame(i) end

-- Each frame gets one opaque block at a different place and size, so a
-- trimmed export has a different rectangle and offset for every frame.
local blocks = {
  { 8, 4, 10, 12 }, { 2, 2, 28, 28 }, { 0, 0, 32, 32 },
  { 16, 20, 6, 4 }, { 4, 10, 12, 8 }, { 20, 1, 9, 30 },
}
for i = 1, 6 do
  local image = Image(32, 32, ColorMode.RGB)
  local b = blocks[i]
  local color = app.pixelColor.rgba(40 * i, 255 - 30 * i, 100, 255)
  for y = b[2], b[2] + b[4] - 1 do
    for x = b[1], b[1] + b[3] - 1 do image:drawPixel(x, y, color) end
  end
  sprite:newCel(layer, i, image, Point(0, 0))
  sprite.frames[i].duration = (i == 3 or i == 4) and 0.15 or 0.1
end

-- Aseprite frames are 1-based in Lua and 0-based in the JSON export.
local function tag(from, to, name, dir, repeats)
  local t = sprite:newTag(from, to)
  t.name = name
  t.aniDir = dir
  if repeats then t.repeats = repeats end
end
tag(3, 4, "WalkUp", AniDir.FORWARD)
tag(1, 2, "Spin", AniDir.REVERSE)
tag(1, 4, "Bob", AniDir.PING_PONG)
tag(1, 3, "BobBack", AniDir.PING_PONG_REVERSE)
tag(2, 3, "Once", AniDir.FORWARD, 1)
if repeatCount > 1 then tag(1, 2, "Thrice", AniDir.FORWARD, repeatCount) end
sprite:saveAs(out)
