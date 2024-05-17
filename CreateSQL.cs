using Core2.Items;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Core2.Common
{
    internal class CreateSQL
    {
        public CreateSQL() { }

        /// <summary>
        /// セレクト文を作成する関数
        /// </summary>
        /// <param name="command"></param>
        /// <param name="param"></param>
        /// <param name="tablename"></param>
        /// <returns></returns>
        public OleDbCommand CreateSelectSQL(OleDbCommand command, string tablename, List<string> columnlist, List<ParametersSQL> param = null, string ordertext = "")
        {
            if (param == null)
            {
                command.CommandText += "SELECT * FROM " + tablename;
                return command;
            }
            else
            {
                //バインドするためのテキストを準備
                string paramstr = "";
                string columnstr = "";

                foreach (var item in param)
                {
                    paramstr += item.CreateText() + " " + item.conjunction;
                }

                foreach (var item in columnlist)
                {
                    columnstr += item + ",";
                }

                columnstr = columnstr.TrimEnd(',');
                paramstr = paramstr.Substring(0, paramstr.Length - 3);

                command.CommandText += @"SELECT " + columnstr + " FROM " + tablename + " WHERE " + paramstr + ordertext;

                foreach (var item in param)
                {
                    item.BindParam(command);
                }
                return command;
            }

        }

        /// <summary>
        /// データを入力したときのアップデート処理用のSQLを返す関数
        /// SQL内でカラムの検索を行い、ヒットしたときはPIdでアップデートを行う
        /// </summary>
        /// <param name="command"></param>
        /// <param name="columnlist"></param>
        /// <param name="param"></param>
        /// <param name="ordertext"></param>
        public OleDbCommand CreateSearchAndUpdateSQL(OleDbCommand command, List<string> lists, List<ParametersSQL> param)
        {

            string sql = "IF NOT EXISTS" +
                         " (SELECT * FROM Product" +
                         " WHERE " + lists[2] + " = ?" +
                         " AND PId = ? )" +
                         " UPDATE Product SET " + lists[2] + " = ? WHERE PId = ?" +
                         " ELSE " +
                         " SELECT '存在しない'";


            command.CommandText = sql;

            foreach (var item in param)
            {
                item.BindParam(command);
            }

            return command;
        }

        public string CreateInsertSQL(List<Object> items, List<string> columnnames, string tablename)
        {
            string sql = "INSERT INTO " + tablename + "(";
            //列名指定を作成
            foreach (string column in columnnames)
            {
                sql += column + ",";
            }

            sql = sql.TrimEnd(',');
            sql += ")VALUES";

            foreach (var item in items)
            {
                sql += "(";
                //列名から検索を実行してセットする値を取得
                foreach (string column in columnnames)
                {
                    PropertyInfo propertyinfo = item.GetType().GetProperty(column);
                    var value = Common.ValueCheckFunctions.CheckFromPropertyToStr(propertyinfo, item);
                    sql += value + ",";
                }

                sql = sql.TrimEnd(',');
                sql += "),";
            }

            sql = sql.TrimEnd(',');

            return sql;
        }

        public string CreateInsertOrUpdateSQL(List<Object> item, List<string> columnnames, string tablename,string sarchcolumn)
        {
            PrdItem prd = item[0] as PrdItem;
            string sarchinfo =  prd.GetType().GetProperty(sarchcolumn).GetValue(prd).ToString();
            PropertyInfo pi = typeof(PrdItem).GetProperty(sarchcolumn);

            if (pi.Equals(typeof(System.String)))
                sarchinfo = "'" + sarchinfo + "'";

            string sql = $"IF EXISTS (SELECT {sarchcolumn} FROM {tablename} WHERE {sarchcolumn} = '{sarchinfo}') ";

            sql += CreateUpdateSQL(item, columnnames, tablename,true);

            sql += " ELSE ";

            sql += CreateInsertSQL(item,columnnames,tablename);

            return sql;
        }

        public string CreateUpdateSQL(Object item, List<string> columnnames, string tablename)
        {
            string sql = "UPDATE " + tablename + " SET ";

            //列名から検索を実行してセットする値を取得
            foreach (string column in columnnames)
            {
                PropertyInfo propertyinfo = item.GetType().GetProperty(column);
                var value = Common.ValueCheckFunctions.CheckFromPropertyToStr(propertyinfo, item);
                sql += column + "=" + value + ",";
            }

            sql = sql.TrimEnd(',');

            PropertyInfo propertyinfo2 = item.GetType().GetProperty("PId");
            var value2 = Common.ValueCheckFunctions.CheckFromPropertyToStr(propertyinfo2, item);

            sql += "WHERE PId = " + value2;

            return sql;
        }
        public string CreateUpdateSQL(List<Object> items, List<string> columnnames, string tablename,bool iandu = false )
        {
            string sql = "UPDATE " + tablename + " SET ";
            foreach(var item in items)
            {
                //列名から検索を実行してセットする値を取得
                foreach (string column in columnnames)
                {
                    PropertyInfo propertyinfo = item.GetType().GetProperty(column);
                    var value = Common.ValueCheckFunctions.CheckFromPropertyToStr(propertyinfo, item);
                    sql += column + "=" + value + ",";
                }

                sql = sql.TrimEnd(',');
            }
            PropertyInfo propertyinfo2 = items[0].GetType().GetProperty("PId");
            PropertyInfo propertyinfo3 = items[0].GetType().GetProperty("shaddy_item_code");
            var value2 = Common.ValueCheckFunctions.CheckFromPropertyToStr(propertyinfo2, items[0]);
            var value3 = Common.ValueCheckFunctions.CheckFromPropertyToStr(propertyinfo3, items[0]);

            if (!iandu)
                sql += "WHERE PId = " + value2;
            else 
            {
                if (value3 == "")
                    sql = "select Code From Product";
                else
                    sql += "WHERE shaddy_item_code = " + value3;
            }
                

            return sql;
        }


        public void CreateDeleteSQL()
        {

        }

        public OleDbCommand ProductListSqlCheck(int cateid, string word, OleDbCommand command, string tablename, List<string> columnlist, List<ParametersSQL> param = null, string ordertext = "", string limittext = "", bool isall = false)
        {

            string sSqlNotShop = "(RakutenGReg=0 OR RakutenGReg IS NULL) AND (ColormeNReg=0 OR ColormeNReg IS NULL) AND (YahooGReg=0 OR YahooGReg IS NULL)";
            string sSqlRRegNobori = " RakutenGReg=1 AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3 OR PrdTypeId=5)";
            string sSQL = "SELECT * FROM Product ";
            bool isdefault = false;
            switch (cateid)
            {
                case 696 // 臨時
               :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE Exp1 LIKE '%スリムサイズ%' ORDER BY PVOrder";
                        break;
                    }

                //       case 716 // 箸休め
                //:
                //           {
                //               if (prd.Hashi)
                //               {
                //                   command.CommandText =  "";
                //                   for (var i = 0; i <= prd.LinkId.GetUpperBound(0); i++)
                //                       command.CommandText +=  ") OR (LinkId" + (i + 1) + "=" + cateid + ") OR (LinkBId" + (i + 1) + "=" + cateid;
                //                   command.CommandText =  "SELECT * FROM Product P WHERE  ((P.CateId=" + cateid + ") OR (P.CateBId=" + cateid + sSQL + ")) ORDER BY PVOrder, PRegDate DESC";
                //               }
                //               else
                //                   command.CommandText =  "SELECT * FROM Product WHERE  CateId=716 AND RakutenGReg=1";
                //               break;
                //           }

                case 1481 // 仮登録
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE temp_reg=1 AND (RakutenGReg=0 OR RakutenGReg IS NULL)";
                        command.CommandText += " ORDER BY PId DESC";
                        break;
                    }

                //       case 1429 // 売れてない商品
                //:
                //           {

                //               if (word != "")
                //                   dbPType.Fill("SELECT * FROM PrdType WHERE PTName LIKE '%" + word + "%'");
                //               if (dbPType.RowCount > 0)
                //               {
                //                   command.CommandText =  "SELECT * FROM Product WHERE NSCount<2 AND NSCount IS NOT NULL AND (";
                //                   for (var i = 0; i <= dbPType.RowCount - 1; i++)
                //                   {
                //                       if (i > 0)
                //                           command.CommandText +=  " OR ";
                //                       command.CommandText +=  "PrdTypeId=" + dbPType.RowsInt("PTId");
                //                       dbPType.NextRow();
                //                   }
                //                   command.CommandText +=  ") ORDER BY NSCount";
                //               }
                //               else
                //                   command.CommandText =  "SELECT * FROM Product WHERE NSCount<2 AND NSCount IS NOT NULL AND PrdTypeId=19 ORDER BY NSCount";
                //               break;
                //           }

                case 1436 // のぼり屋臨時
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE PrdTypeId=128 AND (Exp2='' OR Exp2 IS NULL)";
                        command.CommandText += " ORDER BY PId DESC";
                        break;
                    }

                case 1432 // のぼり屋工房未登録 1437はNKB保留 593は倉庫の奥深く
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE (RakutenGReg=0 OR RakutenGReg IS NULL)  AND SupplierId=5 AND (Bamzai=0 OR Bamzai IS NULL) AND CateId<>1437 AND CateId<>593 AND cateId<>1462";
                        command.CommandText += " ORDER BY CASE CateId WHEN 1432 THEN 1 ELSE 2 END, PId";
                        break;
                    }

                case 1444 // のぼり屋工房未登録2
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE (RakutenGReg=0 OR RakutenGReg IS NULL) AND (PrdTypeId=129 OR PrdTypeId=130 OR PrdTypeId=131)";
                        command.CommandText += " ORDER BY PId";
                        break;
                    }

                case 1442:
                    {
                        command.CommandText = "SELECT * FROM Product WHERE SupplierId=5 AND (Bamzai=0 OR Bamzai IS NULL) AND CateId<>1437 AND CateId<>593";
                        command.CommandText += " ORDER BY PId";
                        break;
                    }

                case 1435 // のぼり屋工房新着
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE PrdTypeId=128";
                        command.CommandText += " ORDER BY PId DESC";
                        break;
                    }

                case 1437 // のぼり屋工房保留
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE CateId=1437 AND PrdTypeId<>129 AND PrdTypeId<>130 AND PrdTypeId<>131";
                        command.CommandText += " ORDER BY PId";
                        break;
                    }

                case 1438 // アーティック未登録
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE (RakutenGReg=0 OR RakutenGReg IS NULL)  AND SupplierId=12";
                        command.CommandText += " ORDER BY PId";
                        break;
                    }

                case 1440 // JANコードなし
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE (JAN='' OR JAN IS NULL) AND PrdTypeId<>1 AND PrdTypeId<>2 AND PrdTypeId<>3 AND PrdTypeId<>5 AND PrdTypeId<>6 AND PrdTypeId<>15 AND PrdTypeId<>21 AND PrdTypeId<>22 AND PrdTypeId<>23 AND PrdTypeId<>24 AND PrdTypeId<>125";
                        command.CommandText += " ORDER BY PId DESC";
                        break;
                    }

                case 1383 // 最近登録
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE PRegDate<='" + DateTime.Now.ToString("yyyy/MM/dd 23:59:59") + "' AND PRegDate>='" + DateTime.Now.AddDays(-3).ToString("yyyy/MM/dd 00:00:00") + "' ORDER BY PRegDate DESC";
                        break;
                    }

                case 691 // 著作未チェック
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE EtcFlag6=0 AND RakutenGReg=1  AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3 OR PrdTypeId=5) AND CreaterId<>12 AND CreaterId<>28";
                        command.CommandText += " ORDER BY PVOrder";
                        break;
                    }

                case 692 // 臨時非表示
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE ExtraHide=1 AND RakutenGReg=1  AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3 OR PrdTypeId=5) AND CreaterId<>12";
                        command.CommandText += " ORDER BY PVOrder";
                        break;
                    }

                case 1467 // デザイナー名フィルタ
         :
                    {
                        if (word == "")
                            command.CommandText = "SELECT  10 *  FROM Product ORDER BY PRegDate DESC";
                        else
                        {
                            //DB dbCre = new DB();
                            //command.CommandText = "SELECT * FROM Creaters WHERE NickName LIKE '%" + word + "%' OR LName LIKE '%" + word + "%'";
                            //if (dbCre.RowCount == 0)
                            //    command.CommandText = "SELECT  10 *  FROM Product ORDER BY PRegDate DESC";
                            //else
                            //{
                            //    command.CommandText = "SELECT * FROM Product WHERE ";
                            //    for (var i = 1; i <= dbCre.RowCount; i++)
                            //    {
                            //        if (i > 1)
                            //            command.CommandText += "OR";
                            //        command.CommandText += " CreaterId=" + dbCre.RowsInt("CreaterId");
                            //        dbCre.NextRow();
                            //    }
                            //    command.CommandText += " ORDER BY PRegDate DESC";
                            //}
                        }
                    }
                    break;

                //               break;
                //           }

                //       case 694 // フィルタ
                //:
                //           {
                //               if (word == "")
                //                   command.CommandText =  "SELECT  100 *  FROM Product WHERE SName LIKE '%" + word + "%' OR Exp1 LIKE '%" + word + "%' ORDER BY PVOrder";
                //               else
                //               {
                //                   DB dbCate = new DB();
                //                   dbCate.Fill("SELECT * FROM Category WHERE CName LIKE '%" + word + "%'");
                //                   if (dbCate.RowCount == 0)
                //                       command.CommandText =  "SELECT * FROM Product WHERE SName LIKE '%" + word + "%' OR Exp1 LIKE '%" + word + "%' ORDER BY PVOrder";
                //                   else
                //                   {
                //                       command.CommandText =  "SELECT * FROM Product WHERE (SName LIKE '%" + word + "%' OR Exp1 LIKE '%" + word + "%'";
                //                       for (var i = 1; i <= dbCate.RowCount; i++)
                //                       {
                //                           command.CommandText +=  " OR CateId=" + dbCate.RowsInt("CateId");
                //                           for (var j = 1; j <= 15; j++)
                //                               command.CommandText +=  " OR LinkId" + j + "=" + dbCate.RowsInt("CateId");
                //                           dbCate.NextRow();
                //                       }
                //                       // 除外カテゴリ
                //                       //if (exclusionCateId > 0)
                //                       //{
                //                       //    command.CommandText +=  ") AND (CateId<>" + exclusionCateId;
                //                       //    for (var i = 1; i <= 15; i++)
                //                       //        command.CommandText +=  " AND LinkId" + i + "<>" + exclusionCateId;
                //                       //}
                //                       command.CommandText +=  ") ORDER BY PVOrder";
                //                   }
                //               }

                //               break;
                //           }

                case 108 // 型番入れたになってない
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE RakutenGReg=1 AND InputCode=0  AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3 OR PrdTypeId=5) ORDER BY PVOrder";
                        break;
                    }

                case 942 // 楽天RPP除外商品
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE AdExclusion=1 AND RakutenGReg=1 AND (PrdTypeId =0 OR PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3 OR PrdTypeId=5 OR PrdTypeId=22 OR PrdTypeId=23) ORDER BY PVOrder";
                        break;
                    }

                case 872 // 作業中　データ未チェック
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE EtcFlag7=0 AND RakutenGReg=1 AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3 OR PrdTypeId=5) ORDER BY PVOrder, PRegDate DESC";
                        break;
                    }

                case 901 // 楽天外国未登録
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE (RegForeign=0 OR RegForeign IS NULL) AND (PrdTypeId=1 OR PrdTypeId=5) ORDER BY PVOrder, PRegDate DESC";
                        break;
                    }

                case 950 // のぼりトップ
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE RakutenGReg=1 AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3 OR PrdTypeId=5) ORDER BY NSNum DESC";
                        break;
                    }

                case 947 // 商品名未修正
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE " + sSqlRRegNobori + " AND (Exp2='' OR Exp2 IS NULL) ORDER BY PVOrder";
                        break;
                    }

                case 948 // 商品名未チェック
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE RakutenGReg=1 AND (Exp2='' OR Exp2 IS NULL) AND  (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3 OR PrdTypeId=5) ORDER BY LEN(PName) DESC";
                        break;
                    }

                case 900 // 
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3 OR PrdTypeId=5) AND RakutenGReg=1 AND ColormeNReg=0 ORDER BY PRegDate DESC";
                        break;
                    }

                case 404 // 画像未作成
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE ThumbImg=0 AND InputCode=1 AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3 OR PrdTypeId=5 OR PrdTypeId=15) ORDER BY ThumbImg, PVOrder";
                        break;
                    }

                case 405 // 全在庫商品
         :
                    {
                        command.CommandText = "SELECT * FROM Product P, Stock S WHERE HaveStock=1 AND P.Code = S.Code ORDER BY PLastDate DESC";
                        break;
                    }

                case 434 // 楽天未登録
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE RakutenGReg=0 AND InputCode=1 AND ImageUped=1 AND MakedImg=1 AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) OR (CateId=434) ORDER BY PVOrder, PRegDate DESC";
                        command.CommandText = "SELECT * FROM Product WHERE RakutenGReg=0 AND InputCode=1 AND ImageUped=1 AND MakedImg=1 ORDER BY PVOrder, PRegDate DESC";
                        break;
                    }

                case 715 // 楽天未更新
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE RakutenGReg=1 AND RakutenGUp=0 AND ImageUped=1 AND MakedImg=1 AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) ORDER BY PVOrder";
                        break;
                    }

                case 167 // のぼり源未登録
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE RakutenGReg=1 AND ColormeNReg=0 AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) ORDER BY PVOrder, PRegDate DESC";
                        break;
                    }

                case 235 // アマゾン未登録
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE AmazonNReg=0 AND PrdTypeId=1 ORDER BY PVOrder, PRegDate DESC";
                        break;
                    }

                case 437 // Yahoo!未登録
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE YahooGReg=0 AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) ORDER BY PVOrder, PRegDate DESC";
                        break;
                    }

                case 297 // 楽天のぼり在庫
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE RakutenStockFlg=1 AND PrdTypeId=1 ORDER BY PLastDate DESC";
                        break;
                    }

                case 78 // 3日以内に更新した商品
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE PLastDate>='" + DateTime.Now.AddDays(-3).ToString("yyyy/MM/dd 00:00:00") + "' AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) ORDER BY PLastDate";
                        break;
                    }

                case 314 // 1日以内に更新した商品
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE PLastDate>='" + DateTime.Now.AddDays(-1).ToString("yyyy/MM/dd 00:00:00") + "' AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) ORDER BY PLastDate";
                        break;
                    }

                case 225 // 最近更新した商品
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE PLastDate>='" + DateTime.Now.AddDays(-60).ToString("yyyy/MM/dd 00:00:00") + "' AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) ORDER BY PLastDate";
                        break;
                    }

                case 717 // 最近登録した商品
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE PRegDate>='" + DateTime.Now.AddMonths(-3).ToString("yyyy/MM/dd 00:00:00") + "' AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) ORDER BY PId DESC";
                        break;
                    }

                case 309 // 名入・値入 PrdTypeId
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE PrdTypeId=2 OR PrdTypeId=3 ORDER BY PVOrder, PRegDate DESC";
                        break;
                    }

                case 687 // 全のぼり旗
         :
                    {
                        command.CommandText = "SELECT * FROM Product P, Stock S WHERE (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3 OR PrdTypeId=5) AND (P.Code = S.Code) ORDER BY PVOrder, PRegDate DESC";
                        break;
                    }

                case 785 // デザイナー未登録
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE " + sSqlNotShop + " AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) ORDER BY PRegDate DESC";
                        break;
                    }

                case 1434 // 外部デザイナー
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE (CreaterId=33 OR CreaterId=34 OR CreaterId=18) ORDER BY PId DESC";
                        break;
                    }

                case 786:
                case 788:
                case 790:
                case 1445 // デザイナー未登録
         :
                    {
                        command.CommandText += "WHERE " + sSqlNotShop + " AND (ThumbImg=0 OR ThumbImg IS NULL) ~ユーザー~";
                        command.CommandText += " AND OnlyOne=1";
                        command.CommandText += " ORDER BY PRegDate DESC";
                        switch (cateid)
                        {
                            case 786 // 東江
                           :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=1 OR CateId=786)");
                                    break;
                                }

                            case 788 // 平松
                     :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=28 OR CateId=788)");
                                    break;
                                }

                            case 790 // 小塚
                     :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=12 OR CateId=790)");
                                    break;
                                }

                            case 1445 // 阿部
                     :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=41 OR CateId=1445)");
                                    break;
                                }
                        }

                        break;
                    }

                case 1017:
                case 1018:
                case 1019:
                case 1446 // 増産待ち
         :
                    {
                        command.CommandText += "WHERE " + sSqlNotShop + " AND (ThumbImg=0 OR ThumbImg IS NULL) AND InputCode=0 ~ユーザー~";
                        command.CommandText += " AND (OnlyOne=0 OR OnlyOne IS NULL)";
                        command.CommandText += " AND (SName IS NULL OR SName='' OR SName='新規' OR SName='のぼり旗')";
                        command.CommandText += " ORDER BY PRegDate DESC";
                        switch (cateid)
                        {
                            case 1017 // 東江
                           :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=1 OR CateId=786)");
                                    break;
                                }

                            case 1019 // 平松
                     :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=28 OR CateId=788)");
                                    break;
                                }

                            case 1018 // 小塚
                     :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=12 OR CateId=790 OR CateId=1018)");
                                    break;
                                }

                            case 1446 // 阿部
                     :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=41 OR CateId=1446)");
                                    break;
                                }
                        }

                        break;
                    }

                case 1020:
                case 1022:
                case 1021:
                case 1447 // 型番チェック済み
         :
                    {
                        command.CommandText += "WHERE " + sSqlNotShop + " AND InputCode=1 ~ユーザー~ ORDER BY PRegDate DESC";
                        switch (cateid)
                        {
                            case 1020 // 東江
                           :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=1 OR CateId=786)");
                                    break;
                                }

                            case 1022 // 平松
                     :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=28 OR CateId=788)");
                                    break;
                                }

                            case 1021 // 小塚
                     :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=12 OR CateId=790)");
                                    break;
                                }

                            case 1447 // 阿部
                     :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=41 OR CateId=1447)");
                                    break;
                                }
                        }

                        break;
                    }

                case 1014:
                case 1016:
                case 1015 // デザイナー型番未チェック
         :
                    {
                        command.CommandText += "WHERE " + sSqlNotShop + " AND (ThumbImg=0 OR ThumbImg IS NULL) AND InputCode=0 ~ユーザー~";
                        command.CommandText += " AND (SName<>'のぼり旗')";
                        command.CommandText += " ORDER BY PRegDate DESC";
                        switch (cateid)
                        {
                            case 1014 // 東江
                           :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=1 OR CateId=786)");
                                    break;
                                }

                            case 1016 // 平松
                     :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=28 OR CateId=788)");
                                    break;
                                }

                            case 1015 // 小塚
                     :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND (CreaterId=12 OR CateId=790)");
                                    break;
                                }
                        }

                        break;
                    }

                case 1377:
                case 1379 // 著作未チェック
         :
                    {
                        command.CommandText += "WHERE (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) ~ユーザー~";
                        command.CommandText += " AND (EtcFlag6=0 OR EtcFlag6 IS NULL) AND RakutenGReg=1";
                        command.CommandText += " ORDER BY PVOrder";
                        switch (cateid)
                        {
                            case 1379 // ひらまつ
                           :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND CreaterId=28");
                                    break;
                                }

                            case 1377 // 小塚
                     :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND CreaterId=12");
                                    break;
                                }
                        }

                        break;
                    }

                case 1380:
                case 1381 // 臨時非表示
         :
                    {
                        command.CommandText += "WHERE (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) ~ユーザー~";
                        command.CommandText += " AND ExtraHide=1 AND RakutenGReg=1";
                        command.CommandText += " ORDER BY PVOrder";
                        switch (cateid)
                        {
                            case 1381 // ひらまつ
                           :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND CreaterId=28");
                                    break;
                                }

                            case 1380 // 小塚
                     :
                                {
                                    command.CommandText = sSQL.Replace("~ユーザー~", "AND CreaterId=12");
                                    break;
                                }
                        }

                        break;
                    }

                case 958 // 東江 データ未チェック
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE EtcFlag7=0 AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) AND CreaterId<>12 AND CreaterId<>14 AND CreaterId<>28 AND CreaterId<>18 ORDER BY PVOrder, NSNum DESC";
                        break;
                    }

                case 959 // 東江 ランキング
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE RakutenGReg=1 AND CreaterId=1 ORDER BY NSNum DESC, NSCount DESC, PVOrder";
                        break;
                    }

                case 965 // 東江 新着
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE RakutenGReg=1 AND CreaterId=1 AND PRegDate>='" + DateTime.Now.AddMonths(-1).ToString("yyyy/MM/dd 00:00:00") + "'  ORDER BY NSNum DESC, NSCount DESC, PVOrder";
                        break;
                    }

                case 956 // 平松 ランキング
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE RakutenGReg=1 AND CreaterId=28 ORDER BY NSNum DESC, NSCount DESC, PVOrder";
                        break;
                    }

                case 952 // 平松 データ未チェック
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE EtcFlag7=0 AND CreaterId=28 ORDER BY NSNum DESC, NSCount DESC, PVOrder";
                        break;
                    }

                case 967 // 平松 新着
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE CreaterId=28 AND PRegDate>='" + DateTime.Now.AddMonths(-1).ToString("yyyy/MM/dd 00:00:00") + "'  ORDER BY NSNum DESC, NSCount DESC, PVOrder";
                        break;
                    }

                case 954 // 小塚 ランキング
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE RakutenGReg=1 AND CreaterId=12 ORDER BY NSNum DESC, NSCount DESC, PVOrder";
                        break;
                    }

                case 951 // 小塚 データ未チェック
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE EtcFlag7=0 AND CreaterId=12 ORDER BY NSNum DESC, NSCount DESC, PVOrder";
                        break;
                    }

                case 966 // 小塚 新着
         :
                    {

                        command.CommandText = "SELECT * FROM Product P WHERE RakutenGReg=1 AND CreaterId=12 AND PRegDate>='" + DateTime.Now.AddMonths(-1).ToString("yyyy/MM/dd 00:00:00") + "'  ORDER BY NSNum DESC, NSCount DESC, PVOrder";
                        break;
                    }

                case 962 // その他
         :
                    {
                        //test
                        isall = true;
                        command.CommandText = "SELECT * FROM Product WHERE Code In ('7A06','7N5W','7H0N','7A0W','74CN','7K13','7JLS','73RE','7J6U','7R50','73R2','7A0A','2KFA','75LC','73R7','7U0Y','78FL','74C5','7KNT','7AJC','75L8','7H1C','7A0H','78EU','28SR','7U07','7H7P','78E1','7H10','E593','78ER','7K1R','1PHW','EPJ8','TSNY','780A','7K1A','0NEE','7K12','7C33','7625','2H5R','73R3','7J6S','7K17','2H5U','73RF','7N0F','73RS','7HG6','7YF1','7YF0','73RG','7N5K','7KNX','73R9','7A05','TS50','7WL2','7N5J','7K7H','7K7F','7RH4','7CCJ','X48Y','GY7Y','73RY','78YK','7840','TTKF','0AW2','7X34','780J','TTK1','FPCN','7JH4','1GK9','E59K','73R8','3WYX','7K15','73RL','0AWE','E5RY','1G04','2UHT','7SWK','7K7C','7HY6','E59G','TRRR','7N50','7PFL','0AWT','07FX','T6CU','7K7U','7WWR','F4TS','256H')";
                        break;
                    }

                case 963 // その他 データ未チェック
        :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE EtcFlag7=0 AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) AND CreaterId<>32 AND CreaterId<>12 AND CreaterId<>28 AND CreaterId<>1 AND CreaterId<>18 ORDER BY NSNum DESC, NSCount DESC, PVOrder";
                        break;
                    }

                case 964 // その他 ランキング
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE RakutenGReg=1 AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) AND CreaterId<>32 AND CreaterId<>12 AND CreaterId<>28 AND CreaterId<>1 AND CreaterId<>18 AND CreaterId<>3 ORDER BY NSNum DESC, NSCount DESC, PVOrder";
                        break;
                    }

                case 720 // 楽天更新済み
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE RakutenGUp=1 AND ImageUped=1 AND MakedImg=1 AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) ORDER BY PVOrder";
                        break;
                    }

                case 721 // 未同梱
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE RakutenGReg=1 AND (EtcFlag4=0 OR EtcFlag4 IS NULL) AND (PrdTypeId=1 OR PrdTypeId=2 OR PrdTypeId=3) ORDER BY PVOrder";
                        break;
                    }

                case 709 // PrdTypeIdチェック
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE PrdTypeId=20 ORDER BY PVOrder, PRegDate DESC";
                        break;
                    }

                case 689 // 商品タイプごと　日付指定
         :
                    {
                        command.CommandText = "SELECT * FROM Product WHERE PLastDate>='2018/01/16' AND PLastDate<='2018/01/17' ORDER BY PLastDate";
                        break;
                    }

                case 828 // 偽NKのぼり
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE PrdTypeId=100 AND  (CateBId=0 OR CateBId IS NULL) ORDER BY PVOrder, PRegDate DESC";
                        break;
                    }

                case 780 // 迷子商品
         :
                    {
                        command.CommandText = "SELECT * FROM Product P WHERE CateId=0 OR Code IS NULL OR Code='' ORDER BY PVOrder, PRegDate DESC";
                        break;
                    }

                case 830 // 楽天SS商品
         :
                    {
                        break;
                    }
                case 9999:
                    {
                        command.CommandText = "SELECT * FROM Product Code In ('7A06','7N5W','7H0N','7A0W','74CN','7K13','7JLS','73RE','7J6U','7R50','73R2','7A0A','2KFA','75LC','73R7','7U0Y','78FL','74C5','7KNT','7AJC','75L8','7H1C','7A0H','78EU','28SR','7U07','7H7P','78E1','7H10','E593','78ER','7K1R','1PHW','EPJ8','TSNY','780A','7K1A','0NEE','7K12','7C33','7625','2H5R','73R3','7J6S','7K17','2H5U','73RF','7N0F','73RS','7HG6','7YF1','7YF0','73RG','7N5K','7KNX','73R9','7A05','TS50','7WL2','7N5J','7K7H','7K7F','7RH4','7CCJ','X48Y','GY7Y','73RY','78YK','7840','TTKF','0AW2','7X34','780J','TTK1','FPCN','7JH4','1GK9','E59K','73R8','3WYX','7K15','73RL','0AWE','E5RY','1G04','2UHT','7SWK','7K7C','7HY6','E59G','TRRR','7N50','7PFL','0AWT','07FX','T6CU','7K7U','7WWR','F4TS','256H')";
                        break;
                    }

                default:
                    {
                        isdefault = true;

                        if (isall)
                        {
                            command = CreateSelectSQL(command, "Product", columnlist, param, ordertext);
                        }
                        else
                        {
                            command = CreateSelectSQL(command, "Product", columnlist, param, ordertext);
                            command.CommandText = "WITH RecursiveCTE AS (" + command.CommandText + " " + limittext + " UNION ALL " +
                                                  "SELECT t.PId,t.Code,t.PName,t.SName,t.NSCount,t.NSNum,t.parent_code,t.parent_id,t.is_parent,t.PVOrder,t.PVOrder2 " +
                                                  "FROM Product t " +
                                                  "INNER JOIN RecursiveCTE r ON t.parent_id = r.PId)" +
                                                  "SELECT *,(SELECT COUNT(*) FROM RecursiveCTE) FROM RecursiveCTE";

                            //command.CommandText = "SELECT * FROM Product WHERE Code IN ('22W1','22WH','22WW','2F1K','35RN','3CFN','3RUP','4RY7','7119','731T','731U','742U','784J','78XX','7AH3','7AHJ','7GUJ','7LFP','7LFR','7N1G','7U11','7U7S','E40R','E4CL','E4JC','E4X9','E7PK','EA50','EA9E','EALG','EAPG','EAPK','EAUE','EAXJ','EEW9','EEWP','EEWR','EEWS','EJXK','EPJF','ES31','ESYE','FTGS','G7JR','TNXE','TTEG','TTEH','XF4W','XPFY','XT4E')";
                        }

                        break;
                    }
            }

            if (!isdefault && !isall)
            {
                command.CommandText += limittext;
            }

            return command;
        }
    }
}
