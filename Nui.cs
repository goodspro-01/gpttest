using Core2.Common;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace Core2
{
    public class Nui
    {
        string server = @"\\Toefileserver1\グッズプロ\商品\ぬいぐるみ関係\ぬい刺しゅうテンプレート";
        string save = "\\\\toe07x\\fs\\グッズプロ\\注文情報\\履歴\\顧客\\278594\\2024年01月16日";
        List<string> nui_setting = new List<string>();


        public class Eye
        {
            public int x { get; set; }
            public int y { get; set; }
        }

        public class Eyebrow
        {
            public int x { get; set; }
            public int y { get; set; }
        }

        public class Mouth
        {
            public int x { get; set; }
            public int y { get; set; }
        }

        public class NuiSetting
        {
            public string changeSkinClass { get; set; }
            public string leftEyebrowFileName { get; set; }
            public string leftEyebrowClass { get; set; }
            public string rightEyebrowFileName { get; set; }
            public string rightEyebrowClass { get; set; }
            public string rightEyeFileName { get; set; }
            public string rightEyeClass2 { get; set; }
            public string rightEyeClass3 { get; set; }
            public string rightEyeClass4 { get; set; }
            public string rightEyeClass5 { get; set; }
            public string leftEyeFileName { get; set; }
            public string leftEyeClass2 { get; set; }
            public string leftEyeClass3 { get; set; }
            public string leftEyeClass4 { get; set; }
            public string leftEyeClass5 { get; set; }
            public string mouthFileName { get; set; }
            public string mouthClass { get; set; }
            public Eye eye { get; set; }
            public Eyebrow eyebrow { get; set; }
            public Mouth mouth { get; set; }
        }

        /// <summary>
        /// ぬいぐるみの設定テキストから画像を作成して保存する関数
        /// </summary>
        /// <param name="text">設定のテキスト</param>
        /// <param name="savepath">保存先のパス</param>
        public void CreateNuiImage(string text, string savepath)
        {
            // TODO:パーツの位置情報の設定
            //透過だしpngよさげ？
            //セッティングクラスに変換
            NuiSetting nsetting = JsonConvert.DeserializeObject<NuiSetting>(text);
            Console.WriteLine(nsetting);
            //セッティングクラスから画像を生成

            //肌画像を作成
            Bitmap face = new Bitmap(512, 512);
            using (var gfx = Graphics.FromImage(face))
            {
                gfx.FillRectangle(
                    new SolidBrush(ColorTranslator.FromHtml(colorDictionary[nsetting.changeSkinClass])),//navy
                    new Rectangle(0, 0, face.Width, face.Height));

                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                gfx.SmoothingMode = SmoothingMode.HighQuality;
            }
            face.Save(savepath + @"\face.png");
            //face.Dispose();
            //眉をセット
            Bitmap lefteyebrow = new Bitmap(server + @"\img\" + nsetting.leftEyebrowFileName + ".png");
            Bitmap righteyebrow = new Bitmap(server + @"\img\" + nsetting.rightEyebrowFileName + ".png");
            using (var gfx = Graphics.FromImage(face))
            {
                gfx.DrawImage(lefteyebrow, 308 - nsetting.eyebrow.x, 60 + nsetting.eyebrow.y);
                gfx.DrawImage(righteyebrow, 48 + nsetting.eyebrow.x, 60 + nsetting.eyebrow.y);
            }
            face.Save(savepath + @"\eyeblow.png");
            lefteyebrow.Dispose();
            righteyebrow.Dispose();

            //瞳をセット
            Bitmap lefteye = new Bitmap(server + @"\img\" + nsetting.leftEyeFileName + "-0l-image.png");
            Bitmap rightey = new Bitmap(server + @"\img\" + nsetting.rightEyeFileName + "-0r-image.png");
            for (var i = 1; i < 7; i++)
            {
                Bitmap lparts = new Bitmap(server + @"\img\" + nsetting.leftEyeFileName + "-" + i + "l.png");
                Bitmap rparts = new Bitmap(server + @"\img\" + nsetting.leftEyeFileName + "-" + i + "r.png");

                if (i >= 2 && i < 6)
                {
                    FillImage(lparts, nsetting.GetType().GetProperty("leftEyeClass" + i).GetValue(nsetting).ToString());
                    FillImage(rparts, nsetting.GetType().GetProperty("rightEyeClass" + i).GetValue(nsetting).ToString());
                }
                    

                using (var gfx = Graphics.FromImage(face))
                {
                    gfx.DrawImage(lparts, 308 - nsetting.eye.x, 130 + nsetting.eye.y);
                    gfx.DrawImage(rparts, 48 + nsetting.eye.x, 130 + nsetting.eye.y);
                }

                lparts.Dispose();
                rparts.Dispose();
            }
            face.Save(savepath + @"\eye.png");

            //口をセット
            Bitmap mouth = new Bitmap(server + @"\img\" + nsetting.mouthFileName + ".png");
            using (var gfx = Graphics.FromImage(face))
            {
                gfx.DrawImage(mouth, 178 + nsetting.mouth.x, 256 + nsetting.mouth.y);
            }
            face.Save(savepath + @"\mouth.png");
            mouth.Dispose();

            //画像をフォルダ配下に置く

            face.Dispose();
        }


        public Bitmap FillImage(Bitmap bit, string colorindex)
        {
            using (var gfx = Graphics.FromImage(bit))
            {
                Color color = ColorTranslator.FromHtml(colorDictionary[colorindex]);
                Console.WriteLine(ColorTranslator.FromHtml(colorDictionary[colorindex]));
                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                gfx.SmoothingMode = SmoothingMode.HighQuality;

                for (int y = 0; y < bit.Height; y++)
                {
                    for (int x = 0; x < bit.Width; x++)
                    {
                        Color pixelColor = bit.GetPixel(x, y);

                        // 透明でない部分を黒で塗りつぶす
                        if (pixelColor.A > 0)
                        {
                            bit.SetPixel(x, y, color);
                        }
                    }
                }
            }

            return bit;
        }


        Dictionary<string, string> colorDictionary = new Dictionary<string, string>
        {
            { "color-06", "#c8303e" },
            { "color-07", "#5d131e" },
            { "color-1", "#dac0cb" },
            { "color-1000", "#e9004e" },
            { "color-1007", "#601119" },
            { "color-1008", "#3a0818" },
            { "color-1012", "#76402b" },
            { "color-1061", "#2a1e63" },
            { "color-1084", "#ece5d5" },
            { "color-1115", "#004ad7" },
            { "color-114", "#005fcf" },
            { "color-1154", "#00a2d3" },
            { "color-119", "#e1955f" },
            { "color-12", "#824311" },
            { "color-120", "#b3622e" },
            { "color-1222", "#5c66bf" },
            { "color-126", "#6e6c87" },
            { "color-14", "#5e3f3e" },
            { "color-145", "#f76422" },
            { "color-146", "#f23a11" },
            { "color-15", "#241b1b" },
            { "color-162", "#165121" },
            { "color-167", "#c2a288" },
            { "color-174", "#6f8a00" },
            { "color-175", "#ba9698" },
            { "color-176", "#c5abb0" },
            { "color-189", "#bf6268" },
            { "color-207", "#a2dd88" },
            { "color-209", "#64d225" },
            { "color-22", "#58b400" },
            { "color-233", "#c65180" },
            { "color-24", "#009433" },
            { "color-3", "#eca3c5" },
            { "color-417", "#f8eed7" },
            { "color-441", "#a995d3" },
            { "color-47", "#e8aecd" },
            { "color-480", "#091825" },
            { "color-548", "#ed6672" },
            { "color-549", "#fb7d8b" },
            { "color-553", "#e8ca6e" },
            { "color-56", "#bd96c8" },
            { "color-58", "#8d4aaf" },
            { "color-59", "#723a9e" },
            { "color-596", "#4c5e67" },
            { "color-60", "#31084d" },
            { "color-62", "#d0d5dd" },
            { "color-63", "#cae2f1" },
            { "color-632", "#7cdaea" },
            { "color-64", "#99c8f0" },
            { "color-664", "#7a6a53" },
            { "color-665", "#6f5a41" },
            { "color-666", "#887669" },
            { "color-676", "#bdbbb8" },
            { "color-678", "#b5b6bc" },
            { "color-69", "#eae98d" },
            { "color-693", "#ffc4c2" },
            { "color-70", "#e9e153" },
            { "color-74", "#ffc900" },
            { "color-75", "#f8660e" },
            { "color-92", "#00aebe" },
            { "color-SB", "#181717" },
            { "color-SW", "#e3e2e2" },
            { "color-m-0028", "#fcf2d6" },
            { "color-m-0048", "#ffd7ac" },
            { "color-m-0728", "#9e6b3c" },
            { "color-m-2050", "#f5dece" },
            { "color-n-0048", "#e8c29d" },
            { "color-n-1475", "#f1d5c3" },
            { "color-n-4725", "#c1a79c" },
            { "color-n-7509", "#be9061" },
            { "color-s-0028", "#f0e0ca" },
            { "color-s-0036", "#a46645" },
            { "color-s-0048", "#f3d0b1" },
            { "color-s-0050", "#ffe6d5" },
            { "color-s-0054", "#f8f6da" },
            { "color-s-0728", "#e9be90" },
            { "color-s-4655", "#e1c3a6" },
        };

    }
}
