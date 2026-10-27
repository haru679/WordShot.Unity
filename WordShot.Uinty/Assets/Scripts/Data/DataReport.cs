using System.Collections.Generic;
using System.Text;

namespace WordShot.Data
{
    /// <summary>
    /// データのチェック結果（エラーと注意）。データを書く人（Aさん）が読んで直せるよう、日本語で書く。
    /// </summary>
    public class DataReport
    {
        readonly List<string> errors = new List<string>();
        readonly List<string> warnings = new List<string>();

        /// <summary>エラー：このままだとゲームで使えない（読み込まない）</summary>
        public IReadOnlyList<string> Errors => errors;

        /// <summary>注意：動くが、直したほうがよい</summary>
        public IReadOnlyList<string> Warnings => warnings;

        public bool HasErrors => errors.Count > 0;

        public void Error(string fileName, string message) => errors.Add($"{fileName}：{message}");

        public void Warning(string fileName, string message) => warnings.Add($"{fileName}：{message}");

        public override string ToString()
        {
            var text = new StringBuilder();
            foreach (string error in errors) text.AppendLine($"[エラー] {error}");
            foreach (string warning in warnings) text.AppendLine($"[注意] {warning}");
            return text.ToString();
        }
    }
}
