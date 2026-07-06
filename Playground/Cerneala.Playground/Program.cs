using System;
using System.Linq;

using var game = new Cerneala.Playground.Game1(args.Contains("--smoke-open", StringComparer.OrdinalIgnoreCase));
game.Run();
