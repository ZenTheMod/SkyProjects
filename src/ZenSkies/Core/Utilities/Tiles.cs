using System;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace ZenSkies.Core;

public static partial class Utilities
{
    public static bool ShowInvisibleTiles => Main.instance.TilesRenderer._shouldShowInvisibleBlocks;

    public static bool IgnoresDrawBlack(int i, int j)
    {
        Tile center = Main.tile[i, j];

        if (!center.BlocksLight)
        {
            return true;
        }

        Tile[] neighbors = [
            Main.tile[Math.Min(i + 1, Main.tile.Width), j],
            Main.tile[Math.Max(i - 1, 0), j],
            Main.tile[i, Math.Min(j + 1, Main.tile.Height)],
            Main.tile[i, Math.Max(j - 1, 0)]
        ];

        return neighbors.Any(t => !t.BlocksLight);
    }

    extension(Tile tile)
    {
        public bool HasSolidTile =>
            tile.HasTile &&
            Main.tileBlockLight[tile.type] &&
            Main.tileSolid[tile.type] &&
            tile.IsFullBlock &&
            (!tile.IsTileInvisible || ShowInvisibleTiles);

        /// <summary>
        /// Whether there is a wall at this position.<br/>
        /// Shorthand for <c>tile.WallType != WallID.None</c>.
        /// </summary>
        public bool HasWall => tile.WallType != WallID.None;

        public bool HasSolidWall =>
            tile.HasWall && !Main.wallLight[tile.WallType] && !WallID.Sets.Transparent[tile.WallType] &&
            (!tile.IsWallInvisible || ShowInvisibleTiles);

        public bool IsAir => !tile.HasTile && !tile.HasWall;

        public bool IsFullBlock => tile.BlockType == BlockType.Solid;

        /// <summary>
        /// Whether the tile/wall at this position ACTUALLY blocks light.
        /// </summary>
        public bool BlocksLight =>
            !tile.IsAir &&
            (tile.HasSolidTile || tile.HasSolidWall);
    }
}
