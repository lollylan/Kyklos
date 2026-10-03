using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Kyklos
{
    /// <summary>Kleiner JSON-Leser/-Schreiber. Objekte sind Dictionary, Listen sind List&lt;object&gt;, Zahlen double.</summary>
    public static class Json
    {
        public static object Parse(string s)
        {
            int i = 0;
            object v = Value(s, ref i);
            Ws(s, ref i);
            if (i != s.Length) throw new FormatException("Unerwartete Zeichen am Ende (Position " + i + ").");
            return v;
        }

        static void Ws(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n' || s[i] == '﻿')) i++;
        }

        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            if (i >= s.Length) throw new FormatException("Unerwartetes Ende.");
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>();
                i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Ws(s, ref i);
                    string k = Str(s, ref i);
                    Ws(s, ref i);
                    if (s[i] != ':') throw new FormatException("':' erwartet (Position " + i + ").");
                    i++;
                    d[k] = Value(s, ref i);
                    Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == '}') { i++; return d; }
                    throw new FormatException("',' oder '}' erwartet (Position " + i + ").");
                }
            }
            if (c == '[')
            {
                var l = new List<object>();
                i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Value(s, ref i));
                    Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == ']') { i++; return l; }
                    throw new FormatException("',' oder ']' erwartet (Position " + i + ").");
                }
            }
            if (c == '"') return Str(s, ref i);
            if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int st = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (i == st) throw new FormatException("Unerwartetes Zeichen '" + c + "' (Position " + i + ").");
            return double.Parse(s.Substring(st, i - st), CultureInfo.InvariantCulture);
        }

        static string Str(string s, ref int i)
        {
            if (s[i] != '"') throw new FormatException("Zeichenkette erwartet (Position " + i + ").");
            i++;
            var sb = new StringBuilder();
            while (true)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                c = s[i++];
                switch (c)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u': sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber)); i += 4; break;
                    default: sb.Append(c); break;
                }
            }
        }

        public static string Write(object v)
        {
            var sb = new StringBuilder();
            Write(sb, v, 0);
            sb.Append("\r\n");
            return sb.ToString();
        }

        static void Write(StringBuilder sb, object v, int ind)
        {
            if (v == null) { sb.Append("null"); return; }
            if (v is string) { WriteStr(sb, (string)v); return; }
            if (v is bool) { sb.Append((bool)v ? "true" : "false"); return; }
            if (v is int) { sb.Append(((int)v).ToString(CultureInfo.InvariantCulture)); return; }
            if (v is double) { sb.Append(((double)v).ToString("R", CultureInfo.InvariantCulture)); return; }
            var d = v as Dictionary<string, object>;
            if (d != null)
            {
                if (d.Count == 0) { sb.Append("{}"); return; }
                sb.Append("{\r\n");
                int k = 0;
                foreach (var kv in d)
                {
                    sb.Append(' ', ind + 2);
                    WriteStr(sb, kv.Key);
                    sb.Append(": ");
                    Write(sb, kv.Value, ind + 2);
                    if (++k < d.Count) sb.Append(',');
                    sb.Append("\r\n");
                }
                sb.Append(' ', ind).Append('}');
                return;
            }
            var l = v as List<object>;
            if (l != null)
            {
                if (l.Count == 0) { sb.Append("[]"); return; }
                sb.Append("[\r\n");
                for (int k = 0; k < l.Count; k++)
                {
                    sb.Append(' ', ind + 2);
                    Write(sb, l[k], ind + 2);
                    if (k + 1 < l.Count) sb.Append(',');
                    sb.Append("\r\n");
                }
                sb.Append(' ', ind).Append(']');
                return;
            }
            throw new NotSupportedException(v.GetType().Name);
        }

        static void WriteStr(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        // Lesehilfen mit Vorgabewerten: fehlende oder falsch getypte Felder kippen die Konfiguration nicht.
        public static string S(Dictionary<string, object> d, string k, string def = "")
        {
            object v; return d != null && d.TryGetValue(k, out v) && v is string ? (string)v : def;
        }
        public static double N(Dictionary<string, object> d, string k, double def = 0)
        {
            object v; return d != null && d.TryGetValue(k, out v) && v is double ? (double)v : def;
        }
        public static int I(Dictionary<string, object> d, string k, int def = 0) { return (int)Math.Round(N(d, k, def)); }
        public static bool B(Dictionary<string, object> d, string k, bool def = false)
        {
            object v; return d != null && d.TryGetValue(k, out v) && v is bool ? (bool)v : def;
        }
        public static Dictionary<string, object> O(Dictionary<string, object> d, string k)
        {
            object v; return d != null && d.TryGetValue(k, out v) ? v as Dictionary<string, object> : null;
        }
        public static List<object> A(Dictionary<string, object> d, string k)
        {
            object v; return d != null && d.TryGetValue(k, out v) && v is List<object> ? (List<object>)v : new List<object>();
        }
    }
}
