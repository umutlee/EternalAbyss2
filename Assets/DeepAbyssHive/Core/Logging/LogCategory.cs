using System;

namespace DeepAbyssHive.Core.Logging
{
    /// <summary>分類列舉，新增時僅需在這裡擴充。</summary>
    public enum LogCategory
    {
        Unset = 0,
        Game,
        Terrain,
        Units,
        Buildings,
        Creep,
        Placement,
        HUD,
        Health,
        Stream,
        Dev,
        AI,
        Path,
        Input,
        Editor,
        Net,
        Save
    }
}