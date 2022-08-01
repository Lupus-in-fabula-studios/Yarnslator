using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using UnityEditor.Experimental.GraphView;

namespace LupusInFabula.Yarnslator{

    [Serializable]
    public class YarnslatorEditor : EditorWindow
    {
        [MenuItem ("Tools/Yarnslator")]
        public static void  ShowWindow () {
            EditorWindow.GetWindow<YarnslatorEditor>("Yarnslator editor");
        }

        private TextAsset stringsFile = null;
        private TextAsset refFile = null;

        private string language = "";

        [Serializable]
        public class YarnEntry : IComparable{
            public string language;
            public string id;
            public string text;
            public string file;
            public string node;
            public int lineNumber;
            public string lineLock;
            public string comment;

            public string ordering;

            public int CompareTo(object obj) {
                if (obj == null) return 1;

                YarnEntry otherEntry = obj as YarnEntry;
                if (otherEntry != null){
                return this.ordering.CompareTo(otherEntry.ordering);
                }
                else
                    return this.ordering.CompareTo(otherEntry.ordering);
            }
        }

        [SerializeField]
        private List<YarnEntry> yarnEntries;
        private List<YarnEntry> refEntries;

        private List<int> filteredEntries = new List<int>();

        private string nodeSearch;
        private string commonFilePath;
        private bool onlyNeedsChange;

        private Vector2 scrollPos;

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Translations string file ");
            TextAsset newStringsFile = EditorGUILayout.ObjectField(stringsFile, typeof(TextAsset), false) as TextAsset;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("References string file ");
            TextAsset newRefFile = EditorGUILayout.ObjectField(refFile, typeof(TextAsset), false) as TextAsset;
            EditorGUILayout.EndHorizontal();

            if(stringsFile != newStringsFile){
                stringsFile = newStringsFile;
                (bool b, string s, string l) = LoadStrings(stringsFile, out yarnEntries);
                if(b == false){
                    stringsFile = null;
                }
                commonFilePath = s;
                language = l;
                nodeSearch = "Unfiltered";
                FilterEntries();
            }

            if(refFile != newRefFile){
                refFile = newRefFile;
                (bool b, string s, string l) = LoadStrings(refFile, out refEntries);
                if(b == false){
                    refFile = null;
                }
            }

            if(stringsFile != null){

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Language used: ");
                string nLang = GUILayout.TextField(language);
                if(nLang != language){
                    language = nLang;
                    foreach(YarnEntry e in yarnEntries){
                        e.language = language;
                    }
                }
                GUILayout.EndHorizontal();

                if(GUILayout.Button("SAVE")){
                    SaveStrings();
                }

                if(GUILayout.Button("UNDO")){
                    Undo.PerformUndo();
                }
                
                GUILayout.BeginHorizontal();
                string f = nodeSearch;
                if(f == "Unfiltered"){
                    f = "Filter";
                }
                EditorGUILayout.LabelField("Show only lines in need of change: ");
                bool n = EditorGUILayout.Toggle(onlyNeedsChange);
                if(n != onlyNeedsChange){
                    onlyNeedsChange = n;
                    FilterEntries();
                }
                if(GUILayout.Button(f, EditorStyles.popup)){
                    List<string> paths = new List<string>();

                    foreach(YarnEntry entry in yarnEntries){
                        string p = entry.file + "/" + entry.node;
                        if(p.StartsWith(commonFilePath)){p = p.Remove(0, commonFilePath.Length);}
                        if(!paths.Contains(p)){paths.Add(p);}
                    }

                    SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), new YarnNodeSearchProvider(paths, (x)=> {nodeSearch = x; FilterEntries();}));
                }
                GUILayout.EndHorizontal();


                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                for (int i = 0; i < filteredEntries.Count; i++)
                {
                    DrawEntry(filteredEntries[i]);
                }
                EditorGUILayout.EndScrollView();
            }
            else{
                EditorGUILayout.HelpBox("Choose a yarn translation strings file to begin editing", MessageType.Info);
            }
        }
        
        void DrawEntry(int id){
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(yarnEntries[id].id, GUILayout.MaxWidth(100));


            GUIStyle style = new GUIStyle(EditorStyles.largeLabel);
            style.wordWrap = true;
            if(refFile != null){
                GUILayout.TextArea("reference: " + '\n' + refEntries[id].text, style, GUILayout.Width(500), GUILayout.Height(100), GUILayout.MinWidth(100));
            }

            string nText = GUILayout.TextArea(yarnEntries[id].text, GUILayout.Width(500), GUILayout.Height(100), GUILayout.MinWidth(100));
            if(yarnEntries[id].text != nText){
                Undo.RegisterCompleteObjectUndo(this, "Changed translation");
                EditorUtility.SetDirty(this);
                yarnEntries[id].text = nText;
            }

            GUILayout.TextArea("file: " + yarnEntries[id].file + '\n' + "line: " + yarnEntries[id].lineNumber + '\n' + "node: " + yarnEntries[id].node, style, GUILayout.Width(500), GUILayout.Height(100), GUILayout.MinWidth(100));

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }

        void FilterEntries(){
            filteredEntries = new List<int>();
            string key = commonFilePath + nodeSearch;
            if(nodeSearch == "Unfiltered"){
                key = "";
            }

            for (int i = 0; i < yarnEntries.Count; i++)
            {
                string entryKey = yarnEntries[i].file + "/" + yarnEntries[i].node;
                if(entryKey.StartsWith(key)){
                    if(!onlyNeedsChange){
                        filteredEntries.Add(i);
                    }else if(yarnEntries[i].text.Contains("(NEEDS UPDATE)")){
                        filteredEntries.Add(i);
                    }
                }
            }
        }

        void Refresh(){
            (bool br, string sr, string lr) = LoadStrings(refFile, out refEntries);
            if(br == false){
                refFile = null;
            }
            (bool b, string s, string l) = LoadStrings(stringsFile, out yarnEntries);
            if(b == false){
                stringsFile = null;
            }
            commonFilePath = s;
            language = l;
            nodeSearch = "Unfiltered";
            FilterEntries();
        }
        

        (bool, string, string) LoadStrings(TextAsset t, out List<YarnEntry> y){
            Debug.Log("Loading strings...");

            List<YarnEntry> outEntries = new List<YarnEntry>();

            if(t != null){
                string[] data = ParseCSV(t.text);
                int columnCount = 8;

                string[] headers = new string[8]{"language","id","text","file","node","lineNumber","lock","comment"};

                for (int i = 0; i < columnCount - 1; i++)
                {
                    if(data[i] != headers[i]){
                        Debug.LogError("The file inputed isn't a translation strings file!");
                        y = outEntries;
                        return (false, "", "");
                    }
                }


                string commonPath = data[columnCount + 3] + "/";
                List<string> languages = new List<string>();
                int rowCount = data.Length / columnCount;

                for (int i = 1; i < rowCount; i++)
                {
                    YarnEntry entry = new YarnEntry();
                    int id = i * columnCount;

                    entry.language = data[id];
                    entry.id = data[id + 1];
                    entry.text = data[id + 2];
                    entry.file = data[id + 3];
                    entry.node = data[id + 4];
                    entry.lineNumber = int.Parse(data[id + 5]);
                    entry.lineLock = data[id + 6];
                    entry.comment = data[id + 7];

                    //entry.ordering = int.Parse(entry.id.Substring(5), System.Globalization.NumberStyles.HexNumber);
                    entry.ordering = entry.file + "/" + entry.node + entry.lineNumber;

                    while(!entry.file.StartsWith(commonPath) && commonPath != ""){
                        List<string> entries = commonPath.Split("/").ToList();
                        entries.RemoveAt(entries.Count - 1);
                        entries.RemoveAt(entries.Count - 1);
                        commonPath = "";
                        foreach(string s in entries){commonPath += s + "/";}
                    }

                    if(!languages.Contains(entry.language)){
                        languages.Add(entry.language);
                    }

                    outEntries.Add(entry);
                }

                outEntries.Sort();

                string languageString = "";
                for (int i = 0; i < languages.Count; i++)
                {
                    languageString += languages[i];
                    if(i < languages.Count - 1){
                        languageString += "/";
                    }
                }

                y = outEntries;
                return (true, commonPath, languageString);
            }else{
                y = new List<YarnEntry>();
                return (false, "", "");
            }
        }

        public override void SaveChanges(){
            SaveStrings();

            base.SaveChanges();
        }

        
        void SaveStrings(){
            string formattedString = "";

            string[] headers = new string[8]{"language","id","text","file","node","lineNumber","lock","comment"};
            for (int i = 0; i < headers.Length; i++)
            {
                formattedString += headers[i];
                if(i < headers.Length - 1){
                    formattedString += ",";
                }
            }

            foreach(YarnEntry entry in yarnEntries){
                formattedString += '\n';

                formattedString += entry.language + ",";
                formattedString += entry.id + ",";

                string text = "";
                for (int i = 0; i < entry.text.Length; i++)
                {
                    char c = entry.text[i];
                    switch(c){
                        case('"'):
                            text += c.ToString() + c.ToString();
                            break;
                        default:
                            text += c.ToString();
                            break;
                    }
                }
                if(text.Contains(',') || text.Contains('"')){
                    formattedString += '"' + text + '"' + ",";
                }else{
                    formattedString += text + ",";
                }

                formattedString += entry.file + ",";
                formattedString += entry.node + ",";
                formattedString += entry.lineNumber + ",";
                formattedString += entry.lineLock + ",";
                formattedString += entry.comment;
            }

            File.WriteAllText(AssetDatabase.GetAssetPath(stringsFile), formattedString);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(stringsFile));
            
            Debug.Log("SAVED");
        }

        string[] ParseCSV(string text){
            List<string> output = new List<string>();

            string currentEntry = "";
            bool ignoreCommas = false;

            for(int i = 0; i < text.Length; i++){
                char c = text[i];
                switch(c){
                    case (','):
                        if(ignoreCommas == false){
                            output.Add(currentEntry);
                            currentEntry = "";
                        }else{
                            currentEntry += c;
                        }
                        break;
                    case ('\n'):
                        output.Add(currentEntry);
                        currentEntry = "";
                        break;
                    case ('"'):
                        ignoreCommas = !ignoreCommas;
                        if(text[i - 1] == '"' && text[i + 1] == '"'){
                            currentEntry += c;
                        }
                        break;
                    default:
                        currentEntry += c;
                        break;
                }
            }

            return output.ToArray();
        }
    }
}
