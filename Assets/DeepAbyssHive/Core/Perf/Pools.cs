using System.Collections.Generic;
using System.Text;

namespace DeepAbyssHive.Core.Perf
{
    /// <summary>簡易物件池（List/StringBuilder）。用後請還。</summary>
    public static class Pools
    {
        private static readonly Stack<List<int>> _listInt = new Stack<List<int>>(16);
        private static readonly Stack<StringBuilder> _sb = new Stack<StringBuilder>(16);

        public static List<int> RentListInt(int capacity = 16) => _listInt.Count > 0 ? _listInt.Pop() : new List<int>(capacity);
        public static void Return(List<int> list) { if (list == null) return; list.Clear(); _listInt.Push(list); }

        public static StringBuilder RentSB(int capacity = 256) => _sb.Count > 0 ? _sb.Pop() : new StringBuilder(capacity);
        public static string ReturnToString(StringBuilder b) { if (b == null) return string.Empty; var s = b.ToString(); b.Length = 0; _sb.Push(b); return s; }
    }
}