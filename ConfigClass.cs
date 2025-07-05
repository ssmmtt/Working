using System.Runtime.InteropServices;
using System.Text;

public class IniConfig
{
    /*
    * 声明API函数
    */
    public string iniPath;

    [DllImport("kernel32")]
    private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

    [DllImport("kernel32")]
    private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal,
        int size, string filePath);

    //        [DllImport("kernel32")]
    //        private static extern int GetPrivateProfileInt(string section, string key, int def, string filePath);

    #region Ini文件管理

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="iniPath">ini文件路径，默认为当前路径下config.ini</param>
    // public IniConfig(string path = "./config.ini")
    public IniConfig(string path)
    {
        // AppDomain.CurrentDomain.SetupInformation.ApplicationBase
        iniPath = path;
    }

    /// <summary>
    /// 查询ini文件是否存在
    /// </summary>
    /// <returns>是否存在</returns>
    public bool FileExist()
    {
        return File.Exists(iniPath);
    }

    #endregion

    #region 默认default节内的键值对操作

    /// <summary>
    /// 读取section，不管section，默认从default里读取
    /// </summary>
    /// <param name="Key">键</param>
    /// <returns>返回值</returns>
    public string ReadKey(string Key)
    {
        return ReadKey("default", Key);
    }
    /// <summary>
    /// 写入ini文件，不管section，默认放在default里
    /// </summary>
    /// <param name="Key">键</param>
    /// <param name="Value">值</param>
    public void WriteKey(string Key, string Value)
    {
        WritePrivateProfileString("default", Key, Value, iniPath);
    }

    /// <summary>
    /// 删除默认default节的key
    /// </summary>
    /// <param name="Key"></param>
    public void DeleteKey(string Key)
    {
        WritePrivateProfileString("default", Key, null, iniPath);
    }

    #endregion

    #region 指定名称的节内的键值对操作

    /// <summary>
    /// 读取ini文件
    /// </summary>
    /// <param name="Section">Section</param>
    /// <param name="Key">键</param>
    /// <returns>返回的值</returns>
    public string ReadKey(string Section, string Key)
    {
        StringBuilder temp = new StringBuilder(256);
        GetPrivateProfileString(Section, Key, "", temp, 256, iniPath);
        return temp.ToString();
    }

    /// <summary>
    /// 写入ini文件
    /// </summary>
    /// <param name="Section">Section</param>
    /// <param name="Key">键</param>
    /// <param name="Value">值</param>
    public void WriteKey(string Section, string Key, string Value)
    {
        WritePrivateProfileString(Section, Key, Value, iniPath);
    }

    /// <summary>
    /// 删除指定的key
    /// </summary>
    /// <param name="Section"></param>
    /// <param name="Key"></param>
    public void DeleteKey(string Section, string Key)
    {
        WritePrivateProfileString(Section, Key, null, iniPath);
    }

    #endregion

    #region  删除节操作

    /// <summary>
    /// 删除ini文件下personal段落下的所有键
    /// </summary>
    /// <param name="Section"></param>
    public void DeleteSection(string Section)
    {
        WriteKey(Section, null, null);
    }

    /// <summary>
    /// 删除ini文件下所有段落
    /// </summary>
    public void DeleteAllSection()
    {
        WriteKey(null, null, null);
    }

    #endregion
}
