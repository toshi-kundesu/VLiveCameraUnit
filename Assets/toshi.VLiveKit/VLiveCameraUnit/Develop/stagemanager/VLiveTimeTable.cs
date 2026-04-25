using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace toshi.VLiveKit
{
    /// <summary>
    /// ライブ用の MasterTimeline と、そこに紐づく各セクション Timeline を管理するタイムテーブル。
    /// どのコンポーネントからでも Get で取得できる。
    /// </summary>
    [DisallowMultipleComponent]
    public class VLiveTimeTable : MonoBehaviour
    {
        [Serializable]
        public class VLiveTimelineSlot
        {
            [Tooltip("例: Camera / Light / FX / MC / SE など")]
            public string sectionName;

            [Tooltip("そのセクションで使う PlayableDirector")]
            public PlayableDirector director;

            [Tooltip("必要ならメモ用途で使う")]
            [TextArea]
            public string note;
        }

        [Header("Master")]
        [SerializeField] private PlayableDirector masterTimeline;

        [Header("Section Timelines")]
        [SerializeField] private List<VLiveTimelineSlot> sectionTimelines = new List<VLiveTimelineSlot>();

        [Header("Auto Find")]
        [SerializeField] private bool autoFindOnAwake = true;

        [SerializeField] private bool includeInactive = true;

        private static VLiveTimeTable cachedInstance;
        private readonly Dictionary<string, PlayableDirector> sectionMap =
            new Dictionary<string, PlayableDirector>(StringComparer.OrdinalIgnoreCase);

        public PlayableDirector MasterTimeline => masterTimeline;
        public IReadOnlyList<VLiveTimelineSlot> SectionTimelines => sectionTimelines;

        private void Awake()
        {
            cachedInstance = this;

            if (autoFindOnAwake)
            {
                AutoCollectChildDirectors();
            }

            RebuildMap();
        }

        private void OnEnable()
        {
            if (cachedInstance == null)
            {
                cachedInstance = this;
            }

            RebuildMap();
        }

        private void OnValidate()
        {
            RebuildMap();
        }

        /// <summary>
        /// シーン上の TimeTable を取得する。
        /// caller が属する階層親から優先的に探し、無ければシーン全体から探す。
        /// </summary>
        public static VLiveTimeTable Get(Component caller = null)
        {
            if (cachedInstance != null)
            {
                return cachedInstance;
            }

            if (caller != null)
            {
                cachedInstance = caller.GetComponentInParent<VLiveTimeTable>(true);
                if (cachedInstance != null)
                {
                    return cachedInstance;
                }
            }

#if UNITY_2023_1_OR_NEWER
            cachedInstance = FindFirstObjectByType<VLiveTimeTable>(FindObjectsInactive.Include);
#else
            cachedInstance = FindObjectOfType<VLiveTimeTable>(true);
#endif
            return cachedInstance;
        }

        /// <summary>
        /// MasterTimeline を取得。
        /// </summary>
        public PlayableDirector GetMasterTimeline()
        {
            return masterTimeline;
        }

        /// <summary>
        /// セクション名から PlayableDirector を取得。
        /// </summary>
        public PlayableDirector GetSectionTimeline(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                return null;
            }

            if (sectionMap.Count == 0)
            {
                RebuildMap();
            }

            sectionMap.TryGetValue(sectionName, out var director);
            return director;
        }

        /// <summary>
        /// セクション名から PlayableDirector を TryGet する。
        /// </summary>
        public bool TryGetSectionTimeline(string sectionName, out PlayableDirector director)
        {
            director = null;

            if (string.IsNullOrWhiteSpace(sectionName))
            {
                return false;
            }

            if (sectionMap.Count == 0)
            {
                RebuildMap();
            }

            return sectionMap.TryGetValue(sectionName, out director) && director != null;
        }

        /// <summary>
        /// 指定 Director がどのセクションに属するかを取得。
        /// </summary>
        public bool TryGetSectionName(PlayableDirector targetDirector, out string sectionName)
        {
            sectionName = null;

            if (targetDirector == null)
            {
                return false;
            }

            for (int i = 0; i < sectionTimelines.Count; i++)
            {
                var slot = sectionTimelines[i];
                if (slot != null && slot.director == targetDirector)
                {
                    sectionName = slot.sectionName;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// セクションを追加または上書き登録。
        /// </summary>
        public void SetSectionTimeline(string sectionName, PlayableDirector director, string note = "")
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                Debug.LogWarning("[VLiveTimeTable] sectionName is null or empty.", this);
                return;
            }

            for (int i = 0; i < sectionTimelines.Count; i++)
            {
                var slot = sectionTimelines[i];
                if (slot == null) continue;

                if (string.Equals(slot.sectionName, sectionName, StringComparison.OrdinalIgnoreCase))
                {
                    slot.director = director;
                    slot.note = note;
                    RebuildMap();
                    return;
                }
            }

            sectionTimelines.Add(new VLiveTimelineSlot
            {
                sectionName = sectionName,
                director = director,
                note = note
            });

            RebuildMap();
        }

        /// <summary>
        /// セクションを削除。
        /// </summary>
        public bool RemoveSectionTimeline(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                return false;
            }

            for (int i = sectionTimelines.Count - 1; i >= 0; i--)
            {
                var slot = sectionTimelines[i];
                if (slot == null) continue;

                if (string.Equals(slot.sectionName, sectionName, StringComparison.OrdinalIgnoreCase))
                {
                    sectionTimelines.RemoveAt(i);
                    RebuildMap();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 子階層の PlayableDirector を自動収集して section に追加する。
        /// 既存の sectionName と重複する場合は上書きしない。
        /// sectionName は GameObject 名を使う。
        /// </summary>
        [ContextMenu("Auto Collect Child Directors")]
        public void AutoCollectChildDirectors()
        {
            if (masterTimeline == null)
            {
                Debug.LogWarning("[VLiveTimeTable] MasterTimeline is not assigned.", this);
                return;
            }

            var root = masterTimeline.transform;
            var foundDirectors = root.GetComponentsInChildren<PlayableDirector>(includeInactive);

            for (int i = 0; i < foundDirectors.Length; i++)
            {
                var director = foundDirectors[i];
                if (director == null || director == masterTimeline)
                {
                    continue;
                }

                var sectionName = director.gameObject.name;

                bool alreadyExists = false;
                for (int j = 0; j < sectionTimelines.Count; j++)
                {
                    var slot = sectionTimelines[j];
                    if (slot == null) continue;

                    if (slot.director == director ||
                        string.Equals(slot.sectionName, sectionName, StringComparison.OrdinalIgnoreCase))
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (!alreadyExists)
                {
                    sectionTimelines.Add(new VLiveTimelineSlot
                    {
                        sectionName = sectionName,
                        director = director,
                        note = "Auto Collected"
                    });
                }
            }

            RebuildMap();
        }

        [ContextMenu("Rebuild Section Map")]
        public void RebuildMap()
        {
            sectionMap.Clear();

            for (int i = 0; i < sectionTimelines.Count; i++)
            {
                var slot = sectionTimelines[i];
                if (slot == null) continue;
                if (string.IsNullOrWhiteSpace(slot.sectionName)) continue;
                if (slot.director == null) continue;

                sectionMap[slot.sectionName] = slot.director;
            }
        }
    }
}