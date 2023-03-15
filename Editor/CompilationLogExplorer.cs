using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class CompilationLogExplorer : EditorWindow
    {
        public List<string> Entries = new List<string>();
        public Vector2 ScrollPosition;
        public int EntriesToShow;
        private readonly Regex _summaries = new Regex(@"\n[^\n\t]+(\n[\t]+[^\n]+)+", RegexOptions.Compiled);
        private readonly Regex _highlightNumbers = new Regex(@"([\d.]+) *(ms|seconds)?", RegexOptions.Compiled);
        private readonly Regex _highlightNames = new Regex(@"\t+[a-zA-Z\d ]+", RegexOptions.Compiled);
        private GUIStyle _style;
    
        [MenuItem("Window/Analysis/Compilation Log Explorer")]
        private static void Init()
        {
            GetWindow<CompilationLogExplorer>(false, "Compilation Log Explorer", true);
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Parse Log", GUILayout.ExpandWidth(false)))
            {
                ReadConsole(Application.consoleLogPath);
            }

            GUILayout.Label($"Total: {Entries.Count} Entries ", GUILayout.ExpandWidth(false));
            GUILayout.Space(32);
            EntriesToShow = EditorGUILayout.IntField("Show N Last Entries", EntriesToShow, GUILayout.ExpandWidth(false));
            EntriesToShow = Mathf.Max(0, EntriesToShow);
            GUILayout.EndHorizontal();

            if (_style == null)
            {
                _style = new GUIStyle(EditorStyles.label);
                _style.richText = true;
                _style.font = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font;
            }
            ScrollPosition = GUILayout.BeginScrollView(ScrollPosition);
            var start = EntriesToShow > 0 ? Mathf.Max(0, Entries.Count - EntriesToShow * 2) : 0;
            for (var i = start; i < Entries.Count; i++)
            {
                EditorGUI.SelectableLabel(GUILayoutUtility.GetRect(new GUIContent(Entries[i]), _style), Entries[i], _style);
            }

            GUILayout.EndScrollView();
        }

        private void ReadConsole(string path)
        {
            try
            {
                var title = "Reading Editor Log File";
                EditorUtility.DisplayProgressBar(title, "reading file", 0f);
                string text;
                using (var sr = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var streamReader = new StreamReader(sr, Encoding.UTF8))
                    {
                        text = streamReader.ReadToEnd();
                    }
                }

                EditorUtility.DisplayProgressBar(title, "parsing log", 0.25f);
                var summaries = _summaries.Matches(text).Select(m => m.Value).ToList();
                Entries.Clear();
                for (var i = 0; i < summaries.Count; i++)
                {
                    EditorUtility.DisplayProgressBar(title, $"highlighting {i} / {summaries.Count} entries",
                        0.5f + 0.5f * (i / (float)summaries.Count));
                    string summary = summaries[i];
                    summary = _highlightNumbers.Replace(summary, (m) =>
                    {
                        var num = m.Groups[1].Value;
                        var type = m.Groups[2].Value;

                        if (!string.IsNullOrWhiteSpace(type))
                        {
                            if (float.TryParse(num, out float f))
                            {
                                if (type == "ms") f = f / 1000f;
                                f = f / 3f;
                                float f2 = f * f;
                                var color = Color.HSVToRGB(
                                    Mathf.Lerp(125 / 360f, 0f, f2),
                                    Mathf.Lerp(0.2f, 0.6f, f * 2),
                                    Mathf.Lerp(0.8f, 1f, f));

                                return
                                    $"<b><color=#{ColorUtility.ToHtmlStringRGB(color)}>{num}</color> <i>{type}</i></b>";
                            }

                            return $"<b>{num} <i>{type}</i></b>";
                        }

                        return $"<b>{num}</b>";
                    });


                    var colorCode = ColorUtility.ToHtmlStringRGB(new Color(0.75f, 0.8f, 1f));
                    summary = _highlightNames.Replace(summary,
                        (m) => $"<color=#{colorCode}>{m.Groups[0].Value}</color>");

                    summary = summary.Replace("\t", "    ");

                    Entries.Add(summary);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}