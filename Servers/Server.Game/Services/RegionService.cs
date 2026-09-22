using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Packets.Server.Game.Structures;
using Server.Game.Models.Settings;

namespace Server.Game.Services
{
    /// <summary>
    ///     Which territory of the world a point belongs to. The original paints the territories
    ///     into the region layer of every map - a bitmap where the colour of a pixel names the
    ///     territory - and looks the colour up in a table of sixteen. This does the same.
    ///
    ///     One pixel is one cell of the region grid, and the file of a map is named after the
    ///     sector the map begins at. Nothing is held in memory but the headers: a respawn happens
    ///     rarely enough to read its three bytes off the disk when it happens, and the whole layer
    ///     of the mainland alone is a hundred and fifty megabytes
    /// </summary>
    public class RegionService
    {
        /// <summary>
        ///     How many units of the world one sector is, gCltSectorSz of the original
        /// </summary>
        private const float SectorSize = 31507.685546875f;

        /// <summary>
        ///     How many cells of the region grid a sector is cut into
        /// </summary>
        private const int CellsPerSector = 512;

        /// <summary>
        ///     How many units of the world one cell is, and so one pixel of the layer
        /// </summary>
        private const float CellSize = SectorSize / CellsPerSector;

        /// <summary>
        ///     Bytes of the BITMAPFILEHEADER and the BITMAPINFOHEADER together
        /// </summary>
        private const int BitmapHeaderSize = 54;

        private readonly ILogger<RegionService> _logger;

        /// <summary>
        ///     Layers of the maps, the one a point falls into is found by its rectangle
        /// </summary>
        private readonly List<RegionLayer> _layers = new List<RegionLayer>();

        /// <summary>
        ///     Territory by its colour, the table the original keeps in the code
        /// </summary>
        private readonly Dictionary<int, int> _territoryByColour = new Dictionary<int, int>();

        /// <summary>
        ///     Creates a new instance and reads the headers of the layers once
        /// </summary>
        public RegionService(IOptions<GameSetting> gameSetting, ILogger<RegionService> logger)
        {
            _logger = logger;

            GameSetting setting = gameSetting.Value;

            foreach (TerritorySetting territory in setting.Territories)
            {
                if (TryParseColour(territory.Rgb, out int colour))
                {
                    _territoryByColour[colour] = territory.No;
                }
            }

            Load(setting.RegionDataDirectory);
        }

        /// <summary>
        ///     Whether the layers were read and the territory of a point can be told at all
        /// </summary>
        public bool IsReady => _layers.Count != 0 && _territoryByColour.Count != 0;

        /// <summary>
        ///     Territory of a point, ETerritory of the original
        /// </summary>
        /// <param name="position">Point of the world</param>
        /// <returns>
        ///     Number of the territory, zero when the point is on no layer or its colour is not
        ///     in the table. Zero is what the original leaves in the cell in that very case
        /// </returns>
        public int GetTerritory(Vector3 position)
        {
            if (position == null)
            {
                return 0;
            }

            RegionLayer layer = _layers.FirstOrDefault(candidate => candidate.Contains(position));

            if (layer == null)
            {
                return 0;
            }

            int x = (int)((position.X - layer.OriginX) / CellSize);

            // The rows of the layer run from the north down, while a bitmap keeps its rows from
            // the bottom up, so the row of the point is counted from the far end of the file
            int z = (int)((position.Z - layer.OriginZ) / CellSize);
            int row = layer.Height - 1 - z;

            if (x < 0 || x >= layer.Width || row < 0 || row >= layer.Height)
            {
                return 0;
            }

            int colour;

            try
            {
                using FileStream stream = new FileStream(layer.Path, FileMode.Open, FileAccess.Read, FileShare.Read);

                // The rows of a bitmap are padded to four bytes
                long stride = (layer.Width * 3 + 3) / 4 * 4;

                stream.Seek(layer.PixelOffset + row * stride + x * 3, SeekOrigin.Begin);

                Span<byte> pixel = stackalloc byte[3];

                if (stream.Read(pixel) != pixel.Length)
                {
                    return 0;
                }

                // The bitmap keeps a pixel as blue, green, red
                colour = (pixel[2] << 16) | (pixel[1] << 8) | pixel[0];
            }
            catch (IOException e)
            {
                _logger.LogError(e, "Can not read the region layer {Path}", layer.Path);

                return 0;
            }

            return _territoryByColour.TryGetValue(colour, out int territory) ? territory : 0;
        }

        /// <summary>
        ///     Reads the headers of every layer of the directory
        /// </summary>
        private void Load(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                _logger.LogInformation("GameSetting.RegionDataDirectory is not set: the territory of a point is unknown, and a character is raised at the point it is bound to");

                return;
            }

            if (!Directory.Exists(directory))
            {
                _logger.LogWarning("The directory of the region layers {Directory} does not exist, the territory of a point stays unknown", directory);

                return;
            }

            foreach (string path in Directory.EnumerateFiles(directory, "territory *.bmp"))
            {
                RegionLayer layer = ReadHeader(path);

                if (layer != null)
                {
                    _layers.Add(layer);
                }
            }

            _logger.LogInformation("Read {Count} region layer(s) of the world from {Directory}", _layers.Count, directory);
        }

        /// <summary>
        ///     Reads the header of one layer: where in the world it begins and how big it is
        /// </summary>
        /// <returns>The layer, or null when the file is not one</returns>
        private RegionLayer ReadHeader(string path)
        {
            // The name of the file carries the sector the map begins at: "territory <x> <z>.bmp"
            string[] parts = Path.GetFileNameWithoutExtension(path).Split(' ');

            if (parts.Length != 3 ||
                !int.TryParse(parts[1], out int sectorX) ||
                !int.TryParse(parts[2], out int sectorZ))
            {
                return null;
            }

            try
            {
                using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                byte[] header = new byte[BitmapHeaderSize];

                if (stream.Read(header, 0, header.Length) != header.Length || header[0] != 'B' || header[1] != 'M')
                {
                    return null;
                }

                int pixelOffset = BitConverter.ToInt32(header, 10);
                int width = BitConverter.ToInt32(header, 18);
                int height = BitConverter.ToInt32(header, 22);
                short bits = BitConverter.ToInt16(header, 28);

                if (width <= 0 || height <= 0 || bits != 24)
                {
                    _logger.LogWarning("The region layer {Path} is {Width}x{Height} of {Bits} bits, and only a twenty four bit one is read", path, width, height, bits);

                    return null;
                }

                return new RegionLayer
                {
                    Path = path,
                    OriginX = sectorX * SectorSize,
                    OriginZ = sectorZ * SectorSize,
                    Width = width,
                    Height = height,
                    PixelOffset = pixelOffset
                };
            }
            catch (IOException e)
            {
                _logger.LogError(e, "Can not read the header of the region layer {Path}", path);

                return null;
            }
        }

        /// <summary>
        ///     Six hex digits of a colour into a number
        /// </summary>
        private static bool TryParseColour(string text, out int colour)
        {
            colour = 0;

            return !string.IsNullOrWhiteSpace(text) &&
                   int.TryParse(text.Trim().TrimStart('#'), System.Globalization.NumberStyles.HexNumber,
                       System.Globalization.CultureInfo.InvariantCulture, out colour);
        }

        /// <summary>
        ///     The region layer of one map
        /// </summary>
        private class RegionLayer
        {
            public string Path { get; set; }
            public float OriginX { get; set; }
            public float OriginZ { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int PixelOffset { get; set; }

            /// <summary>
            ///     Whether the point falls onto this layer
            /// </summary>
            public bool Contains(Vector3 position)
            {
                return position.X >= OriginX && position.X < OriginX + Width * CellSize &&
                       position.Z >= OriginZ && position.Z < OriginZ + Height * CellSize;
            }
        }
    }
}
