using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

namespace LupusInFabula.Yarnslator{
    public class YarnNodeSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        private List<string> listItems = new List<string>();
        private Action<string> onSetIndexCallback;

        public YarnNodeSearchProvider(List<string> items, Action<string> callback){
            listItems = items;
            onSetIndexCallback = callback;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context){
            List<SearchTreeEntry> entries = new List<SearchTreeEntry>();

            entries.Add(new SearchTreeGroupEntry(new GUIContent("Nodes"), 0));
            listItems.Add("Unfiltered");

            List<string> groups = new List<string>();
            foreach(string item in listItems){
                string[] entryTitle = item.Split('/');
                string groupName = "";
                for(int i = 0; i < entryTitle.Length - 1; i++){
                    groupName += entryTitle[i];
                    if(!groups.Contains(groupName)){
                        SearchTreeEntry groupEntry = new SearchTreeEntry(new GUIContent(entryTitle[i]));
                        groupEntry.level = i + 1;
                        string en = "";
                        for (int e = 0; e < i; e++)
                        {
                            en += entryTitle[e] + "/";
                        }
                        en += entryTitle[i];
                        groupEntry.userData = en;
                        entries.Add(groupEntry);
                        entries.Add(new SearchTreeGroupEntry(new GUIContent(entryTitle[i]), i + 1));
                        groups.Add(groupName);
                    }
                    groupName += "/";
                }
                SearchTreeEntry entry = new SearchTreeEntry(new GUIContent(entryTitle.Last()));
                entry.level = entryTitle.Length;
                entry.userData = item;
                entries.Add(entry);
            }
            

            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context){
            onSetIndexCallback?.Invoke((string)SearchTreeEntry.userData);
            return true;
        }
    }
}
