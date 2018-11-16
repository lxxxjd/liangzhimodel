using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using static HTMLEngine;

public partial class HTMLTable
{
    public struct CellInfo
    {
        public int TableId;

        public int Row;

        public int Column;

        public string RawData;

        public string Title;
    }

    /// <summary>
    /// 表抽取规则
    /// </summary>
    public struct TableSearchRule
    {
        public string Name;
        /// <summary>
        /// 父标题
        /// </summary>
        public List<String> SuperTitle;
        /// <summary>
        /// 是否必须一致
        /// </summary>
        public bool IsSuperTitleEq;
        /// <summary>
        /// 标题
        /// </summary>
        public List<String> Title;
        /// <summary>
        /// 是否必须一致
        /// </summary>
        public bool IsTitleEq;
        /// <summary>
        /// 是否必须
        /// </summary>
        public bool IsRequire;
        /// <summary>
        /// 表标题不能包含的文字
        /// </summary>
        public List<String> ExcludeTitle;
        /// <summary>
        /// 抽取内容预处理器
        /// </summary>
        public Func<String, String, String> Normalize;

    }

    /// <summary>
    /// 从表格中抽取信息
    /// </summary>
    /// <param name="root"></param>
    /// <param name="Rules"></param>
    /// <param name="IsMeger"></param>
    /// <returns></returns>
    public static List<CellInfo[]> GetMultiInfo(HTMLEngine.MyRootHtmlNode root, List<TableSearchRule> Rules, bool IsMeger)
    {
        var Container = new List<CellInfo[]>();
        for (int tableIndex = 0; tableIndex < root.TableList.Count; tableIndex++)
        {
            var table = new HTMLTable(root.TableList[tableIndex + 1]);
            var checkResultColumnNo = new int[Rules.Count];
            var checkResultTitle = new string[Rules.Count];
            var HeaderRowNo = -1;
            String[] HeaderRow = null;
            var IsFirstRowOneCell = false;  //第一行是否为整行合并
            for (int TestRowHeader = 1; TestRowHeader < table.RowCount; TestRowHeader++)
            {
                checkResultColumnNo = new int[Rules.Count];
                var IsOneColumnRow = true;  //是否整行合并
                for (int i = 2; i <= table.ColumnCount; i++)
                {
                    if (table.CellValue(TestRowHeader, i) != (table.CellValue(TestRowHeader, 1)))
                    {
                        IsOneColumnRow = false;
                        break;
                    }
                }
                if (IsOneColumnRow)
                {
                    if (TestRowHeader == 1) IsFirstRowOneCell = true;
                    continue;
                }
                HeaderRow = table.GetHeaderRow(TestRowHeader);
                for (int checkItemIdx = 0; checkItemIdx < Rules.Count; checkItemIdx++)
                {
                    //在每个行首单元格检索
                    for (int ColIndex = 0; ColIndex < HeaderRow.Length; ColIndex++)
                    {
                        if (Rules[checkItemIdx].Title != null && Rules[checkItemIdx].Title.Count != 0)
                        {
                            //标题的处理
                            if (Rules[checkItemIdx].IsTitleEq)
                            {
                                //相等模式：规则里面没有该词语
                                if (!Rules[checkItemIdx].Title.Contains(HeaderRow[ColIndex])) continue;
                                if (Rules[checkItemIdx].ExcludeTitle != null)
                                {
                                    var isOK = true;
                                    foreach (var word in Rules[checkItemIdx].ExcludeTitle)
                                    {
                                        if (HeaderRow[ColIndex].Contains(word))
                                        {
                                            isOK = false;
                                            break;
                                        }
                                    }
                                    if (!isOK) continue;
                                }
                            }
                            else
                            {
                                bool IsMatch = false;
                                //包含模式
                                foreach (var r in Rules[checkItemIdx].Title)
                                {
                                    if (HeaderRow[ColIndex].Contains(r))
                                    {
                                        IsMatch = true;
                                        break;
                                    }
                                }
                                if (!IsMatch) continue;
                                if (Rules[checkItemIdx].ExcludeTitle != null)
                                {
                                    var isOK = true;
                                    foreach (var word in Rules[checkItemIdx].ExcludeTitle)
                                    {
                                        if (HeaderRow[ColIndex].Contains(word))
                                        {
                                            isOK = false;
                                            break;
                                        }
                                    }
                                    if (!isOK) continue;
                                }
                            }
                        }

                        //父标题的处理
                        if (Rules[checkItemIdx].SuperTitle != null && Rules[checkItemIdx].SuperTitle.Count != 0)
                        {
                            //具有父标题的情况
                            var IsFoundSuperTitle = false;
                            for (int superRowNo = 1; superRowNo < TestRowHeader; superRowNo++)
                            {
                                var value = table.CellValue(superRowNo, ColIndex + 1);
                                if (Rules[checkItemIdx].IsSuperTitleEq)
                                {
                                    //等于
                                    if (Rules[checkItemIdx].SuperTitle.Contains(value))
                                    {
                                        IsFoundSuperTitle = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    //包含
                                    foreach (var supertitle in Rules[checkItemIdx].SuperTitle)
                                    {
                                        if (value.Contains(supertitle))
                                        {
                                            IsFoundSuperTitle = true;
                                            break;
                                        }
                                    }
                                }
                                if (IsFoundSuperTitle) break;
                            }
                            if (!IsFoundSuperTitle) continue;
                        }
                        checkResultTitle[checkItemIdx] = HeaderRow[ColIndex];
                        checkResultColumnNo[checkItemIdx] = ColIndex + 1;
                        break;
                    }
                    //主字段没有找到，其他不用找了
                    if (checkResultColumnNo[0] == 0) break;
                }

                bool IsAllRequiredItemOK = true;
                for (int checkItemIdx = 0; checkItemIdx < checkResultColumnNo.Length; checkItemIdx++)
                {
                    if (checkResultColumnNo[checkItemIdx] == 0 && Rules[checkItemIdx].IsRequire)
                    {
                        IsAllRequiredItemOK = false;
                        break;
                    }
                }

                if (IsAllRequiredItemOK)
                {
                    if (TestRowHeader == 1 || IsFirstRowOneCell)
                    {
                        HeaderRowNo = TestRowHeader;
                        break;
                    }
                    else
                    {
                        //对于标题栏非首行的情况，如果不是首行是一个大的整行合并单元格，则做严格检查
                        //进行严格的检查,暂时要求全匹配
                        var IsOK = true;
                        for (int i = 0; i < Rules.Count; i++)
                        {
                            if (checkResultColumnNo[i] == 0)
                            {
                                IsOK = false;
                                break;
                            }
                        }
                        if (IsOK)
                        {
                            HeaderRowNo = TestRowHeader;
                            break;
                        }
                    }
                }
            }

            //主字段没有找到，下一张表
            if (HeaderRowNo == -1) continue;

            for (int RowNo = HeaderRowNo; RowNo <= table.RowCount; RowNo++)
            {
                if (RowNo == HeaderRowNo) continue;
                if (table.IsTotalRow(RowNo)) continue;          //非合计行
                var target = table.CellValue(RowNo, checkResultColumnNo[0]);    //主字段非空
                if (target == String.Empty || target == strRowSpanValue || target == strColSpanValue || target == strNullValue) continue;
                if (Rules[0].Title.Contains(target)) continue;

                var RowData = new CellInfo[Rules.Count];
                for (int checkItemIdx = 0; checkItemIdx < Rules.Count; checkItemIdx++)
                {
                    if (checkResultColumnNo[checkItemIdx] == 0) continue;
                    var ColNo = checkResultColumnNo[checkItemIdx];
                    RowData[checkItemIdx].TableId = tableIndex + 1;
                    RowData[checkItemIdx].Row = RowNo;
                    RowData[checkItemIdx].Column = ColNo;
                    RowData[checkItemIdx].Title = checkResultTitle[checkItemIdx];
                    if (table.CellValue(RowNo, ColNo).Equals(strNullValue)) continue;
                    RowData[checkItemIdx].RawData = table.CellValue(RowNo, ColNo);
                    if (Rules[checkItemIdx].Normalize != null)
                    {
                        RowData[checkItemIdx].RawData = Rules[checkItemIdx].Normalize(RowData[checkItemIdx].RawData, HeaderRow[ColNo - 1]);
                    }

                }

                var HasSame = false;
                foreach (var existRow in Container)
                {
                    if (IsSameContent(existRow, RowData))
                    {
                        HasSame = true;
                        break;
                    }
                }
                if (!HasSame) Container.Add(RowData);
            }
        }
        if (IsMeger) Container = MergerMultiInfo(Container);
        return Container;
    }
    public static bool IsSameContent(CellInfo[] Row1, CellInfo[] Row2)
    {
        for (int i = 0; i < Row1.Length; i++)
        {
            if (String.IsNullOrEmpty(Row1[i].RawData) && String.IsNullOrEmpty(Row2[i].RawData)) continue;
            if (String.IsNullOrEmpty(Row1[i].RawData)) return false;
            if (String.IsNullOrEmpty(Row2[i].RawData)) return false;
            if (!Row1[i].RawData.Equals(Row2[i].RawData)) return false;
        }
        return true;
    }
    static List<CellInfo[]> MergerMultiInfo(List<CellInfo[]> rows)
    {
        var dict = new Dictionary<String, CellInfo[]>();
        foreach (var Row in rows)
        {
            var key = Row[0].RawData;
            if (dict.ContainsKey(key))
            {
                //已经有了相同Key的记录
                var Rec = dict[key];
                for (int i = 1; i < Rec.Length; i++)
                {
                    if (!String.IsNullOrEmpty(Row[i].RawData))
                    {
                        if (String.IsNullOrEmpty(Rec[i].RawData) || Rec[i].RawData == strNullValue)
                        {
                            Rec[i].RawData = Row[i].RawData;
                        }
                        else
                        {
                            if (!Rec[i].RawData.Equals(Row[i].RawData))
                            {
                                Rec[i].RawData += "|" + Row[i].RawData;
                            }
                        }
                    }
                }
            }
            else
            {
                dict.Add(key, Row);
            }
        }
        return dict.Values.ToList();
    }

}