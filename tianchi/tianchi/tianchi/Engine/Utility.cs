using HtmlAgilityPack;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using FDDC;
using static ExtractProperyBase;

public static class Utility
{

    /// <summary>
    /// 返回前N位的百分比字典
    /// </summary>
    /// <param name="n"></param>
    /// <param name="dict"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Dictionary<T, int> FindTop<T>(int n, Dictionary<T, int> dict)
    {
        var result = new Dictionary<T, int>();
        if (dict.Count == 0) return result;
        var Rank = dict.Values.ToList();
        Rank.Sort();
        Rank.Reverse();
        float Total = Rank.Sum();
        var pos = Math.Min(Rank.Count, n);
        int limit = Rank[pos - 1];
        foreach (var key in dict.Keys)
        {
            if (dict[key] >= limit)
            {
                var percent = (dict[key] * 100 / Total) + "%";
                result.Add(key, (int)(dict[key] * 100 / Total));
                Program.Training.WriteLine(key + "(" + percent + ")[" + dict[key] + "]");
            }
        }
        return result;
    }

    //获得开始字符结束字符的排列组合
    public static struStartEndStringFeature[] GetStartEndStringArray(string[] StartStringList, string[] EndStringList)
    {
        var KeyWordListArray = new struStartEndStringFeature[StartStringList.Length * EndStringList.Length];
        int cnt = 0;
        foreach (var StartString in StartStringList)
        {
            foreach (var EndString in EndStringList)
            {
                KeyWordListArray[cnt] = new struStartEndStringFeature { StartWith = StartString, EndWith = EndString };
                cnt++;
            }
        }
        return KeyWordListArray;
    }

    //提取某个关键字后的信息
    public static string GetStringAfter(String SearchLine, String KeyWord, String Exclude = "")
    {

        if (Exclude != String.Empty)
        {
            if (SearchLine.IndexOf(Exclude) != -1) return String.Empty;
        }

        int index = SearchLine.IndexOf(KeyWord);
        if (index != -1)
        {
            index = index + KeyWord.Length;
            return SearchLine.Substring(index);
        }
        return String.Empty;
    }

    //提取某个关键字前的信息
    public static string GetStringBefore(String SearchLine, String KeyWord, String Exclude = "")
    {
        if (Exclude != String.Empty)
        {
            if (SearchLine.IndexOf(Exclude) != -1) return String.Empty;
        }
        int index = SearchLine.IndexOf(KeyWord);
        if (index != -1)
        {
            return SearchLine.Substring(0, index);
        }
        return String.Empty;
    }
}