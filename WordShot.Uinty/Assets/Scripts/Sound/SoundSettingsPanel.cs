using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace WordShot.Sounds
{
    /// <summary>
    /// 音量設定の画面（スライダーとミュート）。タイトル画面や、今後作るポーズ画面から Open で開く。
    /// </summary>
    /// <remarks>
    /// プレハブ：Assets/Prefabs/UI/SoundSettingsPanel.prefab
    /// 日本語フォントが決まるまでは、文字化けしないよう表示は英語にしている。
    /// </remarks>
    public class SoundSettingsPanel : MonoBehaviour
    {
        [Serializable]
        class Row
        {
            public VolumeChannel channel;
            public Slider slider;
            public Toggle mute;
            public TMP_Text valueText;
            [Tooltip("スライダーを動かしたときに鳴らす確認用の音（なくてもよい）")]
            public SoundCue testCue;
        }

        const float TestSoundInterval = 0.15f;

        [SerializeField] List<Row> rows = new List<Row>();
        [SerializeField] Button closeButton;

        GameObject returnSelection;   // 閉じたときに、選択を戻す先
        float nextTestSoundTime;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>
        /// 最後に閉じたフレーム。Esc で閉じたとき、同じ Esc でポーズまで解除されないよう、ポーズ画面が見る
        /// </summary>
        public int LastClosedFrame { get; private set; } = -1;

        // 最初に開いたとき（SetActive(true) した瞬間）に1回だけ呼ばれる
        void Awake()
        {
            foreach (Row row in rows)
            {
                row.slider.onValueChanged.AddListener(value => OnSliderChanged(row, value));
                row.mute.onValueChanged.AddListener(muted => Sound.Volumes.SetMuted(row.channel, muted));
            }
            closeButton.onClick.AddListener(Close);
        }

        /// <param name="returnTo">閉じたときに選択を戻すボタン（キーボード操作用）</param>
        public void Open(GameObject returnTo = null)
        {
            returnSelection = returnTo;
            gameObject.SetActive(true);
            Refresh();

            GameObject first = rows.Count > 0 ? rows[0].slider.gameObject : closeButton.gameObject;
            EventSystem.current.SetSelectedGameObject(first);
        }

        public void Close()
        {
            Sound.Volumes.Save();
            LastClosedFrame = Time.frameCount;
            gameObject.SetActive(false);
            if (returnSelection != null) EventSystem.current.SetSelectedGameObject(returnSelection);
        }

        void Update()
        {
            // Esc でも閉じられる
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        // 今の設定を画面に反映する。ここで値を入れても onValueChanged は呼ばれない
        void Refresh()
        {
            foreach (Row row in rows)
            {
                row.slider.SetValueWithoutNotify(Sound.Volumes.GetVolume(row.channel));
                row.mute.SetIsOnWithoutNotify(Sound.Volumes.IsMuted(row.channel));
                UpdateValueText(row);
            }
        }

        void OnSliderChanged(Row row, float value)
        {
            Sound.Volumes.SetVolume(row.channel, value);
            UpdateValueText(row);

            // どのくらいの音量か分かるよう、確認用の音を鳴らす（鳴りすぎないよう間隔をあける）
            if (row.testCue != null && Time.unscaledTime >= nextTestSoundTime)
            {
                Sound.Play(row.testCue);
                nextTestSoundTime = Time.unscaledTime + TestSoundInterval;
            }
        }

        static void UpdateValueText(Row row)
        {
            row.valueText.text = $"{Mathf.RoundToInt(row.slider.value * 100)}%";
        }
    }
}
