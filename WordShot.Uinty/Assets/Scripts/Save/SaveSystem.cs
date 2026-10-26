using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace WordShot.Saves
{
    /// <summary>
    /// セーブデータの読み込みと保存（オートセーブ）。
    /// </summary>
    /// <remarks>
    /// ・保存先：Application.persistentDataPath/save.json（1つ前のセーブを save.json.bak に残す）
    /// ・中身が変わったら RequestSave() を呼ぶ。ファイルへの書き込みは裏で行うので、ゲームは止まらない。
    ///   書いている間にまた呼ばれたら、書き終わったあとにもう1回だけ書く。
    /// ・一時ファイルに書いてから入れ替えるので、書いている途中で終了しても、前のセーブデータは壊れない。
    /// ・ゲームを終了するときに、保存していない変更があれば、その場で保存する。
    /// ・セーブの始まりと終わりは SaveStarted / SaveFinished で知らせる（「セーブ中...」の表示が使う）。
    /// </remarks>
    public static class SaveSystem
    {
        const string FileName = "save.json";
        const string OldProgressKey = "Stage.Cleared";   // セーブデータを作る前に、クリア記録を入れていた PlayerPrefs

        static readonly object fileLock = new object();   // 裏での書き込みと、終了時の書き込みが重ならないように

        static SaveData data;
        static bool isDirty;          // まだファイルに書いていない変更があるか
        static bool saveAgain;        // 書いている間に、もう一度セーブを頼まれたか

        /// <summary>セーブを始めたとき</summary>
        public static event Action SaveStarted;

        /// <summary>セーブが終わったとき（true なら成功）</summary>
        public static event Action<bool> SaveFinished;

        public static bool IsSaving { get; private set; }

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
        static string BackupPath => FilePath + ".bak";
        static string TempPath => FilePath + ".tmp";

        /// <summary>今のセーブデータ。初めて使うときにファイルから読む</summary>
        public static SaveData Data => data ??= Load();

        // このプロジェクトはドメインのリロードが無効なので、再生のたびに読み直す
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            data = null;
            isDirty = false;
            saveAgain = false;
            IsSaving = false;
            SaveStarted = null;
            SaveFinished = null;
            Application.quitting -= SaveOnQuit;
            Application.quitting += SaveOnQuit;
        }

        // ---- 保存 ----

        /// <summary>
        /// オートセーブを頼む。中身を変えたら呼ぶ。裏で保存し、終わったら SaveFinished が呼ばれる
        /// </summary>
        public static void RequestSave()
        {
            isDirty = true;
            if (IsSaving)
            {
                saveAgain = true;
                return;
            }
            _ = SaveInBackground();
        }

        static async Task SaveInBackground()
        {
            IsSaving = true;
            SaveStarted?.Invoke();

            bool succeeded = true;
            do
            {
                saveAgain = false;
                isDirty = false;
                // Unity の機能（パスや JSON）はここで使い、ファイルへの書き込みだけを裏で行う
                Snapshot snapshot = TakeSnapshot();
                try
                {
                    await Task.Run(() => WriteSafely(snapshot));
                }
                catch (Exception e)
                {
                    succeeded = false;
                    isDirty = true;
                    Debug.LogError($"[Save] セーブに失敗しました（{snapshot.path}）：{e.Message}");
                }
            } while (saveAgain && succeeded);

            IsSaving = false;
            SaveFinished?.Invoke(succeeded);
        }

        /// <summary>今すぐ保存する（終了するときなど、待ってでも確実に書きたいとき用）</summary>
        public static void SaveNow()
        {
            Snapshot snapshot = TakeSnapshot();
            try
            {
                WriteSafely(snapshot);
                isDirty = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] セーブに失敗しました（{snapshot.path}）：{e.Message}");
            }
        }

        static void SaveOnQuit()
        {
            if (data != null && (isDirty || IsSaving)) SaveNow();
        }

        // 書き込む内容。番号が大きいほど新しい
        readonly struct Snapshot
        {
            public readonly string path;
            public readonly string json;
            public readonly long number;
            public Snapshot(string path, string json, long number) { this.path = path; this.json = json; this.number = number; }
        }

        static long lastSnapshotNumber;
        static long writtenNumber;      // ファイルに書いた中で、一番新しい番号

        static Snapshot TakeSnapshot()
        {
            Data.version = SaveData.CurrentVersion;
            Data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return new Snapshot(FilePath, JsonUtility.ToJson(Data, true), ++lastSnapshotNumber);
        }

        // 一時ファイルに書いてから入れ替える。前のセーブデータは .bak に残す
        // （終了時の書き込みが先に終わっていたら、それより古い内容は書かない）
        static void WriteSafely(Snapshot snapshot)
        {
            lock (fileLock)
            {
                if (snapshot.number <= writtenNumber) return;

                string path = snapshot.path;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                File.WriteAllText(temp, snapshot.json);
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
                writtenNumber = snapshot.number;
            }
        }

        // ---- 読み込み ----

        static SaveData Load()
        {
            SaveData loaded = TryRead(FilePath) ?? TryRead(BackupPath);
            if (loaded == null)
            {
                loaded = new SaveData();
                MigrateFromPlayerPrefs(loaded);
            }

            // 難易度を足す前の記録（「stage_a」だけ）は、「かんたん」をクリアしたことにする
            loaded.clearedStages = (loaded.clearedStages ?? new List<string>())
                .Select(entry => entry.Contains(':') ? entry : $"{entry}:easy")
                .Distinct()
                .ToList();
            return loaded;
        }

        static SaveData TryRead(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (loaded != null && path == BackupPath)
                    Debug.LogWarning($"[Save] セーブデータが読めなかったので、1つ前のセーブ（{BackupPath}）から読みました");
                return loaded;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] {path} が読めませんでした：{e.Message}");
                return null;
            }
        }

        // セーブデータを作る前は、クリア記録を PlayerPrefs に入れていた。それを引き継いで、セーブデータに書く
        static void MigrateFromPlayerPrefs(SaveData target)
        {
            string old = PlayerPrefs.GetString(OldProgressKey, "");
            if (string.IsNullOrEmpty(old)) return;

            target.clearedStages = old.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            data = target;
            SaveNow();
            PlayerPrefs.DeleteKey(OldProgressKey);
            PlayerPrefs.Save();
            Debug.Log("[Save] PlayerPrefs のクリア記録を、セーブデータに引き継ぎました");
        }

        // ---- 開発用 ----

        /// <summary>開発用：セーブデータのファイルを消して、最初の状態に戻す</summary>
        public static void DeleteAll()
        {
            foreach (string path in new[] { FilePath, BackupPath, TempPath })
            {
                if (File.Exists(path)) File.Delete(path);
            }
            data = new SaveData();
            isDirty = false;
        }
    }
}
