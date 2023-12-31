﻿using System;
using System.Collections.Generic;

namespace Clawssets.Builder
{
    /// <summary>
    /// Representa um grupo de assets.
    /// </summary>
    public class AssetGroup
    {
        /// <summary>
        /// Nome do caminho do arquivo/pasta.
        /// </summary>
        public string Name;
        public AssetType Type;
        public List<string> Files = new List<string>();
    }
}