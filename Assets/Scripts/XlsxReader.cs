using UnityEngine;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;

public class XlsxReader : MonoBehaviour
{
    
    /// <summary>
    /// 读取路径信息
    /// </summary>
    /// <param name="ExcelPath"></param>
    /// <returns></returns>
    public static List<Vector3> ReadExcelPath(string ExcelPath)
    {
        List<Vector3> list = new List<Vector3>();
        string strPah = Application.streamingAssetsPath + "/" + ExcelPath + ".xls";
        FileStream fs = new FileStream(strPah, FileMode.Open);
        using (ExcelPackage package = new ExcelPackage(fs))
        {
            ExcelWorksheet sheet = package.Workbook.Worksheets[1];
            for (int m = sheet.Dimension.Start.Row + 1, n = sheet.Dimension.End.Row; m <= n; m++)
            {
                Vector3 value = new Vector3(float.Parse(sheet.Cells[m, 1].Value+""), float.Parse(sheet.Cells[m, 2].Value+""), float.Parse(sheet.Cells[m, 3].Value+""));
                list.Add(value);
                Debug.Log(value);
            }
        }

        fs.Close();
        return list;
    }
}