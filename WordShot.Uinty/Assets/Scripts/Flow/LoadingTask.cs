using UnityEngine;

namespace WordShot.Flow
{
    /// <summary>
    /// ロード画面の間に行う読み込み1つぶん。シーンの読み込みと一緒に、ゲージの進み具合に含まれる。
    /// </summary>
    /// <remarks>
    /// 使い方：シーンを切り替える前に GameFlow.Instance.AddLoadingTask(task) で登録する。
    /// シーンに置いた画像や音は、シーンの読み込みで一緒に読まれるので、ここに入れなくてよい。
    /// ボスのデータなど、プログラムから読むものを先に読んでおきたいときに使う。
    /// </remarks>
    public interface ILoadingTask
    {
        /// <summary>読み込みを始める（ロード画面になってから呼ばれる）</summary>
        void Begin();

        /// <summary>進み具合（0〜1）</summary>
        float Progress { get; }

        bool IsDone { get; }
    }

    /// <summary>
    /// Resources フォルダの中のものを、ロード画面の間に読み込む。
    /// </summary>
    /// <example>
    /// var bossData = new ResourceLoadTask&lt;TextAsset&gt;("Bosses/shamoji_neko");
    /// GameFlow.Instance.AddLoadingTask(bossData);
    /// GameFlow.Instance.StartStage(stage);
    /// // 次のシーンで bossData.Asset を使う（この task を持っている間は、読み込んだものが片付けられない）
    /// </example>
    public class ResourceLoadTask<T> : ILoadingTask where T : Object
    {
        readonly string path;
        ResourceRequest request;

        public ResourceLoadTask(string path)
        {
            this.path = path;
        }

        /// <summary>読み込んだもの。読み込みが終わる前や、見つからなかったときは null</summary>
        public T Asset => request != null ? request.asset as T : null;

        public float Progress => request != null ? request.progress : 0f;

        public bool IsDone => request != null && request.isDone;

        public void Begin()
        {
            request = Resources.LoadAsync<T>(path);
        }
    }
}
