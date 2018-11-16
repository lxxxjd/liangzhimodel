using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using static HTMLEngine;

public partial class HTMLTable
{
    public const string strNullValue = "<null>";
    public const string strRowSpan = "rowspan";
    public const string strRowSpanValue = "<rowspan>";
    public const string strColSpan = "colspan";
    public const string strColSpanValue = "<colspan>";
    public int RowCount = 0;
    public int ColumnCount = 0;

    Dictionary<String, string> dict = new Dictionary<String, string>();

    public static List<String> GetTable(HtmlNode table, int TableId)
    {
        var tablecontentlist = new List<String>();
        var dict = new Dictionary<String, String>();

        //表格处理：
        foreach (var tablebody in table.ChildNodes)
        {
            //整理出最大行列数
            int MaxRow = 0;
            int MaxColumn = 0;

            foreach (var tableRows in tablebody.ChildNodes)
            {
                if (tableRows.ChildNodes.Count != 0)
                {
                    int xc = 0;
                    foreach (var tableData in tableRows.ChildNodes)
                    {
                        if (tableData.Name == "td")
                        {
                            if (tableData.Attributes[strColSpan] != null)
                            {
                                xc += int.Parse(tableData.Attributes[strColSpan].Value);
                            }
                            else
                            {
                                xc++;
                            }
                        }
                    }
                    if (xc > MaxColumn) MaxColumn = xc;
                    MaxRow++;
                }
            }


            //准备Cell内容字典
            for (int Row = 1; Row < MaxRow + 1; Row++)
            {
                for (int Col = 1; Col < MaxColumn + 1; Col++)
                {
                    dict.Add(Row + "," + Col, String.Empty);
                }
            }

            int CurrentRow = 1;
            int NextNeedToFillColumn = 1;

            foreach (var tableRows in tablebody.ChildNodes)
            {
                if (tableRows.ChildNodes.Count != 0)
                {
                    foreach (var tableData in tableRows.ChildNodes)
                    {
                        //对于#text的过滤
                        if (tableData.Name == "td")
                        {

                            //寻找该行下一个需要填充的格子的列号
                            for (int Col = 1; Col < MaxColumn + 1; Col++)
                            {
                                if (dict[CurrentRow + "," + Col] == String.Empty)
                                {
                                    NextNeedToFillColumn = Col;
                                    break;
                                }
                            }

                            var cellvalue = Normalizer.Normalize(tableData.InnerText);
                            var cellpos = CurrentRow + "," + NextNeedToFillColumn;
                            if (cellvalue == String.Empty)
                            {
                                cellvalue = strNullValue;
                            }
                            dict[CurrentRow + "," + NextNeedToFillColumn] = cellvalue;
                            if (tableData.Attributes[strRowSpan] != null)
                            {
                                //具有RowSpan特性的情况
                                for (int i = 1; i < int.Parse(tableData.Attributes[HTMLTable.strRowSpan].Value); i++)
                                {
                                    dict[(CurrentRow + i) + "," + NextNeedToFillColumn] = strRowSpanValue;
                                }
                            }
                            if (tableData.Attributes[strColSpan] != null)
                            {
                                //具有ColSpan特性的情况
                                for (int i = 1; i < int.Parse(tableData.Attributes[strColSpan].Value); i++)
                                {
                                    dict[CurrentRow + "," + (NextNeedToFillColumn + i)] = strColSpanValue;
                                }
                            }
                        }
                    }
                    CurrentRow++;
                }
            }
        }

        //表格分页的修正
        var NeedToModify = String.Empty;
        foreach (var item in dict)
        {
            if (item.Value == strNullValue)
            {
                var Row = int.Parse(item.Key.Split(",")[0]) - 1;
                var Column = item.Key.Split(",")[1];
                if (Row == 0) continue;
                if (dict[Row + "," + Column] == strRowSpanValue)
                {
                    NeedToModify = item.Key;
                }
            }
        }

        if (NeedToModify != String.Empty) dict[NeedToModify] = strRowSpanValue;

        foreach (var item in dict)
        {
            tablecontentlist.Add(TableId + "," + item.Key + "|" + item.Value);
        }

        return tablecontentlist;
    }


    public HTMLTable(List<String> TableContent)
    {
        for (int i = 0; i < TableContent.Count; i++)
        {
            //Table,Row,Column|Keyword
            var pos = TableContent[i].Split("|")[0];
            var value = TableContent[i].Split("|")[1];

            var RowPos = int.Parse(pos.Split(",")[1]);
            var ColumnPos = int.Parse(pos.Split(",")[2]);

            if (RowCount < RowPos) RowCount = RowPos;
            if (ColumnCount < ColumnPos) ColumnCount = ColumnPos;
            dict.Add(RowPos + "," + ColumnPos, value);
        }
    }

    public string CellValue(int RowPos, int ColPos)
    {
        var pos = RowPos + "," + ColPos;
        if (dict.ContainsKey(pos))
        {
            var content = dict[pos];
            if (content == strRowSpanValue)
            {
                //向上寻找非<rowspan>的内容
                for (int i = RowPos - 1; i >= 0; i--)
                {
                    pos = i + "," + ColPos;
                    if (dict.ContainsKey(pos))
                    {
                        content = dict[pos];
                        if (content != strRowSpanValue) return content;
                    }
                }
            }

            if (content == strColSpanValue)
            {
                //向上寻找非<colspan>的内容
                for (int i = ColPos - 1; i >= 0; i--)
                {
                    pos = RowPos + "," + i;
                    if (dict.ContainsKey(pos))
                    {
                        content = dict[pos];
                        if (content != strColSpanValue) return content;
                    }
                }
            }
            if (!dict.ContainsKey(pos))
            {
                //Console.WriteLine("Error!!!Position Not Found:" + pos);
            }
            else
            {
                return dict[pos];
            }
        }
        return String.Empty;
    }

    public String[] GetHeaderRow(int RowNo = 1)
    {
        var Header = new String[ColumnCount];
        for (int i = 1; i < ColumnCount + 1; i++)
        {
            Header[i - 1] = CellValue(RowNo, i);
        }
        return Header;
    }

    public bool IsTotalRow(int RowNo)
    {
        bool IsTotalRow = false;
        for (int i = 1; i <= ColumnCount; i++)
        {
            var x = CellValue(RowNo, i).Replace(" ", String.Empty);
            if (x.Contains("合计") || x.Contains("小计") || x.Contains("总计") ||
                x == "—" || x == "－" || x == "-" || x == "/" ||
                x == "--" || x == "——")
            {
                IsTotalRow = true;
                break;
            }
        }

        if (!IsTotalRow)
        {
            int RowSpanCnt = 0;
            for (int ColNo = 1; ColNo <= ColumnCount; ColNo++)
            {
                var pos = RowNo + "," + ColNo;
                if (!dict.ContainsKey(pos)) return false;
                if (dict[pos] == strRowSpanValue || dict[pos] == strColSpanValue)
                {
                    RowSpanCnt++;
                }
            }
            if (RowSpanCnt == ColumnCount - 1) IsTotalRow = true;
        }
        return IsTotalRow;
    }


    /// <summary>
    /// /// 分页表格的修复
    /// </summary>
    /// <param name="root"></param>
    public static void FixSpiltTable(MyRootHtmlNode root, AnnouceDocument doc)
    {

        for (int NextTableId = 2; NextTableId <= doc.root.TableList.Count; NextTableId++)
        {
            foreach (var item in doc.root.TableList[NextTableId])
            {
                var FirstTablePos = -1;
                var SecondTablePos = -1;
                foreach (var p in root.Children)
                {
                    foreach (var s in p.Children)
                    {
                        if (s.TableId == NextTableId - 1) FirstTablePos = s.PositionId;
                        if (s.TableId == NextTableId) SecondTablePos = s.PositionId;
                    }
                }

                if (SecondTablePos - FirstTablePos > 200) continue;

                var tablerec = item.Split("|");
                var pos = tablerec[0].Split(",");
                var value = tablerec[1];
                var row = int.Parse(pos[1]);
                //第二张表，第一行存在NULL
                if (row == 1 && value == strNullValue)
                {
                    var table = new HTMLTable(doc.root.TableList[NextTableId - 1]);
                    var nexttable = new HTMLTable(doc.root.TableList[NextTableId]);
                    if (table.ColumnCount != nexttable.ColumnCount) continue;
                    //合并表
                    var offset = table.RowCount;
                    //修改第二张表格的数据
                    foreach (var Nextitem in root.TableList[NextTableId])
                    {
                        tablerec = Nextitem.Split("|");
                        pos = tablerec[0].Split(",");
                        value = tablerec[1];
                        var newtablerec = (NextTableId - 1) + "," + (offset + int.Parse(pos[1])) + "," + pos[2] + "|" + value;
                        root.TableList[NextTableId - 1].Add(newtablerec);
                    }
                    root.TableList[NextTableId].Clear();
                    for (int i = 0; i < root.Children.Count; i++)
                    {
                        for (int j = 0; j < root.Children[i].Children.Count; j++)
                        {
                            var node = root.Children[i].Children[j];
                            if (node.TableId == NextTableId) node.TableId = -1;
                        }
                    }
                    break;
                }
            }
        }

        //1.是否存在连续表格 NextBrother
        for (int i = 0; i < root.Children.Count; i++)
        {
            for (int j = 0; j < root.Children[i].Children.Count; j++)
            {
                var node = root.Children[i].Children[j];
                if (node.TableId != -1)
                {
                    if (node.NextBrother != null)
                    {
                        if (node.NextBrother.TableId != -1)
                        {
                            var nextnode = node.NextBrother;
                            var table = new HTMLTable(root.TableList[node.TableId]);
                            var nexttable = new HTMLTable(root.TableList[nextnode.TableId]);
                            //Console.WriteLine("First  Table:" + table.RowCount + "X" + table.ColumnCount);
                            //Console.WriteLine("Second Table:" + nexttable.RowCount + "X" + nexttable.ColumnCount);
                            if (table.ColumnCount != nexttable.ColumnCount) continue;
                            //Console.WriteLine("Two Tables Has Same Column Count!");
                            //2.连续表格的后一个，往往是有<NULL>的行
                            bool hasnull = false;
                            for (int nullcell = 1; nullcell <= table.ColumnCount; nullcell++)
                            {
                                if (nexttable.CellValue(1, nullcell) == HTMLTable.strNullValue)
                                {
                                    hasnull = true;
                                    break;
                                }
                            }

                            var ComboCompanyName = "";
                            var ComboCompanyNameColumnNo = -1;
                            var CompanyFullNameList = doc.companynamelist.Select((x) => { return x.secFullName; }).Distinct().ToList();
                            //两表同列的元素，是否有能够合并成为公司名称的？注意，需要去除空格！！
                            int MaxColumn = table.ColumnCount;
                            for (int col = 1; col <= MaxColumn; col++)
                            {
                                int TableAMaxRow = table.RowCount;
                                int TableBMaxRow = nexttable.RowCount;
                                for (int RowCntA = 1; RowCntA < TableAMaxRow; RowCntA++)
                                {
                                    for (int RowCntB = 1; RowCntB < TableBMaxRow; RowCntB++)
                                    {
                                        var valueA = table.CellValue(RowCntA, col).Replace(" ", "");
                                        var valueB = nexttable.CellValue(RowCntB, col).Replace(" ", "");
                                        if (valueA != "" && valueB != "")
                                        {
                                            var value = valueA + valueB;
                                            if (CompanyFullNameList.Contains(value))
                                            {
                                                ComboCompanyName = value;
                                                ComboCompanyNameColumnNo = col;
                                                //Console.WriteLine("Found FullName:" + value);
                                                break;
                                            }
                                        }
                                    }
                                    if (ComboCompanyNameColumnNo != -1) break;
                                }
                                if (ComboCompanyNameColumnNo != -1) break;
                            }
                            if (ComboCompanyNameColumnNo != -1)
                            {
                                //补完:注意，不能全部补！！A表以公司名开头，B表以公司名结尾
                                for (int k = 0; k < root.TableList[node.TableId].Count; k++)
                                {
                                    var tablerec = root.TableList[node.TableId][k].Split("|");
                                    var value = tablerec[1].Replace(" ", "");
                                    //A表以公司名开头
                                    if (ComboCompanyName.StartsWith(value))
                                    {
                                        root.TableList[node.TableId][k] = tablerec[0] + "|" + ComboCompanyName;
                                    }
                                }
                                for (int k = 0; k < root.TableList[nextnode.TableId].Count; k++)
                                {
                                    var tablerec = root.TableList[nextnode.TableId][k].Split("|");
                                    var value = tablerec[1].Replace(" ", "");
                                    //A表以公司名开头
                                    if (ComboCompanyName.EndsWith(value))
                                    {
                                        root.TableList[nextnode.TableId][k] = tablerec[0] + "|" + ComboCompanyName;
                                    }
                                }
                            }


                            //特殊业务处理:增减持
                            bool specaillogic = false;
                            var BuyMethod = new string[]{"集中竞价交易","竞价交易","大宗交易","约定式购回"}.ToList();
                            if (doc.GetType() == typeof(StockChange))
                            {
                                //增减持无表头的特殊处理
                                for (int spCell = 1; spCell <= table.ColumnCount; spCell++)
                                {
                                    if (BuyMethod.Contains(nexttable.CellValue(1, spCell)))
                                    {
                                        specaillogic = true;
                                        break;
                                    }
                                }
                            }

                            if (hasnull || ComboCompanyNameColumnNo != -1 || specaillogic)
                            {
                                var offset = table.RowCount;
                                //修改第二张表格的数据
                                foreach (var item in root.TableList[nextnode.TableId])
                                {
                                    var tablerec = item.Split("|");
                                    var pos = tablerec[0].Split(",");
                                    var value = tablerec[1];
                                    var newtablerec = node.TableId + "," + (offset + int.Parse(pos[1])) + "," + pos[2] + "|" + value;
                                    root.TableList[node.TableId].Add(newtablerec);
                                }
                                root.TableList[nextnode.TableId].Clear();
                                nextnode.TableId = -1;
                                //Console.WriteLine("Found Split Tables!!");
                            }
                        }
                    }
                }
            }
        }
    }

    public static void FixNullValue(MyRootHtmlNode root, AnnouceDocument doc)
    {
        var CompanyFullNameList = doc.companynamelist.Select((x) => { return x.secFullName; }).Distinct().ToList();
        var CompanyShortNameList = doc.companynamelist.Select((x) => { return x.secShortName; }).Distinct().ToList();
        for (int tableId = 1; tableId <= root.TableList.Count; tableId++)
        {
            var table = root.TableList[tableId];
            for (int checkItemIdx = 0; checkItemIdx < table.Count; checkItemIdx++)
            {
                var tablerec = table[checkItemIdx].Split("|");
                var pos = tablerec[0].Split(",");
                var value = tablerec[1].Replace(" ", "");
                var col = int.Parse(pos[2]);
                if (CompanyFullNameList.Contains(value) || CompanyShortNameList.Contains(value))
                {
                    for (int fixIdx = 0; fixIdx < table.Count; fixIdx++)
                    {
                        var nullvalue = table[fixIdx].Split("|")[1];
                        var nullcol = int.Parse(table[fixIdx].Split("|")[0].Split(",")[2]);
                        if (nullvalue.Equals(strNullValue) && col == nullcol)
                        {
                            table[fixIdx] = table[fixIdx].Split("|")[0] + "|" + value;
                        }
                    }
                }
            }
        }

        for (int tableId = 1; tableId <= root.TableList.Count; tableId++)
        {
            var table = root.TableList[tableId];
            for (int checkItemIdx = 0; checkItemIdx < table.Count; checkItemIdx++)
            {
                var tablerec = table[checkItemIdx].Split("|");
                var pos = tablerec[0].Split(",");
                var value = tablerec[1].Replace(" ", "");
                var row = int.Parse(pos[1]);
                var col = int.Parse(pos[2]);
                if (value == strNullValue && row != 1)
                {
                    //上一行是RowSpan，或者下一行是RowSpan，则这行也是RowSpan
                    var pre = tableId.ToString() + "," + (row - 1).ToString() + "," + col.ToString() + "|" + strRowSpanValue;
                    if (table.Contains(pre))
                    {
                        table[checkItemIdx] = tablerec[0] + "|" + strRowSpanValue;
                    }
                    else
                    {
                        var next = tableId.ToString() + "," + (row + 1).ToString() + "," + col.ToString() + "|" + strRowSpanValue;
                        if (table.Contains(next))
                        {
                            table[checkItemIdx] = tablerec[0] + "|" + strRowSpanValue;
                        }
                    }
                }
            }
        }

    }

}