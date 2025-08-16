using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioProgramApp.Models
{
    public class ProgramInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Performer { get; set; }
        public string ImageUrl { get; set; }

        // yyyyMMddHHmmss 形式の正確な開始・終了時刻 (ft, to 属性より)
        public string FullStartTime { get; set; }
        public string FullEndTime { get; set; }

        // HHmm 形式の表示用開始・終了時刻 (ftl, tol 属性より)
        public string StartTimeHHmm { get; set; }
        public string EndTimeHHmm { get; set; }

        public string InfoHtml { get; set; } // <info> タグの元のHTMLコンテンツ
        public string InfoText { get; set; } // タグ除去・簡易処理後のプレーンテキスト
    }
}
